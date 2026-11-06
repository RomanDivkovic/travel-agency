using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.BusinessManagement;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.BusinessManagement;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Requests;

public sealed class PostgresTravelRequestStore(TravelAgencyDbContext db, SubmissionSettings settings, TimeProvider clock) : ITravelRequestStore
{
    private static byte[] KeyBytes(Guid key) => SHA256.HashData(Encoding.UTF8.GetBytes(key.ToString("D")));
    public Task<SubmissionResult?> FindReplayAsync(Guid key, string payloadHash, CancellationToken ct) => FindAsync(Convert.ToHexString(KeyBytes(key)), payloadHash, ct);
    private async Task<SubmissionResult?> FindAsync(string keyHash, string payloadHash, CancellationToken ct)
    {
        var existing = await (from key in db.IdempotencyRecords.AsNoTracking()
                              join request in db.TravelRequests.AsNoTracking() on key.RequestId equals request.Id
                              where key.KeyHash == keyHash && key.ExpiresAt > clock.GetUtcNow()
                              select new { key.PayloadHash, request.ReferenceNumber, request.CreatedAt }).SingleOrDefaultAsync(ct);
        if (existing is null) return null;
        if (existing.PayloadHash != payloadHash) return new SubmissionResult(Conflict: true);
        return new SubmissionResult(new SubmissionReceipt(existing.ReferenceNumber, "submitted", existing.CreatedAt), Replayed: true);
    }
    public async Task<SubmissionResult> SaveAsync(Guid key, string payloadHash, SubmitTravelRequest input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var bytes = KeyBytes(key); var hash = Convert.ToHexString(bytes);
        var lockId = BinaryPrimitives.ReadInt64BigEndian(bytes);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
        var replay = await FindAsync(hash, payloadHash, ct);
        if (replay is not null) return replay;
        var code = input.Service switch { "flights" => "FlightsOnly", "accommodation" => "AccommodationOnly", "flight-accommodation" => "FlightsAndAccommodation", _ => "CompleteTrip" };
        var serviceKey = "service:" + code;
        var configured = await (from d in db.ManagedContents join v in db.ContentRevisions on new { d.Key, Version = d.PublishedVersion } equals new { v.Key, v.Version } where d.Key == serviceKey select v.DataJson).SingleOrDefaultAsync(ct);
        if (configured is not null && !ContentSchema.Read<ServiceData>(ContentOperations.Parse(configured)).Enabled) return new(Errors: new() { ["service"] = ["choice"] });
        await db.IdempotencyRecords.Where(k => k.KeyHash == hash).ExecuteDeleteAsync(ct);
        var number = await db.Database.SqlQuery<long>($"SELECT nextval('travel_request_reference') AS \"Value\"").SingleAsync(ct);
        var now = clock.GetUtcNow();
        var reference = $"{settings.ReferencePrefix}-{now.Year}-{number:D6}";
        var service = input.Service switch
        {
            "flights" => TravelService.Flights,
            "accommodation" => TravelService.Accommodation,
            "flight-accommodation" => TravelService.FlightAccommodation,
            _ => TravelService.Complete
        };
        var destination = input.Destination!; var dates = input.Dates!; var travelers = input.Travelers!; var budget = input.Budget!; var contact = input.Contact!;
        var plan = new TravelPlan(destination.Origin, destination.Place, destination.Flexible!.Value, destination.AdditionalStops ?? [],
            dates.Mode!, dates.Departure, dates.Return, dates.Earliest, dates.Latest, dates.DurationDays,
            travelers.Adults!.Value, travelers.ChildrenAges!, travelers.Rooms, budget.Amount!.Value, budget.Currency!, budget.Scope!,
            JsonSerializer.Serialize(input.Preferences, SubmissionHandler.JsonOptions), input.Activities!.Value, input.SpecialRequests);
        var request = TravelInquiry.Create(reference, service, input.Language!, new CustomerContact(contact.Name!, contact.Email!, contact.Phone, contact.PreferredMethod!), plan, now);
        db.TravelRequests.Add(request);
        db.RequestActivities.Add(new RequestActivity { RequestId = request.Id, Type = "Submitted", CreatedAt = request.CreatedAt });
        db.IdempotencyRecords.Add(new IdempotencyRecord { KeyHash = hash, PayloadHash = payloadHash, RequestId = request.Id, ExpiresAt = now.AddDays(settings.IdempotencyRetentionDays) });
        db.NotificationOutbox.AddRange(new NotificationRecord { RequestId = request.Id, Kind = "customer", NextAttemptAt = now }, new NotificationRecord { RequestId = request.Id, Kind = "agent", NextAttemptAt = now });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SubmissionResult(Receipt(request));
    }
    private static SubmissionReceipt Receipt(TravelInquiry request) => new(request.ReferenceNumber, "submitted", request.CreatedAt);
}
