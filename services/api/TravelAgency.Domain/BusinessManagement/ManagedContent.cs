namespace TravelAgency.Domain.BusinessManagement;

public sealed class ManagedContent
{
    public string Key { get; set; } = "";
    public string DraftJson { get; set; } = "{}";
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public int PublishedVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public void Save(string json, string actor, DateTimeOffset now) { DraftJson = json; UpdatedBy = actor; UpdatedAt = now; VersionToken = Guid.NewGuid(); }
    public ContentRevision Publish(string actor, DateTimeOffset now)
    {
        if (DraftJson == "{}") throw new InvalidOperationException("Empty draft.");
        PublishedVersion++; VersionToken = Guid.NewGuid(); UpdatedAt = now; UpdatedBy = actor;
        return new() { Key = Key, Version = PublishedVersion, DataJson = DraftJson, PublishedAt = now, PublishedBy = actor };
    }
}
public sealed class ContentRevision
{
    public string Key { get; set; } = "";
    public int Version { get; set; }
    public string DataJson { get; set; } = "{}";
    public DateTimeOffset PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
}
public sealed class ContentAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public string Action { get; set; } = "";
    public string? ActorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class WebsiteMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Bytes { get; set; } = [];
    public string AltSv { get; set; } = "";
    public string AltEn { get; set; } = "";
    public bool Decorative { get; set; }
    public bool Archived { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
