using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
namespace TravelAgency.Infrastructure.Documents;

public static class ItineraryGeneration
{
    public static ItineraryEntry[] Generate(TravelBooking booking)
    {
        var entries = new List<ItineraryEntry>();
        foreach (var reservation in booking.Reservations.Where(r => r.Status == ReservationStatus.Confirmed))
        {
            var s = reservation.Current();
            void Add(string eventName, string key, DateOnly? date, DateTimeOffset? at, string? zone, string location, string title, string description) => entries.Add(new(Guid.NewGuid(), s.Kind == "Extra" ? s.Extra!.Category : s.Kind, eventName, title, description, date, at, zone, location, reservation.SupplierReference, reservation.Id, reservation.ArrangementVersion, reservation.Id + ":" + key, entries.Count));
            if (s.Flight is { } flight)
            {
                for (var leg = 0; leg < flight.Legs.Length; leg++)
                {
                    var f = flight.Legs[leg]; var title = f.Airline + " " + f.FlightNumber + " · " + f.DepartureAirport + " → " + f.ArrivalAirport;
                    var description = string.Join("\n", new[] { s.Description, flight.CabinClass, flight.BaggageDetails, s.Conditions }.Where(x => x.Length > 0));
                    Add("Departure", $"flight:{leg}:departure", DateOnly.FromDateTime(TravelTimes.Local(f.DepartureAt, f.DepartureTimeZone).DateTime), f.DepartureAt, f.DepartureTimeZone, f.DepartureAirport, title, description);
                    Add("Arrival", $"flight:{leg}:arrival", DateOnly.FromDateTime(TravelTimes.Local(f.ArrivalAt, f.ArrivalTimeZone).DateTime), f.ArrivalAt, f.ArrivalTimeZone, f.ArrivalAirport, title, "");
                }
            }
            else if (s.Accommodation is { } stay)
            {
                var description = string.Join("\n", new[] { s.Description, stay.RoomType, stay.MealPlan, s.Conditions }.Where(x => x.Length > 0));
                Add("CheckIn", "stay:in", stay.CheckIn, null, null, stay.Address + ", " + stay.Destination, s.Title, description);
                Add("CheckOut", "stay:out", stay.CheckOut, null, null, stay.Address + ", " + stay.Destination, s.Title, "");
            }
            else if (s.Extra is { } extra) Add("Service", "extra", extra.Date, null, null, extra.Location, s.Title, string.Join("\n", s.Description, s.Conditions));
        }
        return entries.ToArray();
    }
    public static ItineraryEntry[] Ordered(IEnumerable<ItineraryEntry> entries) => entries.OrderBy(e => e.Date ?? DateOnly.MaxValue).ThenBy(e => e.At is null ? TimeOnly.MinValue : TimeOnly.FromDateTime(TravelTimes.Local(e.At.Value, e.TimeZone!).DateTime)).ThenBy(e => e.SortOrder).ThenBy(e => e.Id).ToArray();
}
