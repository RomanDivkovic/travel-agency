using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationDrafts(TravelAgencyDbContext db, QuotationTransaction work, QuotationSettings settings, TimeProvider clock) : IQuotationDrafts
{
    public async Task<QuotationResult> CreateAsync(Guid requestId, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var lockId = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes("quotation:" + requestId)));
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
        var request = await db.TravelRequests.SingleOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return new(QuotationOutcome.NotFound);
        if (request.Status == RequestStatus.Closed || await db.QuotationSeries.AnyAsync(s => s.RequestId == requestId, ct)) return new(QuotationOutcome.Conflict);
        var next = await db.Database.SqlQuery<long>($"SELECT nextval('quotation_reference') AS \"Value\"").SingleAsync(ct);
        var now = clock.GetUtcNow(); var series = new QuotationSeries { RequestId = requestId, Number = $"{settings.ReferencePrefix}-{now.Year}-{next:D6}" };
        var description = request.Language == "sv" ? "Personlig resehjälp" : "Personal travel assistance";
        var quote = Quotation.Create(series.Id, 1, request.Language, request.Contact.Name, request.Contact.Email, actor,
            new(description, "", "", settings.Terms(request.Language), "", request.Plan.Currency, TaxTreatment.Unconfirmed, now.AddDays(settings.DefaultValidityDays), [new(description, 1, 0, 0)]), now);
        db.QuotationSeries.Add(series); db.Quotations.Add(quote); work.Activity(quote, series, "QuotationCreated", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Saved, quote.Id);
    }
    public async Task<QuotationResult> UpdateAsync(Guid id, QuotationDraftInput input, string actor, CancellationToken ct)
    {
        var draft = input.ToDraft(); if (draft is null || input.VersionToken == Guid.Empty) return new(QuotationOutcome.Invalid);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var series = await work.LockSeriesAsync(id, ct); if (series is null) return new(QuotationOutcome.NotFound);
        var quote = (await work.LoadAsync(id, ct))!;
        if (quote.VersionToken != input.VersionToken || quote.Version != series.CurrentVersion || quote.Status != QuotationStatus.Draft) return new(QuotationOutcome.Conflict);
        try { quote.UpdateDraft(draft, clock.GetUtcNow()); } catch (QuotationRuleException) { return new(QuotationOutcome.Invalid); }
        db.AddRange(quote.Lines); // These replacement draft lines have new client-generated IDs, not existing rows.
        work.Activity(quote, series, "QuotationEdited", actor);
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Saved, quote.Id); }
        catch (DbUpdateConcurrencyException) { return new(QuotationOutcome.Conflict); }
    }
    public async Task<QuotationResult> ReviseAsync(Guid id, Guid versionToken, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var series = await work.LockSeriesAsync(id, ct); if (series is null) return new(QuotationOutcome.NotFound);
        var quote = (await work.LoadAsync(id, ct))!;
        if (quote.VersionToken != versionToken || quote.Version != series.CurrentVersion || quote.Status is not (QuotationStatus.Sent or QuotationStatus.Declined or QuotationStatus.Expired or QuotationStatus.Cancelled)) return new(QuotationOutcome.Conflict);
        var now = clock.GetUtcNow();
        if (work.Expire(quote, series)) { /* Keep the actual expired outcome instead of superseding it. */ }
        var approvedTerms = settings.Terms(quote.Language);
        var revision = quote.Revise(actor, now.AddDays(settings.DefaultValidityDays), string.IsNullOrEmpty(approvedTerms) ? quote.TermsSnapshot : approvedTerms, now);
        if (quote.Status == QuotationStatus.Sent) { quote.Supersede(now); work.Activity(quote, series, "QuotationSuperseded", actor, "Sent"); }
        await work.RevokeAsync(id, ct);
        // Release the partial unique active-version slot inside this same transaction before inserting a revision.
        await db.SaveChangesAsync(ct);
        series.CurrentVersion = revision.Version;
        db.Quotations.Add(revision); work.Activity(revision, series, "QuotationRevised", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Saved, revision.Id);
    }
}
