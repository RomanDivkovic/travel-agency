using TravelAgency.Application.Quotations;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Requests;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Api.Requests;

public sealed class NotificationWorker(IServiceScopeFactory scopes, EmailSettings settings, ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextMaintenance = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTimeOffset.UtcNow >= nextMaintenance)
                {
                    await using var maintenanceScope = scopes.CreateAsyncScope();
                    await maintenanceScope.ServiceProvider.GetRequiredService<SubmissionMaintenance>().RunAsync(stoppingToken);
                    nextMaintenance = DateTimeOffset.UtcNow.AddHours(1);
                }
                await using (var expiryScope = scopes.CreateAsyncScope())
                    await expiryScope.ServiceProvider.GetRequiredService<IQuotationExpiration>().ExpireBatchAsync(stoppingToken);
                await using (var paymentScope = scopes.CreateAsyncScope())
                    await paymentScope.ServiceProvider.GetRequiredService<PaymentMaintenance>().RunAsync(stoppingToken);
                await using (var proposalScope = scopes.CreateAsyncScope())
                    await proposalScope.ServiceProvider.GetRequiredService<ProposalMaintenance>().RunAsync(stoppingToken);
                for (var i = 0; i < 10; i++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (!await scope.ServiceProvider.GetRequiredService<INotificationProcessor>().ProcessOneAsync(stoppingToken)) break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Notification processing unavailable; pending records remain stored."); }
            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
