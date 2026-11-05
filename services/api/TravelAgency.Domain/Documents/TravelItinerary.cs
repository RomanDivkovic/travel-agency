using System.Globalization;
using System.Text.Json;
using TravelAgency.Domain.Research;
namespace TravelAgency.Domain.Documents;

public enum ItineraryStatus { Draft, Published, Superseded, Archived }
public sealed class ItinerarySeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public int? PublishedVersion { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public List<TravelItinerary> Versions { get; set; } = [];
    public void Publish(TravelItinerary v, DateTimeOffset now)
    {
        if (v.SeriesId != Id || v.Version != CurrentVersion || v.Status != ItineraryStatus.Draft || v.Entries().Length == 0 || !ResearchValidation.Text(v.Title, 150)) throw new DocumentRuleException("A complete current itinerary draft is required.");
        foreach (var old in Versions.Where(x => x.Status == ItineraryStatus.Published)) old.Status = ItineraryStatus.Superseded;
        v.Status = ItineraryStatus.Published; v.PublishedAt = now; v.UpdatedAt = now; PublishedVersion = v.Version; VersionToken = Guid.NewGuid();
    }
}
public sealed class TravelItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeriesId { get; set; }
    public int Version { get; set; } = 1;
    public ItineraryStatus Status { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int BookingRevision { get; set; }
    public string TripJson { get; set; } = "{}";
    public string EntriesJson { get; set; } = "[]";
    public string CreatedById { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public ItineraryEntry[] Entries() => JsonSerializer.Deserialize<ItineraryEntry[]>(EntriesJson)!;
    public TripSnapshot Trip() => JsonSerializer.Deserialize<TripSnapshot>(TripJson)!;
}
public sealed record ItineraryEntry(Guid Id, string Kind, string Event, string Title, string Description, DateOnly? Date, DateTimeOffset? At, string? TimeZone, string Location, string SupplierReference, Guid? ReservationId, int? ArrangementVersion, string? SourceKey, int SortOrder)
{
    public bool Confirmed => ReservationId.HasValue;
    public static ItineraryEntry Custom(CustomEntryInput input)
    {
        if (!ResearchValidation.Text(input.Title, 150) || !ResearchValidation.Text(input.Description, 3000, false) || !ResearchValidation.Text(input.Location, 300, false) || input.SortOrder is < 0 or > 10000) throw new DocumentRuleException("Invalid customer-visible entry.");
        DateTimeOffset? at = null; string? zone = null; DateOnly? date = input.Date;
        if (!string.IsNullOrEmpty(input.LocalTime))
        {
            if (!DateTime.TryParseExact(input.LocalTime, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) throw new DocumentRuleException("Invalid local time.");
            zone = input.TimeZone; at = TravelTimes.Resolve(local, zone, input.UtcOffset); date = DateOnly.FromDateTime(local);
        }
        else if (date is null) throw new DocumentRuleException("A travel date is required.");
        return new(input.Id ?? Guid.NewGuid(), "Custom", "Information", input.Title.Trim(), input.Description.Trim(), date, at, zone, input.Location.Trim(), "", null, null, null, input.SortOrder);
    }
}
public sealed record CustomEntryInput(Guid? Id, string Title, string Description, DateOnly? Date, string? LocalTime, string? TimeZone, string? UtcOffset, string Location, int SortOrder);
public static class TravelTimes
{
    public static DateTimeOffset Resolve(DateTime local, string? zoneId, string? explicitOffset)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId ?? ""); local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local)) throw new DocumentRuleException("Local time falls in a daylight-saving gap.");
            var offsets = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local) : [zone.GetUtcOffset(local)];
            TimeSpan offset;
            if (!string.IsNullOrEmpty(explicitOffset)) { if (!TimeSpan.TryParseExact(explicitOffset.TrimStart('+'), @"hh\:mm", CultureInfo.InvariantCulture, out offset)) { if (!TimeSpan.TryParse(explicitOffset, CultureInfo.InvariantCulture, out offset)) throw new DocumentRuleException("Invalid UTC offset."); } if (!offsets.Contains(offset)) throw new DocumentRuleException("UTC offset does not match the timezone."); }
            else { if (offsets.Length != 1) throw new DocumentRuleException("An explicit UTC offset is required for an ambiguous local time."); offset = offsets[0]; }
            return new(local, offset);
        }
        catch (TimeZoneNotFoundException) { throw new DocumentRuleException("Unknown timezone."); }
        catch (InvalidTimeZoneException) { throw new DocumentRuleException("Invalid timezone."); }
        catch (ArgumentException) { throw new DocumentRuleException("Invalid local time or timezone."); }
    }
    public static DateTimeOffset Local(DateTimeOffset at, string zone) => TimeZoneInfo.ConvertTime(at, TimeZoneInfo.FindSystemTimeZoneById(zone));
}
