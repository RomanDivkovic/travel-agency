namespace TravelAgency.Domain.Payments;

public enum PaymentStatus { Pending, Succeeded, Failed, Cancelled }
public sealed class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Provider { get; set; } = "";
    public string ProviderSessionId { get; set; } = "";
    public string? ProviderPaymentId { get; private set; }
    public long AmountMinorUnits { get; set; }
    public string Currency { get; set; } = "";
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? FailureReasonCode { get; set; }
    public int ReconcileAttempts { get; set; }
    public DateTimeOffset NextReconcileAt { get; set; }
    public string? DiscrepancyCode { get; set; }
    public bool Apply(PaymentStatus status, string paymentId, DateTimeOffset now)
    {
        // Verified success wins over delayed failure/cancellation. A terminal provider session cannot be retried.
        if (Status == PaymentStatus.Succeeded || status == PaymentStatus.Pending || Status == status) return false;
        Status = status; ProviderPaymentId = paymentId; UpdatedAt = now;
        if (status == PaymentStatus.Succeeded) { ConfirmedAt = now; FailureReasonCode = null; } else FailureReasonCode = status == PaymentStatus.Failed ? "provider-failed" : "customer-cancelled";
        return true;
    }
}
public sealed class InvoiceAccessToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Hash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public sealed class PaymentEventRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Provider { get; set; } = "";
    public string EventId { get; set; } = "";
    public string Outcome { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
}
public sealed class ResearchEligibility
{
    public Guid RequestId { get; set; }
    public Guid InvoiceId { get; set; }
    public bool IsSimulated { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}
public sealed class InvoiceCounter
{
    public int Id { get; set; } = 1;
    public long Value { get; set; }
}
