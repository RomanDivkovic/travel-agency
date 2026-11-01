using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Infrastructure.Payments;

public sealed class PaymentSettings
{
    public string Provider { get; set; } = "Disabled";
    public bool IssuanceEnabled { get; set; }
    public bool AccountingApproved { get; set; }
    public string PublicOrigin { get; set; } = "";
    public string SellerName { get; set; } = "";
    public string SellerAddress { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public string TaxInformation { get; set; } = "";
    public string TermsSv { get; set; } = "";
    public string TermsEn { get; set; } = "";
    public string TermsVersion { get; set; } = "";
    public string SandboxSigningSecret { get; set; } = "";
    public int AccessDays { get; set; } = 90;
    public string Terms(string language) => language == "sv" ? TermsSv : TermsEn;
    public string[] BlockingReasons(QuotationSettings tax)
    {
        var result = new List<string>();
        if (!IssuanceEnabled) result.Add("disabled");
        if (Provider == "Disabled") result.Add("provider");
        if (!AccountingApproved) result.Add("accounting-review");
        if (PublicOrigin.Length == 0) result.Add("origin");
        if (new[] { SellerName, SellerAddress, RegistrationNumber, TaxInformation }.Any(string.IsNullOrWhiteSpace)) result.Add("business-details");
        if (new[] { TermsSv, TermsEn, TermsVersion }.Any(string.IsNullOrWhiteSpace)) result.Add("terms");
        if (tax.ApprovedTaxTreatments.Length == 0) result.Add("tax");
        return result.ToArray();
    }
    public bool Approves(Invoice i, QuotationSettings tax) => BlockingReasons(tax).Length == 0 && i.IsSimulated == (Provider == "Sandbox")
        && tax.ApprovedTaxTreatments.Contains(i.TaxTreatment) && i.Lines().All(l => i.TaxTreatment == "NoVat" ? l.TaxRateBasisPoints == 0 : tax.ApprovedTaxRatesBasisPoints.Contains(l.TaxRateBasisPoints));
}
