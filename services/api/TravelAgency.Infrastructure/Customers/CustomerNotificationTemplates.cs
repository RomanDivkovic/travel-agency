using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Customers;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Customers;

public static class CustomerNotificationTemplates
{
    public static async Task<EmailMessage?> CreateAsync(NotificationRecord n, TravelAgencyDbContext db, CustomerTokenCodec codec, EmailSettings settings, DateTimeOffset now, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == n.CustomerId && u.IsCustomer && !u.CustomerDisabled, ct); if (user is null) return null;
        var s = JsonSerializer.Deserialize<AccountMailSnapshot>(n.PayloadJson!)!; var sv = s.Language == "sv"; string? link = null; DateTimeOffset? expires = null;
        if (n.CustomerActionId is { } id)
        {
            var a = await db.CustomerActions.AsNoTracking().SingleAsync(a => a.Id == id, ct);
            if (!a.Available(now) || a.SecurityStamp != user.SecurityStamp || n.ProtectedLink is null) return null;
            link = codec.Read(n.ProtectedLink); expires = a.ExpiresAt;
        }
        var label = n.Kind switch { "customer-verify" => sv ? "Verifiera din e-postadress" : "Verify your email address", "customer-reset" => sv ? "Återställ ditt lösenord" : "Reset your password", "customer-claim" => sv ? "Bekräfta koppling av reseförfrågan" : "Confirm travel request association", "customer-linked" => sv ? "Reseförfrågan kopplad till ditt konto" : "Travel request linked to your account", _ => sv ? "Säkerhetsändring på ditt konto" : "Account security change" };
        var body = (sv ? $"Hej {s.FirstName},\n\n{label}.\n" : $"Hello {s.FirstName},\n\n{label}.\n");
        if (link is not null) body += (sv ? $"Länken gäller en gång till {expires:yyyy-MM-dd HH:mm} UTC. Öppna länken och bekräfta själv åtgärden.\n" : $"This single-use link expires at {expires:yyyy-MM-dd HH:mm} UTC. Open the link and explicitly confirm the action.\n") + link + "\n";
        else body += sv ? "Om du inte känner igen ändringen, återställ lösenordet och kontakta reseagenten.\n" : "If you do not recognize this change, reset your password and contact the travel agent.\n";
        body += (sv ? "\nVänliga hälsningar,\n" : "\nKind regards,\n") + settings.BusinessName;
        var html = WebUtility.HtmlEncode(body).Replace("\n", "<br>"); if (link is not null) html = html.Replace(WebUtility.HtmlEncode(link), $"<a href=\"{WebUtility.HtmlEncode(link)}\" rel=\"noreferrer\">{WebUtility.HtmlEncode(label)}</a>");
        return new(n.Id, s.Email, label, body, "<div>" + html + "</div>");
    }
}
