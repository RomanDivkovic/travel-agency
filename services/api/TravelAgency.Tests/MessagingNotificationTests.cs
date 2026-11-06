using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Messaging;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Messaging;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task MessagingNotificationsAreLocalizedDebouncedPrivateAndRespectReadPreferences(string language)
    {
        using var customer = Client(); var u = await RegisterCustomer(customer); await CustomerLogin(customer, u.Email); var id = await StartConversation(customer); using var admin = Client(); await Login(admin); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.Users.Where(x => x.Id == u.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PreferredLanguage, language), Ct); var settings = new MessagingSettings { AgentLanguage = language }; var emailSettings = new EmailSettings(); var now = DateTimeOffset.UtcNow.AddMinutes(1);
        var first = await db.NotificationOutbox.SingleAsync(n => n.ConversationId == id, Ct); var notice = await MessagingNotifications.PrepareAsync(first, db, settings, emailSettings, now, Ct); Assert.NotNull(notice.Message); Assert.Contains(language == "sv" ? "Nytt kundmeddelande" : "New customer message", notice.Message.Subject); Assert.DoesNotContain("TEST initial message", notice.Message.Text); Assert.Contains("/admin/messages/", notice.Message.Text); notice.Complete(now, settings); await db.SaveChangesAsync(Ct); Assert.Null((await MessagingNotifications.PrepareAsync(first, db, settings, emailSettings, now, Ct)).Message);
        using var nextCustomer = await Reply(customer, id, body: "TEST private newer body"); Assert.Equal(HttpStatusCode.Created, nextCustomer.StatusCode); db.ChangeTracker.Clear(); var newest = await db.NotificationOutbox.Where(n => n.ConversationId == id).OrderByDescending(n => n.NextAttemptAt).FirstAsync(Ct); Assert.NotNull((await MessagingNotifications.PrepareAsync(newest, db, settings, emailSettings, now.AddSeconds(1), Ct)).DeferUntil);
        using var response = await Reply(admin, id, true, body: "TEST confidential reply <script>"); Assert.Equal(HttpStatusCode.Created, response.StatusCode); db.ChangeTracker.Clear(); var job = await db.NotificationOutbox.SingleAsync(n => n.ConversationId == id && n.Kind.StartsWith("message-customer-"), Ct); var delivery = await MessagingNotifications.PrepareAsync(job, db, settings, emailSettings, now, Ct); Assert.NotNull(delivery.Message); Assert.Contains(language == "sv" ? "Du har fått" : "You have a new message", delivery.Message.Subject); Assert.DoesNotContain("confidential", delivery.Message.Text); Assert.DoesNotContain("<script>", delivery.Message.Html); Assert.Contains("/account/messages/", delivery.Message.Text);
        using var optout = await CustomerMutate(customer, $"/conversations/{id}/notifications", new ConversationPreferences(false), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, optout.StatusCode); db.ChangeTracker.Clear(); Assert.Null((await MessagingNotifications.PrepareAsync(job, db, settings, emailSettings, now, Ct)).Message);
        using var optin = await CustomerMutate(customer, $"/conversations/{id}/notifications", new ConversationPreferences(true), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, optin.StatusCode); var d = await Conversation(customer, id); using var read = await CustomerMutate(customer, $"/conversations/{id}/read", new ConversationRead(d.LastSequence)); Assert.Equal(HttpStatusCode.NoContent, read.StatusCode); db.ChangeTracker.Clear(); Assert.Null((await MessagingNotifications.PrepareAsync(job, db, settings, emailSettings, now, Ct)).Message);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingOutboxRetriesWithoutAdvancingNotificationWatermarkAndAggregatesRapidMessages()
    {
        using var c = Client(); var u = await RegisterCustomer(c); await CustomerLogin(c, u.Email); var id = await StartConversation(c); using var reply = await Reply(c, id); Assert.Equal(HttpStatusCode.Created, reply.StatusCode); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var early = DateTimeOffset.UtcNow.AddDays(-2); await db.NotificationOutbox.Where(n => n.ConversationId == id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), Ct);
        var failed = new OutboxProcessor(db, new FailingSender(), new(), TimeProvider.System, messagingSettings: new()); Assert.True(await failed.ProcessOneAsync(Ct)); db.ChangeTracker.Clear(); Assert.Equal(0, (await db.Conversations.SingleAsync(x => x.Id == id, Ct)).AgentNotifiedSequence); Assert.Contains(await db.NotificationOutbox.Where(n => n.ConversationId == id).ToArrayAsync(Ct), n => n.Attempts == 1 && n.SentAt == null && n.DeadLetterAt == null);
        await db.NotificationOutbox.Where(n => n.ConversationId == id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), Ct); db.ChangeTracker.Clear(); var sink = new RecordingSender(); var processor = new OutboxProcessor(db, sink, new(), TimeProvider.System, messagingSettings: new()); for (var i = 0; i < 2; i++) Assert.True(await processor.ProcessOneAsync(Ct)); Assert.Single(sink.Messages); Assert.Equal(2, (await db.Conversations.SingleAsync(x => x.Id == id, Ct)).AgentNotifiedSequence);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingRetentionPreservesActiveConversationsAndPurgesOnlyApprovedExpiredClosedHistory()
    {
        using var c = Client(); var u = await RegisterCustomer(c); await CustomerLogin(c, u.Email); var active = await StartConversation(c); var closed = await StartConversation(c); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var conv = await db.Conversations.SingleAsync(x => x.Id == closed, Ct); conv.SetStatus(ConversationStatus.Closed, DateTimeOffset.UtcNow.AddDays(-400)); await db.SaveChangesAsync(Ct); await new MessagingMaintenance(db, new(), TimeProvider.System).RunAsync(Ct); Assert.True(await db.Conversations.AnyAsync(x => x.Id == closed, Ct)); await new MessagingMaintenance(db, new() { PurgeClosedConversations = true }, TimeProvider.System).RunAsync(Ct); Assert.True(await db.Conversations.AnyAsync(x => x.Id == closed, Ct));
        await db.NotificationOutbox.Where(n => n.ConversationId == closed).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, DateTimeOffset.UtcNow.AddDays(-1)), Ct); db.ChangeTracker.Clear(); var sink = new RecordingSender(); await new OutboxProcessor(db, sink, new(), TimeProvider.System, messagingSettings: new() { PurgeClosedConversations = true }).ProcessOneAsync(Ct); Assert.Empty(sink.Messages);
        await new MessagingMaintenance(db, new() { PurgeClosedConversations = true }, TimeProvider.System).RunAsync(Ct); Assert.False(await db.Conversations.AnyAsync(x => x.Id == closed, Ct)); Assert.True(await db.Conversations.AnyAsync(x => x.Id == active, Ct)); Assert.False(await db.ConversationMessages.AnyAsync(x => x.ConversationId == closed, Ct)); Assert.False(await db.NotificationOutbox.AnyAsync(x => x.ConversationId == closed, Ct));
    }
}
