using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Admin;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingAdministratorReadPositionsAreIndependent()
    {
        using var customer = Client(); var owner = await RegisterCustomer(customer); await CustomerLogin(customer, owner.Email); var id = await StartConversation(customer); using var first = Client(); await Login(first); using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<Administrator>>(); var second = new Administrator { UserName = "message-admin-" + Guid.NewGuid().ToString("N") + "@example.test", EmailConfirmed = true, DisplayName = "TEST second administrator" }; second.Email = second.UserName; Assert.True((await users.CreateAsync(second, password)).Succeeded); Assert.True((await users.AddToRoleAsync(second, "Administrator")).Succeeded); customerIds.Add(second.Id);
        using var other = Client(); using var login = await Mutate(other, "/api/admin/auth/login", new { email = second.Email, password }); Assert.Equal(HttpStatusCode.OK, login.StatusCode); Assert.Equal(1, (await MessageCounts(other, true)).UnreadCount); using var read = await Mutate(first, $"/api/admin/conversations/{id}/read", new TravelAgency.Application.Messaging.ConversationRead(1)); Assert.Equal(HttpStatusCode.NoContent, read.StatusCode); Assert.Equal(0, (await MessageCounts(first, true)).UnreadCount); Assert.Equal(1, (await MessageCounts(other, true)).UnreadCount);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingRejectsRealForeignTravelAssociationsAndListsOnlyOwnedLinks()
    {
        using var admin = Client(); await Login(admin); using var owner = Client(); var a = await RegisterCustomer(owner); await CustomerLogin(owner, a.Email); using var other = Client(); var b = await RegisterCustomer(other); await CustomerLogin(other, b.Email); var booking = await ApprovedBooking(admin, contactEmail: b.Email); await ClaimCustomerRequest(other, b.Id, booking.Booking.RequestId);
        using var requestDenied = await CustomerMutate(owner, "/conversations", new TravelAgency.Application.Messaging.NewConversation(Guid.NewGuid(), "TEST", "TEST", booking.Booking.RequestId, null)); Assert.Equal(HttpStatusCode.NotFound, requestDenied.StatusCode); using var bookingDenied = await CustomerMutate(owner, "/conversations", new TravelAgency.Application.Messaging.NewConversation(Guid.NewGuid(), "TEST", "TEST", null, booking.Booking.Id)); Assert.Equal(HttpStatusCode.NotFound, bookingDenied.StatusCode);
        var links = await owner.GetStringAsync("/api/customer/conversations/links?kind=bookings", Ct); Assert.DoesNotContain(booking.Booking.Id.ToString(), links); var ownLinks = await other.GetStringAsync("/api/customer/conversations/links?kind=bookings&id=" + booking.Booking.Id, Ct); Assert.Contains(booking.Booking.Reference, ownLinks); var id = await StartConversation(other, booking: booking.Booking.Id); var d = await Conversation(other, id); Assert.Equal(booking.Booking.RequestId, d.Conversation.RequestId); Assert.Equal(booking.Booking.Id, d.Conversation.BookingId);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingSubmissionsAreRateLimitedPerAuthenticatedParticipant()
    {
        using var c = Client(); var u = await RegisterCustomer(c); await CustomerLogin(c, u.Email); var id = await StartConversation(c); for (var i = 0; i < 19; i++) { using var sent = await Reply(c, id); Assert.Equal(HttpStatusCode.Created, sent.StatusCode); }
        using var limited = await Reply(c, id); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter); Assert.Equal(20, (await Conversation(c, id)).Messages.Length);
    }
}
