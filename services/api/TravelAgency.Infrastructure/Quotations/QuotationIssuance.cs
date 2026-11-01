using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationIssuance(TravelAgencyDbContext db, QuotationTransaction work, QuotationSettings settings, QuotationTokenCodec codec, TimeProvider clock) : IQuotationIssuance
{
    public async Task<QuotationResult> SendAsync(Guid id, Guid versionToken, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var series = await work.LockSeriesAsync(id, ct); if (series is null) return new(QuotationOutcome.NotFound);
        var quote = (await work.LoadAsync(id, ct))!;
        if (quote.VersionToken != versionToken || quote.Version != series.CurrentVersion || quote.Status != QuotationStatus.Draft) return new(QuotationOutcome.Conflict);
        if (!settings.Approves(quote)) return new(QuotationOutcome.IssuanceBlocked);
        var now = clock.GetUtcNow();
        try { quote.Issue(settings.TermsVersion, settings.Acknowledgement(quote.Language), now); } catch (QuotationRuleException) { return new(QuotationOutcome.Invalid); }
        var raw = QuotationTokenCodec.Create();
        db.QuotationTokens.Add(new QuotationAccessToken { QuotationId = quote.Id, Hash = QuotationTokenCodec.Hash(raw)!, ExpiresAt = quote.ValidUntil.AddDays(settings.AccessRetentionDays) });
        var link = settings.PublicOrigin.TrimEnd('/') + "/quote#" + raw;
        work.Notify(quote, series, "quote-issued", codec.ProtectLink(link)); work.Activity(quote, series, "QuotationSent", actor, "Draft");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Saved, quote.Id);
    }
    public async Task<QuotationResult> CancelAsync(Guid id, Guid versionToken, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var series = await work.LockSeriesAsync(id, ct); if (series is null) return new(QuotationOutcome.NotFound);
        var quote = (await work.LoadAsync(id, ct))!;
        if (quote.VersionToken != versionToken || quote.Version != series.CurrentVersion) return new(QuotationOutcome.Conflict);
        if (work.Expire(quote, series)) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Conflict); }
        var previous = quote.Status.ToString();
        try { quote.Cancel(clock.GetUtcNow()); } catch (QuotationRuleException) { return new(QuotationOutcome.Conflict); }
        await work.RevokeAsync(id, ct); work.Activity(quote, series, "QuotationCancelled", actor, previous);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Saved, quote.Id);
    }
}
