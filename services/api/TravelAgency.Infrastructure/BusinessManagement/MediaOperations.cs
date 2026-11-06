using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.BusinessManagement;
using TravelAgency.Domain.BusinessManagement;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.BusinessManagement;

public static class WebsiteImageValidation
{
    public const int MaxBytes = 3 * 1024 * 1024;
    public static string Validate(byte[] bytes, string mime, string original)
    {
        if (bytes.Length > MaxBytes) throw new ContentOperationException(413, "size"); if (bytes.Length < 12) throw new ContentOperationException(415, "image");
        var name = new string(original.Replace('\\', '/').Split('/').Last().Where(c => char.IsLetterOrDigit(c) || " ._-".Contains(c)).Take(120).ToArray()).Trim(' ', '.'); var ext = Path.GetExtension(name).ToLowerInvariant(); var b = bytes.AsSpan(); var valid = false;
        if (mime == "image/png" && ext == ".png" && b.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            var offset = 8; var first = true;
            while (offset <= b.Length - 12)
            {
                var size = BinaryPrimitives.ReadUInt32BigEndian(b[offset..]); if (size > b.Length - offset - 12) break; var tag = b.Slice(offset + 4, 4);
                if (first) { if (!tag.SequenceEqual("IHDR"u8) || size != 13) break; Dimensions(BinaryPrimitives.ReadUInt32BigEndian(b[(offset + 8)..]), BinaryPrimitives.ReadUInt32BigEndian(b[(offset + 12)..])); first = false; }
                offset += (int)size + 12; if (tag.SequenceEqual("IEND"u8)) { valid = size == 0 && offset == b.Length; break; }
            }
        }
        else if (mime == "image/jpeg" && ext is ".jpg" or ".jpeg" && b.StartsWith(new byte[] { 255, 216, 255 }) && b.EndsWith(new byte[] { 255, 217 }))
        {
            var offset = 2; var dimensions = false;
            while (offset + 4 < b.Length && b[offset] == 255)
            {
                var marker = b[offset + 1]; if (marker == 218) { valid = dimensions; break; }
                var length = BinaryPrimitives.ReadUInt16BigEndian(b[(offset + 2)..]); if (length < 2 || offset + 2 + length > b.Length) break;
                if (marker is 192 or 193 or 194) { if (length < 8) break; Dimensions(BinaryPrimitives.ReadUInt16BigEndian(b[(offset + 7)..]), BinaryPrimitives.ReadUInt16BigEndian(b[(offset + 5)..])); dimensions = true; }
                offset += 2 + length;
            }
        }
        else if (mime == "image/webp" && ext == ".webp" && b.StartsWith("RIFF"u8) && b.Slice(8, 4).SequenceEqual("WEBP"u8) && BinaryPrimitives.ReadUInt32LittleEndian(b[4..]) == b.Length - 8 && b.Length >= 30)
        {
            var tag = b.Slice(12, 4); var size = BinaryPrimitives.ReadUInt32LittleEndian(b[16..]);
            if (size <= b.Length - 20)
            {
                if (tag.SequenceEqual("VP8X"u8) && size >= 10) { Dimensions(1 + U24(b[24..]), 1 + U24(b[27..])); valid = (b[20] & 2) == 0; }
                else if (tag.SequenceEqual("VP8 "u8) && size >= 10 && b.Slice(23, 3).SequenceEqual(new byte[] { 157, 1, 42 })) { Dimensions((uint)(BinaryPrimitives.ReadUInt16LittleEndian(b[26..]) & 16383), (uint)(BinaryPrimitives.ReadUInt16LittleEndian(b[28..]) & 16383)); valid = true; }
                else if (tag.SequenceEqual("VP8L"u8) && size >= 5 && b[20] == 47) { var bits = BinaryPrimitives.ReadUInt32LittleEndian(b[21..]); Dimensions((bits & 16383) + 1, ((bits >> 14) & 16383) + 1); valid = true; }
            }
        }
        if (!valid) throw new ContentOperationException(415, "image"); return name;
    }
    private static uint U24(ReadOnlySpan<byte> b) => (uint)(b[0] | b[1] << 8 | b[2] << 16);
    private static void Dimensions(uint w, uint h) { if (w == 0 || h == 0 || w > 10000 || h > 10000 || (ulong)w * h > 25000000) throw new ContentOperationException(415, "image"); }
}
public sealed class MediaOperations(TravelAgencyDbContext db, TimeProvider clock)
{
    public async Task<MediaDto[]> List(int page, CancellationToken ct) { if (page < 1) throw new ContentOperationException(400); return await db.WebsiteMedia.AsNoTracking().OrderByDescending(m => m.CreatedAt).ThenBy(m => m.Id).Skip((Math.Min(page, 10000) - 1) * 20).Take(20).Select(m => new MediaDto(m.Id, m.FileName, m.ContentType, m.AltSv, m.AltEn, m.Decorative, m.Archived, m.VersionToken)).ToArrayAsync(ct); }
    public async Task<MediaDto> Upload(byte[] bytes, string mime, string name, string sv, string en, bool decorative, string actor, CancellationToken ct)
    {
        var safe = WebsiteImageValidation.Validate(bytes, mime, name); ContentSchema.ValidateAlt(sv, en, decorative);
        var m = new WebsiteMedia { FileName = safe, ContentType = mime, Bytes = bytes, AltSv = sv, AltEn = en, Decorative = decorative, CreatedAt = clock.GetUtcNow(), CreatedBy = actor };
        db.WebsiteMedia.Add(m); db.ContentAudits.Add(new() { Key = "media:" + m.Id, Action = "Uploaded", ActorId = actor, CreatedAt = clock.GetUtcNow() }); await db.SaveChangesAsync(ct); return Dto(m);
    }
    public async Task Change(Guid id, MediaChange input, string actor, CancellationToken ct)
    {
        ContentSchema.ValidateAlt(input.AltSv, input.AltEn, input.Decorative); await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(db, ct);
        var m = await db.WebsiteMedia.SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new ContentOperationException(404); if (m.VersionToken != input.VersionToken) throw new ContentOperationException(409, "conflict");
        if (input.Archived && await IsPublic(id, ct)) throw new ContentOperationException(409, "inUse");
        m.AltSv = input.AltSv; m.AltEn = input.AltEn; m.Decorative = input.Decorative; m.Archived = input.Archived; m.VersionToken = Guid.NewGuid(); db.ContentAudits.Add(new() { Key = "media:" + id, Action = "MediaUpdated", ActorId = actor, CreatedAt = clock.GetUtcNow() }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<WebsiteMedia> File(Guid id, bool admin, CancellationToken ct)
    {
        if (!admin && !await IsPublic(id, ct)) throw new ContentOperationException(404); return await db.WebsiteMedia.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id && (admin || !m.Archived), ct) ?? throw new ContentOperationException(404);
    }
    private Task<bool> IsPublic(Guid id, CancellationToken ct) => (from r in db.PublishedMediaReferences join d in db.ManagedContents on new { r.Key, r.Version } equals new { d.Key, Version = d.PublishedVersion } where r.MediaId == id select r.MediaId).AnyAsync(ct);
    public static Task Lock(TravelAgencyDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(624157113)", ct);
    private static MediaDto Dto(WebsiteMedia m) => new(m.Id, m.FileName, m.ContentType, m.AltSv, m.AltEn, m.Decorative, m.Archived, m.VersionToken);
}
