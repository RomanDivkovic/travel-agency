using Microsoft.EntityFrameworkCore;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Messaging;

public sealed class MessagingMaintenance(TravelAgencyDbContext db, MessagingSettings settings, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        if (!settings.PurgeClosedConversations) return; var cutoff = clock.GetUtcNow().AddDays(-settings.ClosedRetentionDays);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var rows = await db.Conversations.FromSqlInterpolated($"SELECT c.* FROM \"Conversations\" c WHERE c.\"Status\" = 'Closed' AND c.\"ClosedAt\" < {cutoff} AND NOT EXISTS (SELECT 1 FROM \"NotificationOutbox\" n WHERE n.\"ConversationId\" = c.\"Id\" AND n.\"SentAt\" IS NULL AND n.\"DeadLetterAt\" IS NULL) ORDER BY c.\"ClosedAt\" LIMIT 20 FOR UPDATE OF c SKIP LOCKED").ToArrayAsync(ct); db.Conversations.RemoveRange(rows); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
