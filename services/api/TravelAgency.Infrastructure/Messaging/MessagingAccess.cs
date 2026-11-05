using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Messaging;

public sealed class MessagingAccess(TravelAgencyDbContext db, CustomerOwnership ownership)
{
    public async Task<CustomerConversation> RequireAsync(Guid id, string actor, bool admin, bool locked, CancellationToken ct)
    {
        if (!admin && !await db.Conversations.AnyAsync(c => c.Id == id && c.CustomerId == actor, ct)) throw new MessagingOperationException(404);
        if (locked) { var found = await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Conversations\" WHERE \"Id\" = {id} FOR NO KEY UPDATE").ToArrayAsync(ct); if (found.Length == 0) throw new MessagingOperationException(404); }
        var c = await db.Conversations.SingleOrDefaultAsync(c => c.Id == id && (admin || c.CustomerId == actor), ct) ?? throw new MessagingOperationException(404); return c;
    }
    public async Task<(Guid? Request, Guid? Booking)> AssociationAsync(string owner, Guid? requestId, Guid? bookingId, CancellationToken ct)
    {
        if (bookingId is { } id) { var request = await db.Bookings.Where(b => b.Id == id && ownership.Owned(owner).Any(o => o.RequestId == b.RequestId)).Select(b => (Guid?)b.RequestId).SingleOrDefaultAsync(ct) ?? throw new MessagingOperationException(404); if (requestId is not null && requestId != request) throw new MessagingOperationException(400); requestId = request; }
        if (requestId is { } rid) await ownership.RequireAsync(owner, rid, ct); return (requestId, bookingId);
    }
    public static void Version(Guid expected, Guid actual) { if (expected == Guid.Empty || expected != actual) throw new MessagingOperationException(409); }
    public void Audit(Guid id, string actor, string type, DateTimeOffset now, string? previous = null, string? current = null, long? sequence = null) => db.ConversationEvents.Add(new() { ConversationId = id, ActorId = actor, Type = type, Previous = previous, Current = current, MessageSequence = sequence, CreatedAt = now });
}
