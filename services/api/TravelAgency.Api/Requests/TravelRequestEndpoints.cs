using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelAgency.Application.Requests;

namespace TravelAgency.Api.Requests;

public static class TravelRequestEndpoints
{
    private static readonly JsonSerializerOptions InputJson = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16, PropertyNameCaseInsensitive = false };
    public static void MapTravelRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/api/travel-requests", SubmitAsync).RequireRateLimiting("travel-requests");
    }
    private static async Task<IResult> SubmitAsync(HttpContext context, SubmissionHandler handler, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!context.Request.HasJsonContentType()) return Results.Problem(statusCode: 415, title: "Use application/json.");
        if (context.Request.ContentLength > 32768) return Results.Problem(statusCode: 413, title: "Request is too large.");
        if (!Guid.TryParseExact(context.Request.Headers["Idempotency-Key"], "D", out var key) || key == Guid.Empty)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["choice"] });
        SubmitTravelRequest? request;
        try
        {
            var buffer = new byte[32769]; var length = 0;
            while (length < buffer.Length)
            {
                var read = await context.Request.Body.ReadAsync(buffer.AsMemory(length), ct);
                if (read == 0) break;
                length += read;
            }
            if (length > 32768) return Results.Problem(statusCode: 413, title: "Request is too large.");
            request = JsonSerializer.Deserialize<SubmitTravelRequest>(buffer.AsSpan(0, length), InputJson);
        }
        catch (JsonException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["choice"] }); }
        catch (BadHttpRequestException exception) when (exception.StatusCode == 413) { return Results.Problem(statusCode: 413, title: "Request is too large."); }
        if (request is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["required"] });
        var logger = loggerFactory.CreateLogger("TravelRequests"); var timer = Stopwatch.StartNew();
        try
        {
            var result = await handler.SubmitAsync(key, request, ct);
            if (result.Errors is not null) return Results.ValidationProblem(result.Errors);
            if (result.Conflict) return Results.Problem(statusCode: 409, title: "This submission key was already used for different answers.");
            var receipt = result.Receipt!;
            if (!result.Replayed) logger.LogInformation("Travel request {ReferenceNumber} stored in {DurationMs} ms. Trace {TraceId}", receipt.ReferenceNumber, timer.ElapsedMilliseconds, context.TraceIdentifier);
            return Results.Json(receipt, statusCode: 201);
        }
        catch (Exception exception) when (exception is NpgsqlException or DbUpdateException or TimeoutException || exception is InvalidOperationException { InnerException: NpgsqlException })
        {
            logger.LogWarning("Travel request storage unavailable. Trace {TraceId}", context.TraceIdentifier);
            return Results.Problem(statusCode: 503, title: "Submission could not be confirmed. Retry using the same key.");
        }
    }
}
