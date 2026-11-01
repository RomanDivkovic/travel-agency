using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Payments;

public sealed class PaymentMaintenance(TravelAgencyDbContext db, InvoiceOperations invoices, PaymentProcessing processing, IPaymentProvider provider, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var overdue = await db.Invoices.Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PaymentPending) && i.DueAt <= now).OrderBy(i => i.DueAt).Select(i => i.Id).Take(20).ToArrayAsync(ct);
        foreach (var id in overdue)
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await invoices.LockAsync(id, ct);
            if (i.MarkOverdue(now)) invoices.Activity(i, "InvoiceOverdue", null); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        var pending = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Pending && p.NextReconcileAt <= now && p.ReconcileAttempts < 8).OrderBy(p => p.NextReconcileAt).Select(p => new { p.Id, p.InvoiceId }).Take(10).ToArrayAsync(ct);
        foreach (var item in pending)
        {
            db.ChangeTracker.Clear(); string session;
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                await invoices.LockAsync(item.InvoiceId, ct); var p = await db.Payments.SingleAsync(p => p.Id == item.Id, ct);
                if (p.Status != PaymentStatus.Pending || p.NextReconcileAt > now || p.ReconcileAttempts >= 8) continue;
                p.ReconcileAttempts++; p.NextReconcileAt = now.AddMinutes(Math.Min(360, Math.Pow(2, p.ReconcileAttempts))); session = p.ProviderSessionId;
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            }
            db.ChangeTracker.Clear();
            try { var evidence = await provider.ReconcileAsync(session, ct); if (evidence is not null) await processing.ApplyAsync(evidence, ct); }
            catch (Exception e) when (e is PaymentProviderException or InvoiceOperationException or HttpRequestException)
            {
                db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct); await invoices.LockAsync(item.InvoiceId, ct);
                var p = await db.Payments.SingleAsync(p => p.Id == item.Id, ct); p.DiscrepancyCode ??= "reconciliation-unavailable"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            }
        }
        db.ChangeTracker.Clear();
        await db.SandboxPayments.Where(s => s.ExpiresAt <= now && s.ProtectedAccess != "").ExecuteUpdateAsync(s => s.SetProperty(x => x.ProtectedAccess, "").SetProperty(x => x.ProtectedReturnUrl, ""), ct);
        await db.NotificationOutbox.Where(n => n.InvoiceId != null && n.ProtectedLink != null && db.InvoiceTokens.Any(t => t.InvoiceId == n.InvoiceId && (t.ExpiresAt <= now || t.RevokedAt != null)))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ProtectedLink, (string?)null), ct);
    }
}
