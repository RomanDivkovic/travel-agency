using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Research;

public sealed class ProposalQueries(TravelAgencyDbContext db, ResearchTransaction work, TimeProvider clock)
{
    public async Task<PageResult<ProposalListItem>> ListAsync(int page, string? status, string? search, Guid? requestId, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || search?.Length > 100 || (!string.IsNullOrEmpty(status) && (!Enum.TryParse<ProposalStatus>(status, out var state) || !Enum.IsDefined(state) || state.ToString() != status))) throw new ResearchOperationException(400);
        var source = from p in db.TravelProposals.AsNoTracking() join s in db.ProposalSeries on p.SeriesId equals s.Id select new { p, s };
        if (requestId is { } id) source = source.Where(x => x.s.RequestId == id);
        if (!string.IsNullOrEmpty(status)) { var filter = Enum.Parse<ProposalStatus>(status); source = source.Where(x => x.p.Status == filter); }
        if (!string.IsNullOrWhiteSpace(search)) { var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%"; source = source.Where(x => EF.Functions.ILike(x.s.Number, pattern, "\\")); }
        var count = await source.CountAsync(ct); var rows = await source.OrderByDescending(x => x.p.UpdatedAt).ThenBy(x => x.p.Id).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        var selectedIds = rows.Where(x => x.p.SelectedAlternativeId != null).Select(x => x.p.SelectedAlternativeId!.Value).ToArray();
        var titles = await db.Set<ProposalAlternative>().AsNoTracking().Where(a => selectedIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Title, ct);
        return new(rows.Select(x => ProposalDtos.Item(x.p, x.s, x.p.SelectedAlternativeId is { } selected && titles.TryGetValue(selected, out var title) ? title : null)).ToArray(), page, 20, count, (int)Math.Ceiling(count / 20d));
    }
    public async Task<AdminProposalDto?> DetailAsync(Guid id, CancellationToken ct)
    {
        var p = await db.TravelProposals.AsNoTracking().Include(p => p.Alternatives).ThenInclude(a => a.Items).SingleOrDefaultAsync(p => p.Id == id, ct); if (p is null) return null;
        var s = await db.ProposalSeries.AsNoTracking().SingleAsync(s => s.Id == p.SeriesId, ct); var eligible = await work.EligibleAsync(s.RequestId, ct);
        return new(id, s.RequestId, p.VersionToken, p.CustomerEmail, s.CurrentVersion == p.Version, s.CurrentVersion == p.Version && p.CanRevise,
            ProposalDtos.Customer(p, s.Number, eligible), p.Alternatives.OrderBy(a => a.SortOrder).Select(a => a.Items.OrderBy(i => i.SortOrder).Select(i => i.ResearchOptionId).ToArray()).ToArray(), await db.ProposalDecisions.Where(d => d.ProposalId == id).Select(d => d.Feedback).SingleOrDefaultAsync(ct), await db.ProposalTokens.AnyAsync(t => t.ProposalId == id && t.RevokedAt == null && t.ExpiresAt > clock.GetUtcNow(), ct));
    }
    public async Task<PageResult<ActivityDto>> HistoryAsync(Guid id, int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000) throw new ResearchOperationException(400);
        var series = await db.TravelProposals.Where(p => p.Id == id).Select(p => (Guid?)p.SeriesId).SingleOrDefaultAsync(ct) ?? throw new ResearchOperationException(404);
        var q = from a in db.RequestActivities.AsNoTracking() join p in db.TravelProposals on a.ProposalId equals p.Id where p.SeriesId == series select new { a, p };
        var count = await q.CountAsync(ct);
        var rows = await (from x in q
                          join u in db.Users on x.a.ActorId equals u.Id into users
                          from u in users.DefaultIfEmpty()
                          orderby x.a.CreatedAt, x.a.Id
                          select new ActivityDto(x.a.Id, x.a.Type, u == null ? null : u.DisplayName, x.a.PreviousValue, x.a.NewValue, x.a.CreatedAt)).Skip((page - 1) * 50).Take(50).ToArrayAsync(ct);
        return new(rows, page, 50, count, (int)Math.Ceiling(count / 50d));
    }
}
