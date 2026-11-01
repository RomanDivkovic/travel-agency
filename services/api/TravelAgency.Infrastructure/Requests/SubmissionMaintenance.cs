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
        // Disabled by default: the owner must approve retention before enabling deletion.
        if (settings.PurgeExpiredRequests)
            await db.TravelRequests.Where(r => r.CreatedAt < now.AddDays(-settings.RequestRetentionDays)).ExecuteDeleteAsync(ct);
    }
}
