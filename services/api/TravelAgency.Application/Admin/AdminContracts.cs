using TravelAgency.Domain.Requests;

namespace TravelAgency.Application.Admin;

public sealed record AdminUserDto(string Id, string DisplayName, string Email, string Role);
public sealed record RequestListItem(Guid Id, string ReferenceNumber, string CustomerName, string ServiceType, string? Destination,
    bool FlexibleDestination, string DateMode, DateOnly? Departure, DateOnly? Return, DateOnly? Earliest, DateOnly? Latest,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string Status);
public sealed record PageResult<T>(T[] Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record AdminRequestDetail(Guid Id, string ReferenceNumber, string ServiceType, string Status, string Language,
    CustomerContact Contact, TravelPlan Plan, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid Version, string[] AllowedStatuses);
public sealed record NoteDto(Guid Id, string Author, string Content, DateTimeOffset CreatedAt);
public sealed record ActivityDto(Guid Id, string Type, string? Author, string? PreviousValue, string? NewValue, DateTimeOffset CreatedAt, int? QuotationVersion = null);
public sealed record DashboardDto(int Total, int Submitted, int UnderReview, int AwaitingCustomer, int Closed, RequestListItem[] RecentlyUpdated);
public sealed record RequestQuery(int Page = 1, int PageSize = 20, string? Status = null, string? ServiceType = null,
    string? Search = null, string Sort = "createdAt_desc", DateOnly? From = null, DateOnly? To = null)
{
    public bool IsValid => Page is >= 1 and <= 100000 && PageSize is >= 1 and <= 100
        && (Status is null || Enum.TryParse<RequestStatus>(Status, false, out var status) && Enum.IsDefined(status) && status.ToString() == Status)
        && (ServiceType is null || ServiceType is "flights" or "accommodation" or "flight-accommodation" or "complete")
        && (Search is null || Search.Length <= 150) && Sort is "createdAt_desc" or "createdAt_asc" or "updatedAt_desc"
        && (From is null || To is null || From <= To) && To != DateOnly.MaxValue;
}
public interface IAdminQueries
{
    Task<PageResult<RequestListItem>> ListAsync(RequestQuery query, CancellationToken ct);
    Task<AdminRequestDetail?> DetailAsync(Guid id, CancellationToken ct);
    Task<DashboardDto> DashboardAsync(CancellationToken ct);
    Task<PageResult<NoteDto>> NotesAsync(Guid id, int page, CancellationToken ct);
    Task<PageResult<ActivityDto>> ActivitiesAsync(Guid id, int page, CancellationToken ct);
}
public enum MutationOutcome { Saved, NotFound, Invalid, Conflict }
public interface IAdminMutations
{
    Task<MutationOutcome> ChangeStatusAsync(Guid id, string status, Guid version, string actor, CancellationToken ct);
    Task<MutationOutcome> AddNoteAsync(Guid id, string? content, string actor, CancellationToken ct);
}
