using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Messaging;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task MessagingFailedCommitRollsBackConversationMessageAuditAndNotificationTogether()
    {
        using var customer = Client(); var u = await RegisterCustomer(customer); await CustomerLogin(customer, u.Email);
        await using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddDbContext<TravelAgencyDbContext>(o => o.AddInterceptors(new FailMessageSave())))); using var c = Client(failing); await CustomerLogin(c, u.Email); var clientId = Guid.NewGuid(); using var failed = await CustomerMutate(c, "/conversations", new NewConversation(clientId, "TEST rollback", "TEST private body", null, null)); Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode); Assert.DoesNotContain("Injected", await failed.Content.ReadAsStringAsync(Ct)); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.False(await db.Conversations.AnyAsync(x => x.CustomerId == u.Id, Ct)); Assert.False(await db.ConversationMessages.AnyAsync(x => x.SenderId == u.Id, Ct)); Assert.False(await db.ConversationEvents.AnyAsync(x => x.ActorId == u.Id, Ct)); Assert.False(await db.NotificationOutbox.AnyAsync(x => x.ConversationId != null, Ct));
        var retry = await StartConversation(customer, key: clientId, subject: "TEST rollback", body: "TEST private body"); Assert.NotEqual(Guid.Empty, retry);
    }
    private sealed class FailMessageSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken ct = default) => data.Context!.ChangeTracker.Entries<ConversationMessage>().Any() ? throw new InvalidOperationException("Injected message transaction failure.") : ValueTask.FromResult(result);
    }
}
