using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Customers;

public sealed partial class CustomerPortalQueries(TravelAgencyDbContext db, CustomerOwnership ownership, TimeProvider clock)
{
    public async Task<PageResult<PortalItem>> ListAsync(string owner, string kind, int page, string? status, string? sort, Guid? requestId, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || sort is not (null or "updated" or "departure" or "reference")) throw new CustomerOperationException(400);
        if (requestId is { } request) await ownership.RequireAsync(owner, request, ct);
        var q = Source(owner, kind, status); if (requestId is { } id) q = q.Where(i => i.RequestId == id);
        var count = await q.CountAsync(ct);
        var ordered = sort switch { "departure" => q.OrderBy(i => i.Departure).ThenBy(i => i.Id), "reference" => q.OrderBy(i => i.Number).ThenBy(i => i.Id), _ => q.OrderByDescending(i => i.UpdatedAt).ThenBy(i => i.Id) };
        var items = await ordered.Skip((page - 1) * 20).Take(20).ToArrayAsync(ct);
        if (kind == "requests") { var journeys = await JourneysAsync(items.Select(i => i.Id).ToArray(), ct); items = items.Select(i => i with { Journey = journeys[i.Id] }).ToArray(); }
        return new(items, page, 20, count, (int)Math.Ceiling(count / 20d));
    }
    private IQueryable<PortalItem> Source(string owner, string kind, string? status)
    {
        var owned = ownership.Owned(owner); status = string.IsNullOrEmpty(status) ? null : status;
        switch (kind)
        {
            case "requests":
                var requests = db.TravelRequests.AsNoTracking().Where(r => owned.Any(o => o.RequestId == r.Id));
                if (status is not null) { var state = Parse<RequestStatus>(status); requests = requests.Where(r => r.Status == state); }
                return requests.Select(r => new PortalItem { Id = r.Id, RequestId = r.Id, Kind = kind, Number = r.ReferenceNumber, Title = r.Service.ToString(), Status = r.Status.ToString(), Destination = r.Plan.Destination, Departure = r.Plan.Departure ?? r.Plan.Earliest, Currency = null, AmountMinorUnits = null, ValidUntil = null, UpdatedAt = r.UpdatedAt });
            case "quotations":
                var quotes = db.Quotations.AsNoTracking().Where(q => q.SentAt != null && db.QuotationSeries.Any(s => s.Id == q.SeriesId && owned.Any(o => o.RequestId == s.RequestId)));
                if (status is not null) { var state = Parse<QuotationStatus>(status); quotes = quotes.Where(q => q.Status == state); }
                return from q in quotes join s in db.QuotationSeries on q.SeriesId equals s.Id join r in db.TravelRequests on s.RequestId equals r.Id select new PortalItem { Id = q.Id, RequestId = r.Id, Kind = kind, Number = s.Number, Title = q.Description, Status = q.Status.ToString(), Destination = r.Plan.Destination, Departure = r.Plan.Departure ?? r.Plan.Earliest, Currency = q.Currency, AmountMinorUnits = q.TotalMinorUnits, ValidUntil = q.ValidUntil, UpdatedAt = q.UpdatedAt };
            case "invoices":
                var invoices = db.Invoices.AsNoTracking().Where(i => i.IssuedAt != null && owned.Any(o => o.RequestId == i.RequestId));
                if (status is not null) { var state = Parse<InvoiceStatus>(status); invoices = invoices.Where(i => i.Status == state); }
                return from i in invoices join r in db.TravelRequests on i.RequestId equals r.Id select new PortalItem { Id = i.Id, RequestId = r.Id, Kind = kind, Number = i.Number!, Title = i.Description, Status = i.Status.ToString(), Destination = r.Plan.Destination, Departure = r.Plan.Departure ?? r.Plan.Earliest, Currency = i.Currency, AmountMinorUnits = i.TotalMinorUnits, ValidUntil = i.DueAt, UpdatedAt = i.UpdatedAt, IsSimulated = i.IsSimulated };
            case "proposals":
                var proposals = db.TravelProposals.AsNoTracking().Where(p => p.SentAt != null && db.ProposalSeries.Any(s => s.Id == p.SeriesId && owned.Any(o => o.RequestId == s.RequestId)));
                if (status is not null) { var state = Parse<ProposalStatus>(status); proposals = proposals.Where(p => p.Status == state); }
                return from p in proposals join s in db.ProposalSeries on p.SeriesId equals s.Id join r in db.TravelRequests on s.RequestId equals r.Id select new PortalItem { Id = p.Id, RequestId = r.Id, Kind = kind, Number = s.Number, Title = p.Title, Status = p.Status.ToString(), Destination = r.Plan.Destination, Departure = r.Plan.Departure ?? r.Plan.Earliest, Currency = null, AmountMinorUnits = null, ValidUntil = p.ValidUntil, UpdatedAt = p.UpdatedAt };
            case "bookings":
                var bookings = db.Bookings.AsNoTracking().Where(b => owned.Any(o => o.RequestId == b.RequestId));
                if (status is not null) { var state = Parse<BookingStatus>(status); bookings = bookings.Where(b => b.Status == state); }
                return from b in bookings join r in db.TravelRequests on b.RequestId equals r.Id select new PortalItem { Id = b.Id, RequestId = r.Id, Kind = kind, Number = b.Reference, Title = "", Status = b.Status.ToString(), Destination = r.Plan.Destination, Departure = b.DepartureDate, Currency = null, AmountMinorUnits = null, ValidUntil = null, UpdatedAt = b.UpdatedAt };
            default: throw new CustomerOperationException(404);
        }
    }
    private static T Parse<T>(string value) where T : struct, Enum => Enum.TryParse<T>(value, out var state) && Enum.IsDefined(state) && state.ToString() == value ? state : throw new CustomerOperationException(400);
    public async Task<CustomerRequestDto> RequestAsync(string owner, Guid id, CancellationToken ct)
    {
        await ownership.RequireAsync(owner, id, ct); var r = await db.TravelRequests.AsNoTracking().SingleAsync(r => r.Id == id, ct); var journeys = await JourneysAsync([id], ct); return Request(r, journeys[id]);
    }
    public async Task<CustomerRequestDto[]> RequestsAsync(string owner, int count, CancellationToken ct)
    {
        var owned = ownership.Owned(owner);
        var rows = await db.TravelRequests.AsNoTracking().Where(r => owned.Any(o => o.RequestId == r.Id)).OrderByDescending(r => r.UpdatedAt).Take(count).ToArrayAsync(ct);
        var journeys = await JourneysAsync(rows.Select(r => r.Id).ToArray(), ct); return rows.Select(r => Request(r, journeys[r.Id])).ToArray();
    }
    private static CustomerRequestDto Request(TravelInquiry r, CustomerJourneyDto journey) => new(r.Id, r.ReferenceNumber, r.Service.ToString(), r.Status.ToString(), r.Language, r.Contact, r.Plan, r.CreatedAt, r.UpdatedAt, journey);
    private async Task<Dictionary<Guid, CustomerJourneyDto>> JourneysAsync(Guid[] ids, CancellationToken ct)
    {
        var requests = await db.TravelRequests.AsNoTracking().Where(r => ids.Contains(r.Id)).ToArrayAsync(ct);
        var quotes = await (from q in db.Quotations.AsNoTracking() join s in db.QuotationSeries on q.SeriesId equals s.Id where ids.Contains(s.RequestId) && q.Version == s.CurrentVersion select new { s.RequestId, Quote = q }).ToDictionaryAsync(x => x.RequestId, ct);
        var invoices = (await db.Invoices.AsNoTracking().Where(i => ids.Contains(i.RequestId)).ToArrayAsync(ct)).GroupBy(i => i.RequestId).ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.CreatedAt).First());
        var paid = await (from e in db.ResearchEligibility join i in db.Invoices on e.InvoiceId equals i.Id where ids.Contains(e.RequestId) && !e.IsSimulated && !i.IsSimulated && i.RequestId == e.RequestId && i.Status == InvoiceStatus.Paid && db.Payments.Any(p => p.InvoiceId == i.Id && p.Status == PaymentStatus.Succeeded && p.AmountMinorUnits == i.TotalMinorUnits && p.Currency == i.Currency && p.Provider != "Sandbox" && p.Provider != "Disabled") select e.RequestId).ToArrayAsync(ct);
        var research = await db.ResearchWorkspaces.Where(w => ids.Contains(w.RequestId)).Select(w => w.RequestId).ToArrayAsync(ct);
        var proposals = await (from p in db.TravelProposals.AsNoTracking() join s in db.ProposalSeries on p.SeriesId equals s.Id where ids.Contains(s.RequestId) && p.Version == s.CurrentVersion select new { s.RequestId, Proposal = p }).ToDictionaryAsync(x => x.RequestId, ct);
        var bookings = await db.Bookings.AsNoTracking().Where(b => ids.Contains(b.RequestId)).ToDictionaryAsync(b => b.RequestId, ct); var now = clock.GetUtcNow();
        return requests.ToDictionary(r => r.Id, r =>
        {
            var q = quotes.GetValueOrDefault(r.Id)?.Quote; var i = invoices.GetValueOrDefault(r.Id); var p = proposals.GetValueOrDefault(r.Id)?.Proposal; var b = bookings.GetValueOrDefault(r.Id);
            var qs = q?.Status == QuotationStatus.Sent && q.ValidUntil <= now ? "Expired" : q?.Status.ToString(); var ps = p?.Status == ProposalStatus.Sent && p.ValidUntil <= now ? "Expired" : p?.Status.ToString();
            var result = Domain.Customers.CustomerJourney.Project(r.Status.ToString(), qs, q?.SentAt is not null, i?.Status.ToString(), i?.IsSimulated ?? false, paid.Contains(r.Id), research.Contains(r.Id), ps, p?.SentAt is not null, b?.Status.ToString());
            return new CustomerJourneyDto(result.Stage, result.Completed, result.NextAction);
        });
    }
    public async Task<CustomerBookingDto> BookingAsync(string owner, Guid id, CancellationToken ct)
    {
        var b = await db.Bookings.AsNoTracking().Include(b => b.Reservations).SingleOrDefaultAsync(b => b.Id == id, ct) ?? throw new CustomerOperationException(404); await ownership.RequireAsync(owner, b.RequestId, ct);
        var p = await db.TravelProposals.AsNoTracking().SingleAsync(p => p.Id == b.ProposalId, ct);
        return new(b.Id, b.RequestId, b.Reference, b.Status.ToString(), p.Trip(), b.Reservations.Select(r => new CustomerBookingReservation(r.Status == ReservationStatus.Confirmed ? r.Current().Title : r.Approved().Title, r.Status == ReservationStatus.Confirmed ? r.ReservationType : r.Approved().Kind == "Extra" ? r.Approved().Extra!.Category : r.Approved().Kind, r.Status.ToString(), r.Status == ReservationStatus.Confirmed ? r.Current() : null, r.Status == ReservationStatus.Confirmed ? r.SupplierReference : null, r.Status == ReservationStatus.Confirmed ? r.ConfirmedAt : null, r.Status == ReservationStatus.Confirmed ? r.TicketingStatus : null)).ToArray());
    }
}
