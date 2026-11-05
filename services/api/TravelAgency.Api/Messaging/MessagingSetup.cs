using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Messaging;
namespace TravelAgency.Api.Messaging;

public static class MessagingSetup
{
    public static void AddMessaging(this WebApplicationBuilder b)
    {
        var s = b.Configuration.GetSection("Messaging").Get<MessagingSettings>() ?? new(); if (s.MaxMessageLength is < 100 or > 5000 || s.NotificationDelaySeconds is < 0 or > 300 || s.NotificationCooldownSeconds is < 30 or > 3600 || s.AgentLanguage is not ("sv" or "en") || s.ClosedRetentionDays is < 30 or > 3650) throw new InvalidOperationException("Invalid messaging limits, notification or retention configuration.");
        b.Services.AddSingleton(s); b.Services.AddScoped<MessagingAccess>(); b.Services.AddScoped<MessagingQueries>(); b.Services.AddScoped<MessagingOperations>(); b.Services.AddScoped<MessagingNotifications>(); b.Services.AddScoped<MessagingMaintenance>();
        b.Services.AddRateLimiter(o => o.AddPolicy("message-submissions", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
    }
}
public sealed class MessagingErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        try { return await next(c); } catch (MessagingOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status switch { 404 => "Conversation or associated resource unavailable.", 409 => "State or submission changed. Reload before retrying.", 412 => "Messaging notifications require an approved portal origin.", _ => "Check conversation fields." }); } catch (MessagingRuleException) { return Results.Problem(statusCode: 400, title: "Check message fields or conversation status."); } catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Conversation changed. Reload before retrying."); }
    }
}
