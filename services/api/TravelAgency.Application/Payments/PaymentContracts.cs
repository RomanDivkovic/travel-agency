using TravelAgency.Domain.Payments;

namespace TravelAgency.Application.Payments;

public sealed record PaymentSessionRequest(Guid AttemptId, Guid InvoiceId, long AmountMinorUnits, string Currency, string ReturnUrl);
public sealed record PaymentSessionResult(string SessionId, string CheckoutUrl, DateTimeOffset ExpiresAt);
public sealed record PaymentWebhookRequest(string Body, string Signature, string Timestamp);
public sealed record VerifiedPaymentEvent(string EventId, string SessionId, string PaymentId, Guid InvoiceId, long AmountMinorUnits, string Currency, PaymentStatus Status, string Merchant);
public interface IPaymentProvider
{
    string Name { get; }
    bool IsSimulated { get; }
    Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken ct);
    VerifiedPaymentEvent VerifyWebhook(PaymentWebhookRequest request);
    Task<VerifiedPaymentEvent?> ReconcileAsync(string sessionId, CancellationToken ct);
}
public sealed class PaymentProviderException : Exception;
public sealed record InvoiceView(string? Number, string QuotationNumber, int QuotationVersion, string Status, bool IsSimulated, string Language,
    string CustomerName, string CustomerAddress, string Description, string ServicePeriod, string Currency, string TaxTreatment,
    long SubtotalMinorUnits, long TaxMinorUnits, long TotalMinorUnits, InvoiceLine[] Lines, string SellerName, string SellerAddress,
    string RegistrationNumber, string TaxInformation, string Terms, string TermsVersion, DateTimeOffset DueAt, DateTimeOffset? IssuedAt, DateTimeOffset? PaidAt, string? LastPaymentStatus = null);
public sealed record AdminInvoiceView(Guid Id, Guid RequestId, Guid QuotationId, Guid VersionToken, InvoiceView Invoice, PaymentAttemptView[] Payments, bool ResearchEligible, bool SandboxResearchEligible, bool CustomerLinkActive);
public sealed record PaymentAttemptView(Guid Id, string Provider, string Status, long AmountMinorUnits, string Currency, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt, string? FailureReasonCode, string? DiscrepancyCode, int ReconcileAttempts);
public sealed record InvoiceListItem(Guid Id, string? Number, string CustomerName, string Status, string Currency, long TotalMinorUnits, DateTimeOffset DueAt, bool IsSimulated);
public sealed record InvoicePage(InvoiceListItem[] Items, int Page, int PageSize, int Total);
public sealed record CurrencySummary(string Currency, bool IsSimulated, long PaidMinorUnits, long OutstandingMinorUnits, int PendingPayments, int OverdueInvoices);
public sealed record PaymentSummary(CurrencySummary[] Currencies, InvoiceListItem[] RecentPayments);
public static class InvoiceDtos
{
    public static InvoiceView Customer(Invoice i) => new(i.Number, i.QuotationNumber, i.QuotationVersion, i.Status.ToString(), i.IsSimulated, i.Language,
        i.CustomerName, i.CustomerAddress, i.Description, i.ServicePeriod, i.Currency, i.TaxTreatment, i.SubtotalMinorUnits, i.TaxMinorUnits, i.TotalMinorUnits, i.Lines(),
        i.SellerName, i.SellerAddress, i.RegistrationNumber, i.TaxInformation, i.Terms, i.TermsVersion, i.DueAt, i.IssuedAt, i.PaidAt);
    public static InvoiceListItem Item(Invoice i) => new(i.Id, i.Number, i.CustomerName, i.Status.ToString(), i.Currency, i.TotalMinorUnits, i.DueAt, i.IsSimulated);
    public static PaymentAttemptView Attempt(PaymentTransaction p) => new(p.Id, p.Provider, p.Status.ToString(), p.AmountMinorUnits, p.Currency, p.CreatedAt, p.ConfirmedAt, p.FailureReasonCode, p.DiscrepancyCode, p.ReconcileAttempts);
}
