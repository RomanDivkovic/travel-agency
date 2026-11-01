using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Requests;

public sealed class SubmissionMaintenance(TravelAgencyDbContext db, SubmissionSettings settings, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await db.IdempotencyRecords.Where(k => k.ExpiresAt <= now).ExecuteDeleteAsync(ct);
        await db.NotificationOutbox.Where(n => n.ProtectedLink != null && db.QuotationTokens.Any(t => t.QuotationId == n.QuotationId && t.ExpiresAt <= now))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ProtectedLink, (string?)null), ct);
        // Disabled by default: the owner must approve retention before enabling deletion.
        if (settings.PurgeExpiredRequests)
            await db.TravelRequests.Where(r => r.CreatedAt < now.AddDays(-settings.RequestRetentionDays) && !db.QuotationSeries.Any(q => q.RequestId == r.Id)).ExecuteDeleteAsync(ct);
    }
}
