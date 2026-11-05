using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Messaging;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Messaging;

public sealed class MessagingQueries(TravelAgencyDbContext db, MessagingAccess access, CustomerOwnership ownership, MessagingSettings settings)
{
    private IQueryable<ConversationItem> Source(string actor, bool admin)
    {
        var role = admin ? MessageSender.Administrator : MessageSender.Customer;
        return from c in db.Conversations.AsNoTracking()
               join u in db.Users on c.CustomerId equals u.Id
               join request in db.TravelRequests on c.RequestId equals request.Id into requests
               from request in requests.DefaultIfEmpty()
               join booking in db.Bookings on c.BookingId equals booking.Id into bookings
               from booking in bookings.DefaultIfEmpty()
               where admin || c.CustomerId == actor
               select new ConversationItem
               {
                   Id = c.Id,
                   Subject = c.Subject,
                   Status = c.Status.ToString(),
                   Priority = admin ? c.Priority.ToString() : null,
                   RequestId = c.RequestId,
                   RequestReference = request == null ? null : request.ReferenceNumber,
                   BookingId = c.BookingId,
                   BookingReference = booking == null ? null : booking.Reference,
                   CustomerName = admin ? u.FirstName + " " + u.LastName : null,
                   CustomerEmail = admin ? u.Email : null,
                   Preview = db.ConversationMessages.Where(m => m.ConversationId == c.Id).OrderByDescending(m => m.Sequence).Select(m => m.Body.Length > 150 ? m.Body.Substring(0, 150) : m.Body).FirstOrDefault() ?? "",
                   LastMessageAt = c.LastMessageAt,
                   UnreadCount = db.ConversationMessages.Count(m => m.ConversationId == c.Id && m.SenderRole != role && m.Sequence > (db.ConversationReads.Where(r => r.ConversationId == c.Id && r.ParticipantId == actor).Select(r => (long?)r.LastReadSequence).FirstOrDefault() ?? 0))
               };

    }
    public async Task<PageResult<ConversationItem>> ListAsync(string actor, bool admin, int page, string? status, string? priority, string? search, bool unread, Guid? requestId, Guid? bookingId, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || search?.Length > 150 || !admin && priority is not null) throw new MessagingOperationException(400);
        if (status is not null && (!Enum.TryParse<ConversationStatus>(status, out var st) || !Enum.IsDefined(st) || st.ToString() != status)) throw new MessagingOperationException(400); if (priority is not null && (!Enum.TryParse<ConversationPriority>(priority, out var pr) || !Enum.IsDefined(pr) || pr.ToString() != priority)) throw new MessagingOperationException(400);
        var q = Source(actor, admin); if (status is not null) q = q.Where(c => c.Status == status); if (priority is not null) q = q.Where(c => c.Priority == priority); if (unread) q = q.Where(c => c.UnreadCount > 0); if (requestId is { } rid) q = q.Where(c => c.RequestId == rid); if (bookingId is { } bid) q = q.Where(c => c.BookingId == bid);
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToUpper(); q = q.Where(c => c.Subject.ToUpper().Contains(s) || (c.CustomerName ?? "").ToUpper().Contains(s) || (c.CustomerEmail ?? "").ToUpper().Contains(s) || (c.RequestReference ?? "").ToUpper().Contains(s) || (c.BookingReference ?? "").ToUpper().Contains(s)); }
        var total = await q.CountAsync(ct); var items = await q.OrderByDescending(c => c.LastMessageAt).ThenBy(c => c.Id).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct); return new(items, page, 20, total, (int)Math.Ceiling(total / 20d));
    }
    public async Task<MessageSummary> SummaryAsync(string actor, bool admin, CancellationToken ct)
    {
        var q = Source(actor, admin); var unread = await q.SumAsync(c => (int?)c.UnreadCount, ct) ?? 0; var awaiting = await q.CountAsync(c => c.Status == (admin ? "AwaitingAgent" : "AwaitingCustomer"), ct); var high = admin ? await q.CountAsync(c => c.Status != "Closed" && (c.Priority == "High" || c.Priority == "Urgent"), ct) : 0; var recent = await q.OrderByDescending(c => c.LastMessageAt).ThenBy(c => c.Id).Take(5).ToArrayAsync(ct); return new(unread, awaiting, high, recent);
    }
    public async Task<ConversationDetail> DetailAsync(Guid id, string actor, bool admin, long? before, CancellationToken ct)
    {
        if (before is <= 0) throw new MessagingOperationException(400); await access.RequireAsync(id, actor, admin, false, ct); var c = await db.Conversations.AsNoTracking().SingleAsync(c => c.Id == id, ct);
        var messages = await (from m in db.ConversationMessages.AsNoTracking() join sender in db.Users on m.SenderId equals sender.Id into senders from sender in senders.DefaultIfEmpty() where m.ConversationId == id && (!before.HasValue || m.Sequence < before.Value) orderby m.Sequence descending select new MessageDto(m.Id, m.Sequence, m.SenderRole.ToString(), sender == null ? "" : m.SenderRole == MessageSender.Administrator ? sender.DisplayName : sender.FirstName, m.Body, m.CreatedAt)).Take(50).ToArrayAsync(ct); Array.Reverse(messages);
        var read = await db.ConversationReads.AsNoTracking().SingleOrDefaultAsync(r => r.ConversationId == id && r.ParticipantId == actor, ct); var events = admin ? await db.ConversationEvents.AsNoTracking().Where(e => e.ConversationId == id).OrderByDescending(e => e.CreatedAt).ThenBy(e => e.Id).Take(50).Select(e => new ConversationEventDto(e.Id, e.Type, e.Previous, e.Current, e.MessageSequence, e.CreatedAt)).ToArrayAsync(ct) : null;
        return new(await Source(actor, admin).SingleAsync(c => c.Id == id, ct), admin ? c.VersionToken : null, settings.MaxMessageLength, c.LastSequence, messages.Length > 0 && messages[0].Sequence > 1, messages, read?.EmailNotifications ?? true, events);
    }
    public async Task<PageResult<ConversationLink>> LinksAsync(string actor, string kind, int page, string? search, Guid? id, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || search?.Length > 150 || kind is not ("requests" or "bookings")) throw new MessagingOperationException(400); var owned = ownership.Owned(actor);
        var q = kind == "requests" ? db.TravelRequests.Where(r => owned.Any(o => o.RequestId == r.Id)).Select(r => new ConversationLink { Id = r.Id, Kind = kind, Reference = r.ReferenceNumber }) : db.Bookings.Where(b => owned.Any(o => o.RequestId == b.RequestId)).Select(b => new ConversationLink { Id = b.Id, Kind = kind, Reference = b.Reference }); if (id is { } selected) q = q.Where(l => l.Id == selected); if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToUpper(); q = q.Where(l => l.Reference.ToUpper().Contains(s)); }
        var total = await q.CountAsync(ct); return new(await q.OrderBy(l => l.Reference).ThenBy(l => l.Id).Skip((page - 1) * 20).Take(20).ToArrayAsync(ct), page, 20, total, (int)Math.Ceiling(total / 20d));
    }
}
