namespace TravelAgency.Domain.Messaging;

public enum ConversationStatus { Open, AwaitingCustomer, AwaitingAgent, Closed }
public enum ConversationPriority { Normal, High, Urgent }
public enum MessageSender { Customer, Administrator }
public sealed class MessagingRuleException(string message) : Exception(message);
public sealed class CustomerConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public Guid ClientId { get; set; }
    public string CreationHash { get; set; } = "";
    public Guid? RequestId { get; set; }
    public Guid? BookingId { get; set; }
    public string Subject { get; set; } = "";
    public ConversationStatus Status { get; set; }
    public ConversationPriority Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public long LastSequence { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public long CustomerNotifiedSequence { get; set; }
    public long AgentNotifiedSequence { get; set; }
    public DateTimeOffset? CustomerNotifyAfter { get; set; }
    public DateTimeOffset? AgentNotifyAfter { get; set; }
    public ConversationMessage Send(string sender, MessageSender role, Guid clientId, string body, int limit, DateTimeOffset now)
    {
        if (Status == ConversationStatus.Closed) throw new MessagingRuleException("This conversation is closed. Start a new conversation or ask the agent to reopen it.");
        if (clientId == Guid.Empty) throw new MessagingRuleException("A submission identifier is required.");
        body = MessageText.Normalize(body, limit); LastSequence++; LastMessageAt = now; UpdatedAt = now; VersionToken = Guid.NewGuid(); Status = role == MessageSender.Customer ? ConversationStatus.AwaitingAgent : ConversationStatus.AwaitingCustomer;
        return new() { ConversationId = Id, Sequence = LastSequence, SenderId = sender, SenderRole = role, ClientId = clientId, Body = body, CreatedAt = now };
    }
    public void SetStatus(ConversationStatus status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status)) throw new MessagingRuleException("Invalid status.");
        Status = status; ClosedAt = status == ConversationStatus.Closed ? now : null; UpdatedAt = now; VersionToken = Guid.NewGuid();
    }
    public void SetPriority(ConversationPriority priority, DateTimeOffset now) { if (!Enum.IsDefined(priority)) throw new MessagingRuleException("Invalid priority."); Priority = priority; UpdatedAt = now; VersionToken = Guid.NewGuid(); }
}
public sealed class ConversationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public long Sequence { get; set; }
    public string? SenderId { get; set; }
    public MessageSender SenderRole { get; set; }
    public Guid ClientId { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class ConversationReadState
{
    public Guid ConversationId { get; set; }
    public string ParticipantId { get; set; } = "";
    public MessageSender Role { get; set; }
    public long LastReadSequence { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public void Read(long through, DateTimeOffset now) { if (through > LastReadSequence) { LastReadSequence = through; LastReadAt = now; } }
}
public sealed class ConversationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public string? ActorId { get; set; }
    public string Type { get; set; } = "";
    public string? Previous { get; set; }
    public string? Current { get; set; }
    public long? MessageSequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public static class MessageText
{
    public static string Normalize(string? value, int max)
    {
        if (value is null || value.Length > max || value.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) throw new MessagingRuleException("Check message length and characters.");
        var text = string.Join('\n', value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(line => line.TrimEnd())).Trim();
        if (text.Length == 0) throw new MessagingRuleException("Message is required."); return text;
    }
    public static string Subject(string? value) { var text = Normalize(value, 150); if (text.Any(c => c is '\n' or '\t')) throw new MessagingRuleException("Subject must be a single line."); return text; }
}
