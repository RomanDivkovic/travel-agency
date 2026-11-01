using TravelAgency.Domain.Quotations;

namespace TravelAgency.Tests;

public sealed class QuotationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    internal static QuotationDraft Draft(DateTimeOffset now, string terms = "TEST TERMS – not legal terms.") => new("Travel help <script>untrusted</script>", "TEST scope", "TEST exclusions", terms, "Customer-facing test note", "SEK", TaxTreatment.VatExclusive, now.AddDays(14), [new("Service", 3, 1995, 2500)]);
    private static Quotation Quote() => Quotation.Create(Guid.NewGuid(), 1, "sv", "Test Customer", "customer@example.test", "test-admin", Draft(Now), Now);
    [Theory]
    [InlineData(TaxTreatment.VatExclusive, 3, 1995, 2500, 5985, 1496, 7481)]
    [InlineData(TaxTreatment.VatIncluded, 3, 1995, 2500, 4788, 1197, 5985)]
    [InlineData(TaxTreatment.VatExclusive, 1, 1, 5000, 1, 1, 2)]
    [InlineData(TaxTreatment.NoVat, 2, 1234, 0, 2468, 0, 2468)]
    public void MinorUnitsAndPerLineRoundingAreExact(TaxTreatment treatment, int quantity, long unit, int rate, long subtotal, long tax, long total)
    {
        var line = QuotationLineItem.Calculate(Guid.NewGuid(), new("TEST service", quantity, unit, rate), treatment, 0);
        Assert.Equal(subtotal, line.SubtotalMinorUnits); Assert.Equal(tax, line.TaxMinorUnits); Assert.Equal(total, line.TotalMinorUnits);
    }
    [Fact]
    public void IssuedSnapshotsCannotBeEditedAndAcceptanceIsVersionSpecificAndIdempotent()
    {
        var quote = Quote(); quote.Issue("test-v1", "TEST acceptance", Now);
        Assert.Throws<QuotationRuleException>(() => quote.UpdateDraft(Draft(Now), Now));
        Assert.True(quote.Respond(true, Now.AddHours(1))); Assert.False(quote.Respond(true, Now.AddHours(2)));
        Assert.Equal("test-v1", quote.AcceptedTermsVersion); Assert.Equal(Now.AddHours(1), quote.AcceptedAt); Assert.Equal(1, quote.Version);
        Assert.Throws<QuotationRuleException>(() => quote.Respond(false, Now.AddHours(2)));
        Assert.Throws<QuotationRuleException>(() => quote.Revise("test-admin", Now.AddDays(20), "Changed", Now));
        Assert.Throws<QuotationRuleException>(() => quote.Cancel(Now));
    }
    [Fact]
    public void RevisionsPreservePricesAndTermsAndCannotReopenTerminalVersions()
    {
        var quote = Quote(); quote.Issue("test-v1", "TEST acceptance", Now);
        var revision = quote.Revise("test-admin", Now.AddDays(20), "TEST new terms", Now.AddHours(1)); quote.Supersede(Now.AddHours(1));
        Assert.Equal(2, revision.Version); Assert.Equal(quote.SeriesId, revision.SeriesId); Assert.Equal(quote.TotalMinorUnits, revision.TotalMinorUnits);
        Assert.NotEqual(quote.TermsSnapshot, revision.TermsSnapshot); Assert.Equal(QuotationStatus.Draft, revision.Status);
        Assert.Throws<QuotationRuleException>(() => quote.Respond(true, Now.AddHours(1)));
        revision.Cancel(Now); Assert.Throws<QuotationRuleException>(() => revision.Respond(true, Now));
    }
    [Fact]
    public void DeadlineIsEnforcedEvenBeforeBackgroundExpiration()
    {
        var quote = Quote(); quote.Issue("test-v1", "TEST acceptance", Now);
        Assert.Throws<QuotationRuleException>(() => quote.Respond(true, quote.ValidUntil)); Assert.True(quote.Expire(quote.ValidUntil)); Assert.False(quote.Expire(quote.ValidUntil));
        Assert.Equal(QuotationStatus.Expired, quote.Status); Assert.Throws<QuotationRuleException>(() => quote.Respond(false, Now));
    }
    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(101, 100, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 100, 10001)]
    public void InvalidLineValuesAreRejected(int quantity, long price, int rate) => Assert.Throws<QuotationRuleException>(() => QuotationLineItem.Calculate(Guid.NewGuid(), new("TEST", quantity, price, rate), TaxTreatment.VatExclusive, 0));
    [Fact]
    public void InvalidCurrencyDatesAndUnconfirmedIssuanceAreRejected()
    {
        var quote = Quote(); Assert.Throws<QuotationRuleException>(() => quote.UpdateDraft(Draft(Now) with { Currency = "JPY" }, Now));
        Assert.Throws<QuotationRuleException>(() => quote.UpdateDraft(Draft(Now) with { ValidUntil = Now }, Now));
        quote.UpdateDraft(Draft(Now) with { TaxTreatment = TaxTreatment.Unconfirmed, Lines = [new("TEST", 1, 100, 0)] }, Now);
        Assert.Throws<QuotationRuleException>(() => quote.Issue("test-v1", "TEST", Now));
    }
}
