using System.Net;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Research;

public static class ProposalNotificationTemplates
{
    public static EmailMessage? Create(NotificationRecord job, TravelProposal p, string number, EmailSettings settings, ProposalTokenCodec codec, DateTimeOffset now)
    {
        var sv = p.Language == "sv"; var agent = job.Kind.StartsWith("proposal-agent-", StringComparison.Ordinal); var issued = job.Kind == "proposal-issued"; var approved = job.Kind.EndsWith("approved", StringComparison.Ordinal);
        if (issued && (p.Status != ProposalStatus.Sent || p.ValidUntil <= now || job.ProtectedLink is null)) return null;
        var title = issued ? (sv ? "Ditt personliga reseförslag" : "Your personalized travel proposal") : approved ? (sv ? "Reseförslag godkänt" : "Travel proposal approved") : (sv ? "Reseförslag avböjt" : "Travel proposal rejected");
        var next = issued ? (sv ? "Granska alternativen, välj ett alternativ och bekräfta ditt beslut via den privata länken." : "Review the alternatives, select one and explicitly confirm your decision through the private link.") : (sv ? "Ditt beslut har registrerats. Inga leverantörsbokningar har genomförts av systemet." : "Your decision was recorded. The system has made no supplier bookings.");
        if (agent) next = sv ? "Kundens beslut har registrerats. Granska den valda versionen och villkoren innan nästa steg. Inga leverantörsbokningar har genomförts." : "The customer's decision was recorded. Review the selected version and terms before the next step. No supplier bookings have been made.";
        var selected = p.Alternatives.SingleOrDefault(a => a.Id == p.SelectedAlternativeId)?.Title;
        var text = $"{(sv ? "Hej" : "Hello")} {(agent ? settings.BusinessName : p.CustomerName)}\n{title}\n{number} v{p.Version}\n{p.Title}\n{p.Trip().Destination}\n{(sv ? "Giltigt till" : "Valid until")}: {p.ValidUntil:yyyy-MM-dd HH:mm} UTC\n{selected}\n{next}\n{(sv ? "Priser och tillgänglighet kan förändras fram till dess att bokningen har genomförts." : "Prices and availability may change until the booking has been completed.")}";
        var html = "<p>" + WebUtility.HtmlEncode(text).Replace("\n", "<br>") + "</p>";
        if (issued) { var link = codec.Read(job.ProtectedLink!); text += "\n" + link; html += $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{(sv ? "Öppna reseförslaget" : "Open proposal")}</a></p>"; }
        return new(job.Id, agent ? settings.AgentRecipient : p.CustomerEmail, title + " — " + number, text, html);
    }
}
