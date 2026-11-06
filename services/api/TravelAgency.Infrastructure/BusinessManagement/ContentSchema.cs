using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TravelAgency.Application.BusinessManagement;
namespace TravelAgency.Infrastructure.BusinessManagement;

public sealed class ContentOperationException(int status, string code = "fields") : Exception { public int Status { get; } = status; public string Code { get; } = code; }
public static partial class ContentSchema
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true };
    public static readonly string[] Services = ["FlightsOnly", "AccommodationOnly", "FlightsAndAccommodation", "CompleteTrip"];
    public static readonly string[] Policies = ["Privacy", "Cookies", "Terms", "Cancellation", "Refunds", "BookingResponsibilities"];
    public static readonly Dictionary<string, JsonElement> Baseline = Load();
    private static Dictionary<string, JsonElement> Load() { using var s = typeof(ContentSchema).Assembly.GetManifestResourceStream("TravelAgency.Infrastructure.BusinessManagement.BaselineContent.json")!; return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(s, Json)!; }
    public static bool IsKey(string key) => Baseline.ContainsKey(key) || key.StartsWith("policy:", StringComparison.Ordinal) && Policies.Contains(key[7..]) || key.StartsWith("faq:", StringComparison.Ordinal) && Guid.TryParseExact(key[4..], "D", out _);
    public static JsonElement Default(string key) => Baseline.TryGetValue(key, out var d) ? d.Clone() : key.StartsWith("faq:", StringComparison.Ordinal) ? Element(new FaqData(false, 10, new("", ""), new("", ""))) : Element(new PolicyData(new("", ""), new("", ""), DateOnly.FromDateTime(DateTime.UtcNow), false, "", null));
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Json);
    public static T Read<T>(JsonElement data) => data.Deserialize<T>(Json) ?? throw new ContentOperationException(400);
    private static void Require(bool valid, string code = "fields") { if (!valid) throw new ContentOperationException(400, code); }
    public static void Text(string value, int limit, bool required = false) => Require(value.Length <= limit && (!required || !string.IsNullOrWhiteSpace(value)) && !value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')));
    public static bool Url(string value) => Uri.TryCreate(value, UriKind.Absolute, out var u) && u.Scheme is "https" or "http" && string.IsNullOrEmpty(u.UserInfo);
    [GeneratedRegex("^[+0-9 ()-]{5,40}$")] private static partial Regex Phone();
    [GeneratedRegex("^\\d{6}-?\\d{4}$")] private static partial Regex Organization();
    private static bool Time(string x, out TimeOnly t) => TimeOnly.TryParseExact(x, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out t);
    public static string Validate(string key, JsonElement input, bool publish)
    {
        Require(IsKey(key) && input.ValueKind == JsonValueKind.Object && input.GetRawText().Length <= 150000);
        try
        {
            object data;
            if (key == "business")
            {
                var b = Read<BusinessData>(input); foreach (var s in new[] { b.BusinessName, b.LegalBusinessName, b.Address, b.City }) Text(s, 200); Text(b.BusinessName, 120, true);
                Text(b.ContactEmail, 254); Require(b.ContactEmail == "" || MailAddress.TryCreate(b.ContactEmail, out var mail) && mail.Address == b.ContactEmail && !b.ContactEmail.Contains('\r') && !b.ContactEmail.Contains('\n'), "email");
                Require(b.ContactPhone == "" || Phone().IsMatch(b.ContactPhone) && b.ContactPhone.Count(char.IsDigit) is >= 6 and <= 15, "phone"); Require(b.WebsiteUrl == "" || Url(b.WebsiteUrl), "url"); Text(b.WebsiteUrl, 500); Text(b.PostalCode, 20);
                Require(b.Country.Length == 2 && b.Country.All(char.IsAsciiLetterUpper)); Require(b.OrganizationNumber == "" || b.Country != "SE" && b.OrganizationNumber.Length <= 40 || Organization().IsMatch(b.OrganizationNumber), "organization");
                Require(b.DefaultLanguage is "sv" or "en" && Currency(b.DefaultCurrency)); Text(b.TimeZone, 100, true); _ = TimeZoneInfo.FindSystemTimeZoneById(b.TimeZone);
                Require(b.OpeningHours.Length <= 21 && b.Exceptions.Length <= 100 && b.Exceptions.Select(x => x.Date).Distinct().Count() == b.Exceptions.Length);
                foreach (var x in b.OpeningHours) Require(x.Day is >= 0 and <= 6 && Time(x.Opens, out var o) && Time(x.Closes, out var c) && c > o, "hours");
                foreach (var day in b.OpeningHours.GroupBy(x => x.Day)) { var ranges = day.OrderBy(x => x.Opens, StringComparer.Ordinal).ToArray(); for (var i = 1; i < ranges.Length; i++) Require(string.CompareOrdinal(ranges[i].Opens, ranges[i - 1].Closes) >= 0, "hours"); }
                foreach (var x in b.Exceptions) Require(x.Closed || Time(x.Opens, out var o) && Time(x.Closes, out var c) && c > o, "hours"); data = b;
            }
            else if (key == "preferences") data = Read<BusinessPreferences>(input);
            else if (key.StartsWith("service:", StringComparison.Ordinal))
            {
                var s = Read<ServiceData>(input); Require(s.Code == key[8..] && Services.Contains(s.Code) && s.DisplayOrder is >= 0 and <= 1000); foreach (var t in new[] { s.Sv, s.En }) { Text(t.Name, 150, true); Text(t.ShortDescription, 1000, true); Text(t.FullDescription, 5000, true); Text(t.Included, 3000, true); Text(t.Excluded, 3000, true); Text(t.Suitable, 1000, true); Text(t.Cta, 100, true); }
                var p = s.Pricing; Require(p.Type is "FixedFee" or "StartingFrom" or "CustomQuotation" && Currency(p.Currency)); Require(p.Type == "CustomQuotation" ? p.AmountMinor is null : p.AmountMinor is >= 0 and <= 1000000000); Require(p.EffectiveUntil is null || p.EffectiveFrom is not null && p.EffectiveUntil > p.EffectiveFrom); data = s;
            }
            else if (key.StartsWith("page:", StringComparison.Ordinal))
            {
                var p = Read<PageData>(input); var schema = Read<PageData>(Baseline[key]); foreach (var fields in new[] { p.Sv, p.En }) { Require(fields.Keys.Order().SequenceEqual(schema.Sv.Keys.Order())); foreach (var t in fields.Values) Text(t, 5000, true); }
                if (p.Image is { } i) ValidateAlt(i.AltSv, i.AltEn, i.Decorative); data = p;
            }
            else if (key.StartsWith("faq:", StringComparison.Ordinal)) { var f = Read<FaqData>(input); Require(f.DisplayOrder is >= 0 and <= 1000); foreach (var t in new[] { f.Sv, f.En }) { Text(t.Question, 250, true); Text(t.Answer, 5000, true); } data = f; }
            else
            {
                var p = Read<PolicyData>(input); foreach (var t in new[] { p.Sv, p.En }) { Text(t.Title, 200, true); Text(t.Content, 30000, true); }
                Text(p.ReviewedBy, 200); Require(p.EffectiveFrom >= new DateOnly(2020, 1, 1));
                if (publish && (!p.LegallyReviewed || string.IsNullOrWhiteSpace(p.ReviewedBy) || p.ReviewedOn is null || p.ReviewedOn > DateOnly.FromDateTime(DateTime.UtcNow))) throw new ContentOperationException(400, "legalReview"); data = p;
            }
            return JsonSerializer.Serialize(data, Json);
        }
        catch (Exception e) when (e is JsonException or ArgumentException or TimeZoneNotFoundException or InvalidTimeZoneException or NullReferenceException) { throw new ContentOperationException(400); }
    }
    public static bool Currency(string value) => value is "SEK" or "EUR" or "USD" or "GBP" or "NOK" or "DKK";
    public static void ValidateAlt(string sv, string en, bool decorative) { Text(sv, 500, !decorative); Text(en, 500, !decorative); if (decorative) Require(sv == "" && en == ""); }
}
