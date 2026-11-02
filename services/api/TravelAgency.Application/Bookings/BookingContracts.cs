using TravelAgency.Application.Research;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Research;

namespace TravelAgency.Application.Bookings;

public sealed record CreateBookingInput(ApprovalEvidence Authorization);
public sealed record BookingVersionInput(Guid VersionToken);
public sealed record BookingNotesInput(Guid VersionToken, string Notes);
public sealed record ReservationUpdateInput(Guid VersionToken, ResearchOptionInput Option, string PaymentResponsibility, string PaymentStatus, string PaymentArrangement);
public sealed record ReapprovalInput(Guid VersionToken, ApprovalEvidence Evidence);
public sealed record ConfirmationInput(Guid VersionToken, string SupplierReference, string Evidence, string DocumentReference, string TicketingStatus, DateTimeOffset ConfirmedAt);
public sealed record FailureInput(Guid VersionToken, string Reason);
public sealed record CancellationInput(Guid VersionToken, string Reason, string SupplierEvidence, long? ChargeMinorUnits, DateTimeOffset CancelledAt);
public sealed record ReservationDto(Guid Id, Guid ProposalItemId, string ReservationType, string Status, ResearchSnapshot Approved, ResearchSnapshot Current,
    bool RequiresReview, bool ReapprovalRecorded, int ArrangementVersion, string? PaymentResponsibility, string PaymentStatus, string PaymentArrangement, string SupplierReference,
    string ConfirmationEvidence, string DocumentReference, string TicketingStatus, string FailureReason, string CancellationReason, string CancellationReference, long? CancellationChargeMinorUnits, DateTimeOffset? ConfirmedAt, DateTimeOffset? CancelledAt);
public sealed record BookingListItem(Guid Id, Guid RequestId, Guid ProposalId, string Reference, string CustomerName, string? Destination, DateOnly? Departure, DateOnly? Return, string Status, int Confirmed, int Pending, int Failed, DateTimeOffset UpdatedAt);
public sealed record BookingDto(BookingListItem Booking, Guid VersionToken, int Revision, string Notes, bool Eligible, CustomerProposalDto Proposal, ReservationDto[] Reservations, BookingMailDto[] Notifications);
public sealed record BookingMailDto(string Kind, int Attempts, DateTimeOffset? SentAt, DateTimeOffset? DeadLetterAt);
public sealed record BookingHistoryDto(Guid Id, Guid? ReservationId, string Type, string Actor, DateTimeOffset CreatedAt, string SnapshotJson);
public sealed record BookingMetrics(int InProgress, int PartiallyConfirmed, int Confirmed, int FailedReservations, int UpcomingDepartures);
public sealed record BookingNotificationSnapshot(string CustomerName, string Email, string Language, string Reference, string? Destination, DateTimeOffset ConfirmedAsOf, ConfirmedReservation[] Reservations);
public sealed record ConfirmedReservation(string Type, ResearchSnapshot Details, string SupplierReference, string ConfirmationEvidence, string DocumentReference, string TicketingStatus, DateTimeOffset ConfirmedAt);
