using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelAgency.Application.Abstractions;

namespace TravelAgency.Tests;

internal sealed class ApiFactory(
    bool? databaseAvailable = null,
    string environment = "Development",
    string? connectionString = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:TravelAgency", connectionString
            ?? "Host=127.0.0.1;Port=1;Database=unavailable_tests;Username=unused;Timeout=1;Command Timeout=1");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");

        if (databaseAvailable.HasValue)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDatabaseReadiness>();
                services.AddScoped<IDatabaseReadiness>(_ => new StubReadiness(databaseAvailable.Value));
            });
        }
    }

    private sealed class StubReadiness(bool available) : IDatabaseReadiness
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken) => Task.FromResult(available);
    }
}
