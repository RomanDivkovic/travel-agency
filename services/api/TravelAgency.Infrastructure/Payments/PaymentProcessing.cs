using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Payments;

public sealed class PaymentProcessing(TravelAgencyDbContext db, InvoiceOperations invoices, IPaymentProvider provider, PaymentSettings settings, TimeProvider clock)
{
    public async Task<PaymentSessionResult> SessionAsync(string raw, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await invoices.CustomerAsync(raw, ct);
        if (i.Status is not (InvoiceStatus.Issued or InvoiceStatus.PaymentPending or InvoiceStatus.Overdue) || i.IsSimulated != provider.IsSimulated || provider.Name == "Disabled") throw new InvoiceOperationException(409);
        var now = Quotation.Utc(clock.GetUtcNow());
        var attempt = await db.Payments.Where(p => p.InvoiceId == i.Id && p.Status == PaymentStatus.Pending && p.ExpiresAt > now).SingleOrDefaultAsync(ct);
        var attemptId = attempt?.Id ?? Guid.NewGuid();
        // Adapter must use AttemptId as its provider idempotency key. Sandbox creates in this same transaction.
        var session = await provider.CreateSessionAsync(new(attemptId, i.Id, i.TotalMinorUnits, i.Currency, settings.PublicOrigin + "/payment/result#" + raw), ct);
        if (attempt is null)
        {
            attempt = new PaymentTransaction
            {
                Id = attemptId,
                InvoiceId = i.Id,
                Provider = provider.Name,
                ProviderSessionId = session.SessionId,
                AmountMinorUnits = i.TotalMinorUnits,
                Currency = i.Currency,
                CreatedAt = now,
                ExpiresAt = session.ExpiresAt,
                NextReconcileAt = now.AddMinutes(1)
            }; db.Payments.Add(attempt); i.BeginPayment(now); invoices.Activity(i, "PaymentStarted", null);
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return session;
    }
    public async Task ApplyAsync(VerifiedPaymentEvent e, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Global event lock prevents a replay referencing another invoice racing the unique event constraint.
        var eventKey = BitConverter.ToInt64(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(provider.Name + ":" + e.EventId)), 0);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({eventKey})", ct);
        var seen = await db.PaymentEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Provider == provider.Name && x.EventId == e.EventId, ct);
        if (seen is not null) { if (seen.InvoiceId != e.InvoiceId || seen.Outcome != "processed") throw new InvoiceOperationException(409); return; }
        var i = await invoices.LockAsync(e.InvoiceId, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.InvoiceId == i.Id && p.Provider == provider.Name && p.ProviderSessionId == e.SessionId, ct);
        if (payment is null) throw new InvoiceOperationException(409);
        var eventRecord = new PaymentEventRecord { InvoiceId = i.Id, Provider = provider.Name, EventId = e.EventId, ReceivedAt = Quotation.Utc(clock.GetUtcNow()), Outcome = "processed" }; db.PaymentEvents.Add(eventRecord);
        if (e.AmountMinorUnits != i.TotalMinorUnits || e.AmountMinorUnits != payment.AmountMinorUnits || e.Currency != i.Currency || e.Currency != payment.Currency || i.IsSimulated != provider.IsSimulated)
        {
            payment.DiscrepancyCode = "amount-or-currency"; eventRecord.Outcome = "mismatch";
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw new InvoiceOperationException(409);
        }
        // Payment IDs are unique and cannot be reassigned to another attempt.
        if (await db.Payments.AnyAsync(p => p.Id != payment.Id && p.Provider == provider.Name && p.ProviderPaymentId == e.PaymentId, ct)) throw new InvoiceOperationException(409);
        var now = Quotation.Utc(clock.GetUtcNow());
        if (payment.Apply(e.Status, e.PaymentId, now))
        {
            if (e.Status == PaymentStatus.Succeeded)
            {
                if (i.MarkPaid(e.AmountMinorUnits, e.Currency, now))
                {
                    db.ResearchEligibility.Add(new ResearchEligibility { RequestId = i.RequestId, InvoiceId = i.Id, IsSimulated = i.IsSimulated, VerifiedAt = now });
                    invoices.Activity(i, "ServiceFeeVerified", null); invoices.Notify(i, "payment-confirmed"); invoices.Notify(i, "payment-agent");
                }
                else payment.DiscrepancyCode = "additional-payment-review";
            }
            else
            {
                invoices.Activity(i, e.Status == PaymentStatus.Failed ? "PaymentFailed" : "PaymentCancelled", null);
                // One failure email per invoice; later attempts remain visible without email flooding.
                if (e.Status == PaymentStatus.Failed && !await db.NotificationOutbox.AnyAsync(n => n.InvoiceId == i.Id && n.Kind == "payment-failed", ct)) invoices.Notify(i, "payment-failed");
            }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
