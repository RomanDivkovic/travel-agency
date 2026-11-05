using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
namespace TravelAgency.Api.Documents;

public static class DocumentSetup
{
    public static void AddDocuments(this WebApplicationBuilder b)
    {
        var settings = b.Configuration.GetSection("TravelDocuments").Get<DocumentSettings>() ?? new();
        if (b.Environment.IsDevelopment() && b.Configuration["TravelDocuments:Provider"] is null) settings.Provider = "LocalEncrypted";
        if (settings.MaxBytes is < 1024 or > 25 * 1024 * 1024 || settings.ArchivedRetentionDays is < 1 or > 3650 || settings.DraftRetentionDays is < 1 or > 365 || settings.ScanTimeoutSeconds is < 1 or > 30 || settings.ScannerPort is < 1 or > 65535 || settings.Provider is not ("Disabled" or "LocalEncrypted") || !IPAddress.TryParse(settings.ScannerHost, out var scannerIp) || !IPAddress.IsLoopback(scannerIp)) throw new InvalidOperationException("Invalid document storage, retention or loopback scanner configuration.");
        if (settings.Provider == "LocalEncrypted")
        {
            if (!b.Environment.IsDevelopment()) throw new InvalidOperationException("The local document adapter is development-only. Configure a reviewed private production adapter before enabling uploads.");
            settings.StorageRoot = Path.GetFullPath(settings.StorageRoot.Length == 0 ? Path.Combine(Path.GetTempPath(), "travel-agency-private-documents") : settings.StorageRoot);
            var repo = new DirectoryInfo(b.Environment.ContentRootPath); while (repo is not null && !File.Exists(Path.Combine(repo.FullName, "TravelAgency.sln"))) repo = repo.Parent;
            var app = Path.GetFullPath(repo?.FullName ?? b.Environment.ContentRootPath);
            if (settings.StorageRoot.Equals(app, StringComparison.OrdinalIgnoreCase) || settings.StorageRoot.StartsWith(app + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || settings.StorageRoot.Contains(Path.DirectorySeparatorChar + "wwwroot" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(settings.StorageRoot).Equals("wwwroot", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Document storage must be private and outside application/public directories.");
        }
        b.Services.AddSingleton(settings); b.Services.AddScoped<ITravelDocumentStorage, LocalDocumentStorage>(); b.Services.AddScoped<IDocumentScanner, ClamAvScanner>(); b.Services.AddScoped<DocumentAccess>(); b.Services.AddScoped<DocumentOperations>(); b.Services.AddScoped<DocumentNotifications>(); b.Services.AddScoped<ItineraryOperations>(); b.Services.AddScoped<ItineraryPdf>(); b.Services.AddScoped<DocumentMaintenance>();
        b.Services.AddRateLimiter(o => o.AddPolicy("document-uploads", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) })));
    }
}
public sealed class DocumentErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        try { return await next(c); }
        catch (DocumentOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status switch { 404 => "Document or itinerary unavailable.", 409 => "State changed or publication requirements are not met. Reload before retrying.", 412 => "Private storage or approved account origin requires configuration.", 413 => "File exceeds the configured upload limit.", 415 => "File type, extension or signature is not supported.", _ => "Check document or itinerary fields." }); }
        catch (DocumentRuleException) { return Results.Problem(statusCode: 400, title: "Check document metadata or local travel times and UTC offset."); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "State changed. Reload before retrying."); }
        catch (InvalidDataException) { return Results.Problem(statusCode: 400, title: "Invalid or oversized multipart upload."); }
        catch (System.Security.Cryptography.CryptographicException) { return Results.Problem(statusCode: 503, title: "Document storage is temporarily unavailable."); }
        catch (IOException) { return Results.Problem(statusCode: 503, title: "Document storage is temporarily unavailable."); }
    }
}
