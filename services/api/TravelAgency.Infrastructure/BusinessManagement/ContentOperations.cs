using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.BusinessManagement;
using TravelAgency.Domain.BusinessManagement;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.BusinessManagement;

public sealed class ContentOperations(TravelAgencyDbContext db, TimeProvider clock)
{
    public async Task<ContentListItem[]> List(CancellationToken ct)
    {
        var stored = await db.ManagedContents.AsNoTracking().ToArrayAsync(ct);
        return ContentSchema.Baseline.Keys.Concat(ContentSchema.Policies.Select(p => "policy:" + p)).Concat(stored.Select(d => d.Key)).Distinct().Order().Select(k =>
        {
            var d = stored.SingleOrDefault(d => d.Key == k); var data = d is null ? ContentSchema.Default(k) : Parse(d.DraftJson);
            var titles = k.StartsWith("faq:", StringComparison.Ordinal) ? (ContentSchema.Read<FaqData>(data).Sv.Question, ContentSchema.Read<FaqData>(data).En.Question) : k.StartsWith("service:", StringComparison.Ordinal) ? (ContentSchema.Read<ServiceData>(data).Sv.Name, ContentSchema.Read<ServiceData>(data).En.Name) : k.StartsWith("policy:", StringComparison.Ordinal) ? (ContentSchema.Read<PolicyData>(data).Sv.Title, ContentSchema.Read<PolicyData>(data).En.Title) : ("", "");
            return new ContentListItem(k, d?.PublishedVersion ?? 0, d?.UpdatedAt, titles.Item1, titles.Item2);
        }).ToArray();
    }
    public async Task<ContentDetail> Detail(string key, CancellationToken ct)
    {
        Key(key); var d = await db.ManagedContents.AsNoTracking().SingleOrDefaultAsync(d => d.Key == key, ct);
        var history = await db.ContentRevisions.AsNoTracking().Where(v => v.Key == key).OrderByDescending(v => v.Version).Take(100).ToArrayAsync(ct);
        var actorIds = history.Where(v => v.PublishedBy != null).Select(v => v.PublishedBy!).Distinct().ToArray();
        var actors = await db.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        return new(key, d?.VersionToken ?? Guid.Empty, d is null ? ContentSchema.Default(key) : Parse(d.DraftJson), d?.PublishedVersion ?? 0, d?.UpdatedAt, d?.UpdatedBy, history.Select(v => new ContentVersionDto(v.Version, Parse(v.DataJson), v.PublishedAt, v.PublishedBy is not null && actors.TryGetValue(v.PublishedBy, out var name) ? name : null)).ToArray());
    }
    public async Task Save(string key, ContentDraftInput input, string actor, CancellationToken ct)
    {
        Key(key); var json = ContentSchema.Validate(key, input.Data, false);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(key, ct);
        var d = await db.ManagedContents.SingleOrDefaultAsync(d => d.Key == key, ct); Check(d?.VersionToken ?? Guid.Empty, input.VersionToken);
        if (d is null) { if (await db.ManagedContents.CountAsync(ct) >= 300) throw new ContentOperationException(409, "limit"); d = new() { Key = key }; db.ManagedContents.Add(d); }
        await ValidateMedia(key, input.Data, ct); d.Save(json, actor, clock.GetUtcNow()); Audit(key, "DraftSaved", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task Publish(string key, ContentAction input, string actor, CancellationToken ct)
    {
        Key(key); if (!input.Confirmed) throw new ContentOperationException(400, "confirmation");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(key, ct);
        var d = await db.ManagedContents.SingleOrDefaultAsync(d => d.Key == key, ct) ?? throw new ContentOperationException(404); Check(d.VersionToken, input.VersionToken);
        var data = Parse(d.DraftJson); _ = ContentSchema.Validate(key, data, true); await ValidateMedia(key, data, ct);
        var v = d.Publish(actor, clock.GetUtcNow()); db.ContentRevisions.Add(v);
        if (key.StartsWith("page:", StringComparison.Ordinal) && ContentSchema.Read<PageData>(data).Image is { } image) db.PublishedMediaReferences.Add(new() { Key = key, Version = v.Version, MediaId = image.Id });
        Audit(key, "Published", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task Restore(string key, ContentAction input, string actor, CancellationToken ct)
    {
        Key(key); await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(key, ct);
        var d = await db.ManagedContents.SingleOrDefaultAsync(d => d.Key == key, ct) ?? throw new ContentOperationException(404); Check(d.VersionToken, input.VersionToken);
        var v = await db.ContentRevisions.AsNoTracking().SingleOrDefaultAsync(v => v.Key == key && v.Version == input.RestoreVersion, ct) ?? throw new ContentOperationException(404);
        d.Save(v.DataJson, actor, clock.GetUtcNow()); Audit(key, "RestoredToDraft", actor); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<PublicContent> Published(CancellationToken ct, string? previewKey = null)
    {
        var docs = ContentSchema.Baseline.Where(x => x.Key != "preferences" && x.Key != "business").ToDictionary(x => x.Key, x => x.Value.Clone());
        var issued = await (from d in db.ManagedContents.AsNoTracking() join v in db.ContentRevisions.AsNoTracking() on new { d.Key, Version = d.PublishedVersion } equals new { v.Key, v.Version } select v).ToArrayAsync(ct);
        foreach (var v in issued) if (v.Key != "preferences") docs[v.Key] = Parse(v.DataJson);
        // Future policies remain private until effective; preserve the latest already-effective revision.
        foreach (var k in issued.Where(x => x.Key.StartsWith("policy:", StringComparison.Ordinal)).Select(x => x.Key))
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var history = await db.ContentRevisions.AsNoTracking().Where(x => x.Key == k).OrderByDescending(x => x.Version).ToArrayAsync(ct);
            var active = history.FirstOrDefault(x => ContentSchema.Read<PolicyData>(Parse(x.DataJson)).EffectiveFrom <= today);
            if (active is null) docs.Remove(k); else docs[k] = Parse(active.DataJson);
        }
        if (previewKey is not null) { Key(previewKey); if (previewKey == "preferences") throw new ContentOperationException(400); docs[previewKey] = (await Detail(previewKey, ct)).Draft; }
        if (docs.TryGetValue("business", out var businessData)) { var business = ContentSchema.Read<BusinessData>(businessData); if (!business.PublicContactVisible) docs["business"] = ContentSchema.Element(business with { ContactEmail = "", ContactPhone = "", Address = "", PostalCode = "", City = "" }); }
        foreach (var k in docs.Keys.Where(k => k.StartsWith("policy:", StringComparison.Ordinal)).ToArray())
        {
            var policy = ContentSchema.Read<PolicyData>(docs[k]);
            docs[k] = ContentSchema.Element(new PublicPolicyData(policy.Sv, policy.En, policy.EffectiveFrom));
        }
        return new(docs);
    }
    public async Task<BusinessPreferences> Preferences(CancellationToken ct)
    {
        var json = await (from d in db.ManagedContents.AsNoTracking() join v in db.ContentRevisions.AsNoTracking() on new { d.Key, Version = d.PublishedVersion } equals new { v.Key, v.Version } where d.Key == "preferences" select v.DataJson).SingleOrDefaultAsync(ct);
        return ContentSchema.Read<BusinessPreferences>(json is null ? ContentSchema.Baseline["preferences"] : Parse(json));
    }
    private async Task ValidateMedia(string key, JsonElement data, CancellationToken ct)
    {
        if (key.StartsWith("page:", StringComparison.Ordinal) && ContentSchema.Read<PageData>(data).Image is { } i)
        {
            await MediaOperations.Lock(db, ct);
            if (!await db.WebsiteMedia.AnyAsync(m => m.Id == i.Id && !m.Archived, ct)) throw new ContentOperationException(400, "media");
        }
    }
    private Task Lock(string key, CancellationToken ct) { var id = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes("cms:" + key))); return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({id})", ct); }
    private void Audit(string key, string action, string actor) => db.ContentAudits.Add(new() { Key = key, Action = action, ActorId = actor, CreatedAt = clock.GetUtcNow() });
    private static void Check(Guid actual, Guid expected) { if (actual != expected) throw new ContentOperationException(409, "conflict"); }
    private static void Key(string key) { if (!ContentSchema.IsKey(key)) throw new ContentOperationException(404); }
    public static JsonElement Parse(string json) { using var d = JsonDocument.Parse(json); return d.RootElement.Clone(); }
}
