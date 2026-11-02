using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Bookings;
using TravelAgency.Domain.Bookings;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private async Task<BookingDto> BookingDetail(HttpClient c, Guid id) => (await c.GetFromJsonAsync<BookingDto>($"/api/admin/bookings/{id}", Ct))!;
    private async Task<BookingDto> ApprovedBooking(HttpClient c, string language = "sv")
    {
        var p = await ProposalDraft(c, language); var token = await SendProposal(c, p); using var approved = await ProposalPublic(c, token, "approve", Approve(p)); Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var created = await Mutate(c, $"/api/admin/proposals/{p.Id}/bookings", new CreateBookingInput(BookingDomainTests.Evidence())); Assert.Equal(HttpStatusCode.Created, created.StatusCode); var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid(); return await BookingDetail(c, id);
    }
    private async Task<BookingDto> SaveReservation(HttpClient c, BookingDto b, Guid rid, long? increase = null)
    {
        var r = b.Reservations.Single(r => r.Id == rid); var now = DateTimeOffset.UtcNow; var s = r.Current;
        var input = new Domain.Research.ResearchOptionInput(s.Kind, s.Title, s.Description, s.PriceMinorUnits + (increase ?? 0), s.Currency, s.PriceBasis, s.Quantity, s.SupplierName, s.SupplierUrl, now, now, s.Conditions, "", s.Flight, s.Accommodation, s.Extra);
        using var saved = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/reservations/{rid}", new ReservationUpdateInput(b.VersionToken, input, "CustomerPaysSupplier", "NotPaid", "TEST explicit customer direct payment"), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode); return await BookingDetail(c, b.Booking.Id);
    }
    private async Task<BookingDto> BookingAction(HttpClient c, BookingDto b, Guid rid, string action, object? extra = null)
    {
        object payload = action switch { "confirm" => new ConfirmationInput(b.VersionToken, "TEST-GENUINE-FIXTURE-REF", "TEST isolated supplier confirmation email", "TEST mail reference", b.Reservations.Single(r => r.Id == rid).Current.Kind == "Flight" ? "Issued" : "NotApplicable", DateTimeOffset.UtcNow), "fail" => new FailureInput(b.VersionToken, "TEST supplier unavailable"), "cancel" => new CancellationInput(b.VersionToken, "TEST customer cancellation", "TEST supplier confirmed cancellation", 100, DateTimeOffset.UtcNow), "reapprove" => new ReapprovalInput(b.VersionToken, BookingDomainTests.Evidence()), _ => new BookingVersionInput(b.VersionToken) };
        using var changed = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/reservations/{rid}/{action}", extra ?? payload); Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode); return await BookingDetail(c, b.Booking.Id);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task BookingAuthorizationCsrfAndApprovedPaidAssociationAreEnforced()
    {
        using var c = Client(); using var denied = await c.GetAsync("/api/admin/bookings", Ct); Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await Login(c); var draft = await ProposalDraft(c); using var noApproval = await Mutate(c, $"/api/admin/proposals/{draft.Id}/bookings", new CreateBookingInput(BookingDomainTests.Evidence())); Assert.Equal(HttpStatusCode.Conflict, noApproval.StatusCode);
        var token = await SendProposal(c, draft); using var approved = await ProposalPublic(c, token, "approve", Approve(draft)); Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var csrf = await c.PostAsJsonAsync($"/api/admin/proposals/{draft.Id}/bookings", new CreateBookingInput(BookingDomainTests.Evidence()), Ct); Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var missing = await Mutate(c, $"/api/admin/proposals/{draft.Id}/bookings", new CreateBookingInput(BookingDomainTests.Evidence() with { Confirmed = false })); Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.ResearchEligibility.Where(e => e.RequestId == draft.RequestId).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsSimulated, true), Ct); }
        using var simulated = await Mutate(c, $"/api/admin/proposals/{draft.Id}/bookings", new CreateBookingInput(BookingDomainTests.Evidence())); Assert.Equal(HttpStatusCode.Forbidden, simulated.StatusCode);
        using var publicData = await c.GetAsync($"/api/public/bookings/{draft.Id}", Ct); Assert.Equal(HttpStatusCode.NotFound, publicData.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task BookingLifecyclePartialFailureReapprovalConfirmationAndCancellationPersist()
    {
        using var c = Client(); await Login(c); var b = await ApprovedBooking(c); Assert.StartsWith("BKG-", b.Booking.Reference); Assert.Equal("Pending", b.Booking.Status); Assert.Equal(2, b.Reservations.Length);
        var first = b.Reservations[0].Id; var second = b.Reservations[1].Id; var original = b.Reservations[0].Approved;
        using var premature = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/send-confirmation", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);
        b = await SaveReservation(c, b, first); b = await BookingAction(c, b, first, "start"); b = await BookingAction(c, b, first, "confirm"); Assert.Equal("PartiallyConfirmed", b.Booking.Status);
        b = await SaveReservation(c, b, second); b = await BookingAction(c, b, second, "start"); b = await BookingAction(c, b, second, "fail"); Assert.Equal("PartiallyConfirmed", b.Booking.Status); Assert.Equal("Confirmed", b.Reservations.Single(r => r.Id == first).Status);
        b = await SaveReservation(c, b, second, 1000); using var unauthorized = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/reservations/{second}/start", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.BadRequest, unauthorized.StatusCode);
        b = await BookingAction(c, b, second, "reapprove"); b = await BookingAction(c, b, second, "start"); b = await BookingAction(c, b, second, "await"); b = await BookingAction(c, b, second, "confirm"); Assert.Equal("Confirmed", b.Booking.Status); Assert.Equal(original.TotalMinorUnits, b.Reservations[0].Approved.TotalMinorUnits);
        using var sent = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/send-confirmation", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
        using var replay = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/send-confirmation", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.NoContent, replay.StatusCode);
        b = await BookingAction(c, b, second, "cancel"); Assert.Equal("PartiallyConfirmed", b.Booking.Status); Assert.Equal("Confirmed", b.Reservations.Single(r => r.Id == first).Status);
        var history = await c.GetFromJsonAsync<PageResult<BookingHistoryDto>>($"/api/admin/bookings/{b.Booking.Id}/activity", Ct); Assert.Contains(history!.Items, e => e.Type == "CustomerReapproval"); Assert.Contains(history.Items, e => e.Type == "BeforeChange");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Single(await db.NotificationOutbox.Where(n => n.BookingId == b.Booking.Id && n.Kind.StartsWith("booking-confirmed-")).ToArrayAsync(Ct));
        Assert.Contains(await db.NotificationOutbox.Where(n => n.BookingId == b.Booking.Id).ToArrayAsync(Ct), n => n.Kind.StartsWith("booking-failed-"));
        var metrics = await c.GetFromJsonAsync<BookingMetrics>("/api/admin/bookings/metrics", Ct); Assert.True(metrics!.PartiallyConfirmed >= 1);
        var list = await c.GetFromJsonAsync<PageResult<BookingListItem>>($"/api/admin/bookings?status=PartiallyConfirmed&search={b.Booking.Reference}&sort=departure", Ct); Assert.Single(list!.Items); Assert.Equal(b.Booking.Id, list.Items[0].Id);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task BookingCrossRequestConcurrencyAndDuplicateCreationAreSafe()
    {
        using var c = Client(); await Login(c); var a = await ApprovedBooking(c); var b = await ApprovedBooking(c);
        using var wrong = await Mutate(c, $"/api/admin/bookings/{a.Booking.Id}/reservations/{b.Reservations[0].Id}/start", new BookingVersionInput(a.VersionToken)); Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        using var duplicate = await Mutate(c, $"/api/admin/proposals/{a.Booking.ProposalId}/bookings", new CreateBookingInput(BookingDomainTests.Evidence())); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var replies = await Task.WhenAll(Mutate(c, $"/api/admin/bookings/{a.Booking.Id}", new BookingNotesInput(a.VersionToken, "TEST first"), HttpMethod.Patch), Mutate(c, $"/api/admin/bookings/{a.Booking.Id}", new BookingNotesInput(a.VersionToken, "TEST second"), HttpMethod.Patch));
        Assert.Single(replies, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(replies, r => r.StatusCode == HttpStatusCode.Conflict); foreach (var r in replies) r.Dispose();
        var before = a.Reservations[0].Current; using var unsafeInput = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/reservations/{b.Reservations[0].Id}/confirm", new ConfirmationInput(b.VersionToken, "TEST", "", "", "Issued", DateTimeOffset.UtcNow)); Assert.Equal(HttpStatusCode.BadRequest, unsafeInput.StatusCode);
        Assert.Equal(before.TotalMinorUnits, (await BookingDetail(c, a.Booking.Id)).Reservations[0].Current.TotalMinorUnits);
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task BookingOutboxDeliversConfirmedSnapshotsAndDiscardsStaleTripConfirmation(string language)
    {
        using var c = Client(); await Login(c); var b = await ApprovedBooking(c, language);
        foreach (var rid in b.Reservations.Select(r => r.Id).ToArray()) { b = await SaveReservation(c, b, rid); b = await BookingAction(c, b, rid, "start"); b = await BookingAction(c, b, rid, "confirm"); }
        using var sent = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/send-confirmation", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.BookingId == b.Booking.Id, Ct);
        await db.NotificationOutbox.Where(n => n.Id != job.Id && n.SentAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, DateTimeOffset.UtcNow.AddDays(1)), Ct);
        var sender = new BookingTestSender(); var processor = new OutboxProcessor(db, sender, new EmailSettings(), TimeProvider.System);
        Assert.True(await processor.ProcessOneAsync(Ct)); Assert.Single(sender.Messages); Assert.Contains("TEST-GENUINE-FIXTURE-REF", sender.Messages[0].Text); Assert.Contains(language == "sv" ? "Din resebokning är bekräftad" : "Your travel booking is confirmed", sender.Messages[0].Subject);
        b = await SaveReservation(c, b, b.Reservations[0].Id); using var newer = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/send-confirmation", new BookingVersionInput(b.VersionToken)); Assert.Equal(HttpStatusCode.NoContent, newer.StatusCode);
        b = await BookingAction(c, b, b.Reservations[0].Id, "cancel"); Assert.True(await processor.ProcessOneAsync(Ct)); Assert.Single(sender.Messages);
        var latest = await db.NotificationOutbox.AsNoTracking().Where(n => n.BookingId == b.Booking.Id && n.Id != job.Id).SingleAsync(Ct); Assert.NotNull(latest.DeadLetterAt); Assert.Null(latest.SentAt);
    }
    private sealed class BookingTestSender : Application.Requests.IEmailSender
    {
        public List<Application.Requests.EmailMessage> Messages { get; } = [];
        public Task SendAsync(Application.Requests.EmailMessage message, CancellationToken cancellationToken) { Messages.Add(message); return Task.CompletedTask; }
    }
}
