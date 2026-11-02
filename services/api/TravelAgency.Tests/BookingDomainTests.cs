using System.Text.Json;
using TravelAgency.Application.Bookings;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Bookings;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Tests;

public sealed class BookingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    internal static ApprovalEvidence Evidence(DateTimeOffset? at = null) => new("TEST customer", "Email", "TEST customer email approval reference", at ?? DateTimeOffset.UtcNow, true);
    internal static ResearchOptionInput Current(ResearchOptionInput? input = null, DateTimeOffset? now = null) => (input ?? ResearchDomainTests.Extra()) with { PriceCheckedAt = now ?? DateTimeOffset.UtcNow, AvailabilityCheckedAt = now ?? DateTimeOffset.UtcNow };
    private static TravelBooking Booking(int count = 2)
    {
        var p = TravelProposal.Create(Guid.NewGuid(), 1, "sv", "TEST", "test@example.test", "actor", new("ARN", "Lisbon", false, [], "fixed", new(2027, 6, 1), new(2027, 6, 7), null, null, null, 2, 0, 1), new("TEST-INV", "SEK", 10000, Now), Now.AddDays(14), Now);
        var items = Enumerable.Range(0, count).Select(n => { var o = ResearchOption.Create(Guid.NewGuid(), Current(now: Now), Now); return ProposalItem.Create(o.Id, o.VersionToken, o.SnapshotJson, n); });
        var a = ProposalAlternative.Create(p.Id, "TEST selected", "TEST details", false, 0, items); p.Edit("TEST", "TEST", "TEST", "TEST", p.ValidUntil, [a], Now); p.Send("TEST", "TEST", Now); p.Respond(true, a.Id, Now);
        return TravelBooking.Create(Guid.NewGuid(), p, a, "BKG-TEST", "actor", Evidence(Now), Now);
    }
    private static void Prepare(SupplierReservation r) => r.Update(Current(now: Now), SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST customer direct authority", Now);
    private static void Confirm(SupplierReservation r) { r.Start(Now); r.Confirm("TEST-REF", "TEST supplier confirmation email", "", "NotApplicable", Now, Now); }
    [Fact]
    public void ApprovalDoesNotConfirmReservationsAndMixedTripRetainsConfirmedItems()
    {
        var b = Booking(); Assert.Equal(BookingStatus.Pending, b.Status); Assert.All(b.Reservations, r => Assert.Equal(ReservationStatus.NotStarted, r.Status));
        var first = b.Reservations[0]; Prepare(first); Confirm(first); b.Recalculate(Now); Assert.Equal(BookingStatus.PartiallyConfirmed, b.Status);
        var second = b.Reservations[1]; Prepare(second); second.Start(Now); second.Fail("TEST supplier unavailable"); b.Recalculate(Now);
        Assert.Equal(BookingStatus.PartiallyConfirmed, b.Status); Assert.Equal(ReservationStatus.Confirmed, first.Status);
        second.Start(Now); second.AwaitConfirmation(); second.Confirm("TEST2", "TEST actual evidence", "", "NotApplicable", Now, Now); b.Recalculate(Now); Assert.Equal(BookingStatus.Confirmed, b.Status); Assert.Equal(Now, b.CompletedAt);
        second.Cancel("TEST requested", "TEST cancellation confirmed", 100, Now, Now); b.Recalculate(Now); Assert.Equal(BookingStatus.PartiallyConfirmed, b.Status); Assert.Null(b.CompletedAt); Assert.NotNull(second.ConfirmedAt);
    }
    [Fact]
    public void PaymentResponsibilityAndFreshChecksRequiredBeforeBooking()
    {
        var r = Booking().Reservations[0]; Assert.Throws<BookingRuleException>(() => r.Start(Now));
        r.Update(ResearchDomainTests.Extra(), SupplierPaymentResponsibility.AgentPaysSupplier, SupplierPaymentStatus.Unknown, "TEST explicit arrangement", Now);
        Assert.Throws<BookingRuleException>(() => r.Start(Now)); Prepare(r); Assert.Throws<BookingRuleException>(() => r.Start(Now)); // changed payment responsibility requires approval
        r.Reapprove(Evidence(Now), Now); r.Start(Now);
        Assert.Throws<BookingRuleException>(() => r.Confirm("", "", "", "NotApplicable", Now, Now));
        Assert.Throws<BookingRuleException>(() => r.Confirm("TEST", "TEST evidence", "javascript:alert(1)", "NotApplicable", Now, Now));
    }
    [Theory]
    [InlineData("price")]
    [InlineData("currency")]
    [InlineData("supplier")]
    [InlineData("conditions")]
    [InlineData("lower-with-change")]
    public void ChangesRequireExplicitSnapshotBoundReapproval(string change)
    {
        var r = Booking().Reservations[0]; Prepare(r); var i = Current(now: Now);
        i = change switch { "price" => i with { PriceMinorUnits = i.PriceMinorUnits + 1 }, "currency" => i with { Currency = "EUR" }, "supplier" => i with { SupplierName = "TEST replacement" }, "conditions" => i with { Conditions = "TEST nonrefundable" }, _ => i with { PriceMinorUnits = 1, Conditions = "TEST changed terms" } };
        r.Update(i, SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.Unknown, "TEST customer direct authority", Now); Assert.True(r.RequiresReview);
        Assert.Throws<BookingRuleException>(() => r.Start(Now)); Assert.Throws<BookingRuleException>(() => r.Reapprove(Evidence(Now) with { Confirmed = false }, Now));
        r.Reapprove(Evidence(Now), Now); r.Start(Now); r.Confirm("TEST", "TEST genuine evidence", "", "NotApplicable", Now, Now);
        var original = r.ApprovedSnapshotJson; r.Update(i with { Conditions = "TEST changed again" }, SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.Unknown, "TEST customer direct authority", Now);
        Assert.Equal(original, r.ApprovedSnapshotJson); Assert.Null(r.ReapprovedVersion); Assert.Equal(ReservationStatus.AwaitingConfirmation, r.Status); Assert.Throws<BookingRuleException>(() => r.Confirm("TEST", "TEST", "", "NotApplicable", Now, Now));
    }
    [Fact]
    public void AnUnapprovedPaymentChangeCannotBeClearedByAnUnrelatedEdit()
    {
        var r = Booking(1).Reservations[0]; Prepare(r);
        r.Update(Current(now: Now), SupplierPaymentResponsibility.AgentPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST changed payment authority", Now);
        Assert.True(r.RequiresReview);
        r.Update(Current(now: Now) with { PriceMinorUnits = 500 }, SupplierPaymentResponsibility.AgentPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST changed payment authority", Now);
        Assert.True(r.RequiresReview); Assert.Throws<BookingRuleException>(() => r.Start(Now));
        r.Reapprove(Evidence(Now), Now); r.Start(Now);
    }
    [Fact]
    public void AnAuthorizedExtraReplacementKeepsItsReservationTypeAccurate()
    {
        var r = Booking(1).Reservations[0]; Prepare(r); var i = Current(now: Now);
        r.Update(i with { Extra = i.Extra! with { Category = "Activity" } }, SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST customer direct authority", Now);
        Assert.Equal("Activity", r.ReservationType); Assert.True(r.RequiresReview);
        Assert.Equal("Transfer", r.Approved().Extra!.Category);
        Assert.Throws<BookingRuleException>(() => r.Start(Now)); r.Reapprove(Evidence(Now), Now); r.Start(Now);
    }
    [Fact]
    public void LowerPriceWithUnchangedTermsDoesNotRequireRenewedApproval()
    {
        var r = Booking(1).Reservations[0]; r.Update(Current(now: Now) with { PriceMinorUnits = 1000 }, SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST direct authority", Now); Assert.False(r.RequiresReview); r.Start(Now);
    }
    [Fact]
    public void FullCancellationNeedsEvidenceAndCannotExecuteSupplierOperations()
    {
        var b = Booking(1); var r = b.Reservations[0]; Prepare(r); Confirm(r);
        Assert.Throws<BookingRuleException>(() => r.Cancel("TEST", "", null, Now, Now)); Assert.Throws<BookingRuleException>(() => r.Cancel("TEST", "TEST", -1, Now, Now));
        r.Cancel("TEST customer", "TEST supplier cancellation", null, Now, Now); b.Recalculate(Now); Assert.Equal(BookingStatus.Cancelled, b.Status); Assert.Equal(Now, b.CancelledAt); Assert.Equal("TEST-REF", r.SupplierReference);
        Assert.Throws<BookingRuleException>(() => r.Start(Now));
    }
    [Theory]
    [InlineData("sv", "Din resebokning är bekräftad")]
    [InlineData("en", "Your travel booking is confirmed")]
    public void ConfirmationEmailsUseActualReferencesEscapeHtmlAndIncludeDates(string language, string heading)
    {
        var s = ResearchOption.Create(Guid.NewGuid(), Current(ResearchDomainTests.Flight(), Now), Now).Snapshot();
        var payload = new BookingNotificationSnapshot("TEST <script>", "test@example.test", language, "BKG-TEST", "TEST Lisbon", Now, [new("Flight", s, "TEST-REAL-REF", "TEST evidence", "TEST mail reference", "Issued", Now)]);
        var n = new NotificationRecord { Kind = "booking-confirmed-1", PayloadJson = JsonSerializer.Serialize(payload) }; var email = BookingNotificationTemplates.Create(n, new EmailSettings());
        Assert.StartsWith(heading, email.Subject); Assert.Contains("TEST-REAL-REF", email.Text); Assert.Contains("2027-06-01", email.Text); Assert.DoesNotContain("<script>", email.Html); Assert.DoesNotContain("SECRET", email.Text); Assert.Contains("&lt;script&gt;", email.Html);
    }
}
