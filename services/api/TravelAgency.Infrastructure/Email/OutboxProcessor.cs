using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Bookings;
using TravelAgency.Infrastructure.Bookings;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Quotations;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Infrastructure.Email;

public sealed class OutboxProcessor(TravelAgencyDbContext db, IEmailSender sender, EmailSettings settings, TimeProvider clock, ILogger<OutboxProcessor>? logger = null, QuotationTokenCodec? quotationCodec = null, InvoiceTokenCodec? invoiceCodec = null, ProposalTokenCodec? proposalCodec = null) : INotificationProcessor
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
            if (notification.BookingId is { } bookingId)
            {
                // Lock this aggregate through delivery. Mutations never lock outbox rows, avoiding reverse lock ordering.
                await db.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR NO KEY UPDATE").ToArrayAsync(ct);
                var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == bookingId, ct);
                message = notification.Kind.StartsWith("booking-confirmed-", StringComparison.Ordinal) && (booking.Status != BookingStatus.Confirmed || booking.Revision != notification.BookingRevision)
                    ? null : BookingNotificationTemplates.Create(notification, settings);
            }
            else if (notification.ProposalId is { } proposalId)
            {
                var proposal = await db.TravelProposals.AsNoTracking().Include(p => p.Alternatives).SingleAsync(p => p.Id == proposalId, ct);
                var number = await db.ProposalSeries.Where(s => s.Id == proposal.SeriesId).Select(s => s.Number).SingleAsync(ct);
                message = ProposalNotificationTemplates.Create(notification, proposal, number, settings, proposalCodec!, clock.GetUtcNow());
            }
            else if (notification.InvoiceId is { } invoiceId)
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
            else
            {
                await sender.SendAsync(message, ct); notification.SentAt = clock.GetUtcNow(); notification.ProtectedLink = null;
                if (notification.BookingId is { } booking) db.RequestActivities.Add(new RequestActivity { RequestId = notification.RequestId, BookingId = booking, Type = "BookingMailSent", CreatedAt = clock.GetUtcNow() });
            }
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
