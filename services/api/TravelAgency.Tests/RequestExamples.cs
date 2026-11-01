using TravelAgency.Application.Requests;
using TravelAgency.Domain.Requests;

namespace TravelAgency.Tests;

internal static class RequestExamples
{
    public static SubmitTravelRequest Input(string service = "complete", string language = "sv")
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);
        var flight = service != "accommodation"; var accommodation = service != "flights";
        return new SubmitTravelRequest(1, language, service,
            new DestinationInput(flight ? "Stockholm" : null, "Lisbon", false, service == "complete" ? ["Porto"] : null),
            new DatesInput("fixed", date, date.AddDays(7), null, null, null), new TravelersInput(2, [7], accommodation ? 2 : null),
            new BudgetInput(25000.50m, "SEK", "total"), new PreferencesInput(flight ? new("direct", "checked", false) : null,
                accommodation ? new("hotel", true, false, true) : null, service == "complete" ? new("balanced", ["culture"]) : null),
            true, "Near a station", new ContactInput("Anna Andersson", "anna@example.test", null, "email"));
    }
    public static TravelInquiry Inquiry(string language = "sv", string name = "Anna Andersson")
    {
        var r = Input(language: language);
        var plan = new TravelPlan("Stockholm", "Lisbon", false, [], "fixed", r.Dates!.Departure, r.Dates.Return, null, null, null,
            2, [7], 2, 25000, "SEK", "total", "{}", false, null);
        return TravelInquiry.Create("TRV-2026-000001", TravelService.Complete, language, new CustomerContact(name, "anna@example.test", null, "email"), plan, DateTimeOffset.UtcNow);
    }
}
