using System.Globalization;
using System.Net;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Requests;

namespace TravelAgency.Infrastructure.Email;

public static class NotificationTemplates
{
    public static EmailMessage Create(Guid id, string kind, TravelInquiry r, EmailSettings settings)
    {
        var swedish = r.Language == "sv";
        var service = (r.Service, swedish) switch
        {
            (TravelService.Flights, true) => "Flyg",
            (TravelService.Accommodation, true) => "Boende",
            (TravelService.FlightAccommodation, true) => "Flyg och boende",
            (TravelService.Complete, true) => "Komplett reseplanering",
            (TravelService.Flights, false) => "Flights",
            (TravelService.Accommodation, false) => "Accommodation",
            (TravelService.FlightAccommodation, false) => "Flights and accommodation",
            _ => "Complete holiday planning"
        };
        string subject; string text; string to;
        if (kind == "customer")
        {
            to = r.Contact.Email;
            var name = r.Contact.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            subject = swedish ? $"Vi har tagit emot din reseförfrågan – {r.ReferenceNumber}" : $"We’ve received your travel inquiry – {r.ReferenceNumber}";
            text = swedish
                ? $"Hej {name},\n\nTack för din reseförfrågan!\n\nVi har tagit emot dina uppgifter och kommer att gå igenom dina önskemål.\n\nDitt referensnummer: {r.ReferenceNumber}\nVald tjänst: {service}\n\nDu får återkoppling med information om nästa steg och ett eventuellt erbjudande för resehjälpen.\n\nObservera att din förfrågan inte innebär att någon resa har bokats.\n\nVänliga hälsningar,\n{settings.BusinessName}"
                : $"Hello {name},\n\nThank you for your travel inquiry!\n\nWe’ve received your information and will review your travel preferences.\n\nYour reference number: {r.ReferenceNumber}\nSelected service: {service}\n\nWe’ll get back to you with information about the next steps and a personalized service quotation.\n\nPlease note that submitting an inquiry does not create a travel booking.\n\nKind regards,\n{settings.BusinessName}";
        }
        else
        {
            to = settings.AgentRecipient;
            subject = $"New travel inquiry – {r.ReferenceNumber}";
            var plan = r.Plan;
            var dates = plan.DateMode == "fixed" ? $"{plan.Departure:yyyy-MM-dd} – {plan.Return:yyyy-MM-dd}" : $"{plan.Earliest:yyyy-MM-dd} – {plan.Latest:yyyy-MM-dd}, {plan.DurationDays} days";
            text = $"Reference: {r.ReferenceNumber}\nService: {service}\nDestination: {plan.Destination ?? "Flexible destination"}\nDates: {dates}\nTravellers: {plan.Adults + plan.ChildrenAges.Length}\nBudget: {plan.BudgetAmount.ToString("0.00", CultureInfo.InvariantCulture)} {plan.Currency} ({plan.BudgetScope})\nCustomer: {r.Contact.Name}\nEmail: {r.Contact.Email}\nPreferred contact: {r.Contact.PreferredMethod}\nPhone: {(r.Contact.PreferredMethod == "phone" ? r.Contact.Phone : "Not requested")}\nSubmitted (UTC): {r.CreatedAt:O}\n\nThis inquiry is not a booking.";
        }
        var language = kind == "customer" ? r.Language : "en";
        var html = $"<!doctype html><html lang=\"{language}\"><head><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta charset=\"utf-8\"></head><body style=\"margin:0;background:#faf8f5;color:#24313b;font:16px/1.6 Arial,sans-serif\"><div style=\"max-width:600px;margin:auto;padding:24px\"><h1 style=\"font-size:24px;color:#173b57\">{WebUtility.HtmlEncode(settings.BusinessName)}</h1><p>{WebUtility.HtmlEncode(text).Replace("\n", "<br>", StringComparison.Ordinal)}</p></div></body></html>";
        return new EmailMessage(id, to, subject, text, html);
    }
}
