using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationExpiration(TravelAgencyDbContext db, QuotationTransaction work, TimeProvider clock) : IQuotationExpiration
{
    public async Task ExpireBatchAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var ids = await db.Quotations.AsNoTracking().Where(q => q.Status == QuotationStatus.Sent && q.ValidUntil <= now).OrderBy(q => q.ValidUntil).Take(20).Select(q => q.Id).ToArrayAsync(ct);
        foreach (var id in ids)
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var series = await work.LockSeriesAsync(id, ct); if (series is null) continue;
            var quote = await work.LoadAsync(id, ct); if (quote is not null) work.Expire(quote, series);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
    }
}
