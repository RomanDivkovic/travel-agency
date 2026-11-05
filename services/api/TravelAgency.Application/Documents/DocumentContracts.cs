using TravelAgency.Domain.Documents;
using TravelAgency.Domain.Research;
namespace TravelAgency.Application.Documents;

public interface ITravelDocumentStorage
{
    Task UploadAsync(string key, ReadOnlyMemory<byte> content, CancellationToken ct);
    Task<byte[]> DownloadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
public interface IDocumentScanner { Task<DocumentScan> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken ct); }
public sealed record DocumentMetadata(string Name, string Description, string Type, Guid? ReservationId, bool NoIdentityDocuments);
public sealed record DocumentChange(Guid VersionToken, string Name, string Description);
public sealed record DocumentVersionInput(Guid VersionToken, int Version);
public sealed record DocumentVersionDto(Guid Id, int Version, string Name, string Description, string OriginalFileName, string ContentType, long Size, string Status, string Scan, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
public sealed record DocumentDto(Guid Id, Guid BookingId, Guid? ReservationId, string? ReservationTitle, string Type, int CurrentVersion, int? PublishedVersion, [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] Guid? VersionToken, DocumentVersionDto[] Versions);
public sealed record ReservationChoice(Guid Id, string Title, string Status);
public sealed record DocumentLibrary(string Reference, DocumentDto[] Items, int Page, int TotalPages, int TotalCount, long MaxBytes, ReservationChoice[] Reservations);
public sealed record ItinerarySave(Guid VersionToken, string Title, string Description, CustomEntryInput[] CustomEntries);
public sealed record ItineraryVersionInput(Guid VersionToken);
public sealed record ItineraryEntryDto(Guid Id, string Kind, string Event, string Title, string Description, DateOnly? Date, DateTimeOffset? At, string? TimeZone, string Location, string SupplierReference, bool Confirmed, int SortOrder);
public sealed record ItineraryVersionDto(Guid Id, int Version, string Status, string Title, string Description, TripSnapshot Trip, ItineraryEntryDto[] Entries, DateTimeOffset? PublishedAt, DateTimeOffset CreatedAt);
public sealed record ItineraryDto(Guid BookingId, string Reference, [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] Guid? VersionToken, int CurrentVersion, int? PublishedVersion, bool SourceChanged, ItineraryVersionDto[] Versions);
