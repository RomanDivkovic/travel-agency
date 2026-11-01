using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TravelAgency.Application.Requests;

public sealed class SubmissionHandler(ITravelRequestStore store, TimeProvider clock)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<SubmissionResult> SubmitAsync(Guid key, SubmitTravelRequest input, CancellationToken cancellationToken)
    {
        var request = Normalize(input);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions))));
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).DateTime);
        var errors = RequestValidator.Validate(request, today);
        // Reject invalid structure without touching storage. Previously accepted temporal answers remain replayable.
        if (errors.Keys.Any(field => field is not ("departure" or "return" or "earliest" or "latest" or "duration")))
            return new SubmissionResult(Errors: errors);
        var replay = await store.FindReplayAsync(key, hash, cancellationToken);
        if (replay is not null) return replay;
        if (errors.Count > 0) return new SubmissionResult(Errors: errors);
        return await store.SaveAsync(key, hash, request, cancellationToken);
    }
    public static SubmitTravelRequest Normalize(SubmitTravelRequest r) => r with
    {
        Destination = r.Destination is { } d ? d with { Origin = d.Origin?.Trim(), Place = d.Place?.Trim(), AdditionalStops = d.AdditionalStops?.Select(s => s?.Trim() ?? "").ToArray() } : null,
        SpecialRequests = string.IsNullOrWhiteSpace(r.SpecialRequests) ? null : r.SpecialRequests.Trim(),
        Website = r.Website == "" ? null : r.Website,
        Contact = r.Contact is { } c ? c with { Name = c.Name?.Trim(), Email = c.Email?.Trim(), Phone = c.Phone?.Trim() } : null
    };
}
