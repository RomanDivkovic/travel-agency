using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Bookings;

namespace TravelAgency.Infrastructure.Customers;

public sealed partial class CustomerPortalQueries
{
    private static readonly string[] PublicEvents = ["Submitted", "QuotationSent", "QuotationAccepted", "QuotationDeclined", "InvoiceIssued", "InvoiceOverdue", "ServiceFeeVerified", "ResearchStarted", "ProposalSent", "ProposalApproved", "ProposalRejected", "ProposalExpired", "CustomerLinked", "DocumentPublished", "ItineraryPublished"];
    public async Task<PageResult<PortalActivity>> ActivityAsync(string owner, Guid? requestId, int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000) throw new CustomerOperationException(400); if (requestId is { } request) await ownership.RequireAsync(owner, request, ct);
        var owned = ownership.Owned(owner); var q = db.RequestActivities.AsNoTracking().Where(a => owned.Any(o => o.RequestId == a.RequestId) && PublicEvents.Contains(a.Type)
            && (a.Type != "ServiceFeeVerified" || db.ResearchEligibility.Any(e => e.RequestId == a.RequestId && !e.IsSimulated)));
        if (requestId is { } id) q = q.Where(a => a.RequestId == id);
        var visible = q.Select(a => new PortalActivity { Id = a.Id, RequestId = a.RequestId, Type = a.Type, CreatedAt = a.CreatedAt });
        var booking = from e in db.BookingEvents.AsNoTracking()
                      join b in db.Bookings on e.BookingId equals b.Id
                      where e.Type == "BookingStatusChanged" && e.CustomerStatus != null && owned.Any(o => o.RequestId == b.RequestId) && (requestId == null || b.RequestId == requestId)
                      select new PortalActivity { Id = e.Id, RequestId = b.RequestId, Type = "Booking" + e.CustomerStatus, CreatedAt = e.CreatedAt };
        var all = visible.Concat(booking); var total = await all.CountAsync(ct);
        var items = await all.OrderByDescending(e => e.CreatedAt).ThenBy(e => e.Id).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        return new(items, page, 20, total, (int)Math.Ceiling(total / 20d));
    }
    public async Task<CustomerDashboardDto> DashboardAsync(string owner, CancellationToken ct)
    {
        var owned = ownership.Owned(owner);
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == owner, ct); var requests = await RequestsAsync(owner, 5, ct); var now = clock.GetUtcNow();
        var quotes = await Source(owner, "quotations", "Sent").Where(i => i.ValidUntil > now).OrderBy(i => i.ValidUntil).Take(10).ToArrayAsync(ct);
        var invoices = await (from i in db.Invoices.AsNoTracking()
                              join r in db.TravelRequests on i.RequestId equals r.Id
                              where i.IssuedAt != null && owned.Any(o => o.RequestId == i.RequestId) && (i.Status == Domain.Payments.InvoiceStatus.Issued || i.Status == Domain.Payments.InvoiceStatus.PaymentPending || i.Status == Domain.Payments.InvoiceStatus.Overdue)
                              orderby i.DueAt
                              select new PortalItem { Id = i.Id, RequestId = r.Id, Kind = "invoices", Number = i.Number!, Title = i.Description, Status = i.Status.ToString(), Destination = r.Plan.Destination, Departure = r.Plan.Departure, Currency = i.Currency, AmountMinorUnits = i.TotalMinorUnits, ValidUntil = i.DueAt, UpdatedAt = i.UpdatedAt, IsSimulated = i.IsSimulated }).Take(10).ToArrayAsync(ct);
        var proposals = await Source(owner, "proposals", "Sent").Where(i => i.ValidUntil > now).OrderBy(i => i.ValidUntil).Take(10).ToArrayAsync(ct);
        var today = DateOnly.FromDateTime(now.UtcDateTime); var upcoming = await Source(owner, "bookings", "Confirmed").Where(i => i.Departure >= today).OrderBy(i => i.Departure).Take(10).ToArrayAsync(ct);
        var activity = await ActivityAsync(owner, null, 1, ct);
        return new(CustomerAccounts.Dto(user), requests, quotes.Concat(invoices).Concat(proposals).OrderBy(i => i.ValidUntil).Take(10).ToArray(), upcoming, activity.Items.Take(10).ToArray());
    }
}
