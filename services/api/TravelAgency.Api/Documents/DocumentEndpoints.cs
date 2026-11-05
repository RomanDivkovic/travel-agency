using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using TravelAgency.Api.Admin;
using TravelAgency.Api.Bookings;
using TravelAgency.Api.Customers;
using TravelAgency.Api.Research;
using TravelAgency.Application.Documents;
using TravelAgency.Infrastructure.Documents;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Api.Documents;

public static class DocumentEndpoints
{
    public static void MapDocuments(this WebApplication app)
    {
        var a = app.MapGroup("/api/admin/bookings/{bookingId:guid}").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<DocumentErrorFilter>().AddEndpointFilter<BookingErrorFilter>().AddEndpointFilter<ResearchErrorFilter>();
        a.MapGet("/documents", async (Guid bookingId, int? page, DocumentOperations ops, CancellationToken ct) => Results.Ok(await ops.ListAsync(bookingId, null, page ?? 1, ct)));
        a.MapPost("/documents", async (Guid bookingId, HttpContext c, DocumentOperations ops, DocumentSettings settings, CancellationToken ct) => await Upload(bookingId, null, c, ops, settings, ct)).RequireRateLimiting("document-uploads");
        a.MapPost("/documents/{id:guid}/replace", async (Guid bookingId, Guid id, HttpContext c, DocumentOperations ops, DocumentSettings settings, CancellationToken ct) => await Upload(bookingId, id, c, ops, settings, ct)).RequireRateLimiting("document-uploads");
        a.MapPatch("/documents/{id:guid}", async (Guid bookingId, Guid id, DocumentChange input, HttpContext c, DocumentOperations ops, CancellationToken ct) => { await ops.ChangeAsync(bookingId, id, new(input.VersionToken, 0), "edit", Actor(c), input, ct); return Results.NoContent(); });
        foreach (var action in new[] { "publish", "archive", "delete", "scan" }) { var op = action; var endpoint = a.MapPost("/documents/{id:guid}/" + op, async (Guid bookingId, Guid id, DocumentVersionInput input, HttpContext c, DocumentOperations ops, CancellationToken ct) => { await ops.ChangeAsync(bookingId, id, input, op, Actor(c), null, ct); return Results.NoContent(); }); if (op == "scan") endpoint.RequireRateLimiting("document-uploads"); }
        a.MapGet("/documents/{id:guid}/download", async (Guid bookingId, Guid id, int? version, HttpContext c, DocumentOperations ops, CancellationToken ct) => File(await ops.DownloadAsync(bookingId, id, version, null, Actor(c), ct), c));
        a.MapGet("/itinerary", async (Guid bookingId, ItineraryOperations ops, CancellationToken ct) => Results.Ok(await ops.GetAsync(bookingId, null, ct)));
        a.MapPost("/itinerary", async (Guid bookingId, HttpContext c, ItineraryOperations ops, CancellationToken ct) => { await ops.ChangeAsync(bookingId, "create", null, null, Actor(c), ct); return Results.NoContent(); });
        a.MapPatch("/itinerary", async (Guid bookingId, ItinerarySave input, HttpContext c, ItineraryOperations ops, CancellationToken ct) => { await ops.ChangeAsync(bookingId, "save", input.VersionToken, input, Actor(c), ct); return Results.NoContent(); });
        foreach (var action in new[] { "generate", "publish", "revise", "archive" }) { var op = action; a.MapPost("/itinerary/" + op, async (Guid bookingId, ItineraryVersionInput input, HttpContext c, ItineraryOperations ops, CancellationToken ct) => { await ops.ChangeAsync(bookingId, op, input.VersionToken, null, Actor(c), ct); return Results.NoContent(); }); }
        a.MapGet("/itinerary/pdf", async (Guid bookingId, int? version, string? language, HttpContext c, ItineraryOperations ops, ItineraryPdf pdf, CancellationToken ct) => { var data = await ops.GetAsync(bookingId, null, ct); var v = data.Versions.SingleOrDefault(v => v.Version == (version ?? data.CurrentVersion)) ?? throw new DocumentOperationException(404); return Results.File(pdf.Render(data, v, Language(language)), "application/pdf", data.Reference + "-itinerary-v" + v.Version + ".pdf"); });
        var customer = app.MapGroup("/api/customer/bookings/{bookingId:guid}").RequireAuthorization(CustomerSetup.Policy).AddEndpointFilter<CustomerCsrfFilter>().AddEndpointFilter<DocumentErrorFilter>().AddEndpointFilter<BookingErrorFilter>().AddEndpointFilter<ResearchErrorFilter>();
        customer.MapGet("/documents", async (Guid bookingId, int? page, HttpContext c, DocumentOperations ops, CancellationToken ct) => Results.Ok(await ops.ListAsync(bookingId, Actor(c), page ?? 1, ct)));
        customer.MapGet("/documents/{id:guid}/download", async (Guid bookingId, Guid id, HttpContext c, DocumentOperations ops, CancellationToken ct) => File(await ops.DownloadAsync(bookingId, id, null, Actor(c), Actor(c), ct), c));
        customer.MapGet("/itinerary", async (Guid bookingId, HttpContext c, ItineraryOperations ops, CancellationToken ct) => Results.Ok(await ops.GetAsync(bookingId, Actor(c), ct)));
        customer.MapGet("/itinerary/pdf", async (Guid bookingId, string? language, HttpContext c, ItineraryOperations ops, DocumentAccess access, ItineraryPdf pdf, TravelAgencyDbContext db, CancellationToken ct) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct); await access.BookingAsync(bookingId, Actor(c), true, ct); var data = await ops.GetAsync(bookingId, Actor(c), ct); var v = data.Versions.Single(); var bytes = pdf.Render(data, v, Language(language ?? await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Users.Where(u => u.Id == Actor(c)).Select(u => u.PreferredLanguage), ct))); await tx.CommitAsync(ct); return Results.File(bytes, "application/pdf", data.Reference + "-itinerary-v" + v.Version + ".pdf");
        });
    }
    private static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static string Language(string? language) => language is null or "sv" ? "sv" : language == "en" ? "en" : throw new DocumentOperationException(400);
    private static IResult File((byte[] Bytes, string Mime, string Name) file, HttpContext c) { c.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin"; return Results.File(file.Bytes, file.Mime, file.Name); }
    private static async Task<IResult> Upload(Guid bookingId, Guid? id, HttpContext c, DocumentOperations ops, DocumentSettings settings, CancellationToken ct)
    {
        settings.RequireStorage(); var limit = settings.MaxBytes + 32768;
        if (c.Request.ContentLength > limit) throw new DocumentOperationException(413);
        if (c.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } feature) feature.MaxRequestBodySize = limit;
        if (!c.Request.HasFormContentType) throw new DocumentOperationException(415);
        var form = await c.Request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = limit, MemoryBufferThreshold = checked((int)limit), ValueLengthLimit = 8192, ValueCountLimit = 4, MultipartHeadersLengthLimit = 4096 }, ct);
        if (form.Files.Count != 1 || form["metadata"].Count != 1) throw new DocumentOperationException(400); var file = form.Files[0]; if (file.Length > settings.MaxBytes) throw new DocumentOperationException(413);
        DocumentMetadata metadata;
        try { metadata = JsonSerializer.Deserialize<DocumentMetadata>(form["metadata"].ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow }) ?? throw new DocumentOperationException(400); } catch (JsonException) { throw new DocumentOperationException(400); }
        Guid? token = Guid.TryParse(form["versionToken"], out var parsed) ? parsed : null;
        await using var input = file.OpenReadStream(); using var output = new MemoryStream(); var buffer = new byte[16384]; int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0) { if (output.Length + read > settings.MaxBytes) throw new DocumentOperationException(413); await output.WriteAsync(buffer.AsMemory(0, read), ct); }
        return Results.Json(new { id = await ops.UploadAsync(bookingId, id, token, metadata, file.FileName, file.ContentType, output.ToArray(), Actor(c), ct) }, statusCode: 201);
    }
}
