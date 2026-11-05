using TravelAgency.Domain.Customers;
namespace TravelAgency.Tests;

public sealed class CustomerJourneyTests
{
    [Theory]
    [InlineData("Declined", "QuotationDeclined")]
    [InlineData("Expired", "QuotationExpired")]
    [InlineData("Superseded", "QuotationSuperseded")]
    public void QuotationExceptionsAreNotPresentedAsProgress(string status, string stage)
    {
        var j = CustomerJourney.Project("Submitted", status, true, null, false, false, false, null, false, null);
        Assert.Equal(stage, j.Stage); Assert.DoesNotContain("QuotationAccepted", j.Completed); Assert.Null(j.NextAction);
    }
    [Fact]
    public void SimulatedPaymentDoesNotCompletePaymentOrResearch()
    {
        var j = CustomerJourney.Project("Submitted", "Accepted", true, "Paid", true, false, true, null, false, null);
        Assert.Equal("PaymentSimulated", j.Stage); Assert.DoesNotContain("PaymentConfirmed", j.Completed); Assert.DoesNotContain("TravelResearch", j.Completed);
    }
    [Theory]
    [InlineData("PartiallyConfirmed")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    public void BookingExceptionsRequireAgentContactWithoutClaimingFullConfirmation(string status)
    {
        var j = CustomerJourney.Project("Submitted", "Accepted", true, "Paid", false, true, true, "Approved", true, status);
        Assert.Equal("Booking" + status, j.Stage); Assert.Equal("ContactAgent", j.NextAction); Assert.DoesNotContain("BookingConfirmed", j.Completed);
    }
    [Fact]
    public void ConfirmedBookingCompletesJourney()
    {
        var j = CustomerJourney.Project("Submitted", "Accepted", true, "Paid", false, true, true, "Approved", true, "Confirmed");
        Assert.Equal("BookingConfirmed", j.Stage); Assert.Contains("PaymentConfirmed", j.Completed); Assert.Contains("BookingConfirmed", j.Completed); Assert.Null(j.NextAction);
    }
}
