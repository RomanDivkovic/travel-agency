using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Api.Requests;

public static partial class RequestFeatureSetup
{
    public static void AddRequestFeature(this WebApplicationBuilder builder)
    {
        var requests = builder.Configuration.GetSection("Requests").Get<SubmissionSettings>() ?? new();
        var email = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new();
        if (!PrefixPattern().IsMatch(requests.ReferencePrefix) || requests.IdempotencyRetentionDays is < 7 or > 365
            || requests.RequestRetentionDays < requests.IdempotencyRetentionDays || requests.RequestRetentionDays > 3650
            || requests.PermitLimit is < 1 or > 1000 || requests.WindowSeconds is < 1 or > 3600)
            throw new InvalidOperationException("Invalid Requests configuration.");
        static bool Mailbox(string value) => MailAddress.TryCreate(value, out var address) && address.Address == value && !value.Contains('\r') && !value.Contains('\n');
        if (email.Provider is not ("Capture" or "Resend") || !Mailbox(email.FromAddress) || !Mailbox(email.AgentRecipient)
            || string.IsNullOrWhiteSpace(email.BusinessName) || email.BusinessName.Length > 120
            || email.MaxAttempts is < 1 or > 10 || email.PollSeconds is < 1 or > 300
            || (email.Provider == "Resend" && (string.IsNullOrWhiteSpace(email.ApiKey) || email.ApiKey.Length > 512 || email.ApiKey.Contains('\r') || email.ApiKey.Contains('\n')))
            || (email.Provider == "Capture" && !builder.Environment.IsDevelopment()))
            throw new InvalidOperationException("Configure a valid email provider and public sender/agent addresses. Capture is development-only.");
        if (!builder.Environment.IsDevelopment() && (email.FromAddress.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
            || email.AgentRecipient.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
            || builder.Configuration["Requests:RequestRetentionDays"] is null))
            throw new InvalidOperationException("Configure production email addresses and an approved request retention period.");
        if (email.Provider == "Capture") email.CaptureDirectory = Path.GetFullPath(email.CaptureDirectory, builder.Environment.ContentRootPath);
        builder.Services.AddSingleton(requests);
        builder.Services.AddSingleton(email);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ITravelRequestStore, PostgresTravelRequestStore>();
        builder.Services.AddScoped<SubmissionHandler>();
        builder.Services.AddScoped<SubmissionMaintenance>();
        builder.Services.AddScoped<INotificationProcessor, OutboxProcessor>();
        builder.Services.AddHttpClient<IEmailSender, EmailSender>(client => client.Timeout = TimeSpan.FromSeconds(10));
        if (email.WorkerEnabled) builder.Services.AddHostedService<NotificationWorker>();
        builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 32768);
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("travel-requests", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = requests.PermitLimit, Window = TimeSpan.FromSeconds(requests.WindowSeconds), QueueLimit = 0, AutoReplenishment = true }));
            options.OnRejected = async (context, ct) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? Math.Ceiling(retry.TotalSeconds) : requests.WindowSeconds;
                context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, seconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("TravelRequests").LogWarning("API rate limit reached. Trace {TraceId}", context.HttpContext.TraceIdentifier);
                await Results.Problem(statusCode: 429, title: "Please wait before retrying.").ExecuteAsync(context.HttpContext);
            };
        });
    }
    [GeneratedRegex("^[A-Z]{2,8}$")]
    private static partial Regex PrefixPattern();
}
