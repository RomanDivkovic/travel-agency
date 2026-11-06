namespace TravelAgency.Infrastructure.Messaging;

public sealed class MessagingOperationException(int status) : Exception { public int Status { get; } = status; }
public sealed class MessagingSettings
{
    public int MaxMessageLength { get; set; } = 5000;
    public int NotificationDelaySeconds { get; set; } = 30;
    public int NotificationCooldownSeconds { get; set; } = 300;
    public bool CustomerEmailNotifications { get; set; } = true;
    public bool AgentEmailNotifications { get; set; } = true;
    public string AgentLanguage { get; set; } = "sv";
    public int ClosedRetentionDays { get; set; } = 365;
    public bool PurgeClosedConversations { get; set; }
}
