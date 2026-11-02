using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Research;

namespace TravelAgency.Domain.Bookings;

public enum BookingStatus { Pending, InProgress, PartiallyConfirmed, Confirmed, Failed, Cancelled }
public enum ReservationStatus { NotStarted, InProgress, AwaitingConfirmation, Confirmed, Failed, Cancelled }
public enum SupplierPaymentResponsibility { CustomerPaysSupplier, AgentPaysSupplier, OtherApprovedArrangement }
public enum SupplierPaymentStatus { Unknown, NotPaid, Pending, Paid, PartiallyPaid, Refunded }
public sealed class BookingRuleException(string message) : Exception(message);
public sealed record ApprovalEvidence(string CustomerName, string Channel, string Reference, DateTimeOffset ApprovedAt, bool Confirmed);
public sealed class TravelBooking
{
    private TravelBooking() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public Guid ProposalId { get; private set; }
    public int ProposalVersion { get; private set; }
    public Guid ApprovedAlternativeId { get; private set; }
    public string Reference { get; private set; } = "";
    public string CreatedByAdministratorId { get; private set; } = "";
    public Guid VersionToken { get; private set; } = Guid.NewGuid();
    public int Revision { get; private set; } = 1;
    public BookingStatus Status { get; private set; }
    public string Notes { get; private set; } = "";
    public string AuthorizationJson { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateOnly? DepartureDate { get; private set; }
    private readonly List<SupplierReservation> reservations = [];
    public IReadOnlyList<SupplierReservation> Reservations => reservations;
    public static TravelBooking Create(Guid request, TravelProposal proposal, ProposalAlternative alternative, string reference, string actor, ApprovalEvidence authority, DateTimeOffset now)
    {
        if (proposal.Status != ProposalStatus.Approved || proposal.SelectedAlternativeId != alternative.Id || alternative.ProposalId != proposal.Id || alternative.Items.Count == 0 || !proposal.Alternatives.Contains(alternative) || string.IsNullOrWhiteSpace(actor) || !ResearchValidation.Text(reference, 40)) throw new BookingRuleException("An approved selected alternative is required.");
        BookingRules.Evidence(authority, now);
        var b = new TravelBooking { RequestId = request, ProposalId = proposal.Id, ProposalVersion = proposal.Version, ApprovedAlternativeId = alternative.Id, Reference = reference, CreatedByAdministratorId = actor, AuthorizationJson = System.Text.Json.JsonSerializer.Serialize(authority), CreatedAt = Quotation.Utc(now), UpdatedAt = Quotation.Utc(now), DepartureDate = proposal.Trip().Departure ?? proposal.Trip().Earliest };
        b.reservations.AddRange(alternative.Items.OrderBy(i => i.SortOrder).Select(i => SupplierReservation.Create(b.Id, i))); return b;
    }
    public void SaveNotes(string notes, DateTimeOffset now) { if (!ResearchValidation.Text(notes, 4000, false)) throw new BookingRuleException("Invalid notes."); Notes = notes.Trim(); Touch(now); }
    public void Recalculate(DateTimeOffset now)
    {
        var states = reservations.Select(r => r.Status).ToArray();
        Status = states.All(s => s == ReservationStatus.Cancelled) ? BookingStatus.Cancelled
            : states.All(s => s == ReservationStatus.Confirmed) ? BookingStatus.Confirmed
            : states.Any(s => s == ReservationStatus.Confirmed) ? BookingStatus.PartiallyConfirmed
            : states.Any(s => s is ReservationStatus.Failed or ReservationStatus.Cancelled) ? BookingStatus.Failed
            : states.All(s => s == ReservationStatus.NotStarted) ? BookingStatus.Pending : BookingStatus.InProgress;
        CompletedAt = Status == BookingStatus.Confirmed ? CompletedAt ?? Quotation.Utc(now) : null;
        CancelledAt = Status == BookingStatus.Cancelled ? CancelledAt ?? Quotation.Utc(now) : null; Touch(now);
    }
    private void Touch(DateTimeOffset now) { Revision++; VersionToken = Guid.NewGuid(); UpdatedAt = Quotation.Utc(now); }
}
public static class BookingRules
{
    public static void Evidence(ApprovalEvidence? e, DateTimeOffset now)
    {
        if (e is null || !e.Confirmed || !ResearchValidation.Text(e.CustomerName, 100) || e.Channel is not ("Email" or "Phone" or "SignedDocument" or "Other") || !ResearchValidation.Text(e.Reference, 1000) || e.ApprovedAt.Offset != TimeSpan.Zero || e.ApprovedAt > now || e.ApprovedAt < now.AddYears(-5)) throw new BookingRuleException("Explicit customer authorization evidence is required.");
    }
}
