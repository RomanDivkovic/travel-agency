using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;
namespace TravelAgency.Infrastructure.Documents;

public sealed record DocumentMail(Guid VersionId, string Resource, string Email, string Name, string Language, string Destination, string Reference, string PortalLink, bool Revised);
public sealed class DocumentNotifications(TravelAgencyDbContext db, CustomerSettings customer, TimeProvider clock)
{
    public void RequireOrigin() { if (customer.PublicOrigin.Length == 0) throw new DocumentOperationException(412); }
    public async Task QueueAsync(TravelBooking booking, string resource, Guid version, bool revised, CancellationToken ct)
    {
        var request = await db.TravelRequests.SingleAsync(r => r.Id == booking.RequestId, ct);
        var owner = await (from o in db.CustomerRequests join u in db.Users on o.CustomerId equals u.Id where o.RequestId == booking.RequestId && u.EmailConfirmed && u.IsCustomer && !u.CustomerDisabled select u).SingleOrDefaultAsync(ct);
        var payload = new DocumentMail(version, resource, owner?.Email ?? request.Contact.Email, owner?.FirstName ?? request.Contact.Name, owner?.PreferredLanguage ?? request.Language, request.Plan.Destination ?? "", booking.Reference, customer.PublicOrigin + "/account/bookings/" + booking.Id + (resource == "document" ? "/documents" : "/itinerary"), revised);
        db.RequestActivities.Add(new Domain.Admin.RequestActivity { BookingId = booking.Id, RequestId = booking.RequestId, Type = resource == "document" ? "DocumentPublished" : "ItineraryPublished", ActorId = null, CreatedAt = clock.GetUtcNow() });
        db.NotificationOutbox.Add(new() { BookingId = booking.Id, RequestId = booking.RequestId, Kind = resource + "-published-" + version.ToString("N"), PayloadJson = JsonSerializer.Serialize(payload), NextAttemptAt = clock.GetUtcNow() });
    }
    public static async Task<EmailMessage?> MessageAsync(NotificationRecord job, TravelAgencyDbContext db, EmailSettings settings, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<DocumentMail>(job.PayloadJson!)!;
        var visible = p.Resource == "document" ? await db.DocumentVersions.AnyAsync(v => v.Id == p.VersionId && v.Status == DocumentStatus.Published && v.Scan == DocumentScan.Clean && db.TravelDocuments.Any(d => d.Id == v.DocumentId && d.PublishedVersion == v.Version), ct)
            : await db.ItineraryVersions.AnyAsync(v => v.Id == p.VersionId && v.Status == ItineraryStatus.Published && db.Itineraries.Any(s => s.Id == v.SeriesId && s.PublishedVersion == v.Version), ct);
        if (!visible) return null; var sv = p.Language == "sv";
        var subject = p.Resource == "document" ? sv ? p.Revised ? "Uppdaterade resedokument" : "Dina resedokument är tillgängliga" : p.Revised ? "Updated travel documents" : "Your travel documents are available" : sv ? p.Revised ? "Din resplan har uppdaterats" : "Din resplan är tillgänglig" : p.Revised ? "Your itinerary has been updated" : "Your travel itinerary is available";
        var text = (sv ? $"Hej {p.Name},\n\n{subject} för resan till {p.Destination}.\nResa: {p.Reference}\nLogga in på kundportalen för att se den publicerade versionen. Skapa och verifiera ett konto samt koppla din förfrågan om du inte redan gjort det.\n" : $"Hello {p.Name},\n\n{subject} for your trip to {p.Destination}.\nTrip: {p.Reference}\nSign in to view the published version. Create and verify an account and link your request if you have not done so yet.\n") + p.PortalLink + (sv ? "\n\nVänliga hälsningar,\n" : "\n\nKind regards,\n") + settings.BusinessName;
        var html = WebUtility.HtmlEncode(text).Replace("\n", "<br>").Replace(WebUtility.HtmlEncode(p.PortalLink), $"<a href=\"{WebUtility.HtmlEncode(p.PortalLink)}\" rel=\"noreferrer\">{WebUtility.HtmlEncode(subject)}</a>"); return new(job.Id, p.Email, subject, text, "<div>" + html + "</div>");
    }
}
