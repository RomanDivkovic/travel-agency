using System.Text.Json.Serialization;
namespace TravelAgency.Application.Messaging;

public sealed record NewConversation(Guid ClientId, string Subject, string Body, Guid? RequestId, Guid? BookingId);
public sealed record SendMessage(Guid ClientId, string Body);
public sealed record ConversationChange(Guid VersionToken, string Value);
public sealed record ConversationRead(long ThroughSequence);
public sealed record ConversationPreferences(bool EmailNotifications);
public sealed record MessageDto(Guid Id, long Sequence, string SenderRole, string SenderName, string Body, DateTimeOffset CreatedAt);
public sealed record ConversationItem
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = "";
    public string Status { get; init; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Priority { get; init; }
    public Guid? RequestId { get; init; }
    public string? RequestReference { get; init; }
    public Guid? BookingId { get; init; }
    public string? BookingReference { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CustomerName { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CustomerEmail { get; init; }
    public string Preview { get; init; } = "";
    public DateTimeOffset LastMessageAt { get; init; }
    public int UnreadCount { get; init; }
}

public sealed record ConversationEventDto(Guid Id, string Type, string? Previous, string? Current, long? MessageSequence, DateTimeOffset CreatedAt);
public sealed record ConversationDetail(ConversationItem Conversation, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? VersionToken, int MaxMessageLength, long LastSequence, bool MoreBefore, MessageDto[] Messages, bool EmailNotifications, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ConversationEventDto[]? History);
public sealed record MessageSummary(int UnreadCount, int AwaitingReply, int HighPriority, ConversationItem[] Recent);
public sealed record ConversationLink { public Guid Id { get; init; } public string Kind { get; init; } = ""; public string Reference { get; init; } = ""; }
