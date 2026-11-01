using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Admin;

public sealed class AdminMutations(TravelAgencyDbContext db, TimeProvider clock) : IAdminMutations
{
    public async Task<MutationOutcome> ChangeStatusAsync(Guid id, string status, Guid version, string actor, CancellationToken ct)
    {
        if (!Enum.TryParse<RequestStatus>(status, false, out var next) || !Enum.IsDefined(next) || next.ToString() != status || version == Guid.Empty) return MutationOutcome.Invalid;
        var request = await db.TravelRequests.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return MutationOutcome.NotFound;
        if (request.Version != version) return MutationOutcome.Conflict;
        if (!TravelInquiry.AllowedTransitions(request.Status).Contains(next)) return MutationOutcome.Invalid;
        var previous = request.Status.ToString(); request.ChangeStatus(next, clock.GetUtcNow());
        db.RequestActivities.Add(new RequestActivity { RequestId = id, ActorId = actor, Type = "StatusChanged", PreviousValue = previous, NewValue = status, CreatedAt = request.UpdatedAt });
        try { await db.SaveChangesAsync(ct); return MutationOutcome.Saved; }
        catch (DbUpdateConcurrencyException) { return MutationOutcome.Conflict; }
    }
    public async Task<MutationOutcome> AddNoteAsync(Guid id, string? content, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4000) return MutationOutcome.Invalid;
        var request = await db.TravelRequests.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return MutationOutcome.NotFound;
        request.Touch(clock.GetUtcNow());
        db.RequestNotes.Add(new RequestNote { RequestId = id, AdministratorId = actor, Content = content.Trim(), CreatedAt = request.UpdatedAt });
        db.RequestActivities.Add(new RequestActivity { RequestId = id, ActorId = actor, Type = "NoteAdded", CreatedAt = request.UpdatedAt });
        try { await db.SaveChangesAsync(ct); return MutationOutcome.Saved; }
        catch (DbUpdateConcurrencyException) { return MutationOutcome.Conflict; }
    }
}
