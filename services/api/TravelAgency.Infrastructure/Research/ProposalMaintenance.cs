using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalMaintenance(TravelAgencyDbContext db, ResearchTransaction work, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow(); var ids = await db.TravelProposals.AsNoTracking().Where(p => p.Status == ProposalStatus.Sent && p.ValidUntil <= now).OrderBy(p => p.ValidUntil).Select(p => p.Id).Take(20).ToArrayAsync(ct);
        foreach (var id in ids) { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); work.Expire(p, s); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        db.ChangeTracker.Clear(); await db.NotificationOutbox.Where(n => n.ProposalId != null && n.ProtectedLink != null && db.ProposalTokens.Any(t => t.ProposalId == n.ProposalId && (t.ExpiresAt <= now || t.RevokedAt != null))).ExecuteUpdateAsync(s => s.SetProperty(n => n.ProtectedLink, (string?)null), ct);
    }
}
