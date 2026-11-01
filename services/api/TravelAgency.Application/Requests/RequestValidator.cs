using System.Net.Mail;
using System.Text.RegularExpressions;

namespace TravelAgency.Application.Requests;

public static partial class RequestValidator
{
    public static Dictionary<string, string[]> Validate(SubmitTravelRequest r, DateOnly today)
    {
        var e = new Dictionary<string, string[]>();
        void Error(string field, string code) => e[field] = [code];
        void Text(string field, string? value, int max, bool required = true)
        {
            if (required && string.IsNullOrWhiteSpace(value)) Error(field, "required");
            else if (value?.Length > max) Error(field, "length");
        }
        void Date(string field, DateOnly? value)
        {
            if (value is null || value < today || value > today.AddYears(10)) Error(field, "date");
        }
        var flight = r.Service is "flights" or "flight-accommodation" or "complete";
        var accommodation = r.Service is "accommodation" or "flight-accommodation" or "complete";
        if (r.SchemaVersion != 1) Error("request", "choice");
        if (r.Service is not ("flights" or "accommodation" or "flight-accommodation" or "complete")) Error("service", "choice");
        if (r.Language is not ("sv" or "en")) Error("language", "choice");
        if (!string.IsNullOrEmpty(r.Website)) Error("request", "choice");
        if (r.Destination is not { } destination) Error("place", "required");
        else
        {
            if (flight) Text("origin", destination.Origin, 150);
            else if (destination.Origin is not null) Error("origin", "choice");
            Text("place", destination.Place, 150, destination.Flexible != true);
            if (destination.Flexible is null) Error("place", "choice");
            if (destination.AdditionalStops is { } stops && (r.Service != "complete" || stops.Length > 20
                || stops.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 150) || stops.Sum(s => s?.Length ?? 0) > 500)) Error("stops", "length");
        }
        if (r.Dates is not { } dates) Error("departure", "required");
        else if (dates.Mode == "fixed")
        {
            Date("departure", dates.Departure);
            if (accommodation || dates.Return.HasValue) Date("return", dates.Return);
            if (dates.Return.HasValue && dates.Departure.HasValue && (dates.Return < dates.Departure || (accommodation && dates.Return == dates.Departure))) Error("return", "dateOrder");
            if (dates.Earliest.HasValue || dates.Latest.HasValue || dates.DurationDays.HasValue) Error("dateMode", "choice");
        }
        else if (dates.Mode == "flexible")
        {
            Date("earliest", dates.Earliest); Date("latest", dates.Latest);
            if (dates.Earliest.HasValue && dates.Latest.HasValue && (dates.Latest < dates.Earliest || (accommodation && dates.Latest == dates.Earliest))) Error("latest", "dateOrder");
            if (dates.DurationDays is null or < 1 or > 90 || (dates.Earliest.HasValue && dates.Latest.HasValue
                && dates.DurationDays > dates.Latest.Value.DayNumber - dates.Earliest.Value.DayNumber + (accommodation ? 0 : 1))) Error("duration", "duration");
            if (dates.Departure.HasValue || dates.Return.HasValue) Error("dateMode", "choice");
        }
        else Error("dateMode", "choice");
        if (r.Travelers is not { } travelers) Error("adults", "required");
        else
        {
            if (travelers.Adults is null or < 1 or > 20) Error("adults", "adults");
            if (travelers.ChildrenAges is null || travelers.ChildrenAges.Length > 10 || travelers.Adults + travelers.ChildrenAges.Length > 20) Error("children", "children");
            if (travelers.ChildrenAges is { } ages) for (var i = 0; i < ages.Length; i++) if (ages[i] is < 0 or > 17) Error($"age-{i}", "age");
            if (accommodation && (travelers.Rooms is null or < 1 or > 20 || travelers.Rooms > travelers.Adults + (travelers.ChildrenAges?.Length ?? 0))) Error("rooms", "rooms");
            if (!accommodation && travelers.Rooms.HasValue) Error("rooms", "choice");
        }
        if (r.Budget is not { } budget) Error("amount", "required");
        else
        {
            if (budget.Amount is null or <= 0 or > 100000000 || decimal.Round(budget.Amount.Value, 2) != budget.Amount) Error("amount", "budget");
            if (budget.Currency is not ("SEK" or "EUR" or "USD")) Error("currency", "choice");
            if (budget.Scope is not ("total" or "per-person")) Error("scope", "choice");
        }
        var preferences = r.Preferences;
        if (flight && (preferences?.Flight is not { Priority: "balanced" or "price" or "direct", Baggage: "cabin" or "checked" or "none", FlexibleAirports: not null })) Error("priority", "choice");
        if (!flight && preferences?.Flight is not null) Error("priority", "choice");
        if (accommodation && preferences?.Accommodation is not { Type: "hotel" or "apartment" or "either", Breakfast: not null, Pool: not null, FamilyFriendly: not null }) Error("accommodationType", "choice");
        if (!accommodation && preferences?.Accommodation is not null) Error("accommodationType", "choice");
        if (r.Service == "complete" && (preferences?.Trip is not { Pace: "relaxed" or "balanced" or "active", Interests: not null } trip
            || trip.Interests.Length > 4 || trip.Interests.Distinct().Count() != trip.Interests.Length || trip.Interests.Any(s => s is not ("beach" or "culture" or "nature" or "food")))) Error("pace", "choice");
        if (r.Service != "complete" && preferences?.Trip is not null) Error("pace", "choice");
        if (r.Activities is null) Error("activities", "choice");
        Text("special", r.SpecialRequests, 2000, false);
        if (r.Contact is not { } contact) Error("name", "required");
        else
        {
            Text("name", contact.Name, 100); Text("email", contact.Email, 254);
            if (!string.IsNullOrWhiteSpace(contact.Email) && (!MailAddress.TryCreate(contact.Email.Trim(), out var address)
                || address.Address != contact.Email.Trim() || !EmailPattern().IsMatch(contact.Email.Trim()))) Error("email", "email");
            if (contact.PreferredMethod is not ("email" or "phone")) Error("contactMethod", "choice");
            if (contact.PreferredMethod == "phone" || !string.IsNullOrWhiteSpace(contact.Phone))
            {
                var phone = contact.Phone ?? ""; var count = phone.Count(char.IsAsciiDigit);
                if (phone.Length > 40 || count is < 6 or > 15 || !PhonePattern().IsMatch(phone)) Error("phone", "phone");
            }
            else Text("phone", contact.Phone, 40, false);
        }
        return e;
    }
    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();
    [GeneratedRegex(@"^[+\d\s().-]+$")]
    private static partial Regex PhonePattern();
}
