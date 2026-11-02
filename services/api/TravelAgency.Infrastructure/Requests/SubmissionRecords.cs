namespace TravelAgency.Infrastructure.Requests;

public sealed class IdempotencyRecord
{
    public string KeyHash { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public Guid RequestId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
public sealed class NotificationRecord
{
    public Guid? BookingId { get; set; }
    public int? BookingRevision { get; set; }
    public string? PayloadJson { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? QuotationId { get; set; }
    public string? ProtectedLink { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string Kind { get; set; } = "";
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeadLetterAt { get; set; }
}
