using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using TravelAgency.Application.Requests;

namespace TravelAgency.Tests;

public sealed class RequestApiTests
{
    [Fact]
    public async Task InvalidInputsAreRejectedBeforeDatabaseAccessAndNoPublicReadIsExposed()
    {
        await using var factory = new ApiFactory(); using var client = factory.CreateClient();
        using var missingKey = await client.PostAsJsonAsync("/api/travel-requests", RequestExamples.Input(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(RequestExamples.Input() with { Budget = new(0, "SEK", "total") }) };
        invalid.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); using var response = await client.SendAsync(invalid, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("amount", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var read = await client.GetAsync("/api/travel-requests/TRV-2026-000001", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }
    [Fact]
    public async Task DatabaseFailureIsSafeAndRateLimitProvidesRetryAfter()
    {
        await using var factory = new RateFactory(); using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(RequestExamples.Input()) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var failed = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); var body = await failed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Password", body); Assert.DoesNotContain("Npgsql", body); Assert.DoesNotContain("anna@", body);
        using var limited = await client.PostAsJsonAsync("/api/travel-requests", RequestExamples.Input(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
    }
    private sealed class RateFactory : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development"); builder.UseSetting("ConnectionStrings:TravelAgency", "Host=127.0.0.1;Port=1;Database=unavailable_tests;Username=unused;Timeout=1;Command Timeout=1");
            builder.UseSetting("Email:WorkerEnabled", "false"); builder.UseSetting("Requests:PermitLimit", "1");
        }
    }
}
