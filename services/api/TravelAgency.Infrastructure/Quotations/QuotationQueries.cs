using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationQueries(TravelAgencyDbContext db) : IQuotationQueries
{
    public async Task<PageResult<QuotationListItem>> ListAsync(QuotationQuery query, CancellationToken ct)
    {
        if (!query.IsValid) throw new ArgumentException("Invalid quotation query.");
        var source = from q in db.Quotations.AsNoTracking() join s in db.QuotationSeries on q.SeriesId equals s.Id join r in db.TravelRequests on s.RequestId equals r.Id select new { q, s, r };
        if (query.RequestId is not null) source = source.Where(x => x.s.RequestId == query.RequestId);
        if (query.Status is not null) { var status = Enum.Parse<QuotationStatus>(query.Status); source = source.Where(x => x.q.Status == status); }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            source = source.Where(x => EF.Functions.ILike(x.s.Number, search, "\\") || EF.Functions.ILike(x.r.ReferenceNumber, search, "\\") || EF.Functions.ILike(x.q.CustomerName, search, "\\") || EF.Functions.ILike(x.q.CustomerEmail, search, "\\"));
        }
        var total = await source.CountAsync(ct);
        var sorted = query.Sort switch { "createdAt_asc" => source.OrderBy(x => x.q.CreatedAt), "updatedAt_desc" => source.OrderByDescending(x => x.q.UpdatedAt), _ => source.OrderByDescending(x => x.q.CreatedAt) };
        var items = await sorted.ThenBy(x => x.q.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(x => new QuotationListItem(x.q.Id, x.s.RequestId,
            x.r.ReferenceNumber, x.s.Number, x.q.Version, x.q.CustomerName, x.r.Service == TravelService.Flights ? "flights" : x.r.Service == TravelService.Accommodation ? "accommodation" : x.r.Service == TravelService.FlightAccommodation ? "flight-accommodation" : "complete",
            x.q.Status.ToString(), x.q.Currency, x.q.TotalMinorUnits, x.q.CreatedAt, x.q.UpdatedAt, x.q.ValidUntil, x.q.SentAt, x.q.AcceptedAt, x.q.DeclinedAt)).ToArrayAsync(ct);
        return new(items, query.Page, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }
    public async Task<AdminQuotationDto?> DetailAsync(Guid id, CancellationToken ct)
    {
        var quote = await db.Quotations.AsNoTracking().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, ct); if (quote is null) return null;
        var series = await db.QuotationSeries.AsNoTracking().SingleAsync(s => s.Id == quote.SeriesId, ct);
        var reference = await db.TravelRequests.Where(r => r.Id == series.RequestId).Select(r => r.ReferenceNumber).SingleAsync(ct);
        var current = series.CurrentVersion == quote.Version;
        return new(id, series.RequestId, reference, quote.VersionToken, quote.CreatedAt, quote.UpdatedAt, quote.SentAt, quote.CustomerEmail, current,
            current && quote.CanRevise, QuotationTransaction.CustomerDto(quote, series.Number));
    }
    public async Task<PageResult<ActivityDto>> HistoryAsync(Guid id, int page, CancellationToken ct)
    {
        var seriesId = await db.Quotations.Where(q => q.Id == id).Select(q => q.SeriesId).SingleAsync(ct);
        var source = from a in db.RequestActivities.AsNoTracking() join q in db.Quotations on a.QuotationId equals q.Id where q.SeriesId == seriesId select new { a, q };
        var count = await source.CountAsync(ct);
        var rows = await (from x in source
                          join u in db.Users on x.a.ActorId equals u.Id into authors
                          from u in authors.DefaultIfEmpty()
                          orderby x.a.CreatedAt, x.a.Id
                          select new ActivityDto(x.a.Id, x.a.Type, u == null ? null : u.DisplayName, x.a.PreviousValue, x.a.NewValue, x.a.CreatedAt, x.q.Version)).Skip((page - 1) * 50).Take(50).ToArrayAsync(ct);
        return new(rows, page, 50, count, (int)Math.Ceiling(count / 50d));
    }
}
