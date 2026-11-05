using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Documents;

public sealed class DocumentMaintenance(TravelAgencyDbContext db, ITravelDocumentStorage storage, DocumentSettings settings, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        if (settings.Provider == "Disabled") return; var now = clock.GetUtcNow();
        var expired = await db.DocumentVersions.Where(v => (v.Status == DocumentStatus.Archived && v.UpdatedAt <= now.AddDays(-settings.ArchivedRetentionDays)) || (v.Status == DocumentStatus.Draft && v.UpdatedAt <= now.AddDays(-settings.DraftRetentionDays))).OrderBy(v => v.UpdatedAt).Take(20).Select(v => new { v.DocumentId, v.BlobId }).ToArrayAsync(ct);
        foreach (var item in expired)
        {
            // Serialize retention with publication using the same request/booking lock order.
            await using var tx = await db.Database.BeginTransactionAsync(ct); var document = await db.TravelDocuments.Where(d => d.Id == item.DocumentId).SingleAsync(ct);
            var booking = await db.Bookings.Where(b => b.Id == document.BookingId).SingleAsync(ct);
            await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"TravelRequests\" WHERE \"Id\" = {booking.RequestId} FOR NO KEY UPDATE").ToArrayAsync(ct);
            await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Bookings\" WHERE \"Id\" = {booking.Id} FOR NO KEY UPDATE").ToArrayAsync(ct);
            var v = await db.DocumentVersions.SingleAsync(v => v.BlobId == item.BlobId, ct); await db.Entry(v).ReloadAsync(ct);
            if ((v.Status == DocumentStatus.Archived && v.UpdatedAt <= now.AddDays(-settings.ArchivedRetentionDays)) || (v.Status == DocumentStatus.Draft && v.UpdatedAt <= now.AddDays(-settings.DraftRetentionDays))) { v.Status = DocumentStatus.Deleted; v.UpdatedAt = now; (await db.DocumentBlobs.SingleAsync(b => b.Id == item.BlobId, ct)).DeleteAfter = now; await db.SaveChangesAsync(ct); }
            await tx.CommitAsync(ct);
        }
        for (var i = 0; i < 20; i++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var blob = (await db.DocumentBlobs.FromSqlInterpolated($"SELECT * FROM \"DocumentBlobs\" WHERE \"PurgedAt\" IS NULL AND (\"DeleteAfter\" <= {now} OR (\"BookingId\" IS NULL AND \"DeleteAfter\" IS NULL)) ORDER BY \"DeleteAfter\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToArrayAsync(ct)).SingleOrDefault(); if (blob is null) break;
            try
            {
                await storage.DeleteAsync(blob.Key, ct); blob.PurgedAt = now; blob.DeleteAfter = null;
                await db.DocumentVersions.Where(v => v.BlobId == blob.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.Name, "").SetProperty(v => v.Description, "").SetProperty(v => v.OriginalFileName, ""), ct);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DocumentOperationException) { blob.Attempts++; blob.DeleteAfter = now.AddMinutes(Math.Min(1440, 1 << Math.Min(blob.Attempts, 10))); }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
    }
}
