using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalCustomers(TravelAgencyDbContext db, ResearchTransaction work, TimeProvider clock)
{
    public async Task<CustomerProposalDto> ExecuteAsync(string raw, bool? approve, ProposalDecisionInput? input, CancellationToken ct)
    {
        var hash = ProposalTokenCodec.Hash(raw); if (hash is null) throw new ResearchOperationException(404);
        var id = await db.ProposalTokens.Where(t => t.Hash == hash).Select(t => (Guid?)t.ProposalId).SingleOrDefaultAsync(ct); if (id is null) throw new ResearchOperationException(404);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id.Value, ct);
        var access = await db.ProposalTokens.AsNoTracking().SingleAsync(t => t.Hash == hash, ct); var now = clock.GetUtcNow();
        if (access.RevokedAt is not null || access.ExpiresAt <= now || p.Version != s.CurrentVersion || p.Status is ProposalStatus.Draft or ProposalStatus.Superseded or ProposalStatus.Cancelled) throw new ResearchOperationException(404);
        work.Expire(p, s); var eligible = await work.EligibleAsync(s.RequestId, ct);
        if (approve is not null)
        {
            if (input is null || !input.Confirmed || input.Version != p.Version || !ResearchValidation.Text(input.Feedback ?? "", 1000, false) || (approve.Value && input.TermsVersion != p.TermsVersion)) throw new ResearchOperationException(400);
            if (!eligible) throw new ResearchOperationException(409);
            try
            {
                if (p.Respond(approve.Value, input.AlternativeId, now))
                {
                    db.ProposalDecisions.Add(new() { ProposalId = p.Id, Approved = approve.Value, AlternativeId = approve.Value ? input.AlternativeId : null, DecidedAt = Quotation.Utc(now), TermsVersion = p.TermsVersion, Acknowledgement = approve.Value ? p.AcceptanceText : "", Feedback = input.Feedback?.Trim() ?? "" });
                    work.Activity(s.RequestId, approve.Value ? "ProposalApproved" : "ProposalRejected", null, p);
                    work.Notify(p, s, approve.Value ? "proposal-approved" : "proposal-rejected"); work.Notify(p, s, approve.Value ? "proposal-agent-approved" : "proposal-agent-rejected");
                }
            }
            catch (ResearchRuleException) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw new ResearchOperationException(409); }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return ProposalDtos.Customer(p, s.Number, eligible);
    }
}
