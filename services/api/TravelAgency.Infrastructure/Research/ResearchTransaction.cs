using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Research;

public sealed class ResearchTransaction(TravelAgencyDbContext db, TimeProvider clock)
{
    public async Task<TravelInquiry> LockAsync(Guid requestId, CancellationToken ct)
    {
        var ids = await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"TravelRequests\" WHERE \"Id\" = {requestId} FOR NO KEY UPDATE").ToArrayAsync(ct);
        if (ids.Length == 0) throw new ResearchOperationException(404);
        return await db.TravelRequests.SingleAsync(r => r.Id == requestId, ct);
    }
    public Task<bool> EligibleAsync(Guid requestId, CancellationToken ct) => (from e in db.ResearchEligibility
                                                                              join i in db.Invoices on e.InvoiceId equals i.Id
                                                                              join q in db.Quotations on i.QuotationId equals q.Id
                                                                              join s in db.QuotationSeries on q.SeriesId equals s.Id
                                                                              join r in db.TravelRequests on e.RequestId equals r.Id
                                                                              where e.RequestId == requestId && i.RequestId == requestId && s.RequestId == requestId && !e.IsSimulated && !i.IsSimulated && i.Status == InvoiceStatus.Paid && q.Status == QuotationStatus.Accepted && r.Status != RequestStatus.Closed
                                                                                  && db.Payments.Any(p => p.InvoiceId == i.Id && p.Status == PaymentStatus.Succeeded && p.Provider != "Sandbox" && p.Provider != "Disabled" && p.AmountMinorUnits == i.TotalMinorUnits && p.Currency == i.Currency)
                                                                              select e.RequestId).AnyAsync(ct);
    public async Task RequireEligibilityAsync(Guid id, CancellationToken ct) { if (!await EligibleAsync(id, ct)) throw new ResearchOperationException(403); }
    public async Task<(TravelProposal Proposal, ProposalSeries Series)> ProposalAsync(Guid id, CancellationToken ct)
    {
        var requestId = await (from p in db.TravelProposals join s in db.ProposalSeries on p.SeriesId equals s.Id where p.Id == id select (Guid?)s.RequestId).SingleOrDefaultAsync(ct);
        if (requestId is null) throw new ResearchOperationException(404); await LockAsync(requestId.Value, ct);
        var proposal = await db.TravelProposals.Include(p => p.Alternatives).ThenInclude(a => a.Items).SingleAsync(p => p.Id == id, ct);
        var series = await db.ProposalSeries.SingleAsync(s => s.Id == proposal.SeriesId, ct); return (proposal, series);
    }
    public void Activity(Guid requestId, string type, string? actor, TravelProposal? proposal = null) => db.RequestActivities.Add(new RequestActivity { RequestId = requestId, ProposalId = proposal?.Id, Type = type, ActorId = actor, NewValue = proposal?.Status.ToString(), CreatedAt = Quotation.Utc(clock.GetUtcNow()) });
    public bool Expire(TravelProposal p, ProposalSeries s) { if (!p.Expire(clock.GetUtcNow())) return false; Activity(s.RequestId, "ProposalExpired", null, p); return true; }
    public void Notify(TravelProposal p, ProposalSeries s, string kind, string? link = null) => db.NotificationOutbox.Add(new NotificationRecord { RequestId = s.RequestId, ProposalId = p.Id, Kind = kind, ProtectedLink = link, NextAttemptAt = Quotation.Utc(clock.GetUtcNow()) });
    public Task RevokeAsync(Guid proposalId, CancellationToken ct) => db.ProposalTokens.Where(t => t.ProposalId == proposalId && t.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, clock.GetUtcNow()), ct);
}
