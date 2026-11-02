using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    // Synthetic provider evidence is inserted only into the isolated test database; no app bypass exists.
    private async Task<Guid> ResearchPaidFixture(HttpClient c, string language = "sv", bool simulated = false)
    {
        var q = await Prepare(c, language); var raw = await Send(c, q); using var accepted = await Public(c, raw, "accept"); Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var quote = await db.Quotations.Include(x => x.Lines).SingleAsync(x => x.Id == q.Id, Ct); var now = DateTimeOffset.UtcNow;
        var invoice = Invoice.Create(quote, q.RequestId, q.Quotation.Number, simulated, now); invoice.Edit(new("TEST address", "TEST future service", now.AddDays(14)), now); invoice.Issue("FIX-" + Guid.NewGuid().ToString("N"), "TEST", "TEST", "TEST", "TEST", "TEST", "TEST", now); invoice.MarkPaid(invoice.TotalMinorUnits, invoice.Currency, now); db.Invoices.Add(invoice);
        var payment = new PaymentTransaction { InvoiceId = invoice.Id, Provider = simulated ? "Sandbox" : "IsolatedFixtureEvidence", ProviderSessionId = Guid.NewGuid().ToString("N"), AmountMinorUnits = invoice.TotalMinorUnits, Currency = invoice.Currency, CreatedAt = now, ExpiresAt = now.AddDays(1), NextReconcileAt = now.AddDays(1) }; payment.Apply(PaymentStatus.Succeeded, Guid.NewGuid().ToString("N"), now); db.Payments.Add(payment); db.ResearchEligibility.Add(new() { RequestId = q.RequestId, InvoiceId = invoice.Id, IsSimulated = simulated, VerifiedAt = now }); await db.SaveChangesAsync(Ct); return q.RequestId;
    }
    private async Task<Guid> Option(HttpClient c, Guid requestId, ResearchOptionInput input)
    {
        using var r = await Mutate(c, $"/api/admin/travel-requests/{requestId}/research/options", input); Assert.Equal(HttpStatusCode.Created, r.StatusCode); return (await r.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
    }
    private static async Task<AdminProposalDto> ProposalDetail(HttpClient c, Guid id) => (await c.GetFromJsonAsync<AdminProposalDto>($"/api/admin/proposals/{id}", Ct))!;
    private async Task<AdminProposalDto> ProposalDraft(HttpClient c, string language = "sv")
    {
        var id = await ResearchPaidFixture(c, language); using var start = await Mutate(c, $"/api/admin/travel-requests/{id}/research/start"); Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
        var flight = await Option(c, id, ResearchDomainTests.Flight()); var stay = await Option(c, id, ResearchDomainTests.Stay()); var extra = await Option(c, id, ResearchDomainTests.Extra("EUR"));
        using var created = await Mutate(c, $"/api/admin/travel-requests/{id}/proposals"); Assert.Equal(HttpStatusCode.Created, created.StatusCode); var proposalId = (await created.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid(); var p = await ProposalDetail(c, proposalId);
        using var saved = await Mutate(c, $"/api/admin/proposals/{proposalId}", new ProposalDraftInput(p.VersionToken, "TEST proposal <script>x</script>", "TEST intro", "TEST summary", "TEST proposal terms — NOT LEGAL.", DateTimeOffset.UtcNow.AddDays(14), [new("Option A", "TEST budget", true, [flight, stay]), new("Option B", "TEST extra", false, [flight, extra])]), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode); return await ProposalDetail(c, proposalId);
    }
    private async Task<string> SendProposal(HttpClient c, AdminProposalDto p)
    {
        using var r = await Mutate(c, $"/api/admin/proposals/{p.Id}/send", new { p.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, r.StatusCode); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.ProposalId == p.Id && n.Kind == "proposal-issued", Ct); var link = scope.ServiceProvider.GetRequiredService<ProposalTokenCodec>().Read(job.ProtectedLink!); var token = new Uri(link).Fragment[1..]; Assert.DoesNotContain(token, job.ProtectedLink!); Assert.Equal(ProposalTokenCodec.Hash(token), (await db.ProposalTokens.SingleAsync(t => t.ProposalId == p.Id, Ct)).Hash); return token;
    }
    private static async Task<HttpResponseMessage> ProposalPublic(HttpClient c, string token, string? action = null, ProposalDecisionInput? input = null)
    {
        using var r = new HttpRequestMessage(action is null ? HttpMethod.Get : HttpMethod.Post, "/api/public/travel-proposals" + (action is null ? "" : "/" + action)); r.Headers.Add("X-Proposal-Token", token); if (action is not null) r.Content = JsonContent.Create(input); return await c.SendAsync(r, Ct);
    }
    private static ProposalDecisionInput Approve(AdminProposalDto p, Guid? id = null) => new(p.Proposal.Version, true, id ?? p.Proposal.Alternatives[0].Id, "TEST-proposal-v1", null);
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ResearchAuthorizationCsrfAndActualPaymentEligibilityAreEnforced()
    {
        using var c = Client(); var id = Guid.NewGuid(); foreach (var path in new[] { "/api/admin/proposals", "/api/admin/proposals/configuration", $"/api/admin/travel-requests/{id}/research" }) { using var r = await c.GetAsync(path, Ct); Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); }
        await Login(c); var unpaid = await Prepare(c); using var denied = await Mutate(c, $"/api/admin/travel-requests/{unpaid.RequestId}/research/start"); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var simulated = await ResearchPaidFixture(c, simulated: true); using var noSimulation = await Mutate(c, $"/api/admin/travel-requests/{simulated}/research/start"); Assert.Equal(HttpStatusCode.Forbidden, noSimulation.StatusCode);
        var real = await ResearchPaidFixture(c); using var noCsrf = await c.PostAsync($"/api/admin/travel-requests/{real}/research/start", null, Ct); Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var started = await Mutate(c, $"/api/admin/travel-requests/{real}/research/start"); Assert.Equal(HttpStatusCode.NoContent, started.StatusCode);
        using var anotherStart = await Mutate(c, $"/api/admin/travel-requests/{real}/research/start"); Assert.Equal(HttpStatusCode.NoContent, anotherStart.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ResearchCrudNotesConcurrencyAndLinkedRemovalAreSafe()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c); var w = (await c.GetFromJsonAsync<ResearchWorkspaceDto>($"/api/admin/travel-requests/{p.RequestId}/research", Ct))!;
        using var notes = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research", new { w.VersionToken, notes = "SECRET workspace" }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, notes.StatusCode);
        using var stale = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research", new { w.VersionToken, notes = "stale" }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var o = w.Options[0]; using var edit = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research/options/{o.Id}", new { o.VersionToken, option = ResearchDomainTests.Flight() with { Title = "Updated flight" } }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
        using var staleSend = await Mutate(c, $"/api/admin/proposals/{p.Id}/send", new { p.VersionToken }); Assert.Equal(HttpStatusCode.Conflict, staleSend.StatusCode);
        using var linked = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research/options/{o.Id}/remove", new { o.VersionToken }); Assert.Equal(HttpStatusCode.Conflict, linked.StatusCode);
        var free = await Option(c, p.RequestId, ResearchDomainTests.Extra()); w = (await c.GetFromJsonAsync<ResearchWorkspaceDto>($"/api/admin/travel-requests/{p.RequestId}/research", Ct))!; var item = w.Options.Single(x => x.Id == free);
        using var removed = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research/options/{free}/remove", new { item.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProposalApprovalIsExplicitVersionScopedIdempotentAndPrivate()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c); var token = await SendProposal(c, p);
        using var review = await ProposalPublic(c, token); Assert.Equal(HttpStatusCode.OK, review.StatusCode); Assert.Contains("no-store", review.Headers.CacheControl!.ToString()); var text = await review.Content.ReadAsStringAsync(Ct); Assert.DoesNotContain("SECRET", text); Assert.DoesNotContain("customerEmail", text); Assert.DoesNotContain("requestId", text); Assert.DoesNotContain("versionToken", text); Assert.DoesNotContain("sourceVersion", text);
        using var missing = await ProposalPublic(c, token, "approve", Approve(p) with { AlternativeId = null }); Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        using var uncheckedApproval = await ProposalPublic(c, token, "approve", Approve(p) with { Confirmed = false }); Assert.Equal(HttpStatusCode.BadRequest, uncheckedApproval.StatusCode);
        using var version = await ProposalPublic(c, token, "approve", Approve(p) with { Version = 9 }); Assert.Equal(HttpStatusCode.BadRequest, version.StatusCode);
        var replies = await Task.WhenAll(ProposalPublic(c, token, "approve", Approve(p)), ProposalPublic(c, token, "approve", Approve(p))); Assert.All(replies, r => { Assert.Equal(HttpStatusCode.OK, r.StatusCode); r.Dispose(); });
        using var opposite = await ProposalPublic(c, token, "reject", new(1, true, null, null, "TEST")); Assert.Equal(HttpStatusCode.Conflict, opposite.StatusCode);
        var after = await ProposalDetail(c, p.Id); Assert.Equal("Approved", after.Proposal.Status); Assert.Equal(p.Proposal.Alternatives[0].Id, after.Proposal.SelectedAlternativeId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Single(await db.ProposalDecisions.Where(d => d.ProposalId == p.Id).ToArrayAsync(Ct)); Assert.Equal(3, await db.NotificationOutbox.CountAsync(n => n.ProposalId == p.Id, Ct)); Assert.Equal(TravelAgency.Domain.Requests.RequestStatus.Submitted, (await db.TravelRequests.SingleAsync(r => r.Id == p.RequestId, Ct)).Status);
        using var revoke = await Mutate(c, $"/api/admin/proposals/{p.Id}/revoke-access", new { after.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        using var unavailable = await ProposalPublic(c, token); Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode); Assert.Equal("Approved", (await ProposalDetail(c, p.Id)).Proposal.Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProposalSnapshotsRevisionCancellationAndExpiryPreserveHistory()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c, "en"); var token = await SendProposal(c, p); var sent = await ProposalDetail(c, p.Id); using var revision = await Mutate(c, $"/api/admin/proposals/{p.Id}/revise", new { sent.VersionToken }); Assert.Equal(HttpStatusCode.Created, revision.StatusCode); var id = (await revision.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid(); var next = await ProposalDetail(c, id); Assert.Equal(2, next.Proposal.Version); Assert.Equal(JsonSerializer.Serialize(p.Proposal.Alternatives[0].Items), JsonSerializer.Serialize(next.Proposal.Alternatives[0].Items));
        using var old = await ProposalPublic(c, token); Assert.Equal(HttpStatusCode.NotFound, old.StatusCode); using var cancelled = await Mutate(c, $"/api/admin/proposals/{id}/cancel", new { next.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        var fresh = await ProposalDraft(c); var raw = await SendProposal(c, fresh); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.TravelProposals.Where(x => x.Id == fresh.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ValidUntil, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct);
        using var expired = await ProposalPublic(c, raw, "approve", Approve(fresh)); Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode); Assert.Equal("Expired", (await ProposalDetail(c, fresh.Id)).Proposal.Status); await scope.ServiceProvider.GetRequiredService<ProposalMaintenance>().RunAsync(Ct); Assert.Single(await db.RequestActivities.Where(a => a.ProposalId == fresh.Id && a.Type == "ProposalExpired").ToArrayAsync(Ct));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CrossRequestItemsAndUnsafeSupplierLinksAreRejected()
    {
        using var c = Client(); await Login(c); var a = await ProposalDraft(c); var b = await ProposalDraft(c);
        using var cross = await Mutate(c, $"/api/admin/proposals/{a.Id}", new ProposalDraftInput(a.VersionToken, "TEST", "TEST", "TEST", "TEST", DateTimeOffset.UtcNow.AddDays(7), [new("TEST", "TEST", false, b.ItemIds[0])]), HttpMethod.Patch); Assert.Equal(HttpStatusCode.BadRequest, cross.StatusCode);
        using var editOther = await Mutate(c, $"/api/admin/travel-requests/{a.RequestId}/research/options/{b.ItemIds[0][0]}", new { versionToken = Guid.NewGuid(), option = ResearchDomainTests.Flight() }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NotFound, editOther.StatusCode);
        using var url = await Mutate(c, $"/api/admin/travel-requests/{a.RequestId}/research/options", ResearchDomainTests.Extra() with { SupplierUrl = "javascript:alert(1)" }); Assert.Equal(HttpStatusCode.BadRequest, url.StatusCode);
        using var invalid = await ProposalPublic(c, "not-a-token"); Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProposalApprovalAndRevisionRaceSerializesOneOutcome()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c); var token = await SendProposal(c, p); var sent = await ProposalDetail(c, p.Id);
        var responses = await Task.WhenAll(ProposalPublic(c, token, "approve", Approve(p)), Mutate(c, $"/api/admin/proposals/{p.Id}/revise", new { sent.VersionToken }));
        try { var current = await ProposalDetail(c, p.Id); if (current.Proposal.Status == "Approved") { Assert.Equal(HttpStatusCode.OK, responses[0].StatusCode); Assert.Equal(HttpStatusCode.Conflict, responses[1].StatusCode); } else { Assert.Equal("Superseded", current.Proposal.Status); Assert.Equal(HttpStatusCode.Created, responses[1].StatusCode); Assert.True(responses[0].StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict); } }
        finally { foreach (var r in responses) r.Dispose(); }
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task SentSourceEditsDoNotChangePublicSnapshotsAndHashExpiryIsEnforced()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c); var token = await SendProposal(c, p); var w = (await c.GetFromJsonAsync<ResearchWorkspaceDto>($"/api/admin/travel-requests/{p.RequestId}/research", Ct))!; var flight = w.Options.First(o => o.Option.Kind == "Flight");
        using var changed = await Mutate(c, $"/api/admin/travel-requests/{p.RequestId}/research/options/{flight.Id}", new { flight.VersionToken, option = ResearchDomainTests.Flight() with { PriceMinorUnits = 99999, Conditions = "CHANGED SOURCE" } }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        var snapshot = (await ProposalDetail(c, p.Id)).Proposal; Assert.Equal(JsonSerializer.Serialize(p.Proposal.Alternatives[0].Items), JsonSerializer.Serialize(snapshot.Alternatives[0].Items));
        using var fake = await ProposalPublic(c, new string('Z', 43)); Assert.Equal(HttpStatusCode.NotFound, fake.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.ProposalTokens.Where(t => t.ProposalId == p.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct);
        using var expired = await ProposalPublic(c, token); Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task ProposalRejectionEmailsEscapeContentAndReuseOutboxRetries(string language)
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c, language); var token = await SendProposal(c, p); using var rejected = await ProposalPublic(c, token, "reject", new(1, true, null, null, "TEST feedback")); Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        using var replay = await ProposalPublic(c, token, "reject", new(1, true, null, null, "Different feedback")); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var proposal = await db.TravelProposals.Include(x => x.Alternatives).SingleAsync(x => x.Id == p.Id, Ct); var codec = scope.ServiceProvider.GetRequiredService<ProposalTokenCodec>(); var jobs = await db.NotificationOutbox.Where(n => n.ProposalId == p.Id).ToArrayAsync(Ct); Assert.Equal(3, jobs.Length);
        foreach (var job in jobs.Where(j => j.Kind != "proposal-issued")) { var message = ProposalNotificationTemplates.Create(job, proposal, p.Proposal.Number, new(), codec, DateTimeOffset.UtcNow)!; Assert.Contains("&lt;script&gt;", message.Html); Assert.DoesNotContain("<script>", message.Html); Assert.Contains(language == "sv" ? "Reseförslag avböjt" : "Travel proposal rejected", message.Subject); Assert.DoesNotContain("SECRET", message.Text); }
        var early = DateTimeOffset.UtcNow.AddYears(-10); await db.NotificationOutbox.Where(n => n.ProposalId == p.Id && n.Kind != "proposal-issued").ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), Ct); db.ChangeTracker.Clear(); Assert.True(await new TravelAgency.Infrastructure.Email.OutboxProcessor(db, new FailingSender(), new(), TimeProvider.System, proposalCodec: codec).ProcessOneAsync(Ct)); Assert.True(await db.NotificationOutbox.AnyAsync(n => n.ProposalId == p.Id && n.Attempts == 1 && n.SentAt == null, Ct));
        Assert.Equal("TEST feedback", (await db.ProposalDecisions.SingleAsync(d => d.ProposalId == p.Id, Ct)).Feedback);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ProposalSendRollbackAndConfigurationGateLeaveNoAccessOrNotification()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c);
        using (var scope = factory.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<ProposalSettings>().SendingEnabled = false;
        using var blocked = await Mutate(c, $"/api/admin/proposals/{p.Id}/send", new { p.VersionToken }); Assert.Equal(HttpStatusCode.PreconditionFailed, blocked.StatusCode);
        using (var scope = factory.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<ProposalSettings>().SendingEnabled = true;
        using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddDbContext<TravelAgencyDbContext>(o => o.AddInterceptors(new FailProposalSave())))); using var bad = Client(failing); await Login(bad); using var error = await Mutate(bad, $"/api/admin/proposals/{p.Id}/send", new { p.VersionToken }); Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        using var final = factory.Services.CreateScope(); var db = final.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal("Draft", (await ProposalDetail(c, p.Id)).Proposal.Status); Assert.False(await db.ProposalTokens.AnyAsync(t => t.ProposalId == p.Id, Ct)); Assert.False(await db.NotificationOutbox.AnyAsync(n => n.ProposalId == p.Id, Ct));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task WorkerExpirationAndProposalListUseRealPersistedState()
    {
        using var c = Client(); await Login(c); var p = await ProposalDraft(c); await SendProposal(c, p); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.TravelProposals.Where(x => x.Id == p.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ValidUntil, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct); await scope.ServiceProvider.GetRequiredService<ProposalMaintenance>().RunAsync(Ct); await scope.ServiceProvider.GetRequiredService<ProposalMaintenance>().RunAsync(Ct);
        var page = (await c.GetFromJsonAsync<PageResult<ProposalListItem>>("/api/admin/proposals?status=Expired&search=" + p.Proposal.Number, Ct))!; Assert.Single(page.Items); Assert.Equal(p.Id, page.Items[0].Id); Assert.Single(await db.RequestActivities.Where(a => a.ProposalId == p.Id && a.Type == "ProposalExpired").ToArrayAsync(Ct));
        using var invalid = await c.GetAsync("/api/admin/proposals?status=invalid", Ct); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PublicProposalRateLimitReturnsSafeRetryWithoutPrivateData()
    {
        using var c = Client();
        for (var n = 0; n < 60; n++) { using var r = await ProposalPublic(c, "malformed-secret"); Assert.Equal(HttpStatusCode.NotFound, r.StatusCode); }
        using var limited = await ProposalPublic(c, "malformed-secret"); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter); Assert.DoesNotContain("malformed-secret", await limited.Content.ReadAsStringAsync(Ct));
    }
    private sealed class FailProposalSave : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesCompletedEventData data, int result, CancellationToken ct = default) => data.Context!.ChangeTracker.Entries<TravelProposal>().Any(e => e.Entity.Status == ProposalStatus.Sent) ? throw new InvalidOperationException("Injected proposal rollback.") : ValueTask.FromResult(result);
    }
}
