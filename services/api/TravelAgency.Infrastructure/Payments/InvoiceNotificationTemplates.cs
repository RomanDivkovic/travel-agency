using System.Globalization;
using System.Net;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Payments;

public static class InvoiceNotificationTemplates
{
    public static EmailMessage? Create(NotificationRecord job, Invoice i, EmailSettings settings, InvoiceTokenCodec codec)
    {
        var sv = i.Language == "sv"; var agent = job.Kind == "payment-agent";
        if (job.Kind == "invoice-issued" && (i.Status == InvoiceStatus.Cancelled || job.ProtectedLink is null)) return null;
        var title = job.Kind switch
        {
            "invoice-issued" => sv ? "Din faktura för serviceavgiften" : "Your service fee invoice",
            "payment-confirmed" => sv ? "Din betalning är registrerad" : "Your payment is confirmed",
            "payment-failed" => sv ? "Betalningen kunde inte genomföras" : "Payment could not be completed",
            "payment-agent" => "Service fee payment verified",
            _ => throw new InvalidOperationException("Unknown invoice notification.")
        };
        var status = job.Kind is "payment-confirmed" or "payment-agent" ? (sv && !agent ? "Serviceavgiften är bekräftad. Förfrågan kan gå vidare till planering." : "Service fee verified. The request is eligible for planning.")
            : job.Kind == "payment-failed" ? (sv ? "Ingen betalning har bekräftats för detta försök. Öppna din faktura för att kontrollera aktuell status och försöka igen." : "No payment was confirmed for this attempt. Open your invoice to check its current status and retry.")
            : (sv ? "Öppna din privata fakturalänk för att granska och betala serviceavgiften." : "Open your private invoice link to review and pay the service fee.");
        var disclaimer = sv && !agent ? "Betalningen gäller endast serviceavgiften. Flyg, boende och andra resetjänster är inte bokade." : "This payment covers the service fee only. Flights, accommodation and other travel services have not been booked.";
        var simulation = i.IsSimulated ? "TEST / SIMULATED — no money collected. This is not a real payment or invoice." : "";
        var amount = (i.TotalMinorUnits / 100m).ToString("F2", CultureInfo.InvariantCulture) + " " + i.Currency;
        var link = job.ProtectedLink is { } envelope ? codec.Read(envelope) : null;
        var text = $"{simulation}\n{(agent ? "" : (sv ? "Hej " : "Hello ") + i.CustomerName)}\n{title}\n{i.Number}\n{i.Description}\n{amount}\n{(sv ? "Förfaller" : "Due")}: {i.DueAt:yyyy-MM-dd} UTC\n{status}\n{disclaimer}";
        var html = "<p>" + WebUtility.HtmlEncode(text).Replace("\n", "<br>") + "</p>";
        if (link is not null) { text += "\n" + link; html += $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{(sv ? "Öppna fakturan" : "Open invoice")}</a></p>"; }
        return new(job.Id, agent ? settings.AgentRecipient : i.CustomerEmail, (i.IsSimulated ? "[TEST] " : "") + title + " — " + i.Number, text, html);
    }
}
