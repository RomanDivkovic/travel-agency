using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Messaging;
using TravelAgency.Domain.Messaging;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Infrastructure.Messaging;

public sealed class MessagingOperations(TravelAgencyDbContext db, MessagingAccess access, CustomerActions accounts, MessagingSettings settings, MessagingNotifications notifications, TimeProvider clock)
{
    public async Task<Guid> CreateAsync(string actor, NewConversation input, CancellationToken ct)
    {
        if (input.ClientId == Guid.Empty) throw new MessagingOperationException(400); var subject = MessageText.Subject(input.Subject); var body = MessageText.Normalize(input.Body, settings.MaxMessageLength); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { subject, body, input.RequestId, input.BookingId }))));
        await using var tx = await db.Database.BeginTransactionAsync(ct); var user = await accounts.LockUserAsync(actor, ct); if (!user.IsCustomer || !user.EmailConfirmed || user.CustomerDisabled) throw new MessagingOperationException(403);
        var existing = await db.Conversations.SingleOrDefaultAsync(c => c.CustomerId == actor && c.ClientId == input.ClientId, ct); if (existing is not null) { if (existing.CreationHash != hash) throw new MessagingOperationException(409); return existing.Id; }
        var relation = await access.AssociationAsync(actor, input.RequestId, input.BookingId, ct); var now = clock.GetUtcNow(); var c = new CustomerConversation { CustomerId = actor, ClientId = input.ClientId, CreationHash = hash, RequestId = relation.Request, BookingId = relation.Booking, Subject = subject, CreatedAt = now, UpdatedAt = now }; db.Conversations.Add(c); var message = c.Send(actor, MessageSender.Customer, input.ClientId, body, settings.MaxMessageLength, now); db.ConversationMessages.Add(message); access.Audit(c.Id, actor, "Created", now); access.Audit(c.Id, actor, "MessageCreated", now, sequence: message.Sequence); notifications.Queue(c, message); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return c.Id;
    }
    public async Task<Guid> SendAsync(Guid id, string actor, bool admin, SendMessage input, CancellationToken ct)
    {
        if (input.ClientId == Guid.Empty) throw new MessagingOperationException(400); var body = MessageText.Normalize(input.Body, settings.MaxMessageLength); await using var tx = await db.Database.BeginTransactionAsync(ct); var c = await access.RequireAsync(id, actor, admin, true, ct);
        var existing = await db.ConversationMessages.SingleOrDefaultAsync(m => m.ConversationId == id && m.SenderId == actor && m.ClientId == input.ClientId, ct); if (existing is not null) { if (existing.Body != body) throw new MessagingOperationException(409); return existing.Id; }
        var previous = c.Status; var now = clock.GetUtcNow(); var message = c.Send(actor, admin ? MessageSender.Administrator : MessageSender.Customer, input.ClientId, body, settings.MaxMessageLength, now); db.ConversationMessages.Add(message); access.Audit(id, actor, "MessageCreated", now, sequence: message.Sequence); if (c.Status != previous) access.Audit(id, actor, "StatusChanged", now, previous.ToString(), c.Status.ToString()); notifications.Queue(c, message); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return message.Id;
    }
    public async Task ChangeAsync(Guid id, string actor, string operation, ConversationChange input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var c = await access.RequireAsync(id, actor, true, true, ct); MessagingAccess.Version(input.VersionToken, c.VersionToken); var now = clock.GetUtcNow();
        if (operation == "status") { if (!Enum.TryParse<ConversationStatus>(input.Value, out var status) || !Enum.IsDefined(status) || status.ToString() != input.Value) throw new MessagingOperationException(400); var previous = c.Status; c.SetStatus(status, now); access.Audit(id, actor, status == ConversationStatus.Closed ? "Closed" : previous == ConversationStatus.Closed ? "Reopened" : "StatusChanged", now, previous.ToString(), status.ToString()); }
        else { if (!Enum.TryParse<ConversationPriority>(input.Value, out var priority) || !Enum.IsDefined(priority) || priority.ToString() != input.Value) throw new MessagingOperationException(400); var previous = c.Priority; c.SetPriority(priority, now); access.Audit(id, actor, "PriorityChanged", now, previous.ToString(), priority.ToString()); }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ReadAsync(Guid id, string actor, bool admin, long through, bool? email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await access.RequireAsync(id, actor, admin, true, ct); if (through < 0 || through > 0 && !await db.ConversationMessages.AnyAsync(m => m.ConversationId == id && m.Sequence == through, ct)) throw new MessagingOperationException(400);
        var read = await db.ConversationReads.SingleOrDefaultAsync(r => r.ConversationId == id && r.ParticipantId == actor, ct); if (read is null) { read = new() { ConversationId = id, ParticipantId = actor, Role = admin ? MessageSender.Administrator : MessageSender.Customer }; db.ConversationReads.Add(read); }
        read.Read(through, clock.GetUtcNow()); if (email.HasValue) read.EmailNotifications = email.Value; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
