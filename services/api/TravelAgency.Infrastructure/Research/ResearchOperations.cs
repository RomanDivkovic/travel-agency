using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ResearchOperations(TravelAgencyDbContext db, ResearchTransaction work, ProposalQueries proposals, TimeProvider clock)
{
    public async Task<ResearchWorkspaceDto> GetAsync(Guid requestId, int page, CancellationToken ct)
    {
        if (!await db.TravelRequests.AnyAsync(r => r.Id == requestId, ct)) throw new ResearchOperationException(404);
        var eligible = await work.EligibleAsync(requestId, ct); var w = await db.ResearchWorkspaces.AsNoTracking().SingleOrDefaultAsync(w => w.RequestId == requestId, ct);
        var options = await db.ResearchOptions.AsNoTracking().Where(o => o.RequestId == requestId).OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).ToArrayAsync(ct);
        return new(eligible, w is null ? "NotStarted" : "InProgress", w?.VersionToken, w?.Notes ?? "", options.Select(o => new ResearchOptionDto(o.Id, o.VersionToken, o.Snapshot(), o.Notes, o.UpdatedAt)).ToArray(), await proposals.ListAsync(page, null, null, requestId, ct));
    }
    public async Task StartAsync(Guid requestId, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await work.LockAsync(requestId, ct); await work.RequireEligibilityAsync(requestId, ct);
        if (!await db.ResearchWorkspaces.AnyAsync(w => w.RequestId == requestId, ct)) { var now = Quotation.Utc(clock.GetUtcNow()); db.ResearchWorkspaces.Add(new() { RequestId = requestId, StartedAt = now, UpdatedAt = now }); work.Activity(requestId, "ResearchStarted", actor); }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task SaveNotesAsync(Guid requestId, Guid version, string? notes, string actor, CancellationToken ct)
    {
        if (!ResearchValidation.Text(notes, 4000, false)) throw new ResearchOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await work.LockAsync(requestId, ct); await work.RequireEligibilityAsync(requestId, ct);
        var w = await db.ResearchWorkspaces.SingleOrDefaultAsync(w => w.RequestId == requestId, ct) ?? throw new ResearchOperationException(409);
        if (w.VersionToken != version) throw new ResearchOperationException(409); w.Notes = notes!.Trim(); w.VersionToken = Guid.NewGuid(); w.UpdatedAt = Quotation.Utc(clock.GetUtcNow()); work.Activity(requestId, "ResearchNotesSaved", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<Guid> SaveOptionAsync(Guid requestId, Guid? optionId, Guid? version, ResearchOptionInput input, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await work.LockAsync(requestId, ct); await work.RequireEligibilityAsync(requestId, ct);
        if (!await db.ResearchWorkspaces.AnyAsync(w => w.RequestId == requestId, ct)) throw new ResearchOperationException(409);
        ResearchOption option;
        if (optionId is { } id) { option = await db.ResearchOptions.SingleOrDefaultAsync(o => o.Id == id && o.RequestId == requestId, ct) ?? throw new ResearchOperationException(404); if (option.VersionToken != version) throw new ResearchOperationException(409); option.Update(input, clock.GetUtcNow()); }
        else { if (await db.ResearchOptions.CountAsync(o => o.RequestId == requestId, ct) >= 100) throw new ResearchOperationException(409); option = ResearchOption.Create(requestId, input, clock.GetUtcNow()); db.ResearchOptions.Add(option); }
        work.Activity(requestId, optionId is not null ? "ResearchItemEdited" : option.Kind == ResearchKind.Flight ? "ResearchFlightAdded" : option.Kind == ResearchKind.Accommodation ? "ResearchStayAdded" : "ResearchExtraAdded", actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return option.Id;
    }
    public async Task RemoveOptionAsync(Guid requestId, Guid optionId, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await work.LockAsync(requestId, ct); await work.RequireEligibilityAsync(requestId, ct);
        var option = await db.ResearchOptions.SingleOrDefaultAsync(o => o.Id == optionId && o.RequestId == requestId, ct) ?? throw new ResearchOperationException(404);
        if (option.VersionToken != version || await db.ProposalItems.AnyAsync(i => i.ResearchOptionId == optionId, ct)) throw new ResearchOperationException(409);
        db.Remove(option); work.Activity(requestId, "ResearchItemRemoved", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
