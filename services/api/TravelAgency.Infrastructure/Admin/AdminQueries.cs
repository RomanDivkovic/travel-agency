using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Admin;

public sealed class AdminQueries(TravelAgencyDbContext db) : IAdminQueries
{
    private static readonly Expression<Func<TravelInquiry, RequestListItem>> ListProjection = r => new(r.Id, r.ReferenceNumber,
        r.Contact.Name, r.Service == TravelService.Flights ? "flights" : r.Service == TravelService.Accommodation ? "accommodation" : r.Service == TravelService.FlightAccommodation ? "flight-accommodation" : "complete",
        r.Plan.Destination, r.Plan.FlexibleDestination, r.Plan.DateMode, r.Plan.Departure, r.Plan.Return, r.Plan.Earliest, r.Plan.Latest,
        r.CreatedAt, r.UpdatedAt, r.Status.ToString());
    private static string Service(TravelService service) => service switch { TravelService.Flights => "flights", TravelService.Accommodation => "accommodation", TravelService.FlightAccommodation => "flight-accommodation", _ => "complete" };
    public async Task<PageResult<RequestListItem>> ListAsync(RequestQuery query, CancellationToken ct)
    {
        if (!query.IsValid) throw new ArgumentException("Invalid query.");
        var source = db.TravelRequests.AsNoTracking();
        if (query.Status is not null) { var status = Enum.Parse<RequestStatus>(query.Status); source = source.Where(r => r.Status == status); }
        if (query.ServiceType is not null)
        {
            var service = query.ServiceType switch { "flights" => TravelService.Flights, "accommodation" => TravelService.Accommodation, "flight-accommodation" => TravelService.FlightAccommodation, _ => TravelService.Complete };
            source = source.Where(r => r.Service == service);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            source = source.Where(r => EF.Functions.ILike(r.ReferenceNumber, pattern, "\\") || EF.Functions.ILike(r.Contact.Name, pattern, "\\") || EF.Functions.ILike(r.Contact.Email, pattern, "\\"));
        }
        if (query.From is not null) { var from = new DateTimeOffset(query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)); source = source.Where(r => r.CreatedAt >= from); }
        if (query.To is not null) { var to = new DateTimeOffset(query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)); source = source.Where(r => r.CreatedAt < to); }
        var total = await source.CountAsync(ct);
        var sorted = query.Sort switch { "createdAt_asc" => source.OrderBy(r => r.CreatedAt), "updatedAt_desc" => source.OrderByDescending(r => r.UpdatedAt), _ => source.OrderByDescending(r => r.CreatedAt) };
        var items = await sorted.ThenBy(r => r.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(ListProjection).ToArrayAsync(ct);
        return new(items, query.Page, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }
    public async Task<AdminRequestDetail?> DetailAsync(Guid id, CancellationToken ct)
    {
        var r = await db.TravelRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        return r is null ? null : new(r.Id, r.ReferenceNumber, Service(r.Service), r.Status.ToString(), r.Language, r.Contact, r.Plan, r.CreatedAt, r.UpdatedAt, r.Version, TravelInquiry.AllowedTransitions(r.Status).Select(s => s.ToString()).ToArray());
    }
    public async Task<DashboardDto> DashboardAsync(CancellationToken ct)
    {
        var counts = await db.TravelRequests.AsNoTracking().GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Status, g => g.Count, ct);
        var recent = await db.TravelRequests.AsNoTracking().OrderByDescending(r => r.UpdatedAt).ThenBy(r => r.Id).Take(5).Select(ListProjection).ToArrayAsync(ct);
        return new(counts.Values.Sum(), counts.GetValueOrDefault(RequestStatus.Submitted), counts.GetValueOrDefault(RequestStatus.UnderReview), counts.GetValueOrDefault(RequestStatus.AwaitingCustomer), counts.GetValueOrDefault(RequestStatus.Closed), recent);
    }
    public async Task<PageResult<NoteDto>> NotesAsync(Guid id, int page, CancellationToken ct)
    {
        var source = db.RequestNotes.AsNoTracking().Where(n => n.RequestId == id);
        var count = await source.CountAsync(ct);
        var rows = await (from n in source join a in db.Users on n.AdministratorId equals a.Id orderby n.CreatedAt, n.Id select new NoteDto(n.Id, a.DisplayName, n.Content, n.CreatedAt)).Skip((page - 1) * 50).Take(50).ToArrayAsync(ct);
        return new(rows, page, 50, count, (int)Math.Ceiling(count / 50d));
    }
    public async Task<PageResult<ActivityDto>> ActivitiesAsync(Guid id, int page, CancellationToken ct)
    {
        var source = db.RequestActivities.AsNoTracking().Where(a => a.RequestId == id);
        var count = await source.CountAsync(ct);
        var rows = await (from a in source join u in db.Users on a.ActorId equals u.Id into authors from u in authors.DefaultIfEmpty() orderby a.CreatedAt, a.Id select new ActivityDto(a.Id, a.Type, u == null ? null : u.DisplayName, a.PreviousValue, a.NewValue, a.CreatedAt)).Skip((page - 1) * 50).Take(50).ToArrayAsync(ct);
        return new(rows, page, 50, count, (int)Math.Ceiling(count / 50d));
    }
}
