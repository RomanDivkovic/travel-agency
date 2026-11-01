using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

[Collection("Request persistence")]
public sealed class AdminTests : IAsyncLifetime
{
    private ApiFactory factory = null!;
    private readonly string email = $"admin-{Guid.NewGuid():N}@example.test";
    private readonly string password = "Test-Only!" + Guid.NewGuid().ToString("N") + "7aA";
    private readonly List<Guid> requests = [];
    private string userId = "";
    public static bool HasDatabase => DatabaseIntegrationTests.HasTestDatabase;
    public async ValueTask InitializeAsync()
    {
        if (!HasDatabase) return;
        var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")!;
        Assert.EndsWith("_tests", new NpgsqlConnectionStringBuilder(connection).Database);
        factory = new ApiFactory(connectionString: connection);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Administrator")) Assert.True((await roles.CreateAsync(new IdentityRole("Administrator"))).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>();
        var user = new Administrator { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "QA Administrator" };
        Assert.True((await users.CreateAsync(user, password)).Succeeded); Assert.True((await users.AddToRoleAsync(user, "Administrator")).Succeeded); userId = user.Id;
    }
    public async ValueTask DisposeAsync()
    {
        if (factory is null) return;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
        await db.TravelRequests.Where(r => requests.Contains(r.Id)).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>(); var user = await users.FindByIdAsync(userId); if (user is not null) await users.DeleteAsync(user);
        await factory.DisposeAsync();
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
    private static async Task<HttpResponseMessage> Mutation(HttpClient client, string path, object data, HttpMethod? method = null)
    {
        var token = (await client.GetFromJsonAsync<JsonElement>("/api/admin/auth/csrf", TestContext.Current.CancellationToken)).GetProperty("token").GetString();
        using var message = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(data) }; message.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(message, TestContext.Current.CancellationToken);
    }
    private async Task Login(HttpClient client)
    {
        using var response = await Mutation(client, "/api/admin/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    private async Task<AdminRequestDetail> CreateRequest(HttpClient publicClient, string service = "complete")
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(RequestExamples.Input(service) with { Contact = RequestExamples.Input().Contact! with { Name = email, Email = email } }) };
        message.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D")); using var response = await publicClient.SendAsync(message, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = (await response.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken))!;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
        var r = await db.TravelRequests.SingleAsync(r => r.ReferenceNumber == receipt.ReferenceNumber, TestContext.Current.CancellationToken); requests.Add(r.Id);
        return (await scope.ServiceProvider.GetRequiredService<IAdminQueries>().DetailAsync(r.Id, TestContext.Current.CancellationToken))!;
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task AnonymousRequestsCannotAccessAnyAdministrativeDataOrMutation()
    {
        using var client = Client(); var id = Guid.NewGuid();
        foreach (var path in new[] { "/api/admin/auth/me", "/api/admin/dashboard/summary", "/api/admin/travel-requests", $"/api/admin/travel-requests/{id}", $"/api/admin/travel-requests/{id}/notes", $"/api/admin/travel-requests/{id}/activity" })
        { using var response = await client.GetAsync(path, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Contains("no-store", response.Headers.CacheControl!.ToString()); }
        foreach (var (path, method) in new[] { ($"/api/admin/travel-requests/{id}/status", HttpMethod.Patch), ($"/api/admin/travel-requests/{id}/notes", HttpMethod.Post), ("/api/admin/auth/logout", HttpMethod.Post) })
        { using var response = await client.SendAsync(new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); }
        using var register = await client.PostAsJsonAsync("/api/admin/auth/register", new { email, password }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.NotFound, register.StatusCode);
        await CreateRequest(client);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task LoginCsrfGenericFailuresCookieFlagsAndLogoutRevocationWork()
    {
        using var client = Client();
        using var missing = await client.PostAsJsonAsync("/api/admin/auth/login", new { email, password }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using var invalid = await Mutation(client, "/api/admin/auth/login", new { email, password = "incorrect" });
        using var unknown = await Mutation(client, "/api/admin/auth/login", new { email = "unknown@example.test", password = "incorrect" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        var a = await invalid.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken); var b = await unknown.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken); Assert.Equal(a.GetProperty("title").GetString(), b.GetProperty("title").GetString());
        using var login = await Mutation(client, "/api/admin/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("travel.admin=")); Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var me = await client.GetFromJsonAsync<AdminUserDto>("/api/admin/auth/me", TestContext.Current.CancellationToken); Assert.Equal(userId, me!.Id);
        using var missingLogoutToken = await client.PostAsync("/api/admin/auth/logout", null, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, missingLogoutToken.StatusCode);
        using var logout = await Mutation(client, "/api/admin/auth/logout", new { }); Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var revoked = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, BaseAddress = new Uri("http://localhost") }); revoked.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        using var stale = await revoked.GetAsync("/api/admin/dashboard/summary", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PasswordLockoutAndIpRateLimitAreEnforced()
    {
        using var client = Client();
        for (var n = 0; n < 5; n++) { using var failure = await Mutation(client, "/api/admin/auth/login", new { email, password = "incorrect" }); Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode); }
        using var locked = await Mutation(client, "/api/admin/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        using var scope = factory.Services.CreateScope(); var user = await scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>().FindByIdAsync(userId); Assert.True(user!.LockoutEnd > DateTimeOffset.UtcNow);
        for (var n = 0; n < 4; n++) { using var failure = await Mutation(client, "/api/admin/auth/login", new { email = "unknown@example.test", password }); Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode); }
        using var limited = await Mutation(client, "/api/admin/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.True(limited.Headers.Contains("Retry-After"));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CookieExpiresAndAnAuthenticatedNonAdministratorIsForbidden()
    {
        var clock = new TestClock();
        await using var timed = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, o => o.TimeProvider = clock)));
        using var client = timed.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") }); await Login(client);
        clock.Now += TimeSpan.FromMinutes(15);
        using var active = await client.GetAsync("/api/admin/auth/me", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        Assert.False(active.Headers.TryGetValues("Set-Cookie", out var renewed) && renewed.Any(c => c.StartsWith("travel.admin=")));
        clock.Now += TimeSpan.FromMinutes(16); using var expired = await client.GetAsync("/api/admin/auth/me", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        using var normal = Client(); await Login(normal);
        using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>(); var user = (await users.FindByIdAsync(userId))!; Assert.True((await users.RemoveFromRoleAsync(user, "Administrator")).Succeeded);
        using var forbidden = await normal.GetAsync("/api/admin/travel-requests", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task RequestQueriesStatusConcurrencyPrivateNotesAndActivitiesUseRealDatabase()
    {
        using var publicClient = Client(); var r = await CreateRequest(publicClient); await CreateRequest(publicClient, "flights");
        using var client = Client(); await Login(client);
        var page = (await client.GetFromJsonAsync<PageResult<RequestListItem>>("/api/admin/travel-requests?search=" + Uri.EscapeDataString(email) + "&pageSize=1&sort=createdAt_asc", TestContext.Current.CancellationToken))!;
        Assert.Equal(2, page.TotalCount); Assert.Single(page.Items); Assert.Equal(2, page.TotalPages); Assert.Equal(r.Id, page.Items[0].Id);
        var filtered = (await client.GetFromJsonAsync<PageResult<RequestListItem>>("/api/admin/travel-requests?search=" + Uri.EscapeDataString(email) + "&serviceType=flights&status=Submitted", TestContext.Current.CancellationToken))!; Assert.Single(filtered.Items);
        using var invalid = await client.GetAsync("/api/admin/travel-requests?pageSize=101", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var invalidDate = await client.GetAsync("/api/admin/travel-requests?from=not-a-date", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, invalidDate.StatusCode);
        using var notFound = await client.GetAsync($"/api/admin/travel-requests/{Guid.NewGuid()}", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        using var csrf = await client.PatchAsJsonAsync($"/api/admin/travel-requests/{r.Id}/status", new { status = "UnderReview", r.Version }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var badTransition = await Mutation(client, $"/api/admin/travel-requests/{r.Id}/status", new { status = "AwaitingCustomer", r.Version }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.BadRequest, badTransition.StatusCode);
        var updates = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Mutation(client, $"/api/admin/travel-requests/{r.Id}/status", new { status = "UnderReview", r.Version }, HttpMethod.Patch)));
        Assert.Single(updates, x => x.StatusCode == HttpStatusCode.NoContent); Assert.Single(updates, x => x.StatusCode == HttpStatusCode.Conflict); foreach (var response in updates) response.Dispose();
        using var note = await Mutation(client, $"/api/admin/travel-requests/{r.Id}/notes", new { content = "Private <script>alert(1)</script>" }); Assert.Equal(HttpStatusCode.NoContent, note.StatusCode);
        using var invalidNote = await Mutation(client, $"/api/admin/travel-requests/{r.Id}/notes", new { content = new string('x', 4001) }); Assert.Equal(HttpStatusCode.BadRequest, invalidNote.StatusCode);
        using var forgedAuthor = await Mutation(client, $"/api/admin/travel-requests/{r.Id}/notes", new { content = "Forged", administratorId = "untrusted-author" }); Assert.Equal(HttpStatusCode.BadRequest, forgedAuthor.StatusCode);
        var notes = (await client.GetFromJsonAsync<PageResult<NoteDto>>($"/api/admin/travel-requests/{r.Id}/notes", TestContext.Current.CancellationToken))!; Assert.Single(notes.Items); Assert.Equal("QA Administrator", notes.Items[0].Author);
        var history = (await client.GetFromJsonAsync<PageResult<ActivityDto>>($"/api/admin/travel-requests/{r.Id}/activity", TestContext.Current.CancellationToken))!; Assert.Equal(new[] { "Submitted", "StatusChanged", "NoteAdded" }, history.Items.Select(a => a.Type)); Assert.DoesNotContain(history.Items, a => a.NewValue?.Contains("script") == true);
        var detail = (await client.GetFromJsonAsync<AdminRequestDetail>($"/api/admin/travel-requests/{r.Id}", TestContext.Current.CancellationToken))!; Assert.Equal("UnderReview", detail.Status); Assert.NotEqual(r.Version, detail.Version); Assert.NotEmpty(detail.Plan.PreferencesJson);
        var summary = (await client.GetFromJsonAsync<DashboardDto>("/api/admin/dashboard/summary", TestContext.Current.CancellationToken))!; Assert.True(summary.Total >= 2); Assert.True(summary.UnderReview >= 1);
        using var publicDetail = await publicClient.GetAsync($"/api/travel-requests/{r.ReferenceNumber}", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.NotFound, publicDetail.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProvisioningNeverOverwritesExistingAdministratorCredentials()
    {
        using var scope = factory.Services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<AdminProvisioner>();
        Assert.False(await provisioner.ProvisionAsync(email, "Different-Password!23", "Other name", TestContext.Current.CancellationToken));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>();
        var user = (await users.FindByIdAsync(userId))!;
        Assert.True(await users.CheckPasswordAsync(user, password)); Assert.Equal("QA Administrator", user.DisplayName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.ProvisionAsync("another@example.test", password, "Another", TestContext.Current.CancellationToken));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PasswordOnlyEndpointCannotBypassConfiguredTotp()
    {
        using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>();
        var user = (await users.FindByIdAsync(userId))!;
        Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
        Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
        using var client = Client(); using var response = await Mutation(client, "/api/admin/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("travel.admin=")));
        using var me = await client.GetAsync("/api/admin/auth/me", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProductionCookiesHaveSecureHostOnlyAndHttpOnlyProtections()
    {
        await using var production = new ApiFactory(environment: "Production", connectionString: Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION"));
        using var client = production.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var response = await Mutation(client, "/api/admin/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-travel.admin="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
        using var me = await client.GetAsync("/api/admin/auth/me", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }
    [Fact]
    public void StatusWorkflowRejectsReopeningAndQueryBoundariesAreExplicit()
    {
        Assert.Empty(TravelInquiry.AllowedTransitions(RequestStatus.Closed)); Assert.DoesNotContain(RequestStatus.AwaitingCustomer, TravelInquiry.AllowedTransitions(RequestStatus.Submitted));
        Assert.False(new RequestQuery(PageSize: 101).IsValid); Assert.False(new RequestQuery(Status: "0").IsValid); Assert.False(new RequestQuery(Sort: "unsafe").IsValid);
    }
    private sealed class TestClock : TimeProvider { public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
}
