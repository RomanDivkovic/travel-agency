using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationTransaction(TravelAgencyDbContext db, TimeProvider clock)
{
    public async Task<QuotationSeries?> LockSeriesAsync(Guid quotationId, CancellationToken ct)
    {
        var id = await db.Quotations.Where(q => q.Id == quotationId).Select(q => (Guid?)q.SeriesId).SingleOrDefaultAsync(ct);
        if (id is null) return null;
        return (await db.QuotationSeries.FromSqlInterpolated($"SELECT * FROM \"QuotationSeries\" WHERE \"Id\" = {id.Value} FOR UPDATE").ToListAsync(ct)).SingleOrDefault();
    }
    public Task<Quotation?> LoadAsync(Guid id, CancellationToken ct) => db.Quotations.Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, ct);
    public void Activity(Quotation quote, QuotationSeries series, string type, string? actor, string? previous = null)
    {
        db.RequestActivities.Add(new RequestActivity
        {
            RequestId = series.RequestId,
            QuotationId = quote.Id,
            ActorId = actor,
            Type = type,
            PreviousValue = previous,
            NewValue = quote.Status.ToString(),
            CreatedAt = Quotation.Utc(clock.GetUtcNow())
        });
    }
    public bool Expire(Quotation quote, QuotationSeries series)
    {
        if (!quote.Expire(clock.GetUtcNow())) return false;
        Activity(quote, series, "QuotationExpired", null, "Sent"); return true;
    }
    public async Task RevokeAsync(Guid id, CancellationToken ct)
    {
        var now = Quotation.Utc(clock.GetUtcNow());
        await db.QuotationTokens.Where(t => t.QuotationId == id && t.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }
    public void Notify(Quotation quote, QuotationSeries series, string kind, string? protectedLink = null) => db.NotificationOutbox.Add(new NotificationRecord
    { RequestId = series.RequestId, QuotationId = quote.Id, Kind = kind, ProtectedLink = protectedLink, NextAttemptAt = Quotation.Utc(clock.GetUtcNow()) });
    public static CustomerQuotationDto CustomerDto(Quotation q, string number) => new(number, q.Version, q.Status.ToString(), q.Language, q.CustomerName, q.Description,
        q.IncludedServices, q.ExcludedServices, q.CustomerNotes, q.Currency, q.TaxTreatment.ToString(), q.SubtotalMinorUnits, q.TaxMinorUnits, q.TotalMinorUnits,
        q.ValidUntil, q.TermsSnapshot, q.TermsVersion, q.AcceptanceTextSnapshot, q.AcceptedAt, q.DeclinedAt, q.Lines.OrderBy(l => l.SortOrder).Select(l => new QuotationLineDto(l.Description, l.Quantity, l.UnitPriceMinorUnits, l.TaxRateBasisPoints, l.SubtotalMinorUnits, l.TaxMinorUnits, l.TotalMinorUnits)).ToArray());
}
