using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationCustomers(TravelAgencyDbContext db, QuotationTransaction work, TimeProvider clock) : IQuotationCustomers
{
    public Task<QuotationResult> ReviewAsync(string token, CancellationToken ct) => ExecuteAsync(token, null, ct);
    public Task<QuotationResult> RespondAsync(string token, bool accept, bool confirmed, CancellationToken ct) => confirmed ? ExecuteAsync(token, accept, ct) : Task.FromResult(new QuotationResult(QuotationOutcome.Invalid));
    private async Task<QuotationResult> ExecuteAsync(string token, bool? decision, CancellationToken ct)
    {
        var hash = QuotationTokenCodec.Hash(token); if (hash is null) return new(QuotationOutcome.NotFound);
        var quotationId = await db.QuotationTokens.Where(t => t.Hash == hash).Select(t => (Guid?)t.QuotationId).SingleOrDefaultAsync(ct);
        if (quotationId is null) return new(QuotationOutcome.NotFound);
        return await ExecuteCoreAsync(quotationId.Value, hash, null, decision, ct);
    }
    public Task<QuotationResult> OwnedAsync(string owner, Guid id, bool? decision, bool confirmed, CancellationToken ct) => decision is not null && !confirmed ? Task.FromResult(new QuotationResult(QuotationOutcome.Invalid)) : ExecuteCoreAsync(id, null, owner, decision, ct);
    private async Task<QuotationResult> ExecuteCoreAsync(Guid quotationId, string? hash, string? owner, bool? decision, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var series = await work.LockSeriesAsync(quotationId, ct); if (series is null) return new(QuotationOutcome.NotFound);
        // Recheck revocation/expiration after acquiring the same lock used for revisions and cancellation.
        if (owner is not null) await new CustomerOwnership(db).RequireAsync(owner, series.RequestId, ct);
        var access = hash is null ? null : await db.QuotationTokens.AsNoTracking().SingleOrDefaultAsync(t => t.Hash == hash, ct);
        var now = clock.GetUtcNow();
        if (owner is null && (access is null || access.RevokedAt is not null || access.ExpiresAt <= now)) return new(QuotationOutcome.NotFound);
        var quote = (await work.LoadAsync(quotationId, ct))!;
        if (quote.SentAt is null || (owner is null && (quote.Version != series.CurrentVersion || quote.Status is QuotationStatus.Draft or QuotationStatus.Superseded or QuotationStatus.Cancelled))) return new(QuotationOutcome.NotFound);
        if (decision is not null && quote.Version != series.CurrentVersion) return new(QuotationOutcome.Conflict);
        work.Expire(quote, series);
        if (decision is not null)
        {
            try
            {
                if (quote.Respond(decision.Value, now))
                {
                    work.Activity(quote, series, decision.Value ? "QuotationAccepted" : "QuotationDeclined", null, "Sent");
                    work.Notify(quote, series, decision.Value ? "quote-accepted" : "quote-declined");
                    work.Notify(quote, series, decision.Value ? "agent-accepted" : "agent-declined");
                }
            }
            catch (QuotationRuleException) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(QuotationOutcome.Conflict); }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(QuotationOutcome.Saved, Quotation: QuotationTransaction.CustomerDto(quote, series.Number));
    }
}
