using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Abstractions;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task LivenessRemainsHealthyWhenDatabaseIsUnreachable()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"healthy\"}", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
    }

    [Theory]
    [InlineData(true, HttpStatusCode.OK, "healthy")]
    [InlineData(false, HttpStatusCode.ServiceUnavailable, "unhealthy")]
    public async Task ReadinessReflectsDatabaseAvailability(bool available, HttpStatusCode expectedCode, string expectedStatus)
    {
        await using var factory = new ApiFactory(available);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(expectedStatus, payload?.Status);
    }

    [Fact]
    public async Task RealConnectionFailureReturnsSafeUnhealthyResponse()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("{\"status\":\"unhealthy\"}", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartupResolvesDatabaseServicesWithNpgsql()
    {
        await using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        Assert.IsType<DatabaseReadiness>(scope.ServiceProvider.GetRequiredService<IDatabaseReadiness>());
        var database = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", database.Database.ProviderName);
        Assert.Empty(database.Model.GetEntityTypes());
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task DevelopmentCorsAllowsOnlyConfiguredOrigin(string origin, bool allowed)
    {
        await using var factory = new ApiFactory(false);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed) Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task ProductionDoesNotEnableDevelopmentCors()
    {
        await using var factory = new ApiFactory(false, "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:5173");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task MissingRouteReturnsProblemDetails()
    {
        await using var factory = new ApiFactory(false);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/missing", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record HealthResponse(string Status);
}
