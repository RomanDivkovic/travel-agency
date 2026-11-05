using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Bookings;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Documents;

public sealed class DocumentAccess(TravelAgencyDbContext db, BookingOperations bookings, CustomerOwnership ownership)
{
    public async Task<TravelBooking> BookingAsync(Guid id, string? owner, bool locked, CancellationToken ct)
    {
        if (owner is not null)
        {
            var request = await db.Bookings.Where(b => b.Id == id && ownership.Owned(owner).Any(o => o.RequestId == b.RequestId)).Select(b => (Guid?)b.RequestId).SingleOrDefaultAsync(ct);
            if (request is null) throw new DocumentOperationException(404);
        }
        if (locked) return await bookings.LockAsync(id, ct);
        return await db.Bookings.AsNoTracking().Include(b => b.Reservations).SingleOrDefaultAsync(b => b.Id == id, ct) ?? throw new DocumentOperationException(404);
    }
    public static void Version(Guid expected, Guid actual) { if (expected == Guid.Empty || expected != actual) throw new DocumentOperationException(409); }
    public void Audit(TravelBooking b, string actor, string type, object safe) => db.BookingEvents.Add(new BookingEvent { BookingId = b.Id, ActorId = actor, Type = type, CreatedAt = DateTimeOffset.UtcNow, SnapshotJson = System.Text.Json.JsonSerializer.Serialize(safe) });
    public async Task<TravelDocument> DocumentAsync(Guid booking, Guid document, CancellationToken ct) => await db.TravelDocuments.Include(d => d.Versions).SingleOrDefaultAsync(d => d.BookingId == booking && d.Id == document, ct) ?? throw new DocumentOperationException(404);
}
