namespace TravelAgency.Application.Requests;

public sealed record SubmitTravelRequest(int? SchemaVersion, string? Language, string? Service,
    DestinationInput? Destination, DatesInput? Dates, TravelersInput? Travelers, BudgetInput? Budget,
    PreferencesInput? Preferences, bool? Activities, string? SpecialRequests, ContactInput? Contact, string? Website = null);
public sealed record DestinationInput(string? Origin, string? Place, bool? Flexible, string[]? AdditionalStops);
public sealed record DatesInput(string? Mode, DateOnly? Departure, DateOnly? Return, DateOnly? Earliest, DateOnly? Latest, int? DurationDays);
public sealed record TravelersInput(int? Adults, int[]? ChildrenAges, int? Rooms);
public sealed record BudgetInput(decimal? Amount, string? Currency, string? Scope);
public sealed record PreferencesInput(FlightInput? Flight, AccommodationInput? Accommodation, TripInput? Trip);
public sealed record FlightInput(string? Priority, string? Baggage, bool? FlexibleAirports);
public sealed record AccommodationInput(string? Type, bool? Breakfast, bool? Pool, bool? FamilyFriendly);
public sealed record TripInput(string? Pace, string[]? Interests);
public sealed record ContactInput(string? Name, string? Email, string? Phone, string? PreferredMethod);
public sealed record SubmissionReceipt(string ReferenceNumber, string Status, DateTimeOffset SubmittedAt);
public sealed record SubmissionResult(SubmissionReceipt? Receipt = null, bool Conflict = false, bool Replayed = false,
    Dictionary<string, string[]>? Errors = null);
public sealed class SubmissionSettings
{
    public string ReferencePrefix { get; set; } = "TRV";
    public int IdempotencyRetentionDays { get; set; } = 30;
    public int RequestRetentionDays { get; set; } = 365;
    public bool PurgeExpiredRequests { get; set; }
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
public interface ITravelRequestStore
{
    Task<SubmissionResult?> FindReplayAsync(Guid key, string payloadHash, CancellationToken cancellationToken);
    Task<SubmissionResult> SaveAsync(Guid key, string payloadHash, SubmitTravelRequest request, CancellationToken cancellationToken);
}
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
public sealed record EmailMessage(Guid Id, string To, string Subject, string Text, string Html);
public interface INotificationProcessor
{
    Task<bool> ProcessOneAsync(CancellationToken cancellationToken);
}
public sealed class EmailDeliveryException(bool retryable) : Exception("Email transport did not accept the notification.")
{
    public bool Retryable { get; } = retryable;
}
