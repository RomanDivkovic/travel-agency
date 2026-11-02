using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalDrafts(TravelAgencyDbContext db, ResearchTransaction work, ProposalSettings settings, TimeProvider clock)
{
    public async Task<Guid> CreateAsync(Guid requestId, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var r = await work.LockAsync(requestId, ct); await work.RequireEligibilityAsync(requestId, ct);
        if (!await db.ResearchWorkspaces.AnyAsync(w => w.RequestId == requestId, ct) || await db.ProposalSeries.AnyAsync(s => s.RequestId == requestId, ct)) throw new ResearchOperationException(409);
        var i = await (from e in db.ResearchEligibility join invoice in db.Invoices on e.InvoiceId equals invoice.Id where e.RequestId == requestId select invoice).SingleAsync(ct);
        var next = await db.Database.SqlQuery<long>($"SELECT nextval('proposal_reference') AS \"Value\"").SingleAsync(ct); var now = clock.GetUtcNow();
        var s = new ProposalSeries { RequestId = requestId, Number = $"PROP-{now.Year}-{next:D6}" }; var plan = r.Plan;
        var trip = new TripSnapshot(plan.Origin, plan.Destination, plan.FlexibleDestination, plan.AdditionalStops, plan.DateMode, plan.Departure, plan.Return, plan.Earliest, plan.Latest, plan.DurationDays, plan.Adults, plan.ChildrenAges.Length, plan.Rooms);
        var p = TravelProposal.Create(s.Id, 1, r.Language, r.Contact.Name, r.Contact.Email, actor, trip, new(i.Number!, i.Currency, i.TotalMinorUnits, i.PaidAt!.Value), now.AddDays(settings.DefaultValidityDays), now);
        p.Edit(p.Title, "", "", settings.Terms(p.Language), p.ValidUntil, [], now); db.ProposalSeries.Add(s); db.TravelProposals.Add(p); work.Activity(requestId, "ProposalCreated", actor, p);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return p.Id;
    }
    public async Task UpdateAsync(Guid id, ProposalDraftInput input, string actor, CancellationToken ct)
    {
        if (input is null || input.Alternatives is null || input.Alternatives.Length > 6) throw new ResearchOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); await work.RequireEligibilityAsync(s.RequestId, ct);
        Check(p, s, input.VersionToken); if (p.Status != ProposalStatus.Draft) throw new ResearchOperationException(409);
        var choices = new List<ProposalAlternative>();
        foreach (var (a, order) in input.Alternatives.Select((a, n) => (a, n)))
        {
            if (a is null || !ResearchValidation.Text(a.Title, 200) || !ResearchValidation.Text(a.Description, 2000) || a.ItemIds is not { Length: >= 1 and <= 20 } || a.ItemIds.Distinct().Count() != a.ItemIds.Length) throw new ResearchOperationException(400);
            var source = await db.ResearchOptions.Where(o => a.ItemIds.Contains(o.Id) && o.RequestId == s.RequestId).ToArrayAsync(ct);
            if (source.Length != a.ItemIds.Length) throw new ResearchOperationException(400);
            var items = a.ItemIds.Select((oid, n) => { var option = source.Single(o => o.Id == oid); return ProposalItem.Create(oid, option.VersionToken, option.SnapshotJson, n); }).ToList();
            if (ProposalDtos.Costs(items.Select(i => i.Snapshot())).Any(c => c.TotalMinorUnits > Quotation.MaxTotal)) throw new ResearchOperationException(400);
            choices.Add(ProposalAlternative.Create(p.Id, a.Title, a.Description, a.Recommended, order, items));
        }
        // Delete old draft associations before inserting replacement order slots, all in the same transaction.
        db.RemoveRange(p.Alternatives); await db.SaveChangesAsync(ct);
        p.Edit(input.Title, input.Introduction, input.Summary, input.TermsSnapshot, input.ValidUntil, choices.ToArray(), clock.GetUtcNow()); db.AddRange(p.Alternatives); work.Activity(s.RequestId, "ProposalEdited", actor, p);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<Guid> ReviseAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await work.ProposalAsync(id, ct); await work.RequireEligibilityAsync(s.RequestId, ct); Check(p, s, version);
        if (!p.CanRevise) throw new ResearchOperationException(409); var now = clock.GetUtcNow(); work.Expire(p, s); var revision = p.Revise(actor, now.AddDays(settings.DefaultValidityDays), now);
        if (p.Status == ProposalStatus.Sent) { p.Supersede(now); work.Activity(s.RequestId, "ProposalSuperseded", actor, p); }
        await work.RevokeAsync(id, ct); await db.SaveChangesAsync(ct); s.CurrentVersion = revision.Version;
        db.TravelProposals.Add(revision); work.Activity(s.RequestId, "ProposalRevised", actor, revision); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return revision.Id;
    }
    public static void Check(TravelProposal p, ProposalSeries s, Guid version) { if (version == Guid.Empty || p.VersionToken != version || s.CurrentVersion != p.Version) throw new ResearchOperationException(409); }
}
