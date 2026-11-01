using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Quotations;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Payments;

public sealed class InvoiceOperationException(int status) : Exception { public int Status { get; } = status; }
public sealed class InvoiceOperations(TravelAgencyDbContext db, PaymentSettings settings, QuotationSettings tax, InvoiceTokenCodec codec, IPaymentProvider provider, TimeProvider clock)
{
    public async Task<Invoice> LockAsync(Guid id, CancellationToken ct) => (await db.Invoices.FromSqlInterpolated($"SELECT * FROM \"Invoices\" WHERE \"Id\" = {id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault() ?? throw new InvoiceOperationException(404);
    public async Task<Guid> CreateAsync(Guid quotationId, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // The accepted quote is immutable; its row serializes duplicate invoice creation.
        var q = (await db.Quotations.FromSqlInterpolated($"SELECT * FROM \"Quotations\" WHERE \"Id\" = {quotationId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault() ?? throw new InvoiceOperationException(404);
        await db.Entry(q).Collection(x => x.Lines).LoadAsync(ct);
        if (q.Status != QuotationStatus.Accepted || await db.Invoices.AnyAsync(i => i.QuotationId == quotationId, ct)) throw new InvoiceOperationException(409);
        var series = await db.QuotationSeries.SingleAsync(s => s.Id == q.SeriesId, ct);
        var i = Invoice.Create(q, series.RequestId, series.Number, provider.IsSimulated, clock.GetUtcNow()); db.Invoices.Add(i); Activity(i, "InvoiceCreated", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return i.Id;
    }
    public async Task EditAsync(Guid id, Guid version, InvoiceDetails details, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await LockAsync(id, ct); CheckVersion(i, version);
        i.Edit(details, clock.GetUtcNow()); Activity(i, "InvoiceEdited", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task IssueAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await LockAsync(id, ct); CheckVersion(i, version);
        if (i.Status != InvoiceStatus.Draft) throw new InvoiceOperationException(409);
        if (!settings.Approves(i, tax)) throw new InvoiceOperationException(412);
        var q = await db.Quotations.AsNoTracking().SingleAsync(q => q.Id == i.QuotationId, ct);
        if (q.Status != QuotationStatus.Accepted || q.Version != i.QuotationVersion || q.TotalMinorUnits != i.TotalMinorUnits || q.Currency != i.Currency) throw new InvoiceOperationException(409);
        // Transactional counter: rolled-back issuance consumes no invoice number.
        var counter = (await db.InvoiceCounters.FromSqlRaw("SELECT * FROM \"InvoiceCounters\" WHERE \"Id\" = 1 FOR UPDATE").ToListAsync(ct)).Single(); counter.Value++;
        var now = Quotation.Utc(clock.GetUtcNow()); var prefix = i.IsSimulated ? "TEST-INV" : "INV";
        i.Issue($"{prefix}-{now:yyyy}-{counter.Value:D6}", settings.SellerName, settings.SellerAddress, settings.RegistrationNumber, settings.TaxInformation, settings.Terms(i.Language), settings.TermsVersion, now);
        var raw = InvoiceTokenCodec.Create(); db.InvoiceTokens.Add(new InvoiceAccessToken { InvoiceId = id, Hash = InvoiceTokenCodec.Hash(raw)!, ExpiresAt = now.AddDays(settings.AccessDays) });
        Notify(i, "invoice-issued", codec.Protect(settings.PublicOrigin + "/invoice#" + raw)); Activity(i, "InvoiceIssued", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task CancelAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await LockAsync(id, ct); CheckVersion(i, version); i.CancelDraft(clock.GetUtcNow()); Activity(i, "InvoiceCancelled", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RevokeAccessAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await LockAsync(id, ct); CheckVersion(i, version); i.RecordAccessRevocation(clock.GetUtcNow());
        await db.InvoiceTokens.Where(t => t.InvoiceId == id && t.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, clock.GetUtcNow()), ct);
        await db.NotificationOutbox.Where(n => n.InvoiceId == id && n.ProtectedLink != null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ProtectedLink, (string?)null), ct);
        Activity(i, "InvoiceLinkRevoked", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<Invoice> CustomerAsync(string raw, CancellationToken ct)
    {
        var hash = InvoiceTokenCodec.Hash(raw); if (hash is null) throw new InvoiceOperationException(404);
        var id = await db.InvoiceTokens.Where(t => t.Hash == hash && t.RevokedAt == null && t.ExpiresAt > clock.GetUtcNow()).Select(t => (Guid?)t.InvoiceId).SingleOrDefaultAsync(ct);
        if (id is null) throw new InvoiceOperationException(404);
        var i = await LockAsync(id.Value, ct);
        if (!await db.InvoiceTokens.AnyAsync(t => t.InvoiceId == i.Id && t.Hash == hash && t.RevokedAt == null && t.ExpiresAt > clock.GetUtcNow(), ct) || i.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled) throw new InvoiceOperationException(404);
        if (i.MarkOverdue(clock.GetUtcNow())) Activity(i, "InvoiceOverdue", null);
        return i;
    }
    public async Task<InvoiceView> ReviewAsync(string raw, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await CustomerAsync(raw, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); var lastStatus = await db.Payments.Where(p => p.InvoiceId == i.Id).OrderByDescending(p => p.CreatedAt).Select(p => (PaymentStatus?)p.Status).FirstOrDefaultAsync(ct);
        return InvoiceDtos.Customer(i) with { LastPaymentStatus = lastStatus?.ToString() };
    }
    public void Activity(Invoice i, string type, string? actor) => db.RequestActivities.Add(new RequestActivity { RequestId = i.RequestId, QuotationId = i.QuotationId, ActorId = actor, Type = type, NewValue = i.Status.ToString(), CreatedAt = Quotation.Utc(clock.GetUtcNow()) });
    public void Notify(Invoice i, string kind, string? envelope = null) => db.NotificationOutbox.Add(new NotificationRecord { RequestId = i.RequestId, InvoiceId = i.Id, Kind = kind, ProtectedLink = envelope, NextAttemptAt = Quotation.Utc(clock.GetUtcNow()) });
    private static void CheckVersion(Invoice i, Guid version) { if (version == Guid.Empty || i.VersionToken != version) throw new InvoiceOperationException(409); }
}
