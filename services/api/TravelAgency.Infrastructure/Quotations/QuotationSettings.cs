using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationSettings
{
    public bool IssuanceEnabled { get; set; }
    public string ReferencePrefix { get; set; } = "QUO";
    public string PublicOrigin { get; set; } = "";
    public int DefaultValidityDays { get; set; } = 14;
    public int AccessRetentionDays { get; set; } = 30;
    public int PublicPermitLimit { get; set; } = 60;
    public string[] ApprovedTaxTreatments { get; set; } = [];
    public int[] ApprovedTaxRatesBasisPoints { get; set; } = [];
    public string TermsVersion { get; set; } = "";
    public string TermsSv { get; set; } = "";
    public string TermsEn { get; set; } = "";
    public string AcceptanceTextSv { get; set; } = "";
    public string AcceptanceTextEn { get; set; } = "";
    public string Terms(string language) => (language == "sv" ? TermsSv : TermsEn).Trim();
    public string Acknowledgement(string language) => (language == "sv" ? AcceptanceTextSv : AcceptanceTextEn).Trim();
    public string[] BlockingReasons()
    {
        var reasons = new List<string>();
        if (!IssuanceEnabled) reasons.Add("disabled");
        if (string.IsNullOrWhiteSpace(PublicOrigin)) reasons.Add("origin");
        if (ApprovedTaxTreatments.Length == 0 || (ApprovedTaxTreatments.Any(t => t is "VatExclusive" or "VatIncluded") && ApprovedTaxRatesBasisPoints.Length == 0)) reasons.Add("tax");
        if (string.IsNullOrWhiteSpace(TermsVersion) || string.IsNullOrWhiteSpace(TermsSv) || string.IsNullOrWhiteSpace(TermsEn)) reasons.Add("terms");
        if (string.IsNullOrWhiteSpace(AcceptanceTextSv) || string.IsNullOrWhiteSpace(AcceptanceTextEn)) reasons.Add("acceptance");
        return reasons.ToArray();
    }
    public bool Approves(Quotation q) => BlockingReasons().Length == 0 && ApprovedTaxTreatments.Contains(q.TaxTreatment.ToString())
        && q.TermsSnapshot == Terms(q.Language) && q.Lines.All(l => q.TaxTreatment == TaxTreatment.NoVat ? l.TaxRateBasisPoints == 0 : ApprovedTaxRatesBasisPoints.Contains(l.TaxRateBasisPoints));
    public QuotationConfigurationDto ToDto() => new(BlockingReasons().Length == 0, BlockingReasons(), ApprovedTaxTreatments, ApprovedTaxRatesBasisPoints, TermsVersion, TermsSv, TermsEn, DefaultValidityDays);
}
