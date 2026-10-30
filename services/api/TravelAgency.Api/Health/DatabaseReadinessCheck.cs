using Microsoft.Extensions.Diagnostics.HealthChecks;
using TravelAgency.Application.Abstractions;

namespace TravelAgency.Api.Health;

public sealed class DatabaseReadinessCheck(IDatabaseReadiness database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database unavailable.");
    }
}
