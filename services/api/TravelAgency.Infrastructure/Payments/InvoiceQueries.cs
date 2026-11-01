using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Payments;

public sealed class InvoiceQueries(TravelAgencyDbContext db, TimeProvider clock)
{
    public async Task<AdminInvoiceView?> DetailAsync(Guid id, CancellationToken ct)
    {
        var i = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (i is null) return null;
        var payments = await db.Payments.AsNoTracking().Where(p => p.InvoiceId == id).OrderByDescending(p => p.CreatedAt).Take(100).ToArrayAsync(ct);
        var eligibility = await db.ResearchEligibility.AsNoTracking().SingleOrDefaultAsync(e => e.InvoiceId == id, ct);
        return new(i.Id, i.RequestId, i.QuotationId, i.VersionToken, InvoiceDtos.Customer(i), payments.Select(InvoiceDtos.Attempt).ToArray(), eligibility is { IsSimulated: false }, eligibility is { IsSimulated: true }, await db.InvoiceTokens.AnyAsync(t => t.InvoiceId == id && t.RevokedAt == null && t.ExpiresAt > clock.GetUtcNow(), ct));
    }
    public async Task<InvoicePage> ListAsync(int page, string? status, string? search, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || search?.Length > 100 || (!string.IsNullOrEmpty(status) && (!Enum.TryParse<InvoiceStatus>(status, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != status))) throw new InvoiceOperationException(400);
        var q = db.Invoices.AsNoTracking().AsQueryable(); if (!string.IsNullOrEmpty(status)) { var state = Enum.Parse<InvoiceStatus>(status); q = q.Where(i => i.Status == state); }
        if (!string.IsNullOrWhiteSpace(search)) { var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%"; q = q.Where(i => i.Number != null && EF.Functions.ILike(i.Number, pattern)); }
        var total = await q.CountAsync(ct); var items = await q.OrderByDescending(i => i.CreatedAt).ThenBy(i => i.Id).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        return new(items.Select(InvoiceDtos.Item).ToArray(), page, 20, total);
    }
    public async Task<PaymentSummary> SummaryAsync(CancellationToken ct)
    {
        var amounts = await db.Invoices.AsNoTracking().GroupBy(i => new { i.Currency, i.IsSimulated }).Select(g => new
        {
            g.Key.Currency,
            g.Key.IsSimulated,
            Paid = g.Sum(i => i.Status == InvoiceStatus.Paid ? i.TotalMinorUnits : 0),
            Outstanding = g.Sum(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PaymentPending || i.Status == InvoiceStatus.Overdue ? i.TotalMinorUnits : 0),
            Overdue = g.Count(i => i.Status == InvoiceStatus.Overdue)
        }).ToArrayAsync(ct);
        var pendingByCurrency = await (from p in db.Payments.AsNoTracking()
                                       join i in db.Invoices.AsNoTracking() on p.InvoiceId equals i.Id
                                       where p.Status == PaymentStatus.Pending && i.Status != InvoiceStatus.Paid
                                       group p by new { i.Currency, i.IsSimulated } into g
                                       select new { g.Key.Currency, g.Key.IsSimulated, Count = g.Select(p => p.InvoiceId).Distinct().Count() }).ToArrayAsync(ct);
        var recent = await db.Invoices.AsNoTracking().Where(i => i.Status == InvoiceStatus.Paid).OrderByDescending(i => i.PaidAt).Take(10).ToArrayAsync(ct);
        return new(amounts.Select(x => new CurrencySummary(x.Currency, x.IsSimulated, x.Paid, x.Outstanding, pendingByCurrency.FirstOrDefault(p => p.Currency == x.Currency && p.IsSimulated == x.IsSimulated)?.Count ?? 0, x.Overdue)).ToArray(), recent.Select(InvoiceDtos.Item).ToArray());
    }
}
