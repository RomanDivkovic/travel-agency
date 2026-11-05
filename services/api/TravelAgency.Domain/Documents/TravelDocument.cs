using TravelAgency.Domain.Research;
namespace TravelAgency.Domain.Documents;

public enum TravelDocumentType { FlightTicket, BoardingPass, FlightConfirmation, HotelConfirmation, TransferVoucher, ActivityVoucher, TravelInsurance, GeneralTravelInformation, Other }
public enum DocumentStatus { Draft, Published, Archived, Deleted }
public enum DocumentScan { Quarantined, Clean, Infected }
public sealed class DocumentRuleException(string message) : Exception(message);
public sealed class TravelDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid? ReservationId { get; set; }
    public TravelDocumentType Type { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public int? PublishedVersion { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public List<TravelDocumentVersion> Versions { get; set; } = [];
    public void Touch() => VersionToken = Guid.NewGuid();
    public void Publish(TravelDocumentVersion v, DateTimeOffset now)
    {
        if (v.DocumentId != Id || v.Version != CurrentVersion || v.Status != DocumentStatus.Draft || v.Scan != DocumentScan.Clean) throw new DocumentRuleException("Only the current clean draft can be published.");
        foreach (var old in Versions.Where(x => x.Status == DocumentStatus.Published)) { old.Status = DocumentStatus.Archived; old.ArchivedAt = now; old.UpdatedAt = now; }
        v.Status = DocumentStatus.Published; v.PublishedAt = now; v.UpdatedAt = now; PublishedVersion = v.Version; Touch();
    }
    public void Hide(TravelDocumentVersion v, bool delete, DateTimeOffset now)
    {
        if (v.DocumentId != Id || v.Status == DocumentStatus.Deleted || (!delete && v.Status == DocumentStatus.Archived)) throw new DocumentRuleException("Document version is unavailable.");
        v.Status = delete ? DocumentStatus.Deleted : DocumentStatus.Archived; v.ArchivedAt ??= now; v.UpdatedAt = now;
        if (PublishedVersion == v.Version) PublishedVersion = null; Touch();
    }
}
public sealed class TravelDocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid BlobId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentScan Scan { get; set; }
    public DateTimeOffset? ScannedAt { get; set; }
    public string UploadedById { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
public static class DocumentRules
{
    public static void Metadata(string name, string description) { if (!ResearchValidation.Text(name, 150) || !ResearchValidation.Text(description, 2000, false)) throw new DocumentRuleException("Invalid document metadata."); }
}
