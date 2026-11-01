namespace TravelAgency.Infrastructure.Email;

public sealed class EmailSettings
{
    public string Provider { get; set; } = "Capture";
    public string BusinessName { get; set; } = "Your Travel Planner";
    public string FromAddress { get; set; } = "local@travel.invalid";
    public string AgentRecipient { get; set; } = "agent@travel.invalid";
    public string ApiKey { get; set; } = "";
    public string CaptureDirectory { get; set; } = ".local-mail";
    public int MaxAttempts { get; set; } = 8;
    public int PollSeconds { get; set; } = 5;
    public bool WorkerEnabled { get; set; } = true;
}
