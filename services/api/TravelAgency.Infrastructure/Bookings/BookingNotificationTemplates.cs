using System.Globalization;
using System.Net;
using System.Text.Json;
using TravelAgency.Application.Bookings;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Bookings;

public sealed record BookingFailureSnapshot(string Reference, string Language, string Supplier, string Reason);
public static class BookingNotificationTemplates
{
    public static EmailMessage Create(NotificationRecord n, EmailSettings settings)
    {
        if (n.Kind.StartsWith("booking-failed-", StringComparison.Ordinal))
        {
            var f = JsonSerializer.Deserialize<BookingFailureSnapshot>(n.PayloadJson!)!; var sv = f.Language == "sv";
            var subject = (sv ? "Leverantörsbokning misslyckades – " : "Supplier reservation failed – ") + f.Reference;
            var body = $"{f.Reference}\n{f.Supplier}\n{f.Reason}\n" + (sv ? "Granska bokningen. Övriga bekräftade reservationer är oförändrade." : "Review the booking. Other confirmed reservations remain intact.");
            return Message(n.Id, settings.AgentRecipient, subject, body);
        }
        var s = JsonSerializer.Deserialize<BookingNotificationSnapshot>(n.PayloadJson!)!; var swedish = s.Language == "sv";
        var text = swedish ? $"Hej {s.CustomerName},\n\nDin resebokning har nu bekräftats hos de aktuella leverantörerna.\nBokningsreferens: {s.Reference}\nResmål: {s.Destination}\nBekräftat per {s.ConfirmedAsOf:yyyy-MM-dd HH:mm} UTC\n" : $"Hello {s.CustomerName},\n\nYour travel booking has now been confirmed by the relevant suppliers.\nBooking reference: {s.Reference}\nDestination: {s.Destination}\nConfirmed as of {s.ConfirmedAsOf:yyyy-MM-dd HH:mm} UTC\n";
        foreach (var r in s.Reservations)
        {
            var d = r.Details;
            text += $"\n{d.Title}\n{d.SupplierName}\n{(swedish ? "Leverantörsreferens" : "Supplier reference")}: {r.SupplierReference}\n{r.ConfirmationEvidence}\n{r.ConfirmedAt:yyyy-MM-dd HH:mm} UTC\n{d.Currency} {(d.TotalMinorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture)}\n";
            if (d.Flight is { } flight) foreach (var leg in flight.Legs) text += $"{leg.Airline} {leg.FlightNumber}: {leg.DepartureAirport} {leg.DepartureAt:yyyy-MM-dd HH:mm zzz} → {leg.ArrivalAirport} {leg.ArrivalAt:yyyy-MM-dd HH:mm zzz}\n";
            if (d.Flight is { } f) text += $"{f.CabinClass}; {f.BaggageDetails}\n" + (swedish ? $"Biljett utfärdad: {(r.TicketingStatus == "Issued" ? "Ja" : "Nej")}\n" : $"Ticket issued: {(r.TicketingStatus == "Issued" ? "Yes" : "No")}\n");
            if (d.Accommodation is { } a) text += $"{a.Destination}, {a.Address}\n{a.CheckIn:yyyy-MM-dd} – {a.CheckOut:yyyy-MM-dd}\n{a.RoomType}; {a.Guests}; {a.MealPlan}\n";
            if (d.Extra is { } e) text += $"{e.Location}; {e.Date:yyyy-MM-dd}\n";
            text += $"{d.Conditions}\n{r.DocumentReference}\n";
        }
        text += swedish ? $"\nSpara leverantörernas bokningsreferenser. Mer reseinformation och eventuella dokument lämnas när de är tillgängliga. Leverantörskostnader är separata från den redan betalda serviceavgiften.\n\nVänliga hälsningar,\n{settings.BusinessName}" : $"\nPlease keep each supplier's booking reference. Further travel information and any documents will be provided when available. Supplier costs are separate from the service fee already paid.\n\nKind regards,\n{settings.BusinessName}";
        return Message(n.Id, s.Email, (swedish ? "Din resebokning är bekräftad – " : "Your travel booking is confirmed – ") + s.Reference, text);
    }
    private static EmailMessage Message(Guid id, string to, string subject, string text) => new(id, to, subject, text, "<div>" + WebUtility.HtmlEncode(text).Replace("\n", "<br>") + "</div>");
}
