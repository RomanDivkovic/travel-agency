using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelAgency.Application.Requests;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Infrastructure.Email;

public sealed class OutboxProcessor(TravelAgencyDbContext db, IEmailSender sender, EmailSettings settings, TimeProvider clock, ILogger<OutboxProcessor>? logger = null, QuotationTokenCodec? quotationCodec = null, InvoiceTokenCodec? invoiceCodec = null) : INotificationProcessor
{
    public static TimeSpan RetryDelay(int attempts) => TimeSpan.FromSeconds(Math.Min(3600, 30 * Math.Pow(2, Math.Clamp(attempts - 1, 0, 20))));
    public async Task<bool> ProcessOneAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = clock.GetUtcNow();
        var notifications = await db.NotificationOutbox.FromSqlInterpolated($"SELECT * FROM \"NotificationOutbox\" WHERE \"SentAt\" IS NULL AND \"DeadLetterAt\" IS NULL AND \"NextAttemptAt\" <= {now} ORDER BY \"NextAttemptAt\", \"Id\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        var notification = notifications.SingleOrDefault();
        if (notification is null) return false;
        notification.Attempts++;
        logger?.LogInformation("Notification {NotificationId} attempt {Attempt}", notification.Id, notification.Attempts);
        var request = await db.TravelRequests.SingleAsync(r => r.Id == notification.RequestId, ct);
        var retryable = true;
        try
        {
            EmailMessage? message;
            if (notification.InvoiceId is { } invoiceId)
            {
                var invoice = await db.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoiceId, ct);
                message = InvoiceNotificationTemplates.Create(notification, invoice, settings, invoiceCodec!);
            }
            else if (notification.QuotationId is { } quoteId)
            {
                var quote = await db.Quotations.AsNoTracking().Include(q => q.Lines).SingleAsync(q => q.Id == quoteId, ct);
                var number = await db.QuotationSeries.Where(s => s.Id == quote.SeriesId).Select(s => s.Number).SingleAsync(ct);
                message = QuotationNotificationTemplates.Create(notification, quote, number, settings, quotationCodec!, clock.GetUtcNow());
            }
            else message = NotificationTemplates.Create(notification.Id, notification.Kind, request, settings);
            if (message is null) { notification.DeadLetterAt = clock.GetUtcNow(); notification.ProtectedLink = null; }
            else { await sender.SendAsync(message, ct); notification.SentAt = clock.GetUtcNow(); notification.ProtectedLink = null; }
        }
        catch (EmailDeliveryException failure) { retryable = failure.Retryable; }
        catch (IOException) { }
        catch (Exception exception) when (exception is not OperationCanceledException) { retryable = false; }
        if (notification.SentAt is null)
        {
            if (!retryable || notification.Attempts >= settings.MaxAttempts) notification.DeadLetterAt = clock.GetUtcNow();
            else notification.NextAttemptAt = clock.GetUtcNow().Add(RetryDelay(notification.Attempts));
        }
        logger?.LogInformation("Notification {NotificationId} outcome {Outcome}", notification.Id, notification.SentAt.HasValue ? "accepted" : notification.DeadLetterAt.HasValue ? "failed" : "retry");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
