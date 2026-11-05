using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Customers;
using TravelAgency.Application.Quotations;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task OwnedQuotationUsesExistingDecisionEngineAndPreservesGuestReview()
    {
        using var admin = Client(); await Login(admin); var email = CustomerEmail(); var q = await Prepare(admin, contactEmail: email); var guest = await Send(admin, q);
        using var c = Client(); var u = await RegisterCustomer(c, email); await CustomerLogin(c, email); await ClaimCustomerRequest(c, u.Id, q.RequestId);
        using var uncheckedReply = await CustomerMutate(c, $"/quotations/{q.Id}/accept", new { confirmed = false }); Assert.Equal(HttpStatusCode.BadRequest, uncheckedReply.StatusCode);
        using var accepted = await CustomerMutate(c, $"/quotations/{q.Id}/accept", new { confirmed = true }); Assert.Equal(HttpStatusCode.OK, accepted.StatusCode); Assert.Equal("Accepted", (await accepted.Content.ReadFromJsonAsync<CustomerQuotationDto>(Ct))!.Status);
        using var replay = await CustomerMutate(c, $"/quotations/{q.Id}/accept", new { confirmed = true }); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var opposite = await CustomerMutate(c, $"/quotations/{q.Id}/decline", new { confirmed = true }); Assert.Equal(HttpStatusCode.Conflict, opposite.StatusCode);
        using var guestReview = await Public(c, guest); Assert.Equal("Accepted", (await guestReview.Content.ReadFromJsonAsync<CustomerQuotationDto>(Ct))!.Status);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task OwnedProposalApprovalAndPartialBookingNeverExposePrivateDraftChanges()
    {
        using var admin = Client(); await Login(admin); var email = CustomerEmail(); var p = await ProposalDraft(admin, contactEmail: email); await SendProposal(admin, p);
        using var c = Client(); var u = await RegisterCustomer(c, email); await CustomerLogin(c, email); await ClaimCustomerRequest(c, u.Id, p.RequestId);
        using var missing = await CustomerMutate(c, $"/proposals/{p.Id}/approve", Approve(p) with { Confirmed = false }); Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using var approved = await CustomerMutate(c, $"/proposals/{p.Id}/approve", Approve(p)); Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var invoice = await db.Invoices.SingleAsync(i => i.RequestId == p.RequestId, Ct);
        using var document = await c.GetAsync($"/api/customer/invoices/{invoice.Id}/document", Ct); Assert.Equal(HttpStatusCode.OK, document.StatusCode); Assert.Contains("text/html", document.Content.Headers.ContentType!.ToString());
        using var created = await Mutate(admin, $"/api/admin/proposals/{p.Id}/bookings", new TravelAgency.Application.Bookings.CreateBookingInput(BookingDomainTests.Evidence())); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Ct)).GetProperty("id").GetGuid(); var b = await BookingDetail(admin, id); var first = b.Reservations[0].Id;
        b = await SaveReservation(admin, b, first); b = await BookingAction(admin, b, first, "start"); b = await BookingAction(admin, b, first, "confirm");
        var own = await c.GetFromJsonAsync<CustomerBookingDto>($"/api/customer/bookings/{id}", Ct); Assert.Equal("PartiallyConfirmed", own!.Status); Assert.Single(own.Reservations, r => r.ConfirmedDetails is not null); Assert.Single(own.Reservations, r => r.ConfirmedDetails is null);
        var timeline = await c.GetStringAsync($"/api/customer/activity?requestId={p.RequestId}", Ct); Assert.Contains("BookingPartiallyConfirmed", timeline); Assert.DoesNotContain("SnapshotJson", timeline); Assert.DoesNotContain("TEST isolated supplier confirmation email", timeline);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task OwnedInvoiceCheckoutUsesAccountReturnAndCannotFakeSuccessfulPayment()
    {
        using var admin = Client(); await Login(admin); var (invoice, _) = await IssueInvoice(admin, "en");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var request = await db.TravelRequests.SingleAsync(r => r.Id == invoice.RequestId, Ct);
        using var c = Client(); var u = await RegisterCustomer(c, request.Contact.Email); await CustomerLogin(c, u.Email); await ClaimCustomerRequest(c, u.Id, request.Id);
        using var first = await CustomerMutate(c, $"/invoices/{invoice.Id}/payment-session"); Assert.Equal(HttpStatusCode.OK, first.StatusCode); var a = (await first.Content.ReadFromJsonAsync<TravelAgency.Application.Payments.PaymentSessionResult>(Ct))!;
        using var again = await CustomerMutate(c, $"/invoices/{invoice.Id}/payment-session"); Assert.Equal(HttpStatusCode.OK, again.StatusCode); var b = (await again.Content.ReadFromJsonAsync<TravelAgency.Application.Payments.PaymentSessionResult>(Ct))!; Assert.Equal(a.SessionId, b.SessionId);
        Assert.Equal("http://localhost:5174/account/invoices/" + invoice.Id, scope.ServiceProvider.GetRequiredService<TravelAgency.Infrastructure.Payments.InvoiceTokenCodec>().Read((await db.SandboxPayments.SingleAsync(p => p.SessionId == a.SessionId, Ct)).ProtectedReturnUrl));
        using var current = await c.GetAsync($"/api/customer/invoices/{invoice.Id}", Ct); Assert.Equal(HttpStatusCode.OK, current.StatusCode); Assert.Contains("PaymentPending", await current.Content.ReadAsStringAsync(Ct)); Assert.False(await db.ResearchEligibility.AnyAsync(e => e.RequestId == request.Id, Ct));
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task AccountMailIsLocalizedEscapedRetriedAndRevokedLinksAreSuppressed(string language)
    {
        using var c = Client(); var u = await RegisterCustomer(c, verify: false); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.CustomerId == u.Id, Ct);
        job.PayloadJson = System.Text.Json.JsonSerializer.Serialize(new AccountMailSnapshot(u.Email, "TEST <script>", language)); await db.SaveChangesAsync(Ct);
        var codec = scope.ServiceProvider.GetRequiredService<CustomerTokenCodec>(); var message = await CustomerNotificationTemplates.CreateAsync(job, db, codec, new(), DateTimeOffset.UtcNow, Ct); Assert.NotNull(message); Assert.Contains("&lt;script&gt;", message.Html); Assert.DoesNotContain("<script>", message.Html); Assert.Contains(language == "sv" ? "Verifiera" : "Verify", message.Subject);
        await db.NotificationOutbox.Where(n => n.Id == job.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, DateTimeOffset.UtcNow.AddYears(-10)), Ct); db.ChangeTracker.Clear();
        Assert.True(await new OutboxProcessor(db, new FailingSender(), new(), TimeProvider.System, customerCodec: codec).ProcessOneAsync(Ct)); Assert.True(await db.NotificationOutbox.AnyAsync(n => n.Id == job.Id && n.Attempts == 1 && n.SentAt == null, Ct));
        await db.CustomerActions.Where(a => a.CustomerId == u.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.RevokedAt, DateTimeOffset.UtcNow), Ct); await db.NotificationOutbox.Where(n => n.Id == job.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, DateTimeOffset.UtcNow.AddYears(-10)), Ct); db.ChangeTracker.Clear(); var sink = new RecordingSender();
        Assert.True(await new OutboxProcessor(db, sink, new(), TimeProvider.System, customerCodec: codec).ProcessOneAsync(Ct)); Assert.Empty(sink.Messages);
    }
}
