using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAgency.Application.Requests;

namespace TravelAgency.Infrastructure.Email;

public sealed class EmailSender(HttpClient client, EmailSettings settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (settings.Provider == "Capture")
        {
            var directory = Path.GetFullPath(settings.CaptureDirectory);
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var destination = Path.Combine(directory, $"{message.Id:D}.json");
            if (File.Exists(destination)) return;
            var temp = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
            try
            {
                var fileOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                await using (var stream = new FileStream(temp, fileOptions))
                    await JsonSerializer.SerializeAsync(stream, message, SubmissionHandler.JsonOptions, ct);
                try { File.Move(temp, destination); } catch (IOException) when (File.Exists(destination)) { }
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return;
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Add("Idempotency-Key", $"notification-{message.Id:D}");
        request.Content = JsonContent.Create(new { from = settings.FromAddress, to = new[] { message.To }, subject = message.Subject, html = message.Html, text = message.Text });
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw new EmailDeliveryException(response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)response.StatusCode >= 500);
        }
        catch (HttpRequestException) { throw new EmailDeliveryException(true); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new EmailDeliveryException(true); }
    }
}
