using System.Globalization;
using System.Net;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Quotations;

public static class QuotationNotificationTemplates
{
    public static EmailMessage? Create(NotificationRecord job, Quotation quote, string number, EmailSettings settings, QuotationTokenCodec codec, DateTimeOffset now)
    {
        var swedish = quote.Language == "sv"; var customer = !job.Kind.StartsWith("agent-", StringComparison.Ordinal);
        var reference = $"{number} (v{quote.Version})";
        var amount = (quote.TotalMinorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture) + " " + quote.Currency;
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var until = TimeZoneInfo.ConvertTime(quote.ValidUntil, zone).ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture);
        var greeting = swedish ? $"Hej {quote.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]}," : $"Hello {quote.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]},";
        string subject; string text; string? link = null;
        switch (job.Kind)
        {
            case "quote-issued":
                if (quote.Status != QuotationStatus.Sent || quote.ValidUntil <= now || job.ProtectedLink is null) return null;
                link = codec.ReadLink(job.ProtectedLink);
                subject = swedish ? $"Din serviceoffert – {reference}" : $"Your service quotation – {reference}";
                text = swedish
                    ? $"{greeting}\n\nHär är din personliga serviceoffert: {reference}.\n{quote.Description}\n\nTotal serviceavgift: {amount}\nGiltig till: {until} (Europe/Stockholm).\n\nLäs tjänstens omfattning, pris och villkor via den privata länken. Dela inte länken. Ett godkännande innebär inte att en resa bokas eller att researbetet börjar automatiskt."
                    : $"{greeting}\n\nYour personalized service quotation is ready: {reference}.\n{quote.Description}\n\nTotal service fee: {amount}\nValid until: {until} (Europe/Stockholm).\n\nReview the scope, price and terms through the private link. Do not share the link. Acceptance does not book travel or automatically start research.";
                break;
            case "quote-accepted":
                subject = swedish ? $"Du har godkänt offerten – {reference}" : $"You accepted the quotation – {reference}";
                text = swedish ? $"{greeting}\n\nVi har registrerat ditt godkännande av {reference}.\nTotal serviceavgift: {amount}\n\nNästa steg är betalning av den överenskomna serviceavgiften. Du får mer information när betalningsfunktionen är tillgänglig. Godkännandet registrerar ingen betalning eller resebokning och startar inte researbetet automatiskt."
                    : $"{greeting}\n\nWe have recorded your acceptance of {reference}.\nTotal service fee: {amount}\n\nThe next step is payment of the agreed service fee. You will receive more information when payment functionality is available. Acceptance records no payment or travel booking and does not automatically start research.";
                break;
            case "quote-declined":
                subject = swedish ? $"Du har tackat nej till offerten – {reference}" : $"You declined the quotation – {reference}";
                text = swedish ? $"{greeting}\n\nVi har registrerat ditt nej till {reference}. Tack för att du tog dig tid att gå igenom erbjudandet."
                    : $"{greeting}\n\nWe have recorded your decline of {reference}. Thank you for taking the time to review the offer.";
                break;
            default:
                if (job.Kind is not ("agent-accepted" or "agent-declined")) throw new InvalidOperationException("Unknown quotation notification.");
                var outcome = job.Kind == "agent-accepted" ? "accepted" : "declined";
                subject = $"Service quotation {outcome} – {reference}";
                text = $"The customer {outcome} {reference}.\nService fee: {amount}\nResponse (UTC): {(quote.AcceptedAt ?? quote.DeclinedAt):O}\n\nReview the enquiry in the administrator dashboard. This response records no payment or booking and does not activate travel research.";
                break;
        }
        var language = customer ? quote.Language : "en";
        var html = $"<!doctype html><html lang=\"{language}\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head><body style=\"margin:0;background:#faf8f5;color:#24313b;font:16px/1.6 Arial,sans-serif\"><div style=\"max-width:600px;margin:auto;padding:24px;overflow-wrap:anywhere\"><h1 style=\"font-size:24px\">{WebUtility.HtmlEncode(settings.BusinessName)}</h1><p>{WebUtility.HtmlEncode(text).Replace("\n", "<br>", StringComparison.Ordinal)}</p>";
        if (link is not null) html += $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{(swedish ? "Öppna offerten" : "Review quotation")}</a></p>";
        html += "</div></body></html>";
        return new(job.Id, customer ? quote.CustomerEmail : settings.AgentRecipient, subject, text + (link is null ? "" : "\n\n" + link), html);
    }
}
