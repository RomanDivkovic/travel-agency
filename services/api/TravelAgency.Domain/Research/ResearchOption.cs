using System.Text.Json;
using TravelAgency.Domain.Quotations;

namespace TravelAgency.Domain.Research;

public sealed class ResearchRuleException(string message) : Exception(message);
public enum ResearchKind { Flight, Accommodation, Extra }
public sealed record FlightLeg(string Airline, string FlightNumber, string DepartureAirport, string ArrivalAirport, DateTimeOffset DepartureAt, DateTimeOffset ArrivalAt, string DepartureTimeZone, string ArrivalTimeZone, int Journey = 1);
public sealed record FlightDetails(FlightLeg[] Legs, string CabinClass, string BaggageDetails);
public sealed record AccommodationDetails(string Destination, string Address, string AccommodationType, string RoomType, DateOnly CheckIn, DateOnly CheckOut, int Guests, string MealPlan, string Amenities);
public sealed record ExtraDetails(string Category, string Location, DateOnly? Date);
public sealed record ResearchOptionInput(string Kind, string Title, string Description, long PriceMinorUnits, string Currency, string PriceBasis, int Quantity,
    string SupplierName, string SupplierUrl, DateTimeOffset PriceCheckedAt, DateTimeOffset? AvailabilityCheckedAt, string Conditions, string Notes,
    FlightDetails? Flight, AccommodationDetails? Accommodation, ExtraDetails? Extra);
public sealed record ResearchSnapshot(string Kind, string Title, string Description, long PriceMinorUnits, string Currency, string PriceBasis, int Quantity,
    long TotalMinorUnits, string SupplierName, string SupplierUrl, DateTimeOffset PriceCheckedAt, DateTimeOffset? AvailabilityCheckedAt, string Conditions,
    FlightDetails? Flight, AccommodationDetails? Accommodation, ExtraDetails? Extra);
public sealed class ResearchWorkspace
{
    public Guid RequestId { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public string Notes { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
public sealed class ResearchOption
{
    private ResearchOption() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public Guid VersionToken { get; private set; } = Guid.NewGuid();
    public ResearchKind Kind { get; private set; }
    public string SnapshotJson { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public static ResearchOption Create(Guid requestId, ResearchOptionInput input, DateTimeOffset now)
    {
        var option = new ResearchOption { RequestId = requestId, CreatedAt = Quotation.Utc(now) }; option.Update(input, now); return option;
    }
    public void Update(ResearchOptionInput input, DateTimeOffset now)
    {
        if (input is null) throw new ResearchRuleException("Option fields required.");
        if (SnapshotJson.Length > 0 && input.Kind != Kind.ToString()) throw new ResearchRuleException("An option's category cannot change.");
        var snapshot = ResearchValidation.Validate(input, now); Kind = Enum.Parse<ResearchKind>(input.Kind);
        SnapshotJson = JsonSerializer.Serialize(snapshot); Notes = input.Notes.Trim(); UpdatedAt = Quotation.Utc(now); VersionToken = Guid.NewGuid();
    }
    public ResearchSnapshot Snapshot() => JsonSerializer.Deserialize<ResearchSnapshot>(SnapshotJson)!;
}
public static class ResearchValidation
{
    public static bool Text(string? value, int max, bool required = true) => value is not null && value.Length <= max && (!required || !string.IsNullOrWhiteSpace(value));
    public static bool SupplierUrl(string? value) => value is { Length: <= 2000 } && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https"
        && uri.UserInfo.Length == 0 && !uri.IsLoopback && Uri.CheckHostName(uri.Host) == UriHostNameType.Dns;
    public static bool Zoned(DateTimeOffset time, string? zone)
    {
        if (zone is not { Length: > 0 and <= 100 }) return false;
        try { return TimeZoneInfo.FindSystemTimeZoneById(zone).GetUtcOffset(time) == time.Offset; }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { return false; }
    }
    public static ResearchSnapshot Validate(ResearchOptionInput i, DateTimeOffset now)
    {
        if (i is null || !Enum.TryParse<ResearchKind>(i.Kind, out var kind) || !Enum.IsDefined(kind) || kind.ToString() != i.Kind
            || !Text(i.Title, 200) || !Text(i.Description, 2000, false) || !Text(i.SupplierName, 200) || !SupplierUrl(i.SupplierUrl) || !Text(i.Conditions, 3000)
            || !Text(i.Notes, 4000, false) || i.Currency is not ("SEK" or "EUR" or "USD") || i.PriceBasis is not ("Total" or "PerPerson" or "PerNight" or "PerRoomNight" or "PerGroup" or "PerItem")
            || i.PriceMinorUnits is < 0 or > Quotation.MaxTotal || i.Quantity is < 1 or > 1000 || (i.PriceBasis == "Total" && i.Quantity != 1)
            || i.PriceCheckedAt.Offset != TimeSpan.Zero || i.PriceCheckedAt > now || i.PriceCheckedAt < DateTimeOffset.UnixEpoch
            || (i.AvailabilityCheckedAt is { } availability && (availability.Offset != TimeSpan.Zero || availability > now || availability < DateTimeOffset.UnixEpoch))) throw new ResearchRuleException("Check option fields, price basis, source and timestamps.");
        var total = checked(i.PriceMinorUnits * i.Quantity); if (total > Quotation.MaxTotal) throw new ResearchRuleException("Estimated cost exceeds the limit.");
        if (kind == ResearchKind.Flight)
        {
            if (i.Flight is not { Legs.Length: >= 1 and <= 10 } f || i.Accommodation is not null || i.Extra is not null || !Text(f.CabinClass, 100) || !Text(f.BaggageDetails, 1000)) throw new ResearchRuleException("Complete flight details required.");
            for (var n = 0; n < f.Legs.Length; n++)
            {
                var l = f.Legs[n];
                if (l is null || l.Journey is < 1 or > 10 || (n > 0 && l.Journey < f.Legs[n - 1].Journey) || !Text(l.Airline, 100) || !Text(l.FlightNumber, 40, false) || !Text(l.DepartureAirport, 100) || !Text(l.ArrivalAirport, 100)
                    || !Zoned(l.DepartureAt, l.DepartureTimeZone) || !Zoned(l.ArrivalAt, l.ArrivalTimeZone) || l.ArrivalAt <= l.DepartureAt || (n > 0 && l.DepartureAt < f.Legs[n - 1].ArrivalAt)) throw new ResearchRuleException("Check flight order, offsets and time zones.");
            }
        }
        if (kind == ResearchKind.Accommodation && (i.Accommodation is not { } a || i.Flight is not null || i.Extra is not null || !Text(a.Destination, 150) || !Text(a.Address, 500)
            || !Text(a.AccommodationType, 100) || !Text(a.RoomType, 200) || !Text(a.MealPlan, 300) || !Text(a.Amenities, 1000, false) || a.Guests is < 1 or > 100 || a.CheckIn < new DateOnly(2000, 1, 1) || a.CheckOut <= a.CheckIn)) throw new ResearchRuleException("Check accommodation details.");
        if (kind == ResearchKind.Extra && (i.Extra is not { } e || i.Flight is not null || i.Accommodation is not null || e.Category is not ("Transfer" or "Transport" or "Activity" or "Event" or "Other") || !Text(e.Location, 200))) throw new ResearchRuleException("Check extra details.");
        return new(i.Kind, i.Title.Trim(), i.Description.Trim(), i.PriceMinorUnits, i.Currency, i.PriceBasis, i.Quantity, total, i.SupplierName.Trim(), i.SupplierUrl.Trim(),
            Quotation.Utc(i.PriceCheckedAt), i.AvailabilityCheckedAt, i.Conditions.Trim(), i.Flight, i.Accommodation, i.Extra);
    }
}
