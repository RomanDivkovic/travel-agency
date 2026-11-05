using TravelAgency.Application.Admin;
using TravelAgency.Domain.Quotations;

namespace TravelAgency.Application.Quotations;

public sealed record QuotationDraftInput(Guid VersionToken, string? Description, string? IncludedServices, string? ExcludedServices,
    string? TermsSnapshot, string? CustomerNotes, string? Currency, string? TaxTreatment, DateTimeOffset? ValidUntil, LineSpecification[]? Lines)
{
    public QuotationDraft? ToDraft()
    {
        if (Description is null || IncludedServices is null || ExcludedServices is null || TermsSnapshot is null || CustomerNotes is null || Currency is null
            || !Enum.TryParse<TaxTreatment>(TaxTreatment, false, out var treatment) || treatment.ToString() != TaxTreatment || !Enum.IsDefined(treatment)
            || ValidUntil is null || Lines is null || Lines.Any(l => l is null)) return null;
        return new(Description, IncludedServices, ExcludedServices, TermsSnapshot, CustomerNotes, Currency, treatment, ValidUntil.Value, Lines);
    }
}
public sealed record QuotationLineDto(string Description, int Quantity, long UnitPriceMinorUnits, int TaxRateBasisPoints, long SubtotalMinorUnits, long TaxMinorUnits, long TotalMinorUnits);
public sealed record CustomerQuotationDto(string Number, int Version, string Status, string Language, string CustomerName, string Description,
    string IncludedServices, string ExcludedServices, string CustomerNotes, string Currency, string TaxTreatment, long SubtotalMinorUnits,
    long TaxMinorUnits, long TotalMinorUnits, DateTimeOffset ValidUntil, string TermsSnapshot, string TermsVersion, string AcceptanceText,
    DateTimeOffset? AcceptedAt, DateTimeOffset? DeclinedAt, QuotationLineDto[] Lines);
public sealed record AdminQuotationDto(Guid Id, Guid RequestId, string RequestReference, Guid VersionToken, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, DateTimeOffset? SentAt, string CustomerEmail, bool IsCurrentVersion, bool CanRevise, CustomerQuotationDto Quotation);
public sealed record QuotationListItem(Guid Id, Guid RequestId, string RequestReference, string Number, int Version, string CustomerName,
    string ServiceType, string Status, string Currency, long TotalMinorUnits, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset ValidUntil, DateTimeOffset? SentAt, DateTimeOffset? AcceptedAt, DateTimeOffset? DeclinedAt);
public sealed record QuotationQuery(int Page = 1, int PageSize = 20, string? Status = null, string? Search = null, string Sort = "createdAt_desc", Guid? RequestId = null)
{
    public bool IsValid => Page is >= 1 and <= 100000 && PageSize is >= 1 and <= 100 && (Search is null || Search.Length <= 150)
        && Sort is "createdAt_desc" or "createdAt_asc" or "updatedAt_desc"
        && (Status is null || Enum.TryParse<QuotationStatus>(Status, false, out var status) && Enum.IsDefined(status) && status.ToString() == Status);
}
public sealed record QuotationConfigurationDto(bool IssuanceReady, string[] BlockingReasons, string[] ApprovedTaxTreatments, int[] ApprovedTaxRatesBasisPoints,
    string TermsVersion, string TermsSv, string TermsEn, int DefaultValidityDays);
public enum QuotationOutcome { Saved, NotFound, Invalid, Conflict, IssuanceBlocked }
public sealed record QuotationResult(QuotationOutcome Outcome, Guid? Id = null, CustomerQuotationDto? Quotation = null);
public interface IQuotationDrafts
{
    Task<QuotationResult> CreateAsync(Guid requestId, string actor, CancellationToken ct);
    Task<QuotationResult> UpdateAsync(Guid id, QuotationDraftInput input, string actor, CancellationToken ct);
    Task<QuotationResult> ReviseAsync(Guid id, Guid versionToken, string actor, CancellationToken ct);
}
public interface IQuotationIssuance
{
    Task<QuotationResult> SendAsync(Guid id, Guid versionToken, string actor, CancellationToken ct);
    Task<QuotationResult> CancelAsync(Guid id, Guid versionToken, string actor, CancellationToken ct);
}
public interface IQuotationCustomers
{
    Task<QuotationResult> OwnedAsync(string owner, Guid id, bool? decision, bool confirmed, CancellationToken ct);
    Task<QuotationResult> ReviewAsync(string token, CancellationToken ct);
    Task<QuotationResult> RespondAsync(string token, bool accept, bool confirmed, CancellationToken ct);
}
public interface IQuotationQueries
{
    Task<PageResult<QuotationListItem>> ListAsync(QuotationQuery query, CancellationToken ct);
    Task<AdminQuotationDto?> DetailAsync(Guid id, CancellationToken ct);
    Task<PageResult<ActivityDto>> HistoryAsync(Guid id, int page, CancellationToken ct);
}
public interface IQuotationExpiration { Task ExpireBatchAsync(CancellationToken ct); }
