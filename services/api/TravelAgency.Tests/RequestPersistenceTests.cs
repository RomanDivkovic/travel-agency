using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

[Collection("Request persistence")]
public sealed class RequestPersistenceTests : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        if (HasDatabase) { await using var db = Database(); await db.Database.MigrateAsync(TestContext.Current.CancellationToken); }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public static bool HasDatabase => DatabaseIntegrationTests.HasTestDatabase;
    private static string Connection
    {
        get { var value = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")!; Assert.EndsWith("_tests", new NpgsqlConnectionStringBuilder(value).Database); return value; }
    }
    private static TravelAgencyDbContext Database(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<TravelAgencyDbContext>().UseNpgsql(Connection).AddInterceptors(interceptors).Options);
    private static async Task<HttpResponseMessage> Send(HttpClient client, SubmitTravelRequest input, Guid key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key.ToString("D")); return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
    [Fact(Skip = "Requires isolated PostgreSQL.", SkipUnless = nameof(HasDatabase))]
    public async Task ConcurrentDuplicatesReturnOneReceiptAndAtomicOutboxWhileChangesConflict()
    {
        await using var factory = new ApiFactory(connectionString: Connection); using var client = factory.CreateClient();
        await using var db = Database(); await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var key = Guid.NewGuid(); var input = RequestExamples.Input();
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Send(client, input, key)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var receipts = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken)));
        var reference = receipts[0]!.ReferenceNumber; Assert.All(receipts, r => Assert.Equal(receipts[0], r));
        var stored = await db.TravelRequests.SingleAsync(r => r.ReferenceNumber == reference, TestContext.Current.CancellationToken);
        Assert.Equal("submitted", receipts[0]!.Status); Assert.Equal(input.Dates!.Departure, stored.Plan.Departure);
        Assert.Equal(2, await db.NotificationOutbox.CountAsync(n => n.RequestId == stored.Id, TestContext.Current.CancellationToken));
        Assert.Contains("direct", stored.Plan.PreferencesJson); Assert.Equal(input.Contact!.Email, stored.Contact.Email);
        using var conflict = await Send(client, input with { Budget = input.Budget! with { Amount = 10000 } }, key);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var second = await Send(client, input, Guid.NewGuid()); Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondReceipt = await second.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken); Assert.NotEqual(reference, secondReceipt!.ReferenceNumber);
        await db.TravelRequests.Where(r => r.ReferenceNumber == reference || r.ReferenceNumber == secondReceipt.ReferenceNumber).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        foreach (var response in responses) response.Dispose();
    }
    [Fact(Skip = "Requires isolated PostgreSQL.", SkipUnless = nameof(HasDatabase))]
    public async Task SaveFailureRollsBackRequestsKeysAndNotifications()
    {
        await using var db = Database(new FailAfterSave()); var before = await db.TravelRequests.CountAsync(TestContext.Current.CancellationToken);
        var store = new TravelAgency.Infrastructure.Requests.PostgresTravelRequestStore(db, new(), TimeProvider.System);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(Guid.NewGuid(), new string('A', 64), RequestExamples.Input(), TestContext.Current.CancellationToken));
        await using var check = Database(); Assert.Equal(before, await check.TravelRequests.CountAsync(TestContext.Current.CancellationToken));
    }
    [Fact(Skip = "Requires isolated PostgreSQL.", SkipUnless = nameof(HasDatabase))]
    public async Task ConcurrentWorkersCaptureEachNotificationOnceAndRetriesAreBounded()
    {
        await using var factory = new ApiFactory(connectionString: Connection); using var client = factory.CreateClient();
        using var response = await Send(client, RequestExamples.Input("flights", "en"), Guid.NewGuid()); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = (await response.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken))!;
        await using var db = Database(); var request = await db.TravelRequests.SingleAsync(r => r.ReferenceNumber == receipt.ReferenceNumber, TestContext.Current.CancellationToken);
        var early = DateTimeOffset.Parse("2000-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await db.NotificationOutbox.Where(n => n.RequestId == request.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.NextAttemptAt, early), TestContext.Current.CancellationToken);
        var sink = new RecordingSender();
        await using var db1 = Database(); await using var db2 = Database();
        await Task.WhenAll(new OutboxProcessor(db1, sink, new(), TimeProvider.System).ProcessOneAsync(TestContext.Current.CancellationToken), new OutboxProcessor(db2, sink, new(), TimeProvider.System).ProcessOneAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, sink.Messages.Count); Assert.Equal(2, sink.Messages.Select(m => m.Id).Distinct().Count());
        Assert.Equal(2, await db.NotificationOutbox.CountAsync(n => n.RequestId == request.Id && n.SentAt != null, TestContext.Current.CancellationToken));
        var directory = Path.Combine(Path.GetTempPath(), "travel-mail-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var http = new HttpClient(); var capture = new EmailSender(http, new EmailSettings { CaptureDirectory = directory });
            foreach (var message in sink.Messages) { await capture.SendAsync(message, TestContext.Current.CancellationToken); await capture.SendAsync(message, TestContext.Current.CancellationToken); }
            Assert.Equal(2, Directory.GetFiles(directory, "*.json").Length);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        // Exercise retry and dead-letter state against a saved request without losing it.
        var target = await db.NotificationOutbox.FirstAsync(n => n.RequestId == request.Id, TestContext.Current.CancellationToken);
        target.SentAt = null; target.Attempts = 0; target.NextAttemptAt = early; await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await using var failingDb = Database(); var processor = new OutboxProcessor(failingDb, new FailingSender(), new EmailSettings { MaxAttempts = 2 }, TimeProvider.System);
        await processor.ProcessOneAsync(TestContext.Current.CancellationToken);
        await db.Entry(target).ReloadAsync(TestContext.Current.CancellationToken); Assert.Equal(1, target.Attempts); Assert.Null(target.SentAt); Assert.True(target.NextAttemptAt > DateTimeOffset.UtcNow); Assert.Null(target.DeadLetterAt);
        target.NextAttemptAt = early; await db.SaveChangesAsync(TestContext.Current.CancellationToken); failingDb.ChangeTracker.Clear();
        await processor.ProcessOneAsync(TestContext.Current.CancellationToken); await db.Entry(target).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, target.Attempts); Assert.NotNull(target.DeadLetterAt); Assert.True(await db.TravelRequests.AnyAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken));
        await db.TravelRequests.Where(r => r.Id == request.Id).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }
    [Fact(Skip = "Requires isolated PostgreSQL.", SkipUnless = nameof(HasDatabase))]
    public async Task ConcurrentNewRequestsHaveDistinctReferencesAndAcceptedDatesRemainReplayable()
    {
        await using var factory = new ApiFactory(connectionString: Connection); using var client = factory.CreateClient();
        var input = RequestExamples.Input("flights");
        input = input with { Dates = input.Dates! with { Departure = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), Return = null } };
        var keys = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        var responses = await Task.WhenAll(keys.Select(key => Send(client, input, key)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var receipts = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<SubmissionReceipt>(TestContext.Current.CancellationToken)));
        Assert.Equal(8, receipts.Select(r => r!.ReferenceNumber).Distinct().Count());
        await using var db = Database(); var clock = new LaterClock();
        var store = new TravelAgency.Infrastructure.Requests.PostgresTravelRequestStore(db, new(), clock);
        var replay = await new SubmissionHandler(store, clock).SubmitAsync(keys[0], input, TestContext.Current.CancellationToken);
        Assert.True(replay.Replayed); Assert.Equal(receipts[0], replay.Receipt);
        var references = receipts.Select(r => r!.ReferenceNumber).ToArray();
        await db.TravelRequests.Where(r => references.Contains(r.ReferenceNumber)).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        foreach (var response in responses) response.Dispose();
    }
    private sealed class LaterClock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddDays(2); }
    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default) => throw new InvalidOperationException("Injected transaction failure");
    }
    private sealed class RecordingSender : IEmailSender
    {
        public ConcurrentBag<EmailMessage> Messages { get; } = [];
        public async Task SendAsync(EmailMessage message, CancellationToken ct) { Messages.Add(message); await Task.Delay(50, ct); }
    }
    private sealed class FailingSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct) => throw new EmailDeliveryException(true);
    }
}
