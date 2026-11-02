namespace TravelAgency.Domain.Admin;

public sealed class RequestNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string AdministratorId { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class RequestActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? QuotationId { get; set; }
    public string Type { get; set; } = "Submitted";
    public string? ActorId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
