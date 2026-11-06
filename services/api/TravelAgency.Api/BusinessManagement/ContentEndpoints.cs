using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Api.Admin;
using TravelAgency.Application.BusinessManagement;
using TravelAgency.Infrastructure.BusinessManagement;
namespace TravelAgency.Api.BusinessManagement;

public static class ContentEndpoints
{
    public static void AddBusinessManagement(this WebApplicationBuilder b) { b.Services.AddScoped<ContentOperations>(); b.Services.AddScoped<MediaOperations>(); }
    public static void UseBusinessContentLimits(this WebApplication app) => app.Use(async (context, next) =>
    {
        if (context.User.IsInRole("Administrator") && context.Request.Path.StartsWithSegments("/api/admin/cms") && context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            limit.MaxRequestBodySize = context.Request.Path == "/api/admin/cms/media" && HttpMethods.IsPost(context.Request.Method) ? WebsiteImageValidation.MaxBytes + 16384 : 1024 * 1024;
        await next(context);
    });
    public static void MapBusinessManagement(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/cms").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<ContentErrors>();
        admin.MapGet("/documents", (ContentOperations s, CancellationToken ct) => s.List(ct));
        admin.MapGet("/documents/{key}", (string key, ContentOperations s, CancellationToken ct) => s.Detail(key, ct));
        admin.MapPost("/documents/{key}/draft", async (string key, ContentDraftInput input, ClaimsPrincipal u, ContentOperations s, CancellationToken ct) => { await s.Save(key, input, Actor(u), ct); return Results.NoContent(); });
        admin.MapPost("/documents/{key}/publish", async (string key, ContentAction input, ClaimsPrincipal u, ContentOperations s, CancellationToken ct) => { await s.Publish(key, input, Actor(u), ct); return Results.NoContent(); });
        admin.MapPost("/documents/{key}/restore", async (string key, ContentAction input, ClaimsPrincipal u, ContentOperations s, CancellationToken ct) => { await s.Restore(key, input, Actor(u), ct); return Results.NoContent(); });
        admin.MapGet("/preview/{key}", (string key, ContentOperations s, CancellationToken ct) => s.Published(ct, key));
        admin.MapGet("/media", (int? page, MediaOperations s, CancellationToken ct) => s.List(page ?? 1, ct));
        admin.MapPost("/media", Upload);
        admin.MapPatch("/media/{id:guid}", async (Guid id, MediaChange input, ClaimsPrincipal u, MediaOperations s, CancellationToken ct) => { await s.Change(id, input, Actor(u), ct); return Results.NoContent(); });
        admin.MapGet("/media/{id:guid}", async (Guid id, MediaOperations s, CancellationToken ct) => { var m = await s.File(id, true, ct); return Results.Bytes(m.Bytes, m.ContentType); });
        var pub = app.MapGroup("/api/public/website").AddEndpointFilter<ContentErrors>();
        pub.MapGet("", async (HttpContext c, ContentOperations s, CancellationToken ct) =>
        {
            var data = await s.Published(ct); var tag = '"' + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, ContentSchema.Json)))) + '"';
            c.Response.Headers.CacheControl = "public, no-cache, must-revalidate"; c.Response.Headers.ETag = tag;
            return c.Request.Headers.IfNoneMatch.Any(x => x == tag) ? Results.StatusCode(304) : Results.Ok(data);
        });
        pub.MapGet("/media/{id:guid}", async (Guid id, MediaOperations s, CancellationToken ct) => { var m = await s.File(id, false, ct); return Results.Bytes(m.Bytes, m.ContentType); });
    }
    private static async Task<IResult> Upload(HttpContext c, MediaOperations s, CancellationToken ct)
    {
        if (!c.Request.HasFormContentType) throw new ContentOperationException(415, "image");
        if (c.Request.ContentLength is null || c.Request.ContentLength > WebsiteImageValidation.MaxBytes + 16384) throw new ContentOperationException(413, "size");
        var form = await c.Request.ReadFormAsync(ct); if (form.Files.Count != 1) throw new ContentOperationException(400); var file = form.Files[0];
        if (file.Length > WebsiteImageValidation.MaxBytes) throw new ContentOperationException(413, "size"); using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, ct);
        var dto = await s.Upload(buffer.ToArray(), file.ContentType, file.FileName, form["altSv"].ToString(), form["altEn"].ToString(), form["decorative"] == "true", Actor(c.User), ct); return Results.Ok(dto);
    }
    private static string Actor(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
public sealed class ContentErrors : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        try { return await next(c); } catch (ContentOperationException e) { return Results.Problem(statusCode: e.Status, title: "Content operation could not be completed.", extensions: new Dictionary<string, object?> { ["code"] = e.Code }); } catch (BadHttpRequestException e) { return Results.Problem(statusCode: e.StatusCode, title: "Invalid or oversized content request."); } catch (InvalidDataException) { return Results.Problem(statusCode: 400, title: "Invalid multipart upload."); } catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Content changed. Reload before retrying."); }
    }
}
