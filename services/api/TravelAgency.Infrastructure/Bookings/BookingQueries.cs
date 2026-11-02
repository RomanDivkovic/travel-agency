using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Bookings;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Bookings;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Infrastructure.Bookings;

public sealed class BookingQueries(TravelAgencyDbContext db, ResearchTransaction research)
{
    public async Task<PageResult<BookingListItem>> ListAsync(int page, string? status, string? search, string? sort, Guid? requestId, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || search?.Length > 100 || sort is not (null or "updated" or "departure" or "reference")) throw new BookingOperationException(400);
        var q = db.Bookings.AsNoTracking();
        if (requestId is { } id) q = q.Where(b => b.RequestId == id);
        if (!string.IsNullOrEmpty(status)) { if (!Enum.TryParse<BookingStatus>(status, out var state) || !Enum.IsDefined(state) || state.ToString() != status) throw new BookingOperationException(400); q = q.Where(b => b.Status == state); }
        if (!string.IsNullOrWhiteSpace(search)) { var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%"; q = q.Where(b => EF.Functions.ILike(b.Reference, pattern, "\\") || db.TravelRequests.Any(r => r.Id == b.RequestId && EF.Functions.ILike(r.Contact.Name, pattern, "\\"))); }
        var count = await q.CountAsync(ct);
        var ordered = sort switch { "departure" => q.OrderBy(b => b.DepartureDate).ThenBy(b => b.Id), "reference" => q.OrderBy(b => b.Reference).ThenBy(b => b.Id), _ => q.OrderByDescending(b => b.UpdatedAt).ThenBy(b => b.Id) };
        var rows = await ordered.Include(b => b.Reservations).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        var ids = rows.Select(b => b.ProposalId).ToArray(); var proposals = await db.TravelProposals.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        return new(rows.Select(b => Item(b, proposals[b.ProposalId])).ToArray(), page, 20, count, (int)Math.Ceiling(count / 20d));
    }
    public async Task<BookingDto> DetailAsync(Guid id, CancellationToken ct)
    {
        var b = await db.Bookings.AsNoTracking().Include(b => b.Reservations).SingleOrDefaultAsync(b => b.Id == id, ct) ?? throw new BookingOperationException(404);
        var p = await db.TravelProposals.AsNoTracking().Include(p => p.Alternatives).ThenInclude(a => a.Items).SingleAsync(p => p.Id == b.ProposalId, ct); var number = await db.ProposalSeries.Where(s => s.Id == p.SeriesId).Select(s => s.Number).SingleAsync(ct);
        var notifications = await db.NotificationOutbox.Where(n => n.BookingId == id).OrderByDescending(n => n.NextAttemptAt).Take(20).Select(n => new BookingMailDto(n.Kind, n.Attempts, n.SentAt, n.DeadLetterAt)).ToArrayAsync(ct);
        return new(Item(b, p), b.VersionToken, b.Revision, b.Notes, await research.EligibleAsync(b.RequestId, ct), ProposalDtos.Customer(p, number, false), b.Reservations.Select(r => new ReservationDto(r.Id, r.ProposalItemId, r.ReservationType, r.Status.ToString(), r.Approved(), r.Current(), r.RequiresReview, r.ReapprovedVersion == r.ArrangementVersion, r.ArrangementVersion, r.PaymentResponsibility?.ToString(), r.PaymentStatus.ToString(), r.PaymentArrangement, r.SupplierReference, r.ConfirmationEvidence, r.ConfirmationDocumentReference, r.TicketingStatus, r.FailureReason, r.CancellationReason, r.CancellationReference, r.CancellationChargeMinorUnits, r.ConfirmedAt, r.CancelledAt)).ToArray(), notifications);
    }
    public async Task<PageResult<BookingHistoryDto>> HistoryAsync(Guid id, int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000) throw new BookingOperationException(400);
        if (!await db.Bookings.AnyAsync(b => b.Id == id, ct)) throw new BookingOperationException(404);
        var q = db.BookingEvents.AsNoTracking().Where(e => e.BookingId == id); var count = await q.CountAsync(ct);
        var rows = await (from e in q join a in db.Users on e.ActorId equals a.Id orderby e.CreatedAt descending, e.Id select new BookingHistoryDto(e.Id, e.ReservationId, e.Type, a.DisplayName, e.CreatedAt, e.SnapshotJson)).Skip((page - 1) * 50).Take(50).ToArrayAsync(ct);
        return new(rows, page, 50, count, (int)Math.Ceiling(count / 50d));
    }
    public async Task<BookingMetrics> MetricsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var until = today.AddDays(30);
        return new(await db.Bookings.CountAsync(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Pending, ct), await db.Bookings.CountAsync(b => b.Status == BookingStatus.PartiallyConfirmed, ct), await db.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed, ct), await db.SupplierReservations.CountAsync(r => r.Status == ReservationStatus.Failed, ct), await db.Bookings.CountAsync(b => b.Status != BookingStatus.Cancelled && b.DepartureDate >= today && b.DepartureDate <= until, ct));
    }
    private static BookingListItem Item(TravelBooking b, Domain.Research.TravelProposal p) => new(b.Id, b.RequestId, b.ProposalId, b.Reference, p.CustomerName, p.Trip().Destination, p.Trip().Departure ?? p.Trip().Earliest, p.Trip().Return ?? p.Trip().Latest, b.Status.ToString(), b.Reservations.Count(r => r.Status == ReservationStatus.Confirmed), b.Reservations.Count(r => r.Status is ReservationStatus.NotStarted or ReservationStatus.InProgress or ReservationStatus.AwaitingConfirmation), b.Reservations.Count(r => r.Status == ReservationStatus.Failed), b.UpdatedAt);
}
