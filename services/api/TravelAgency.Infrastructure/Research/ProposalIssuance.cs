using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalIssuance(TravelAgencyDbContext db, ResearchTransaction work, ProposalSettings settings, ProposalTokenCodec codec, TimeProvider clock)
{
    public async Task SendAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); await work.RequireEligibilityAsync(s.RequestId, ct); ProposalDrafts.Check(p, s, version);
        if (p.Status != ProposalStatus.Draft) throw new ResearchOperationException(409);
        if (settings.BlockingReasons().Length > 0 || p.TermsSnapshot != settings.Terms(p.Language)) throw new ResearchOperationException(412);
        var sourceIds = p.Alternatives.SelectMany(a => a.Items).Select(i => i.ResearchOptionId).Distinct().ToArray(); var options = await db.ResearchOptions.Where(o => o.RequestId == s.RequestId && sourceIds.Contains(o.Id)).ToArrayAsync(ct);
        if (p.Alternatives.SelectMany(a => a.Items).Any(i => !options.Any(o => o.Id == i.ResearchOptionId && o.VersionToken == i.SourceVersion))) throw new ResearchOperationException(409);
        var now = clock.GetUtcNow(); p.Send(settings.TermsVersion, settings.Acknowledgement(p.Language), now);
        var raw = ProposalTokenCodec.Create(); db.ProposalTokens.Add(new() { ProposalId = id, Hash = ProposalTokenCodec.Hash(raw)!, ExpiresAt = p.ValidUntil.AddDays(settings.AccessRetentionDays) });
        work.Notify(p, s, "proposal-issued", codec.Protect(settings.PublicOrigin + "/travel-proposal#" + raw)); work.Activity(s.RequestId, "ProposalSent", actor, p);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RevokeAccessAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); ProposalDrafts.Check(p, s, version); p.RecordAccessRevocation(clock.GetUtcNow());
        await work.RevokeAsync(id, ct); await db.NotificationOutbox.Where(n => n.ProposalId == id && n.ProtectedLink != null).ExecuteUpdateAsync(x => x.SetProperty(n => n.ProtectedLink, (string?)null), ct);
        work.Activity(s.RequestId, "ProposalLinkRevoked", actor, p); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task CancelAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); ProposalDrafts.Check(p, s, version);
        if (work.Expire(p, s)) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw new ResearchOperationException(409); }
        p.Cancel(clock.GetUtcNow()); await work.RevokeAsync(id, ct); work.Activity(s.RequestId, "ProposalCancelled", actor, p); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
