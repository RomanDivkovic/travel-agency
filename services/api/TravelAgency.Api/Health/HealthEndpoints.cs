using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TravelAgency.Api.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponseAsync
        });
        app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponseAsync
        });
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        // Do not include database exceptions or connection details in public responses.
        return context.Response.WriteAsJsonAsync(
            new { status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy" },
            context.RequestAborted);
    }
}
