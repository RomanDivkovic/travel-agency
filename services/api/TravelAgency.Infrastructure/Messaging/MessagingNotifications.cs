using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;
namespace TravelAgency.Infrastructure.Messaging;

public sealed record ConversationMail(bool ToCustomer, string Origin);
public sealed record MessagingDelivery(EmailMessage? Message, DateTimeOffset? DeferUntil, CustomerConversation? Conversation, bool ToCustomer, long Sequence)
{
    public void Complete(DateTimeOffset now, MessagingSettings settings)
    {
        if (Conversation is null) return; if (ToCustomer) { Conversation.CustomerNotifiedSequence = Sequence; Conversation.CustomerNotifyAfter = now.AddSeconds(settings.NotificationCooldownSeconds); } else { Conversation.AgentNotifiedSequence = Sequence; Conversation.AgentNotifyAfter = now.AddSeconds(settings.NotificationCooldownSeconds); }
    }
}
public sealed class MessagingNotifications(TravelAgencyDbContext db, MessagingSettings settings, CustomerSettings customers, TimeProvider clock)
{
    public void Queue(CustomerConversation c, ConversationMessage message)
    {
        var toCustomer = message.SenderRole == MessageSender.Administrator; if (toCustomer ? !settings.CustomerEmailNotifications : !settings.AgentEmailNotifications) return;
        if (customers.PublicOrigin.Length == 0) throw new MessagingOperationException(412);
        db.NotificationOutbox.Add(new() { ConversationId = c.Id, Kind = (toCustomer ? "message-customer-" : "message-agent-") + message.Id.ToString("N"), PayloadJson = JsonSerializer.Serialize(new ConversationMail(toCustomer, customers.PublicOrigin)), NextAttemptAt = clock.GetUtcNow().AddSeconds(settings.NotificationDelaySeconds) });
    }
    public static async Task<MessagingDelivery> PrepareAsync(NotificationRecord job, TravelAgencyDbContext db, MessagingSettings settings, EmailSettings email, DateTimeOffset now, CancellationToken ct)
    {
        var c = await db.Conversations.SingleOrDefaultAsync(c => c.Id == job.ConversationId, ct); if (c is null) return new(null, null, null, false, 0); if (settings.PurgeClosedConversations && c.Status == ConversationStatus.Closed && c.ClosedAt < now.AddDays(-settings.ClosedRetentionDays)) return new(null, null, null, false, 0);
        var payload = JsonSerializer.Deserialize<ConversationMail>(job.PayloadJson!)!; var customer = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == c.CustomerId && u.IsCustomer && u.EmailConfirmed && !u.CustomerDisabled, ct); if (customer is null) return new(null, null, null, false, 0);
        var recipient = payload.ToCustomer ? MessageSender.Customer : MessageSender.Administrator; var source = payload.ToCustomer ? MessageSender.Administrator : MessageSender.Customer;
        var notified = payload.ToCustomer ? c.CustomerNotifiedSequence : c.AgentNotifiedSequence; var last = await db.ConversationMessages.Where(m => m.ConversationId == c.Id && m.SenderRole == source).Select(m => (long?)m.Sequence).MaxAsync(ct) ?? 0;
        var read = payload.ToCustomer ? (await db.ConversationReads.Where(r => r.ConversationId == c.Id && r.ParticipantId == c.CustomerId).Select(r => (long?)r.LastReadSequence).SingleOrDefaultAsync(ct) ?? 0) : (await db.ConversationReads.Where(r => r.ConversationId == c.Id && r.Role == recipient).Select(r => (long?)r.LastReadSequence).MaxAsync(ct) ?? 0);
        var optedOut = payload.ToCustomer && await db.ConversationReads.AnyAsync(r => r.ConversationId == c.Id && r.ParticipantId == c.CustomerId && !r.EmailNotifications, ct);
        if (last <= Math.Max(notified, read) || optedOut || (payload.ToCustomer ? !settings.CustomerEmailNotifications : !settings.AgentEmailNotifications)) return new(null, null, null, false, 0);
        var after = payload.ToCustomer ? c.CustomerNotifyAfter : c.AgentNotifyAfter; if (after > now) return new(null, after, null, payload.ToCustomer, 0);
        var sv = (payload.ToCustomer ? customer.PreferredLanguage : settings.AgentLanguage) == "sv"; var link = payload.Origin + (payload.ToCustomer ? "/account/messages/" : "/admin/messages/") + c.Id;
        var subject = payload.ToCustomer ? sv ? "Du har fått ett nytt meddelande om din resa" : "You have a new message about your trip" : sv ? "Nytt kundmeddelande — " + c.Subject : "New customer message — " + c.Subject;
        var text = payload.ToCustomer ? (sv ? $"Hej {customer.FirstName},\n\nDu har fått ett nytt meddelande från {email.BusinessName}.\n\nÄrende: {c.Subject}\n\nLogga in på din kundportal för att läsa meddelandet och svara.\n" : $"Hello {customer.FirstName},\n\nYou have received a new message from {email.BusinessName}.\n\nConversation: {c.Subject}\n\nSign in to your customer portal to read the message and reply.\n") : (sv ? $"Hej,\n\nEtt nytt kundmeddelande har kommit in.\nKund: {customer.FirstName} {customer.LastName}\nÄrende: {c.Subject}\n\nÖppna adminpanelen för att läsa och svara.\n" : $"Hello,\n\nA new customer message has arrived.\nCustomer: {customer.FirstName} {customer.LastName}\nConversation: {c.Subject}\n\nOpen the administrator inbox to read and reply.\n");
        text += "\n" + link + "\n\n" + (sv ? "Vänliga hälsningar," : "Kind regards,") + "\n" + email.BusinessName; var html = WebUtility.HtmlEncode(text).Replace("\n", "<br>").Replace(WebUtility.HtmlEncode(link), $"<a href=\"{WebUtility.HtmlEncode(link)}\" rel=\"noreferrer\">{WebUtility.HtmlEncode(payload.ToCustomer ? sv ? "Öppna meddelanden" : "Open messages" : sv ? "Öppna inkorgen" : "Open inbox")}</a>");
        return new(new(job.Id, payload.ToCustomer ? customer.Email! : email.AgentRecipient, subject, text, "<div>" + html + "</div>"), null, c, payload.ToCustomer, last);
    }
}
