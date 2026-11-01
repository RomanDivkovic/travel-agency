using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TravelAgency.Application.Quotations;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Tests;

[Collection("Request persistence")]
public sealed partial class QuotationApiTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> factory = null!;
    private ApiFactory original = null!;
    private readonly List<Guid> requests = [];
    private readonly string email = $"quote-admin-{Guid.NewGuid():N}@example.test";
    private readonly string password = "Test-only!7Aa" + Guid.NewGuid().ToString("N");
    private string userId = "";
    public static bool HasDatabase => DatabaseIntegrationTests.HasTestDatabase;
    private static string Connection => Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")!;
    public async ValueTask InitializeAsync()
    {
        if (!HasDatabase) return;
        Assert.EndsWith("_tests", new NpgsqlConnectionStringBuilder(Connection).Database);
        original = new ApiFactory(connectionString: Connection);
        factory = original.WithWebHostBuilder(b =>
        {
            b.UseSetting("Payments:Provider", "Sandbox"); b.UseSetting("Payments:IssuanceEnabled", "true"); b.UseSetting("Payments:AccountingApproved", "true");
            b.UseSetting("Payments:PublicOrigin", "http://localhost:5174"); b.UseSetting("Payments:SandboxSigningSecret", "TEST-ONLY-signing-secret-at-least-32-characters");
            b.UseSetting("Payments:SellerName", "TEST business"); b.UseSetting("Payments:SellerAddress", "TEST address"); b.UseSetting("Payments:RegistrationNumber", "TEST not a registration");
            b.UseSetting("Payments:TaxInformation", "TEST tax only"); b.UseSetting("Payments:TermsSv", "TEST invoice terms sv — NOT LEGAL"); b.UseSetting("Payments:TermsEn", "TEST invoice terms en — NOT LEGAL"); b.UseSetting("Payments:TermsVersion", "TEST-v1");
            b.UseSetting("Quotations:IssuanceEnabled", "true"); b.UseSetting("Quotations:PublicOrigin", "http://localhost:5174");
            b.UseSetting("Quotations:ApprovedTaxTreatments:0", "VatExclusive"); b.UseSetting("Quotations:ApprovedTaxTreatments:1", "NoVat");
            b.UseSetting("Quotations:ApprovedTaxRatesBasisPoints:0", "2500"); b.UseSetting("Quotations:TermsVersion", "test-only-v1");
            b.UseSetting("Quotations:TermsSv", "TEST TERMS – not legal terms."); b.UseSetting("Quotations:TermsEn", "TEST TERMS – not legal terms.");
            b.UseSetting("Quotations:AcceptanceTextSv", "TEST acknowledgement – not legal wording."); b.UseSetting("Quotations:AcceptanceTextEn", "TEST acknowledgement – not legal wording.");
        });
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); if (!await roles.RoleExistsAsync("Administrator")) Assert.True((await roles.CreateAsync(new IdentityRole("Administrator"))).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>(); var user = new Administrator { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Quotation QA" };
        Assert.True((await users.CreateAsync(user, password)).Succeeded); Assert.True((await users.AddToRoleAsync(user, "Administrator")).Succeeded); userId = user.Id;
    }
    public async ValueTask DisposeAsync()
    {
        if (factory is null) return;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
        await db.Invoices.Where(i => requests.Contains(i.RequestId)).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await db.TravelRequests.Where(r => requests.Contains(r.Id)).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>(); var user = await users.FindByIdAsync(userId); if (user is not null) Assert.True((await users.DeleteAsync(user)).Succeeded);
        await factory.DisposeAsync(); await original.DisposeAsync();
    }
    private HttpClient Client(WebApplicationFactory<Program>? source = null) => (source ?? factory).CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), HandleCookies = true });
    private static async Task<HttpResponseMessage> Mutate(HttpClient client, string path, object? data = null, HttpMethod? method = null)
    {
        var token = (await client.GetFromJsonAsync<JsonElement>("/api/admin/auth/csrf", TestContext.Current.CancellationToken)).GetProperty("token").GetString();
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = data is null ? null : JsonContent.Create(data) }; request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
    private async Task Login(HttpClient client) { using var response = await Mutate(client, "/api/admin/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
    private async Task<AdminQuotationDto> Create(HttpClient client, string language = "sv")
    {
        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(RequestExamples.Input("complete", language)) }; post.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        using var result = await client.SendAsync(post, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Created, result.StatusCode); var receipt = (await result.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken))!;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var id = await db.TravelRequests.Where(r => r.ReferenceNumber == receipt.ReferenceNumber).Select(r => r.Id).SingleAsync(TestContext.Current.CancellationToken); requests.Add(id);
        using var created = await Mutate(client, $"/api/admin/travel-requests/{id}/quotations"); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var quoteId = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid(); return await Detail(client, quoteId);
    }
    private static async Task<AdminQuotationDto> Detail(HttpClient client, Guid id) => (await client.GetFromJsonAsync<AdminQuotationDto>($"/api/admin/quotations/{id}", TestContext.Current.CancellationToken))!;
    private static QuotationDraftInput Input(AdminQuotationDto q, DateTimeOffset? until = null) => new(q.VersionToken, "TEST service <script>untrusted</script>", "TEST included", "TEST excluded", "TEST TERMS – not legal terms.", "TEST customer note", "SEK", "VatExclusive", until ?? DateTimeOffset.UtcNow.AddDays(14), [new("TEST service", 3, 1995, 2500)]);
    private async Task<AdminQuotationDto> Prepare(HttpClient client, string language = "sv", DateTimeOffset? until = null)
    {
        var q = await Create(client, language); using var saved = await Mutate(client, $"/api/admin/quotations/{q.Id}", Input(q, until), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode); return await Detail(client, q.Id);
    }
    private async Task<string> Send(HttpClient client, AdminQuotationDto q)
    {
        using var sent = await Mutate(client, $"/api/admin/quotations/{q.Id}/send", new { q.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.QuotationId == q.Id && n.Kind == "quote-issued", TestContext.Current.CancellationToken);
        var link = scope.ServiceProvider.GetRequiredService<QuotationTokenCodec>().ReadLink(job.ProtectedLink!); var raw = new Uri(link).Fragment[1..];
        Assert.Matches("^[A-Za-z0-9_-]{43}$", raw); Assert.DoesNotContain(raw, job.ProtectedLink!); var access = await db.QuotationTokens.SingleAsync(t => t.QuotationId == q.Id, TestContext.Current.CancellationToken); Assert.Equal(QuotationTokenCodec.Hash(raw), access.Hash); return raw;
    }
    private static async Task<HttpResponseMessage> Public(HttpClient client, string token, string? action = null, bool confirmed = true)
    {
        using var request = new HttpRequestMessage(action is null ? HttpMethod.Get : HttpMethod.Post, "/api/public/quotations" + (action is null ? "" : "/" + action));
        request.Headers.Add("X-Quotation-Token", token); if (action is not null) request.Content = JsonContent.Create(new { confirmed }); return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task AdministrativeEndpointsRequireAuthorizationAndPublicLinksDoNotUseReferences()
    {
        using var client = Client(); var id = Guid.NewGuid();
        foreach (var path in new[] { "/api/admin/quotations", "/api/admin/quotations/configuration", $"/api/admin/quotations/{id}", $"/api/admin/quotations/{id}/activity", $"/api/admin/travel-requests/{id}/quotations" })
        { using var response = await client.GetAsync(path, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); }
        foreach (var path in new[] { $"/api/admin/travel-requests/{id}/quotations", $"/api/admin/quotations/{id}/send", $"/api/admin/quotations/{id}/revise", $"/api/admin/quotations/{id}/cancel" })
        { using var response = await client.PostAsJsonAsync(path, new { }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); }
        using var patch = await client.PatchAsJsonAsync($"/api/admin/quotations/{id}", new { }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Unauthorized, patch.StatusCode);
        foreach (var token in new[] { "QUO-2026-000001", "not-a-token", new string('A', 43) }) { using var response = await Public(client, token); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); }
        await Login(client); using var noToken = await client.GetAsync("/api/public/quotations", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.NotFound, noToken.StatusCode);
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task IssuedQuotationIsImmutableAndAcceptanceIsIdempotentWithSafeNotifications(string language)
    {
        using var client = Client(); await Login(client); var q = await Prepare(client, language); Assert.Equal(7481, q.Quotation.TotalMinorUnits);
        using var stale = await Mutate(client, $"/api/admin/quotations/{q.Id}", Input(q) with { VersionToken = Guid.NewGuid() }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var csrf = await client.PostAsJsonAsync($"/api/admin/quotations/{q.Id}/send", new { q.VersionToken }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        var token = await Send(client, q); var issued = await Detail(client, q.Id); using var edited = await Mutate(client, $"/api/admin/quotations/{q.Id}", Input(issued), HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, edited.StatusCode);
        using var anon = Client(); using var review = await Public(anon, token); Assert.Equal(HttpStatusCode.OK, review.StatusCode); Assert.Contains("no-store", review.Headers.CacheControl!.ToString()); Assert.Equal("no-referrer", review.Headers.GetValues("Referrer-Policy").Single());
        var json = await review.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken); Assert.False(json.TryGetProperty("customerEmail", out _)); Assert.False(json.TryGetProperty("requestId", out _)); Assert.False(json.TryGetProperty("versionToken", out _));
        var sink = new RecordingSender(); await Drain(sink);
        var email = Assert.Single(sink.Messages, m => m.Subject.Contains(q.Quotation.Number)); Assert.Contains("/quote#" + token, email.Text); Assert.DoesNotContain("<script>", email.Html); Assert.Contains(language == "sv" ? "Din serviceoffert" : "Your service quotation", email.Subject);
        using var uncheckedResponse = await Public(anon, token, "accept", false); Assert.Equal(HttpStatusCode.BadRequest, uncheckedResponse.StatusCode);
        using var accepted = await Public(anon, token, "accept"); using var replay = await Public(anon, token, "accept"); Assert.Equal(HttpStatusCode.OK, accepted.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var receipt = await accepted.Content.ReadFromJsonAsync<CustomerQuotationDto>(TestContext.Current.CancellationToken); var again = await replay.Content.ReadFromJsonAsync<CustomerQuotationDto>(TestContext.Current.CancellationToken); Assert.Equal(receipt!.AcceptedAt, again!.AcceptedAt); Assert.Equal("Accepted", receipt.Status);
        using var opposite = await Public(anon, token, "decline"); Assert.Equal(HttpStatusCode.Conflict, opposite.StatusCode); await Drain(sink);
        Assert.Equal(3, sink.Messages.Count(m => m.Subject.Contains(q.Quotation.Number)));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(3, await db.NotificationOutbox.CountAsync(n => n.QuotationId == q.Id, TestContext.Current.CancellationToken));
        Assert.Single(await db.RequestActivities.Where(a => a.QuotationId == q.Id && a.Type == "QuotationAccepted").ToArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(TravelAgency.Domain.Requests.RequestStatus.Submitted, (await db.TravelRequests.FindAsync([q.RequestId], TestContext.Current.CancellationToken))!.Status);
        Assert.Equal("test-only-v1", (await db.Quotations.FindAsync([q.Id], TestContext.Current.CancellationToken))!.AcceptedTermsVersion);
    }
    private async Task Drain(RecordingSender sink)
    {
        for (var i = 0; i < 15; i++) { using var scope = factory.Services.CreateScope(); var processor = new OutboxProcessor(scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(), sink, new(), TimeProvider.System, quotationCodec: scope.ServiceProvider.GetRequiredService<QuotationTokenCodec>()); if (!await processor.ProcessOneAsync(TestContext.Current.CancellationToken)) break; }
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task RevisionsRetainHistoryAndRevokeOldLinksAndCancellationRevokesCurrentLink()
    {
        using var client = Client(); await Login(client); var original = await Prepare(client); var token = await Send(client, original); var issued = await Detail(client, original.Id);
        using var revision = await Mutate(client, $"/api/admin/quotations/{issued.Id}/revise", new { issued.VersionToken }); Assert.Equal(HttpStatusCode.Created, revision.StatusCode);
        var id = (await revision.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid(); var draft = await Detail(client, id);
        Assert.Equal(2, draft.Quotation.Version); Assert.Equal(issued.Quotation.Number, draft.Quotation.Number); var old = await Detail(client, issued.Id); Assert.Equal("Superseded", old.Quotation.Status); Assert.Equal(issued.Quotation.TermsSnapshot, old.Quotation.TermsSnapshot); Assert.Equal(issued.Quotation.TotalMinorUnits, old.Quotation.TotalMinorUnits);
        using var revoked = await Public(client, token, "accept"); Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
        var nextToken = await Send(client, draft); Assert.NotEqual(token, nextToken); var sent = await Detail(client, id);
        using var cancel = await Mutate(client, $"/api/admin/quotations/{id}/cancel", new { sent.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        using var cancelled = await Public(client, nextToken); Assert.Equal(HttpStatusCode.NotFound, cancelled.StatusCode);
        var history = await client.GetFromJsonAsync<TravelAgency.Application.Admin.PageResult<TravelAgency.Application.Admin.ActivityDto>>($"/api/admin/quotations/{id}/activity", TestContext.Current.CancellationToken); Assert.Contains(history!.Items, a => a.Type == "QuotationSuperseded"); Assert.Contains(history.Items, a => a.QuotationVersion == 2);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task DeclineIsIdempotentAndCannotBecomeAcceptance()
    {
        using var client = Client(); await Login(client); var q = await Prepare(client); var token = await Send(client, q);
        using var first = await Public(client, token, "decline"); using var replay = await Public(client, token, "decline"); Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var accept = await Public(client, token, "accept"); Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode); var detail = await Detail(client, q.Id); Assert.Equal("Declined", detail.Quotation.Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ConcurrentAcceptsCreateOneDecisionAndRevisionCannotRaceAcceptance()
    {
        using var client = Client(); await Login(client); var q = await Prepare(client); var token = await Send(client, q);
        var results = await Task.WhenAll(Public(client, token, "accept"), Public(client, token, "accept")); Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode)); foreach (var r in results) r.Dispose();
        var second = await Prepare(client); var secondToken = await Send(client, second); var sent = await Detail(client, second.Id);
        var acceptTask = Public(client, secondToken, "accept"); var revisionTask = Mutate(client, $"/api/admin/quotations/{sent.Id}/revise", new { sent.VersionToken }); await Task.WhenAll(acceptTask, revisionTask);
        using var accepted = await acceptTask; using var revised = await revisionTask;
        if (accepted.StatusCode == HttpStatusCode.OK) Assert.Equal(HttpStatusCode.Conflict, revised.StatusCode); else { Assert.Equal(HttpStatusCode.NotFound, accepted.StatusCode); Assert.Equal(HttpStatusCode.Created, revised.StatusCode); }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(1, await db.RequestActivities.CountAsync(a => a.QuotationId == q.Id && a.Type == "QuotationAccepted", TestContext.Current.CancellationToken));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ExpiryIsEnforcedSynchronouslyAndBackgroundProcessingIsIdempotent()
    {
        var clock = new MutableClock(); await using var timed = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(clock)));
        using var client = Client(timed); await Login(client); var q = await Prepare(client, until: clock.Now.AddMinutes(10)); var token = await Send(client, q); var background = await Prepare(client, until: clock.Now.AddMinutes(10)); await Send(client, background); clock.Now += TimeSpan.FromMinutes(11);
        using var accepted = await Public(client, token, "accept"); Assert.Equal(HttpStatusCode.Conflict, accepted.StatusCode);
        using var review = await Public(client, token); Assert.Equal("Expired", (await review.Content.ReadFromJsonAsync<CustomerQuotationDto>(TestContext.Current.CancellationToken))!.Status);
        using (var scope = timed.Services.CreateScope()) { await scope.ServiceProvider.GetRequiredService<IQuotationExpiration>().ExpireBatchAsync(TestContext.Current.CancellationToken); await scope.ServiceProvider.GetRequiredService<IQuotationExpiration>().ExpireBatchAsync(TestContext.Current.CancellationToken); }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(1, await db.RequestActivities.CountAsync(a => a.QuotationId == q.Id && a.Type == "QuotationExpired", TestContext.Current.CancellationToken)); Assert.Equal(QuotationStatus.Expired, (await db.Quotations.FindAsync([background.Id], TestContext.Current.CancellationToken))!.Status); }
        clock.Now += TimeSpan.FromDays(31); using var expiredToken = await Public(client, token); Assert.Equal(HttpStatusCode.NotFound, expiredToken.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task IssuanceRequiresApprovedBusinessConfigurationAndPublicAccessIsRateLimited()
    {
        using var client = Client(); await Login(client); var q = await Prepare(client);
        await using var blocked = factory.WithWebHostBuilder(b => b.UseSetting("Quotations:IssuanceEnabled", "false")); using var blockedClient = Client(blocked); await Login(blockedClient);
        using var send = await Mutate(blockedClient, $"/api/admin/quotations/{q.Id}/send", new { q.VersionToken }); Assert.Equal(HttpStatusCode.PreconditionFailed, send.StatusCode);
        await using var limited = factory.WithWebHostBuilder(b => b.UseSetting("Quotations:PublicPermitLimit", "1")); using var publicClient = Client(limited);
        using var first = await Public(publicClient, new string('A', 43)); Assert.Equal(HttpStatusCode.NotFound, first.StatusCode); using var second = await Public(publicClient, new string('A', 43)); Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode); Assert.True(second.Headers.Contains("Retry-After"));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task FailedSendRollsBackFrozenStateTokenActivityAndOutbox()
    {
        using var client = Client(); await Login(client); var q = await Prepare(client);
        await using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddDbContext<TravelAgencyDbContext>(o => o.AddInterceptors(new FailIssuedSave())))); using var failedClient = Client(failing); await Login(failedClient);
        using var response = await Mutate(failedClient, $"/api/admin/quotations/{q.Id}/send", new { q.VersionToken }); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(QuotationStatus.Draft, (await db.Quotations.FindAsync([q.Id], TestContext.Current.CancellationToken))!.Status);
        Assert.False(await db.RequestActivities.AnyAsync(a => a.QuotationId == q.Id && a.Type == "QuotationSent", TestContext.Current.CancellationToken));
        Assert.False(await db.QuotationTokens.AnyAsync(t => t.QuotationId == q.Id, TestContext.Current.CancellationToken)); Assert.False(await db.NotificationOutbox.AnyAsync(n => n.QuotationId == q.Id, TestContext.Current.CancellationToken));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task SearchPaginationAndConcurrentInitialCreationUseServerConstraints()
    {
        await using var scoped = factory.WithWebHostBuilder(b => b.UseSetting("Quotations:ReferencePrefix", "TST")); using var client = Client(scoped); await Login(client);
        var first = await Create(client); var second = await Create(client);
        var page = (await client.GetFromJsonAsync<TravelAgency.Application.Admin.PageResult<QuotationListItem>>("/api/admin/quotations?search=TST-&pageSize=1&sort=createdAt_asc", TestContext.Current.CancellationToken))!;
        Assert.Equal(2, page.TotalCount); Assert.Equal(first.Id, Assert.Single(page.Items).Id);
        var next = (await client.GetFromJsonAsync<TravelAgency.Application.Admin.PageResult<QuotationListItem>>("/api/admin/quotations?search=TST-&pageSize=1&page=2&sort=createdAt_asc", TestContext.Current.CancellationToken))!; Assert.Equal(second.Id, Assert.Single(next.Items).Id);
        var filtered = (await client.GetFromJsonAsync<TravelAgency.Application.Admin.PageResult<QuotationListItem>>("/api/admin/quotations?search=TST-&status=Accepted", TestContext.Current.CancellationToken))!; Assert.Empty(filtered.Items);
        using var invalid = await client.GetAsync("/api/admin/quotations?pageSize=101", TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var concurrent = await Task.WhenAll(Mutate(client, $"/api/admin/travel-requests/{first.RequestId}/quotations"), Mutate(client, $"/api/admin/travel-requests/{first.RequestId}/quotations")); Assert.All(concurrent, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode)); foreach (var r in concurrent) r.Dispose();
        Assert.NotEqual(first.Quotation.Number, second.Quotation.Number);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task NotificationFailureKeepsIssuedStateAndCiphertextAndRevokedDeliveryIsSuppressed()
    {
        using var client = Client(); await Login(client); var q = await Prepare(client); await Send(client, q); var sent = await Detail(client, q.Id);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
        var early = DateTimeOffset.Parse("2000-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await db.NotificationOutbox.Where(n => n.QuotationId == q.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), TestContext.Current.CancellationToken);
        var codec = scope.ServiceProvider.GetRequiredService<QuotationTokenCodec>();
        var failing = new OutboxProcessor(db, new FailingSender(), new(), TimeProvider.System, quotationCodec: codec); Assert.True(await failing.ProcessOneAsync(TestContext.Current.CancellationToken));
        var job = await db.NotificationOutbox.SingleAsync(n => n.QuotationId == q.Id, TestContext.Current.CancellationToken); Assert.Equal(1, job.Attempts); Assert.Null(job.SentAt); Assert.NotNull(job.ProtectedLink); Assert.Null(job.DeadLetterAt); Assert.Equal(QuotationStatus.Sent, (await db.Quotations.FindAsync([q.Id], TestContext.Current.CancellationToken))!.Status);
        using var cancelled = await Mutate(client, $"/api/admin/quotations/{q.Id}/cancel", new { sent.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        await db.NotificationOutbox.Where(n => n.QuotationId == q.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), TestContext.Current.CancellationToken); db.ChangeTracker.Clear();
        var sink = new RecordingSender(); Assert.True(await new OutboxProcessor(db, sink, new(), TimeProvider.System, quotationCodec: codec).ProcessOneAsync(TestContext.Current.CancellationToken)); Assert.Empty(sink.Messages);
        job = await db.NotificationOutbox.SingleAsync(n => n.QuotationId == q.Id, TestContext.Current.CancellationToken); Assert.Null(job.SentAt); Assert.NotNull(job.DeadLetterAt); Assert.Null(job.ProtectedLink);
    }
    private sealed class FailingSender : IEmailSender { public Task SendAsync(EmailMessage message, CancellationToken ct) => throw new EmailDeliveryException(true); }
    private sealed class MutableClock : TimeProvider { public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class RecordingSender : IEmailSender { public ConcurrentBag<EmailMessage> Messages { get; } = []; public Task SendAsync(EmailMessage message, CancellationToken ct) { Messages.Add(message); return Task.CompletedTask; } }
    private sealed class FailIssuedSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken ct = default) => data.Context!.ChangeTracker.Entries<Quotation>().Any(e => e.Entity.Status == QuotationStatus.Sent) ? throw new InvalidOperationException("Injected quotation transaction failure.") : ValueTask.FromResult(result);
    }
}
