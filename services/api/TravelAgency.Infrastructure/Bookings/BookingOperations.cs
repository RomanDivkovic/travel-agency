using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Bookings;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Infrastructure.Bookings;

public sealed class BookingOperationException(int status) : Exception { public int Status { get; } = status; }
public sealed class BookingOperations(TravelAgencyDbContext db, ResearchTransaction research, TimeProvider clock)
{
    public async Task<Guid> CreateAsync(Guid proposalId, CreateBookingInput input, string actor, CancellationToken ct)
    {
        if (input?.Authorization is null) throw new BookingOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var (p, s) = await research.ProposalAsync(proposalId, ct); await RequireEligible(p, s.RequestId, ct);
        if (await db.Bookings.AnyAsync(b => b.RequestId == s.RequestId, ct)) throw new BookingOperationException(409);
        var next = await db.Database.SqlQuery<long>($"SELECT nextval('booking_reference') AS \"Value\"").SingleAsync(ct); var now = clock.GetUtcNow();
        if (input.Authorization.ApprovedAt < p.ApprovedAt) throw new BookingRuleException("Booking authority must cover the approved proposal.");
        var b = TravelBooking.Create(s.RequestId, p, p.Alternatives.Single(a => a.Id == p.SelectedAlternativeId), $"BKG-{now.Year}-{next:D6}", actor, input.Authorization, now);
        db.Bookings.Add(b); Event(b, null, "BookingCreated", actor, new { input.Authorization, b.ProposalId, b.ProposalVersion, b.ApprovedAlternativeId });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return b.Id;
    }
    public async Task ChangeAsync(Guid id, Guid? reservationId, Guid version, string operation, object? input, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var b = await LockAsync(id, ct);
        if (version == Guid.Empty || b.VersionToken != version) throw new BookingOperationException(409);
        var p = await db.TravelProposals.SingleAsync(p => p.Id == b.ProposalId, ct);
        if (operation is not ("fail" or "cancel" or "notes")) await RequireEligible(p, b.RequestId, ct);
        var now = clock.GetUtcNow(); var previousStatus = b.Status;
        SupplierReservation? r = reservationId is { } rid ? b.Reservations.SingleOrDefault(r => r.Id == rid) ?? throw new BookingOperationException(404) : null;
        if (operation == "notes") b.SaveNotes(((BookingNotesInput)input!).Notes, now);
        else
        {
            if (r is null) throw new BookingOperationException(400);
            Event(b, r, "BeforeChange", actor, r);
            switch (operation)
            {
                case "update":
                    var u = (ReservationUpdateInput)input!;
                    if (!Enum.TryParse<SupplierPaymentResponsibility>(u.PaymentResponsibility, out var who) || !Enum.IsDefined(who) || who.ToString() != u.PaymentResponsibility || !Enum.TryParse<SupplierPaymentStatus>(u.PaymentStatus, out var payment) || !Enum.IsDefined(payment) || payment.ToString() != u.PaymentStatus) throw new BookingOperationException(400);
                    r.Update(u.Option, who, payment, u.PaymentArrangement, now); break;
                case "reapprove":
                    var approval = ((ReapprovalInput)input!).Evidence;
                    var changedAt = await db.BookingEvents.Where(e => e.ReservationId == r.Id && e.Type == "ReservationUpdated").MaxAsync(e => (DateTimeOffset?)e.CreatedAt, ct);
                    if (approval is null || changedAt is null || approval.ApprovedAt < changedAt) throw new BookingRuleException("Approval must follow the changed arrangement.");
                    r.Reapprove(approval, now); break;
                case "start": r.Start(now); break;
                case "await": r.AwaitConfirmation(); break;
                case "confirm": var c = (ConfirmationInput)input!; r.Confirm(c.SupplierReference, c.Evidence, c.DocumentReference, c.TicketingStatus, c.ConfirmedAt, now); break;
                case "fail": r.Fail(((FailureInput)input!).Reason); break;
                case "cancel": var cancel = (CancellationInput)input!; r.Cancel(cancel.Reason, cancel.SupplierEvidence, cancel.ChargeMinorUnits, cancel.CancelledAt, now); break;
                default: throw new BookingOperationException(400);
            }
            b.Recalculate(now);
        }
        var type = operation switch { "update" => "ReservationUpdated", "reapprove" => "CustomerReapproval", "start" => "ReservationStarted", "confirm" => "ReservationConfirmed", "fail" => "ReservationFailed", "cancel" => "ReservationCancelled", "await" => "AwaitingConfirmation", _ => "BookingNotes" };
        Event(b, r, type, actor, operation == "reapprove" ? input! : r is null ? new { b.Notes } : r);
        if (operation == "update" && r!.RequiresReview && r.ReapprovedVersion != r.ArrangementVersion) Event(b, r, "PriceTermsReview", actor, new { r.ArrangementVersion });
        if (previousStatus != b.Status) Event(b, null, "BookingStatusChanged", actor, new { Previous = previousStatus.ToString(), Current = b.Status.ToString() });
        if (operation == "fail") db.NotificationOutbox.Add(new NotificationRecord { RequestId = b.RequestId, BookingId = b.Id, BookingRevision = b.Revision, Kind = $"booking-failed-{b.Revision}", PayloadJson = JsonSerializer.Serialize(new BookingFailureSnapshot(b.Reference, p.Language, r!.Current().SupplierName, r.FailureReason)), NextAttemptAt = now });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task SendAsync(Guid id, Guid version, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var b = await LockAsync(id, ct); var p = await db.TravelProposals.SingleAsync(p => p.Id == b.ProposalId, ct); await RequireEligible(p, b.RequestId, ct);
        if (version == Guid.Empty || b.VersionToken != version || b.Status != BookingStatus.Confirmed || b.Reservations.Any(r => r.Status != ReservationStatus.Confirmed || r.ConfirmedAt is null)) throw new BookingOperationException(409);
        var kind = $"booking-confirmed-{b.Revision}";
        if (!await db.NotificationOutbox.AnyAsync(n => n.BookingId == id && n.Kind == kind, ct))
        {
            var snapshot = new BookingNotificationSnapshot(p.CustomerName, p.CustomerEmail, p.Language, b.Reference, p.Trip().Destination, clock.GetUtcNow(), b.Reservations.Select(r => new ConfirmedReservation(r.ReservationType, r.Current(), r.SupplierReference, r.ConfirmationEvidence, r.ConfirmationDocumentReference, r.TicketingStatus, r.ConfirmedAt!.Value)).ToArray());
            db.NotificationOutbox.Add(new NotificationRecord { RequestId = b.RequestId, BookingId = b.Id, BookingRevision = b.Revision, Kind = kind, PayloadJson = JsonSerializer.Serialize(snapshot), NextAttemptAt = clock.GetUtcNow() }); Event(b, null, "BookingMailQueued", actor, new { b.Revision });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    public async Task<TravelBooking> LockAsync(Guid id, CancellationToken ct)
    {
        var requestId = await db.Bookings.Where(b => b.Id == id).Select(b => (Guid?)b.RequestId).SingleOrDefaultAsync(ct) ?? throw new BookingOperationException(404);
        await research.LockAsync(requestId, ct);
        await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Bookings\" WHERE \"Id\" = {id} FOR NO KEY UPDATE").ToArrayAsync(ct);
        return await db.Bookings.Include(b => b.Reservations).SingleAsync(b => b.Id == id, ct);
    }
    private async Task RequireEligible(TravelProposal p, Guid request, CancellationToken ct)
    {
        await research.RequireEligibilityAsync(request, ct);
        if (p.Status != ProposalStatus.Approved || p.SelectedAlternativeId is null || !await db.ProposalSeries.AnyAsync(s => s.Id == p.SeriesId && s.RequestId == request && s.CurrentVersion == p.Version, ct)
            || !await db.ProposalDecisions.AnyAsync(d => d.ProposalId == p.Id && d.Approved && d.AlternativeId == p.SelectedAlternativeId && d.TermsVersion == p.TermsVersion && d.Acknowledgement == p.AcceptanceText, ct)) throw new BookingOperationException(409);
    }
    private void Event(TravelBooking b, SupplierReservation? r, string type, string actor, object snapshot)
    {
        db.BookingEvents.Add(new BookingEvent { BookingId = b.Id, ReservationId = r?.Id, Type = type, CustomerStatus = type == "BookingStatusChanged" ? b.Status.ToString() : null, ActorId = actor, CreatedAt = clock.GetUtcNow(), SnapshotJson = JsonSerializer.Serialize(snapshot) });
        if (type != "BeforeChange") db.RequestActivities.Add(new RequestActivity { RequestId = b.RequestId, BookingId = b.Id, Type = "BookingUpdated", ActorId = actor, NewValue = b.Status.ToString(), CreatedAt = clock.GetUtcNow() });
    }
}
