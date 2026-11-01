using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private async Task<AdminInvoiceView> InvoiceDraft(HttpClient client, string language = "sv")
    {
        var q = await Prepare(client, language); var raw = await Send(client, q); using var accepted = await Public(client, raw, "accept"); Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var created = await Mutate(client, $"/api/admin/quotations/{q.Id}/invoice"); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid(); return await InvoiceDetail(client, id);
    }
    private static async Task<AdminInvoiceView> InvoiceDetail(HttpClient client, Guid id) => (await client.GetFromJsonAsync<AdminInvoiceView>($"/api/admin/invoices/{id}", Ct))!;
    private async Task<(AdminInvoiceView Invoice, string Token)> IssueInvoice(HttpClient client, string language = "sv")
    {
        var d = await InvoiceDraft(client, language);
        using var saved = await Mutate(client, $"/api/admin/invoices/{d.Id}", new { d.VersionToken, customerAddress = "TEST billing address <script>x</script>", servicePeriod = "TEST agreed future service", dueAt = DateTimeOffset.UtcNow.AddDays(14) }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        d = await InvoiceDetail(client, d.Id); using var issued = await Mutate(client, $"/api/admin/invoices/{d.Id}/issue", new { d.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, issued.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.InvoiceId == d.Id && n.Kind == "invoice-issued", Ct);
        var token = new Uri(scope.ServiceProvider.GetRequiredService<InvoiceTokenCodec>().Read(job.ProtectedLink!)).Fragment[1..];
        Assert.DoesNotContain(token, job.ProtectedLink!); Assert.Equal(InvoiceTokenCodec.Hash(token), (await db.InvoiceTokens.SingleAsync(t => t.InvoiceId == d.Id, Ct)).Hash);
        return (await InvoiceDetail(client, d.Id), token);
    }
    private static async Task<HttpResponseMessage> InvoicePublic(HttpClient client, string token, string? action = null)
    {
        using var r = new HttpRequestMessage(action is null ? HttpMethod.Get : HttpMethod.Post, "/api/public/invoices" + (action is null ? "" : "/" + action)); r.Headers.Add("X-Invoice-Token", token); if (action is not null) r.Content = JsonContent.Create(new { }); return await client.SendAsync(r, Ct);
    }
    private static async Task<PaymentSessionResult> Session(HttpClient client, string token)
    {
        using var response = await InvoicePublic(client, token, "payment-session"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<PaymentSessionResult>(Ct))!;
    }
    private PaymentWebhookRequest Signed(VerifiedPaymentEvent evidence)
    {
        using var scope = factory.Services.CreateScope(); return scope.ServiceProvider.GetRequiredService<SandboxPaymentProvider>().SignForSandbox(evidence);
    }
    private static async Task<HttpResponseMessage> Webhook(HttpClient client, PaymentWebhookRequest signed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/Sandbox") { Content = new StringContent(signed.Body) }; request.Headers.Add("X-Payment-Signature", signed.Signature); request.Headers.Add("X-Payment-Timestamp", signed.Timestamp); return await client.SendAsync(request, Ct);
    }
    private static VerifiedPaymentEvent Evidence(AdminInvoiceView i, PaymentSessionResult session, PaymentStatus status = PaymentStatus.Succeeded) => new(Guid.NewGuid().ToString("N"), session.SessionId, "test-payment-" + session.SessionId, i.Id, i.Invoice.TotalMinorUnits, i.Invoice.Currency, status, "development-simulator");
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PaymentAuthorizationCsrfDraftAcceptanceAndDuplicateInvariants()
    {
        using var c = Client(); var id = Guid.NewGuid(); foreach (var path in new[] { "/api/admin/invoices", "/api/admin/invoices/configuration", "/api/admin/payments/summary", $"/api/admin/invoices/{id}", $"/api/admin/invoices/{id}/payments", $"/api/admin/invoices/{id}/document" }) { using var r = await c.GetAsync(path, Ct); Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); }
        await Login(c); var q = await Prepare(c); using var rejected = await Mutate(c, $"/api/admin/quotations/{q.Id}/invoice"); Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        var d = await InvoiceDraft(c); using var noCsrf = await c.PostAsJsonAsync($"/api/admin/invoices/{d.Id}/issue", new { d.VersionToken }, Ct); Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        var creates = await Task.WhenAll(Mutate(c, $"/api/admin/quotations/{d.QuotationId}/invoice"), Mutate(c, $"/api/admin/quotations/{d.QuotationId}/invoice")); Assert.All(creates, r => { Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); r.Dispose(); });
        using var badToken = await InvoicePublic(c, d.Invoice.QuotationNumber); Assert.Equal(HttpStatusCode.NotFound, badToken.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task InvoiceIssuanceFreezesDetailsAndHasUniqueNumbersAndScopedAccess()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var (other, _) = await IssueInvoice(c, "en"); Assert.NotEqual(i.Invoice.Number, other.Invoice.Number); Assert.StartsWith("TEST-INV-", i.Invoice.Number);
        using var review = await InvoicePublic(c, token); Assert.Equal(HttpStatusCode.OK, review.StatusCode); Assert.Contains("no-store", review.Headers.CacheControl!.ToString());
        var json = await review.Content.ReadAsStringAsync(Ct); Assert.DoesNotContain("customerEmail", json); Assert.DoesNotContain("versionToken", json); Assert.DoesNotContain("requestId", json); Assert.DoesNotContain(token, json);
        using var asQuote = await Public(c, token); Assert.Equal(HttpStatusCode.NotFound, asQuote.StatusCode);
        using var edit = await Mutate(c, $"/api/admin/invoices/{i.Id}", new { i.VersionToken, customerAddress = "changed", servicePeriod = "changed", dueAt = DateTimeOffset.UtcNow.AddDays(20) }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.BadRequest, edit.StatusCode);
        using var doc = await c.GetAsync($"/api/admin/invoices/{i.Id}/document", Ct); Assert.Equal(HttpStatusCode.OK, doc.StatusCode); var html = await doc.Content.ReadAsStringAsync(Ct); Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>", html); Assert.Contains(i.Invoice.Number!, html);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); using var revoke = await Mutate(c, $"/api/admin/invoices/{i.Id}/revoke-access", new { i.VersionToken }); Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.False((await InvoiceDetail(c, i.Id)).CustomerLinkActive);
        using var revoked = await InvoicePublic(c, token); Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PaymentSessionReuseAndConcurrentVerifiedDeliveryAreIdempotent()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var sessions = await Task.WhenAll(Session(c, token), Session(c, token)); Assert.Equal(sessions[0].SessionId, sessions[1].SessionId); Assert.DoesNotContain(token, sessions[0].CheckoutUrl);
        var signed = Signed(Evidence(i, sessions[0])); var responses = await Task.WhenAll(Webhook(c, signed), Webhook(c, signed), Webhook(c, signed)); Assert.All(responses, r => { Assert.Equal(HttpStatusCode.NoContent, r.StatusCode); r.Dispose(); });
        var detail = await InvoiceDetail(c, i.Id); Assert.Equal("Paid", detail.Invoice.Status); Assert.Single(detail.Payments); Assert.False(detail.ResearchEligible); Assert.True(detail.SandboxResearchEligible);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Single(await db.PaymentEvents.Where(e => e.InvoiceId == i.Id).ToArrayAsync(Ct)); Assert.Single(await db.ResearchEligibility.Where(e => e.InvoiceId == i.Id).ToArrayAsync(Ct)); Assert.Equal(2, await db.NotificationOutbox.CountAsync(n => n.InvoiceId == i.Id && n.Kind.StartsWith("payment-"), Ct));
        using var retry = await InvoicePublic(c, token, "payment-session"); Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(TravelAgency.Domain.Requests.RequestStatus.Submitted, (await db.TravelRequests.SingleAsync(r => r.Id == i.RequestId, Ct)).Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task WebhookRejectsInvalidSignaturesTimestampMerchantAmountCurrencyAndUnknownReferences()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var session = await Session(c, token); var e = Evidence(i, session); var valid = Signed(e);
        foreach (var invalid in new[] { valid with { Signature = "00" }, valid with { Timestamp = "1" }, valid with { Body = valid.Body + " " } }) { using var r = await Webhook(c, invalid); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode); }
        using var merchant = await Webhook(c, Signed(e with { Merchant = "wrong-account" })); Assert.Equal(HttpStatusCode.BadRequest, merchant.StatusCode);
        using var amount = await Webhook(c, Signed(e with { EventId = "amount-" + e.EventId, AmountMinorUnits = e.AmountMinorUnits + 1 })); Assert.Equal(HttpStatusCode.Conflict, amount.StatusCode);
        using var currency = await Webhook(c, Signed(e with { EventId = "currency-" + e.EventId, Currency = "EUR" })); Assert.Equal(HttpStatusCode.Conflict, currency.StatusCode);
        using var reference = await Webhook(c, Signed(e with { EventId = "reference-" + e.EventId, SessionId = "unknown" })); Assert.Equal(HttpStatusCode.Conflict, reference.StatusCode);
        using var unknown = await Webhook(c, Signed(e with { EventId = "unknown-" + e.EventId, InvoiceId = Guid.NewGuid() })); Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.NotEqual("Paid", (await InvoiceDetail(c, i.Id)).Invoice.Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task DelayedFailureCannotUndoSuccessAndRetryUsesNewSession()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var session = await Session(c, token);
        using var failed = await Webhook(c, Signed(Evidence(i, session, PaymentStatus.Failed))); Assert.Equal(HttpStatusCode.NoContent, failed.StatusCode);
        var next = await Session(c, token); Assert.NotEqual(session.SessionId, next.SessionId);
        using var success = await Webhook(c, Signed(Evidence(i, next))); Assert.Equal(HttpStatusCode.NoContent, success.StatusCode);
        using var delayed = await Webhook(c, Signed(Evidence(i, next, PaymentStatus.Cancelled))); Assert.Equal(HttpStatusCode.NoContent, delayed.StatusCode);
        Assert.Equal("Paid", (await InvoiceDetail(c, i.Id)).Invoice.Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MissedWebhookIsRecoveredFromProviderEvidenceAndOverdueCanBeSettled()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var session = await Session(c, token);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.SandboxPayments.Where(s => s.SessionId == session.SessionId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PaymentStatus.Succeeded), Ct);
            await db.Payments.Where(p => p.InvoiceId == i.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.NextReconcileAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Ct);
            await db.Invoices.Where(x => x.Id == i.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DueAt, DateTimeOffset.UtcNow.AddDays(-1)), Ct);
            await scope.ServiceProvider.GetRequiredService<PaymentMaintenance>().RunAsync(Ct);
        }
        var detail = await InvoiceDetail(c, i.Id); Assert.Equal("Paid", detail.Invoice.Status); Assert.Equal(1, detail.Payments[0].ReconcileAttempts);
        using var another = factory.Services.CreateScope(); var database = another.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Single(await database.RequestActivities.Where(a => a.RequestId == i.RequestId && a.Type == "InvoiceOverdue").ToArrayAsync(Ct));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PaymentTransactionRollbackLeavesNoPaidRecordEligibilityOrNotifications()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var session = await Session(c, token);
        using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddDbContext<TravelAgencyDbContext>(o => o.AddInterceptors(new FailPaymentSave()))));
        using var broken = Client(failing); using var r = await Webhook(broken, Signed(Evidence(i, session))); Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.NotEqual("Paid", (await InvoiceDetail(c, i.Id)).Invoice.Status);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.False(await db.ResearchEligibility.AnyAsync(e => e.InvoiceId == i.Id, Ct)); Assert.False(await db.PaymentEvents.AnyAsync(e => e.InvoiceId == i.Id, Ct)); Assert.False(await db.NotificationOutbox.AnyAsync(e => e.InvoiceId == i.Id && e.Kind == "payment-confirmed", Ct));
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task InvoiceAndPaymentEmailsReuseOutboxEscapeContentAndRetry(string language)
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c, language); var session = await Session(c, token); using var paid = await Webhook(c, Signed(Evidence(i, session))); Assert.Equal(HttpStatusCode.NoContent, paid.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var codec = scope.ServiceProvider.GetRequiredService<InvoiceTokenCodec>(); var settings = new EmailSettings();
        var jobs = await db.NotificationOutbox.Where(n => n.InvoiceId == i.Id).ToArrayAsync(Ct); Assert.Equal(3, jobs.Length);
        var invoice = await db.Invoices.SingleAsync(x => x.Id == i.Id, Ct);
        foreach (var job in jobs) { var message = InvoiceNotificationTemplates.Create(job, invoice, settings, codec)!; Assert.StartsWith("[TEST]", message.Subject); Assert.DoesNotContain("<script>", message.Html); Assert.Contains("&lt;script&gt;", message.Html); Assert.Contains("TEST / SIMULATED", message.Text); }
        var early = DateTimeOffset.UtcNow.AddYears(-10); await db.NotificationOutbox.Where(n => n.InvoiceId == i.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), Ct); db.ChangeTracker.Clear();
        Assert.True(await new OutboxProcessor(db, new FailingSender(), settings, TimeProvider.System, invoiceCodec: codec).ProcessOneAsync(Ct)); Assert.True(await db.NotificationOutbox.AnyAsync(n => n.InvoiceId == i.Id && n.Attempts == 1 && n.SentAt == null, Ct));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task InvoiceGatesAndProviderOutageDoNotCreateFinancialSideEffects()
    {
        using var c = Client(); await Login(c); var draft = await InvoiceDraft(c);
        using (var scope = factory.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<PaymentSettings>().AccountingApproved = false;
        using var gated = await Mutate(c, $"/api/admin/invoices/{draft.Id}/issue", new { draft.VersionToken }); Assert.Equal(HttpStatusCode.PreconditionFailed, gated.StatusCode);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.False(await db.InvoiceTokens.AnyAsync(t => t.InvoiceId == draft.Id, Ct)); Assert.False(await db.NotificationOutbox.AnyAsync(n => n.InvoiceId == draft.Id, Ct)); Assert.Null((await db.Invoices.SingleAsync(i => i.Id == draft.Id, Ct)).Number); }
        using (var scope = factory.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<PaymentSettings>().AccountingApproved = true;
        var (issued, token) = await IssueInvoice(c);
        using var outage = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services => services.AddScoped<IPaymentProvider, UnavailablePaymentProvider>()));
        using var unavailable = Client(outage); using var response = await InvoicePublic(unavailable, token, "payment-session"); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Issued", (await InvoiceDetail(c, issued.Id)).Invoice.Status); Assert.Empty((await InvoiceDetail(c, issued.Id)).Payments);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task PendingCancelledExpiredTokensAndBackendMetricsRemainTruthful()
    {
        using var c = Client(); await Login(c); var (i, token) = await IssueInvoice(c); var session = await Session(c, token);
        using var pending = await Webhook(c, Signed(Evidence(i, session, PaymentStatus.Pending))); Assert.Equal(HttpStatusCode.NoContent, pending.StatusCode);
        Assert.Equal("PaymentPending", (await InvoiceDetail(c, i.Id)).Invoice.Status);
        using var cancelled = await Webhook(c, Signed(Evidence(i, session, PaymentStatus.Cancelled))); Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.NotEqual("Paid", (await InvoiceDetail(c, i.Id)).Invoice.Status);
        var summary = (await c.GetFromJsonAsync<PaymentSummary>("/api/admin/payments/summary", Ct))!; Assert.Contains(summary.Currencies, x => x.Currency == "SEK" && x.IsSimulated && x.OutstandingMinorUnits >= i.Invoice.TotalMinorUnits && x.PaidMinorUnits == 0);
        var list = (await c.GetFromJsonAsync<InvoicePage>("/api/admin/invoices?status=PaymentPending&search=" + i.Invoice.Number, Ct))!; Assert.Single(list.Items); Assert.Equal(i.Id, list.Items[0].Id);
        using var invalid = await c.GetAsync("/api/admin/invoices?status=not-a-status", Ct); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.InvoiceTokens.Where(t => t.InvoiceId == i.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct);
        using var expired = await InvoicePublic(c, token); Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task IssuanceRollbackDoesNotConsumeInvoiceNumberOrQueueEmail()
    {
        using var c = Client(); await Login(c); var d = await InvoiceDraft(c);
        using var edited = await Mutate(c, $"/api/admin/invoices/{d.Id}", new { d.VersionToken, customerAddress = "TEST", servicePeriod = "TEST", dueAt = DateTimeOffset.UtcNow.AddDays(7) }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode); d = await InvoiceDetail(c, d.Id);
        long counter; using (var scope = factory.Services.CreateScope()) counter = (await scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>().InvoiceCounters.SingleAsync(Ct)).Value;
        using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddDbContext<TravelAgencyDbContext>(o => o.AddInterceptors(new FailInvoiceIssuance()))));
        using var bad = Client(failing); await Login(bad); using var response = await Mutate(bad, $"/api/admin/invoices/{d.Id}/issue", new { d.VersionToken }); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var final = factory.Services.CreateScope(); var db = final.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(counter, (await db.InvoiceCounters.SingleAsync(Ct)).Value); Assert.Equal("Draft", (await InvoiceDetail(c, d.Id)).Invoice.Status); Assert.False(await db.NotificationOutbox.AnyAsync(n => n.InvoiceId == d.Id, Ct)); Assert.False(await db.InvoiceTokens.AnyAsync(t => t.InvoiceId == d.Id, Ct));
    }
    [Fact]
    public void DevelopmentPaymentSimulatorCannotRunInProduction()
    {
        using var production = new ApiFactory(environment: "Production"); using var blocked = production.WithWebHostBuilder(b => { b.UseSetting("Payments:Provider", "Sandbox"); b.UseSetting("Payments:SandboxSigningSecret", "TEST-only-secret-with-at-least-32-characters"); });
        Assert.Throws<InvalidOperationException>(() => blocked.CreateClient());
    }
    private sealed class FailInvoiceIssuance : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken ct = default) => data.Context!.ChangeTracker.Entries<Invoice>().Any(e => e.Entity.Status == InvoiceStatus.Issued) ? throw new InvalidOperationException("Injected issuance rollback.") : ValueTask.FromResult(result);
    }
    private sealed class UnavailablePaymentProvider : IPaymentProvider
    {
        public string Name => "Sandbox"; public bool IsSimulated => true;
        public Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken ct) => throw new PaymentProviderException();
        public VerifiedPaymentEvent VerifyWebhook(PaymentWebhookRequest request) => throw new PaymentProviderException();
        public Task<VerifiedPaymentEvent?> ReconcileAsync(string sessionId, CancellationToken ct) => throw new PaymentProviderException();
    }
    private sealed class FailPaymentSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken ct = default) => data.Context!.ChangeTracker.Entries<Invoice>().Any(e => e.Entity.Status == InvoiceStatus.Paid) ? throw new InvalidOperationException("Injected payment failure.") : ValueTask.FromResult(result);
    }
}
