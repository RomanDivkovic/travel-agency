using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Documents;

public sealed class DocumentOperations(TravelAgencyDbContext db, DocumentAccess access, ITravelDocumentStorage storage, IDocumentScanner scanner, DocumentSettings settings, DocumentNotifications notices, TimeProvider clock)
{
    public async Task<Guid> UploadAsync(Guid bookingId, Guid? documentId, Guid? expected, DocumentMetadata metadata, string filename, string mime, byte[] bytes, string actor, CancellationToken ct)
    {
        settings.RequireStorage(); DocumentRules.Metadata(metadata.Name, metadata.Description);
        if (!metadata.NoIdentityDocuments || !Enum.TryParse<TravelDocumentType>(metadata.Type, out var type) || !Enum.IsDefined(type) || type.ToString() != metadata.Type) throw new DocumentOperationException(400);
        filename = DocumentFileValidation.Validate(bytes, mime, filename, settings.MaxBytes);
        var booking = await access.BookingAsync(bookingId, null, false, ct); if (metadata.ReservationId is { } rid && !booking.Reservations.Any(r => r.Id == rid)) throw new DocumentOperationException(400);
        var blob = new DocumentBlob { BookingId = bookingId, Key = CustomerTokenCodec.Create(), DeleteAfter = clock.GetUtcNow().AddHours(1) };
        // Commit a cleanup lease before writing bytes: a crash/rollback cannot orphan a file indefinitely.
        db.DocumentBlobs.Add(blob); await db.SaveChangesAsync(ct); await storage.UploadAsync(blob.Key, bytes, ct); var scan = await scanner.ScanAsync(bytes, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct); booking = await access.BookingAsync(bookingId, null, true, ct);
        TravelDocument document;
        if (documentId is { } id)
        {
            document = await access.DocumentAsync(bookingId, id, ct); DocumentAccess.Version(expected ?? Guid.Empty, document.VersionToken);
            if (document.CurrentVersion >= 100 || document.Type != type || document.ReservationId != metadata.ReservationId) throw new DocumentOperationException(409);
            document.CurrentVersion++; document.Touch();
        }
        else
        {
            if (await db.TravelDocuments.CountAsync(d => d.BookingId == bookingId, ct) >= 500) throw new DocumentOperationException(409);
            document = new() { BookingId = bookingId, ReservationId = metadata.ReservationId, Type = type, CreatedAt = clock.GetUtcNow() }; db.TravelDocuments.Add(document);
        }
        var now = clock.GetUtcNow(); var newVersion = new TravelDocumentVersion() { DocumentId = document.Id, BlobId = blob.Id, Version = document.CurrentVersion, Name = metadata.Name.Trim(), Description = metadata.Description.Trim(), OriginalFileName = filename, ContentType = mime, Size = bytes.LongLength, Scan = scan, ScannedAt = scan == DocumentScan.Quarantined ? null : now, UploadedById = actor, CreatedAt = now, UpdatedAt = now }; document.Versions.Add(newVersion); db.DocumentVersions.Add(newVersion);
        blob.DeleteAfter = null; access.Audit(booking, actor, documentId is null ? "DocumentUploaded" : "DocumentReplaced", new { document.Id, Version = document.CurrentVersion });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return document.Id;
    }
    public async Task ChangeAsync(Guid bookingId, Guid id, DocumentVersionInput input, string op, string actor, DocumentChange? metadata, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var booking = await access.BookingAsync(bookingId, null, true, ct); var d = await access.DocumentAsync(bookingId, id, ct); DocumentAccess.Version(input.VersionToken, d.VersionToken);
        var v = d.Versions.SingleOrDefault(v => v.Version == (op == "edit" ? d.CurrentVersion : input.Version)) ?? throw new DocumentOperationException(404); var now = clock.GetUtcNow();
        switch (op)
        {
            case "edit":
                if (v.Status != DocumentStatus.Draft || v.Version != d.CurrentVersion || metadata is null) throw new DocumentOperationException(409);
                DocumentRules.Metadata(metadata.Name, metadata.Description); v.Name = metadata.Name.Trim(); v.Description = metadata.Description.Trim(); v.UpdatedAt = now; d.Touch(); break;
            case "publish":
                if (d.ReservationId is { } rid && !booking.Reservations.Any(r => r.Id == rid && r.Status == ReservationStatus.Confirmed)) throw new DocumentOperationException(409);
                if (v.Scan != DocumentScan.Clean || v.Status != DocumentStatus.Draft) throw new DocumentOperationException(409);
                notices.RequireOrigin(); d.Publish(v, now); await notices.QueueAsync(booking, "document", v.Id, d.CurrentVersion > 1, ct); break;
            case "archive": d.Hide(v, false, now); break;
            case "delete": d.Hide(v, true, now); (await db.DocumentBlobs.SingleAsync(b => b.Id == v.BlobId, ct)).DeleteAfter = now; break;
            case "scan":
                if (v.Status != DocumentStatus.Draft || v.Version != d.CurrentVersion) throw new DocumentOperationException(409);
                var blob = await db.DocumentBlobs.SingleAsync(b => b.Id == v.BlobId && b.PurgedAt == null, ct); v.Scan = await scanner.ScanAsync(await storage.DownloadAsync(blob.Key, ct), ct); v.ScannedAt = v.Scan == DocumentScan.Quarantined ? null : now; v.UpdatedAt = now; d.Touch(); break;
            default: throw new DocumentOperationException(400);
        }
        access.Audit(booking, actor, "Document" + op, new { d.Id, v.Version }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<DocumentLibrary> ListAsync(Guid bookingId, string? owner, int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000) throw new DocumentOperationException(400); var booking = await access.BookingAsync(bookingId, owner, false, ct);
        var q = db.TravelDocuments.AsNoTracking().Where(d => d.BookingId == bookingId);
        if (owner is not null) q = q.Where(d => d.Versions.Any(v => v.Version == d.PublishedVersion && v.Status == DocumentStatus.Published && v.Scan == DocumentScan.Clean));
        var count = await q.CountAsync(ct); var documents = await q.OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id).Include(d => d.Versions).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        return new(booking.Reference, documents.Select(d => Dto(d, booking, owner is null)).ToArray(), page, (int)Math.Ceiling(count / 20d), count, settings.MaxBytes, owner is null ? booking.Reservations.Select(r => new ReservationChoice(r.Id, r.Current().Title, r.Status.ToString())).ToArray() : []);
    }
    public static DocumentDto Dto(TravelDocument d, TravelBooking b, bool admin)
    {
        var reservation = b.Reservations.FirstOrDefault(r => r.Id == d.ReservationId);
        var versions = d.Versions.Where(v => admin || v.Status == DocumentStatus.Published && v.Scan == DocumentScan.Clean && v.Version == d.PublishedVersion).OrderByDescending(v => v.Version).Select(v => new DocumentVersionDto(v.Id, v.Version, v.Name, v.Description, v.OriginalFileName, v.ContentType, v.Size, v.Status.ToString(), v.Scan.ToString(), v.CreatedAt, v.PublishedAt)).ToArray();
        return new(d.Id, d.BookingId, d.ReservationId, reservation?.Approved().Title, d.Type.ToString(), admin ? d.CurrentVersion : d.PublishedVersion!.Value, d.PublishedVersion, admin ? d.VersionToken : null, versions);
    }
    public async Task<(byte[] Bytes, string Mime, string Name)> DownloadAsync(Guid bookingId, Guid id, int? version, string? owner, string actor, CancellationToken ct)
    {
        // Keep publication/archive/ownership stable until the complete bounded file has been read.
        await using var tx = await db.Database.BeginTransactionAsync(ct); var booking = await access.BookingAsync(bookingId, owner, true, ct); var d = await access.DocumentAsync(bookingId, id, ct);
        var v = d.Versions.SingleOrDefault(v => v.Version == (owner is null ? version ?? d.CurrentVersion : d.PublishedVersion)) ?? throw new DocumentOperationException(404);
        if (v.Status == DocumentStatus.Deleted || v.Scan != DocumentScan.Clean || owner is not null && (v.Status != DocumentStatus.Published || v.Version != d.PublishedVersion)) throw new DocumentOperationException(404);
        var blob = await db.DocumentBlobs.SingleOrDefaultAsync(b => b.Id == v.BlobId && b.PurgedAt == null, ct) ?? throw new DocumentOperationException(404); var bytes = await storage.DownloadAsync(blob.Key, ct);
        if (owner is null) access.Audit(booking, actor, "DocumentAccessed", new { d.Id, v.Version });
        else db.RequestActivities.Add(new Domain.Admin.RequestActivity { BookingId = bookingId, RequestId = booking.RequestId, Type = "DocumentDownloaded", CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (bytes, v.ContentType, v.OriginalFileName);
    }
}
