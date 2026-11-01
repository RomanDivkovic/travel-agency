namespace TravelAgency.Domain.Requests;

public enum TravelService { Flights, Accommodation, FlightAccommodation, Complete }
public enum RequestStatus { Submitted }
public sealed record CustomerContact(string Name, string Email, string? Phone, string PreferredMethod);
public sealed record TravelPlan(string? Origin, string? Destination, bool FlexibleDestination, string[] AdditionalStops,
    string DateMode, DateOnly? Departure, DateOnly? Return, DateOnly? Earliest, DateOnly? Latest, int? DurationDays,
    int Adults, int[] ChildrenAges, int? Rooms, decimal BudgetAmount, string Currency, string BudgetScope,
    string PreferencesJson, bool Activities, string? SpecialRequests);

public sealed class TravelInquiry
{
    private TravelInquiry() { }
    public Guid Id { get; private set; }
    public string ReferenceNumber { get; private set; } = "";
    public TravelService Service { get; private set; }
    public RequestStatus Status { get; private set; }
    public string Language { get; private set; } = "";
    public CustomerContact Contact { get; private set; } = null!;
    public TravelPlan Plan { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TravelInquiry Create(string reference, TravelService service, string language,
        CustomerContact contact, TravelPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 40 || !Enum.IsDefined(service)
            || language is not ("sv" or "en") || now.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Email)
            || contact.Email.Contains('\r') || contact.Email.Contains('\n')
            || plan.BudgetAmount <= 0 || plan.Adults < 1 || plan.Adults + plan.ChildrenAges.Length > 20
            || plan.ChildrenAges.Any(age => age is < 0 or > 17))
            throw new ArgumentException("Invalid travel inquiry invariants.");
        // PostgreSQL timestamptz has microsecond precision: original and replay receipts must match exactly.
        now = now.AddTicks(-(now.Ticks % 10));
        return new TravelInquiry
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = reference,
            Service = service,
            Status = RequestStatus.Submitted,
            Language = language,
            Contact = contact,
            Plan = plan with { ChildrenAges = [.. plan.ChildrenAges], AdditionalStops = [.. plan.AdditionalStops] },
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
