namespace TravelAgency.Domain.Customers;

public sealed record JourneyProjection(string Stage, string[] Completed, string? NextAction);
public static class CustomerJourney
{
    public static JourneyProjection Project(string request, string? quotation, bool quotationIssued, string? invoice, bool simulated, bool realPaid, bool research,
        string? proposal, bool proposalIssued, string? booking)
    {
        var done = new List<string> { "InquirySubmitted" };
        if (request is "UnderReview" or "AwaitingCustomer" || quotationIssued) done.Add("UnderReview");
        if (quotationIssued) done.Add("QuotationSent"); if (quotation == "Accepted") done.Add("QuotationAccepted");
        if (realPaid) done.Add("PaymentConfirmed"); if (research && realPaid) done.Add("TravelResearch");
        if (proposalIssued) done.Add("ProposalSent"); if (proposal == "Approved") done.Add("ProposalApproved");
        if (booking is "InProgress" or "PartiallyConfirmed" or "Confirmed") done.Add("BookingInProgress"); if (booking == "Confirmed") done.Add("BookingConfirmed");
        var stage = booking is not null ? "Booking" + booking
            : proposal is "Approved" or "Rejected" or "Expired" or "Cancelled" or "Superseded" ? "Proposal" + proposal
            : proposal == "Sent" ? "ProposalSent" : research && realPaid ? "TravelResearch"
            : realPaid ? "PaymentConfirmed" : invoice == "Paid" && simulated ? "PaymentSimulated"
            : invoice is "PaymentPending" or "Overdue" ? invoice == "Overdue" ? "PaymentOverdue" : "PaymentPending"
            : invoice == "Issued" ? "AwaitingPayment"
            : quotation is "Accepted" or "Declined" or "Expired" or "Cancelled" or "Superseded" ? "Quotation" + quotation
            : quotation == "Sent" ? "QuotationSent" : request == "Closed" ? "Closed" : request is "UnderReview" or "AwaitingCustomer" ? request : "InquirySubmitted";
        var next = stage switch { "QuotationSent" => "ReviewQuotation", "AwaitingPayment" or "PaymentPending" or "PaymentOverdue" => "ViewInvoice", "ProposalSent" => "ReviewProposal", "AwaitingCustomer" or "BookingFailed" or "BookingPartiallyConfirmed" or "BookingCancelled" => "ContactAgent", _ => null };
        return new(stage, done.ToArray(), next);
    }
}
