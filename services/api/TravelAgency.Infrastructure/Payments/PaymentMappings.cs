using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Payments;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;

namespace TravelAgency.Infrastructure.Payments;

public static class PaymentMappings
{
    public static void ConfigurePayments(this ModelBuilder m)
    {
        var i = m.Entity<Invoice>(); i.HasKey(x => x.Id); i.HasIndex(x => x.QuotationId).IsUnique(); i.HasIndex(x => x.Number).IsUnique();
        i.HasIndex(x => new { x.Status, x.DueAt }); i.HasIndex(x => x.CreatedAt); i.Property(x => x.VersionToken).IsConcurrencyToken();
        i.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); i.Property(x => x.LinesJson).HasColumnType("jsonb");
        i.Property(x => x.Number).HasMaxLength(40); i.Property(x => x.Currency).HasMaxLength(3); i.Property(x => x.TaxTreatment).HasMaxLength(16);
        i.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        i.HasOne<Quotation>().WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Restrict);
        i.ToTable(t => { t.HasCheckConstraint("CK_Invoice_Money", "\"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" BETWEEN 1 AND 10000000000"); t.HasCheckConstraint("CK_Invoice_Status", "\"Status\" IN ('Draft','Issued','PaymentPending','Paid','Overdue','Cancelled')"); });
        var p = m.Entity<PaymentTransaction>(); p.HasKey(x => x.Id); p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        p.Property(x => x.Provider).HasMaxLength(24); p.Property(x => x.ProviderSessionId).HasMaxLength(100); p.Property(x => x.ProviderPaymentId).HasMaxLength(100); p.Property(x => x.Currency).HasMaxLength(3);
        p.HasIndex(x => new { x.Provider, x.ProviderSessionId }).IsUnique(); p.HasIndex(x => new { x.Provider, x.ProviderPaymentId }).IsUnique();
        p.HasIndex(x => new { x.Status, x.NextReconcileAt }); p.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        p.ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "\"AmountMinorUnits\" BETWEEN 1 AND 10000000000"));
        var e = m.Entity<PaymentEventRecord>(); e.HasKey(x => x.Id); e.Property(x => x.Provider).HasMaxLength(24); e.Property(x => x.EventId).HasMaxLength(100);
        e.HasIndex(x => new { x.Provider, x.EventId }).IsUnique(); e.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        var a = m.Entity<InvoiceAccessToken>(); a.HasKey(x => x.Id); a.Property(x => x.Hash).HasMaxLength(64); a.HasIndex(x => x.Hash).IsUnique(); a.HasIndex(x => x.InvoiceId).IsUnique(); a.HasIndex(x => x.ExpiresAt);
        a.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        var r = m.Entity<ResearchEligibility>(); r.HasKey(x => x.RequestId); r.HasIndex(x => x.InvoiceId).IsUnique(); r.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        r.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<InvoiceCounter>().HasKey(x => x.Id); m.Entity<InvoiceCounter>().HasData(new InvoiceCounter());
        var s = m.Entity<SandboxPayment>(); s.HasKey(x => x.Id); s.HasIndex(x => x.SessionId).IsUnique(); s.HasIndex(x => x.AccessHash).IsUnique(); s.Property(x => x.Status).HasConversion<string>();
        s.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
