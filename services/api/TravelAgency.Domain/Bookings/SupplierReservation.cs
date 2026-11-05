using System.Text.Json;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Research;

namespace TravelAgency.Domain.Bookings;

public sealed class SupplierReservation
{
    private SupplierReservation() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BookingId { get; private set; }
    public Guid ProposalItemId { get; private set; }
    public string ReservationType { get; private set; } = "";
    public string ApprovedSnapshotJson { get; private set; } = "";
    public string CurrentSnapshotJson { get; private set; } = "";
    public ReservationStatus Status { get; private set; }
    public bool RequiresReview { get; private set; }
    public int ArrangementVersion { get; private set; } = 1;
    public int? ReapprovedVersion { get; private set; }
    public SupplierPaymentResponsibility? PaymentResponsibility { get; private set; }
    public SupplierPaymentStatus PaymentStatus { get; private set; }
    public string PaymentArrangement { get; private set; } = "";
    public string SupplierReference { get; private set; } = "";
    public string ConfirmationEvidence { get; private set; } = "";
    public string ConfirmationDocumentReference { get; private set; } = "";
    public string TicketingStatus { get; private set; } = "NotApplicable";
    public string FailureReason { get; private set; } = "";
    public string CancellationReason { get; private set; } = "";
    public string CancellationReference { get; private set; } = "";
    public long? CancellationChargeMinorUnits { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public static SupplierReservation Create(Guid bookingId, ProposalItem item)
    {
        var s = item.Snapshot(); var type = TypeOf(s);
        return new SupplierReservation { BookingId = bookingId, ProposalItemId = item.Id, ReservationType = type, ApprovedSnapshotJson = item.SnapshotJson, CurrentSnapshotJson = item.SnapshotJson, TicketingStatus = s.Kind == "Flight" ? "NotTicketed" : "NotApplicable" };
    }
    public ResearchSnapshot Approved() => JsonSerializer.Deserialize<ResearchSnapshot>(ApprovedSnapshotJson)!;
    public ResearchSnapshot Current() => JsonSerializer.Deserialize<ResearchSnapshot>(CurrentSnapshotJson)!;
    public void Update(ResearchOptionInput input, SupplierPaymentResponsibility responsibility, SupplierPaymentStatus payment, string arrangement, DateTimeOffset now)
    {
        if (Status == ReservationStatus.Cancelled || !Enum.IsDefined(responsibility) || !Enum.IsDefined(payment) || !ResearchValidation.Text(arrangement, 1000)) throw new BookingRuleException("Explicit supplier payment arrangement required.");
        var next = ResearchValidation.Validate(input, now); var previous = Current();
        if (next.Kind != Approved().Kind) throw new BookingRuleException("Use a replacement of the same reservation category.");
        var changed = Material(previous) != Material(next) || next.TotalMinorUnits != previous.TotalMinorUnits || next.Currency != previous.Currency || PaymentResponsibility != responsibility || PaymentArrangement != arrangement.Trim();
        if (changed)
        {
            ArrangementVersion++; ReapprovedVersion = null;
            RequiresReview = RequiresReview || Material(Approved()) != Material(next) || next.Currency != Approved().Currency || next.TotalMinorUnits > Approved().TotalMinorUnits
                || (PaymentResponsibility is not null && (PaymentResponsibility != responsibility || PaymentArrangement != arrangement.Trim()));
            if (Status == ReservationStatus.Confirmed) { Status = ReservationStatus.AwaitingConfirmation; ConfirmedAt = null; SupplierReference = ""; ConfirmationEvidence = ""; }
        }
        ReservationType = TypeOf(next); CurrentSnapshotJson = JsonSerializer.Serialize(next); PaymentResponsibility = responsibility; PaymentStatus = payment; PaymentArrangement = arrangement.Trim();
    }
    public void Reapprove(ApprovalEvidence evidence, DateTimeOffset now)
    {
        if (!RequiresReview || Status == ReservationStatus.Cancelled) throw new BookingRuleException("No pending arrangement review.");
        BookingRules.Evidence(evidence, now); if (evidence.ApprovedAt < now.AddDays(-90)) throw new BookingRuleException("Current customer approval evidence required."); ReapprovedVersion = ArrangementVersion;
    }
    public void Start(DateTimeOffset now)
    {
        if (Status is not (ReservationStatus.NotStarted or ReservationStatus.Failed)) throw new BookingRuleException("Cannot start reservation.");
        RequireAuthorization(); RequireCurrentChecks(now); Status = ReservationStatus.InProgress; StartedAt ??= Quotation.Utc(now); FailureReason = "";
    }
    public void AwaitConfirmation() { if (Status != ReservationStatus.InProgress) throw new BookingRuleException("Reservation must be in progress."); Status = ReservationStatus.AwaitingConfirmation; }
    public void Confirm(string reference, string evidence, string document, string ticketing, DateTimeOffset confirmedAt, DateTimeOffset now)
    {
        if (Status is not (ReservationStatus.InProgress or ReservationStatus.AwaitingConfirmation or ReservationStatus.Confirmed)) throw new BookingRuleException("Reservation not awaiting confirmation.");
        RequireAuthorization(); RequireCurrentChecks(now); var snapshot = Current();
        if (!ResearchValidation.Text(reference, 200, false) || !ResearchValidation.Text(evidence, 2000) || !ResearchValidation.Text(document, 1000, false)
            || ((document.Contains("://") || document.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || document.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) && !ResearchValidation.SupplierUrl(document)) || confirmedAt.Offset != TimeSpan.Zero || confirmedAt > now || confirmedAt < StartedAt
            || snapshot.AvailabilityCheckedAt is null || snapshot.PriceCheckedAt < now.AddHours(-24) || snapshot.AvailabilityCheckedAt < now.AddHours(-24)
            || (snapshot.Kind == "Flight" ? ticketing is not ("NotTicketed" or "Issued") : ticketing != "NotApplicable")) throw new BookingRuleException("Recent price/availability checks and genuine supplier confirmation evidence are required.");
        SupplierReference = reference.Trim(); ConfirmationEvidence = evidence.Trim(); ConfirmationDocumentReference = document.Trim(); TicketingStatus = ticketing; ConfirmedAt = Quotation.Utc(confirmedAt); Status = ReservationStatus.Confirmed;
    }
    public void Fail(string reason) { if (Status is not (ReservationStatus.InProgress or ReservationStatus.AwaitingConfirmation)) throw new BookingRuleException("Cannot fail reservation."); if (!ResearchValidation.Text(reason, 2000)) throw new BookingRuleException("Failure reason required."); FailureReason = reason.Trim(); Status = ReservationStatus.Failed; }
    public void Cancel(string reason, string supplierEvidence, long? charge, DateTimeOffset at, DateTimeOffset now)
    {
        if (Status == ReservationStatus.Cancelled || !ResearchValidation.Text(reason, 2000) || !ResearchValidation.Text(supplierEvidence, 1000) || charge is < 0 or > 10_000_000_000 || at.Offset != TimeSpan.Zero || at > now || at < (StartedAt ?? now.AddDays(-1))) throw new BookingRuleException("Confirmed supplier cancellation evidence required.");
        CancellationReason = reason.Trim(); CancellationReference = supplierEvidence.Trim(); CancellationChargeMinorUnits = charge; CancelledAt = Quotation.Utc(at); Status = ReservationStatus.Cancelled;
    }
    private void RequireCurrentChecks(DateTimeOffset now)
    {
        var s = Current(); if (s.AvailabilityCheckedAt is null || s.PriceCheckedAt < now.AddHours(-24) || s.AvailabilityCheckedAt < now.AddHours(-24)) throw new BookingRuleException("Verify current supplier price and availability before manual booking.");
    }
    private void RequireAuthorization() { if (PaymentResponsibility is null || (RequiresReview && ReapprovedVersion != ArrangementVersion)) throw new BookingRuleException("Customer reapproval and payment responsibility required before booking."); }
    private static string TypeOf(ResearchSnapshot s) => s.Kind == "Extra" ? s.Extra!.Category switch { "Transfer" => "Transfer", "Activity" => "Activity", _ => "Other" } : s.Kind;
    private static string Material(ResearchSnapshot s) => JsonSerializer.Serialize(new { s.Kind, s.Title, s.Description, s.SupplierName, s.SupplierUrl, s.PriceBasis, s.Quantity, s.Conditions, s.Flight, s.Accommodation, s.Extra });
}
public sealed class BookingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid? ReservationId { get; set; }
    public string Type { get; set; } = "";
    public string? CustomerStatus { get; set; }
    public string ActorId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string SnapshotJson { get; set; } = "";
}
