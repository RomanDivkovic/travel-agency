using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Documents;

public sealed class ItineraryOperations(TravelAgencyDbContext db, DocumentAccess access, DocumentNotifications notices, TimeProvider clock)
{
    public async Task<ItineraryDto> GetAsync(Guid bookingId, string? owner, CancellationToken ct)
    {
        var b = await access.BookingAsync(bookingId, owner, false, ct); var s = await db.Itineraries.AsNoTracking().Include(s => s.Versions).SingleOrDefaultAsync(s => s.BookingId == bookingId, ct);
        if (s is null || owner is not null && s.PublishedVersion is null) throw new DocumentOperationException(404);
        var versions = s.Versions.Where(v => owner is null || v.Status == ItineraryStatus.Published && v.Version == s.PublishedVersion).OrderByDescending(v => v.Version).ToArray(); if (versions.Length == 0) throw new DocumentOperationException(404);
        var active = versions.FirstOrDefault(v => v.Version == (owner is null ? s.CurrentVersion : s.PublishedVersion)) ?? versions[0];
        return new(bookingId, b.Reference, owner is null ? s.VersionToken : null, owner is null ? s.CurrentVersion : s.PublishedVersion!.Value, s.PublishedVersion, b.Revision != active.BookingRevision, versions.Select(Dto).ToArray());
    }
    public static ItineraryVersionDto Dto(TravelItinerary v) => new(v.Id, v.Version, v.Status.ToString(), v.Title, v.Description, v.Trip(), ItineraryGeneration.Ordered(v.Entries()).Select(e => new ItineraryEntryDto(e.Id, e.Kind, e.Event, e.Title, e.Description, e.Date, e.At, e.TimeZone, e.Location, e.SupplierReference, e.Confirmed, e.SortOrder)).ToArray(), v.PublishedAt, v.CreatedAt);
    public async Task ChangeAsync(Guid bookingId, string operation, Guid? version, ItinerarySave? input, string actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var b = await access.BookingAsync(bookingId, null, true, ct); var now = clock.GetUtcNow();
        var s = await db.Itineraries.Include(s => s.Versions).SingleOrDefaultAsync(s => s.BookingId == bookingId, ct);
        if (operation == "create")
        {
            if (s is not null) throw new DocumentOperationException(409); s = new() { BookingId = bookingId }; var p = await db.TravelProposals.SingleAsync(p => p.Id == b.ProposalId, ct);
            s.Versions.Add(new() { SeriesId = s.Id, Title = b.Reference, TripJson = JsonSerializer.Serialize(p.Trip()), BookingRevision = b.Revision, CreatedById = actor, CreatedAt = now, UpdatedAt = now }); db.Itineraries.Add(s);
        }
        else
        {
            if (s is null) throw new DocumentOperationException(404); DocumentAccess.Version(version ?? Guid.Empty, s.VersionToken); var v = s.Versions.Single(v => v.Version == s.CurrentVersion);
            if (operation is "revise")
            {
                if (v.Status == ItineraryStatus.Draft || s.CurrentVersion >= 100) throw new DocumentOperationException(409);
                var next = new TravelItinerary { SeriesId = s.Id, Version = ++s.CurrentVersion, Title = v.Title, Description = v.Description, TripJson = v.TripJson, EntriesJson = v.EntriesJson, BookingRevision = v.BookingRevision, CreatedById = actor, CreatedAt = now, UpdatedAt = now }; s.Versions.Add(next); db.ItineraryVersions.Add(next);
            }
            else if (operation == "archive")
            {
                if (v.Status == ItineraryStatus.Archived) throw new DocumentOperationException(409); v.Status = ItineraryStatus.Archived; v.UpdatedAt = now; foreach (var published in s.Versions.Where(x => x.Status == ItineraryStatus.Published)) { published.Status = ItineraryStatus.Archived; published.UpdatedAt = now; }
                s.PublishedVersion = null;
            }
            else
            {
                if (v.Status != ItineraryStatus.Draft) throw new DocumentOperationException(409);
                if (operation == "generate")
                {
                    var generated = ItineraryGeneration.Generate(b); if (generated.Length == 0) throw new DocumentOperationException(409);
                    v.EntriesJson = JsonSerializer.Serialize(ItineraryGeneration.Ordered(generated.Concat(v.Entries().Where(e => !e.Confirmed)))); v.BookingRevision = b.Revision;
                    var dates = generated.Where(e => e.Date.HasValue).Select(e => e.Date!.Value).ToArray(); if (dates.Length > 0) v.TripJson = JsonSerializer.Serialize(v.Trip() with { Departure = dates.Min(), Return = dates.Max(), Earliest = null, Latest = null, DateMode = "fixed" });
                }
                else if (operation == "save")
                {
                    if (input is null || !ResearchValidation.Text(input.Title, 150) || !ResearchValidation.Text(input.Description, 3000, false) || input.CustomEntries is null || input.CustomEntries.Length > 100) throw new DocumentOperationException(400);
                    var custom = input.CustomEntries.Select(ItineraryEntry.Custom).ToArray(); if (custom.Select(e => e.Id).Distinct().Count() != custom.Length) throw new DocumentOperationException(400);
                    var generated = v.Entries().Where(e => e.Confirmed).ToArray(); if (custom.Any(c => generated.Any(g => g.Id == c.Id))) throw new DocumentOperationException(400);
                    v.Title = input.Title.Trim(); v.Description = input.Description.Trim(); v.EntriesJson = JsonSerializer.Serialize(ItineraryGeneration.Ordered(generated.Concat(custom)));
                }
                else if (operation == "publish")
                {
                    if (v.BookingRevision != b.Revision || !v.Entries().Any(e => e.Confirmed)) throw new DocumentOperationException(409);
                    // Confirmed entries can only originate from this locked booking; editing accepts custom entries only.
                    notices.RequireOrigin(); s.Publish(v, now); await notices.QueueAsync(b, "itinerary", v.Id, v.Version > 1, ct);
                }
                else throw new DocumentOperationException(400);
                v.UpdatedAt = now;
            }
            s.VersionToken = Guid.NewGuid();
        }
        access.Audit(b, actor, "Itinerary" + operation, new { s.Id, s.CurrentVersion }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
