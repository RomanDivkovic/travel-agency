using TravelAgency.Domain.Requests;
using TravelAgency.Domain.Research;

namespace TravelAgency.Application.Customers;

public sealed record CustomerRegistration(string FirstName, string LastName, string Email, string Password, string PasswordConfirmation, string? Phone, string PreferredLanguage = "sv");
public sealed record CustomerLogin(string Email, string Password);
public sealed record CustomerEmailInput(string Email);
public sealed record CustomerTokenInput(string Token);
public sealed record CustomerResetInput(string Token, string Password, string PasswordConfirmation);
public sealed record CustomerProfileInput(string FirstName, string LastName, string? Phone, string PreferredLanguage);
public sealed record CustomerLanguageInput(string PreferredLanguage);
public sealed record CustomerPasswordInput(string CurrentPassword, string Password, string PasswordConfirmation);
public sealed record CustomerClaimInput(string Reference);
public sealed record CustomerUserDto(string Id, string FirstName, string LastName, string Email, string? Phone, string PreferredLanguage, DateTimeOffset? LastLoginAt, DateTimeOffset? SessionExpiresAt = null);
public sealed record PortalItem(Guid Id, Guid RequestId, string Kind, string Number, string Title, string Status, string? Destination, DateOnly? Departure,
    string? Currency, long? AmountMinorUnits, DateTimeOffset? ValidUntil, DateTimeOffset UpdatedAt)
{ public PortalItem() : this(Guid.Empty, Guid.Empty, "", "", "", "", null, null, null, null, null, default) { } public CustomerJourneyDto? Journey { get; init; } public bool IsSimulated { get; init; } }
public sealed record CustomerJourneyDto(string Stage, string[] CompletedStages, string? NextAction);
public sealed record CustomerRequestDto(Guid Id, string Reference, string Service, string Status, string Language, CustomerContact Contact, TravelPlan Plan,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, CustomerJourneyDto Journey);
public sealed record PortalActivity(Guid Id, Guid RequestId, string Type, DateTimeOffset CreatedAt) { public PortalActivity() : this(Guid.Empty, Guid.Empty, "", default) { } }
public sealed record CustomerDashboardDto(CustomerUserDto Customer, CustomerRequestDto[] Requests, PortalItem[] ActionsRequired, PortalItem[] UpcomingTrips, PortalActivity[] RecentActivity);
public sealed record CustomerBookingReservation(string Title, string Type, string Status, ResearchSnapshot? ConfirmedDetails, string? SupplierReference, DateTimeOffset? ConfirmedAt, string? TicketingStatus);
public sealed record CustomerBookingDto(Guid Id, Guid RequestId, string Reference, string Status, TripSnapshot Trip, CustomerBookingReservation[] Reservations);
public sealed record AccountMailSnapshot(string Email, string FirstName, string Language);
