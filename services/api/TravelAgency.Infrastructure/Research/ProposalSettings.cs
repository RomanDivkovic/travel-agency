using Microsoft.AspNetCore.DataProtection;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalSettings
{
    public bool SendingEnabled { get; set; }
    public string PublicOrigin { get; set; } = "";
    public int AccessRetentionDays { get; set; } = 30;
    public int DefaultValidityDays { get; set; } = 14;
    public string TermsVersion { get; set; } = "";
    public string TermsSv { get; set; } = "";
    public string TermsEn { get; set; } = "";
    public string AcceptanceTextSv { get; set; } = "";
    public string AcceptanceTextEn { get; set; } = "";
    public string Terms(string language) => (language == "sv" ? TermsSv : TermsEn).Trim();
    public string Acknowledgement(string language) => (language == "sv" ? AcceptanceTextSv : AcceptanceTextEn).Trim();
    public string[] BlockingReasons() => new[] { !SendingEnabled ? "disabled" : null, PublicOrigin.Length == 0 ? "origin" : null,
        new[] { TermsVersion, TermsSv, TermsEn, AcceptanceTextSv, AcceptanceTextEn }.Any(string.IsNullOrWhiteSpace) ? "reviewed-terms" : null }.OfType<string>().ToArray();
}
public sealed class ProposalTokenCodec(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("TravelAgency.Proposals.DeliveryLink.v1");
    public static string Create() => QuotationTokenCodec.Create();
    public static string? Hash(string? value) => QuotationTokenCodec.Hash(value);
    public string Protect(string value) => protector.Protect(value);
    public string Read(string value) => protector.Unprotect(value);
}
public sealed class ResearchOperationException(int status) : Exception { public int Status { get; } = status; }
