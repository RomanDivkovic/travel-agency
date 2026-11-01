using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Tests;

public sealed class PaymentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static Quotation Quote(bool accepted)
    {
        var q = Quotation.Create(Guid.NewGuid(), 1, "sv", "TEST", "test@example.test", "admin", QuotationDomainTests.Draft(Now), Now);
        q.Issue("TEST", "TEST", Now); if (accepted) q.Respond(true, Now); return q;
    }
    [Fact]
    public void InvoiceRequiresAcceptanceAndCopiesExactImmutableAmounts()
    {
        Assert.Throws<InvoiceRuleException>(() => Invoice.Create(Quote(false), Guid.NewGuid(), "TEST", true, Now));
        var q = Quote(true); var i = Invoice.Create(q, Guid.NewGuid(), "TEST", true, Now);
        Assert.Equal(q.Id, i.QuotationId); Assert.Equal(q.Version, i.QuotationVersion); Assert.Equal(q.TotalMinorUnits, i.TotalMinorUnits);
        Assert.Equal(q.TaxMinorUnits, i.TaxMinorUnits); Assert.Equal(q.SubtotalMinorUnits, i.SubtotalMinorUnits); Assert.Equal(q.Lines.Count, i.Lines().Length);
        Assert.Equal(i.TotalMinorUnits, i.Lines().Sum(l => l.TotalMinorUnits));
    }
    [Fact]
    public void InvoiceTransitionsEnforceDraftDetailsAndPreserveIssuedSnapshot()
    {
        var i = Invoice.Create(Quote(true), Guid.NewGuid(), "TEST", true, Now);
        Assert.Throws<InvoiceRuleException>(() => i.MarkPaid(i.TotalMinorUnits, i.Currency, Now));
        Assert.Throws<InvoiceRuleException>(() => i.Issue("TEST", "TEST", "TEST", "TEST", "TEST", "TEST", "TEST", Now));
        i.Edit(new("TEST address", "TEST agreed future planning", Now.AddDays(2)), Now); i.Issue("TEST-INV-1", "TEST", "TEST", "TEST", "TEST", "TEST", "TEST", Now);
        Assert.Throws<InvoiceRuleException>(() => i.Edit(new("Changed", "Changed", Now.AddDays(3)), Now)); Assert.Throws<InvoiceRuleException>(() => i.CancelDraft(Now));
        i.BeginPayment(Now); Assert.Equal(InvoiceStatus.PaymentPending, i.Status); Assert.True(i.MarkOverdue(Now.AddDays(3))); Assert.False(i.MarkOverdue(Now.AddDays(4)));
        Assert.Throws<InvoiceRuleException>(() => i.MarkPaid(i.TotalMinorUnits + 1, i.Currency, Now)); Assert.Throws<InvoiceRuleException>(() => i.MarkPaid(i.TotalMinorUnits, "EUR", Now));
        Assert.True(i.MarkPaid(i.TotalMinorUnits, i.Currency, Now.AddDays(4))); var paidAt = i.PaidAt;
        Assert.False(i.MarkPaid(i.TotalMinorUnits, i.Currency, Now.AddDays(5))); Assert.Equal(paidAt, i.PaidAt);
    }
    [Fact]
    public void CancellationAndUnapprovedIssuanceAreClosedByDefault()
    {
        var i = Invoice.Create(Quote(true), Guid.NewGuid(), "TEST", true, Now); i.CancelDraft(Now);
        Assert.Throws<InvoiceRuleException>(() => i.BeginPayment(Now)); Assert.Throws<InvoiceRuleException>(() => i.MarkPaid(i.TotalMinorUnits, i.Currency, Now));
        Assert.Contains("disabled", new PaymentSettings().BlockingReasons(new QuotationSettings()));
    }
    [Fact]
    public void SuccessIsMonotonicAcrossDelayedEvents()
    {
        var p = new PaymentTransaction(); Assert.True(p.Apply(PaymentStatus.Failed, "test-payment", Now)); Assert.True(p.Apply(PaymentStatus.Succeeded, "test-payment", Now.AddMinutes(1)));
        Assert.False(p.Apply(PaymentStatus.Failed, "test-payment", Now.AddMinutes(2))); Assert.False(p.Apply(PaymentStatus.Cancelled, "test-payment", Now.AddMinutes(3))); Assert.False(p.Apply(PaymentStatus.Succeeded, "test-payment", Now.AddMinutes(4)));
        Assert.Equal(PaymentStatus.Succeeded, p.Status); Assert.Equal(Now.AddMinutes(1), p.ConfirmedAt);
    }
}
