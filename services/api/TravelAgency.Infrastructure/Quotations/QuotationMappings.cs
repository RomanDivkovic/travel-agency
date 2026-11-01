using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Quotations;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;

namespace TravelAgency.Infrastructure.Quotations;

public static class QuotationMappings
{
    public static void ConfigureQuotations(this ModelBuilder model)
    {
        model.HasSequence<long>("quotation_reference");
        var series = model.Entity<QuotationSeries>(); series.HasKey(s => s.Id);
        series.Property(s => s.Number).HasMaxLength(40).IsRequired(); series.HasIndex(s => s.Number).IsUnique();
        series.HasIndex(s => s.RequestId).IsUnique();
        series.HasOne<TravelInquiry>().WithMany().HasForeignKey(s => s.RequestId).OnDelete(DeleteBehavior.Cascade);
        var quote = model.Entity<Quotation>(); quote.HasKey(q => q.Id);
        quote.HasOne<QuotationSeries>().WithMany().HasForeignKey(q => q.SeriesId).OnDelete(DeleteBehavior.Cascade);
        quote.HasOne<Administrator>().WithMany().HasForeignKey(q => q.CreatedByAdministratorId).OnDelete(DeleteBehavior.Restrict);
        quote.Property(q => q.Status).HasConversion<string>().HasMaxLength(16);
        quote.Property(q => q.TaxTreatment).HasConversion<string>().HasMaxLength(16);
        quote.Property(q => q.VersionToken).IsConcurrencyToken();
        quote.HasIndex(q => new { q.SeriesId, q.Version }).IsUnique();
        quote.HasIndex(q => q.SeriesId).IsUnique().HasDatabaseName("IX_Quotation_ActiveVersion").HasFilter("\"Status\" IN ('Draft', 'Sent', 'Accepted')");
        quote.HasIndex(q => new { q.Status, q.ValidUntil }); quote.HasIndex(q => q.CreatedAt); quote.HasIndex(q => q.UpdatedAt);
        quote.Property(q => q.Language).HasMaxLength(2).IsRequired(); quote.Property(q => q.Currency).HasMaxLength(3).IsRequired();
        quote.Property(q => q.CustomerName).HasMaxLength(100).IsRequired(); quote.Property(q => q.CustomerEmail).HasMaxLength(254).IsRequired();
        quote.Property(q => q.Description).HasMaxLength(1000).IsRequired(); quote.Property(q => q.IncludedServices).HasMaxLength(2000).IsRequired();
        quote.Property(q => q.ExcludedServices).HasMaxLength(2000).IsRequired(); quote.Property(q => q.CustomerNotes).HasMaxLength(2000).IsRequired();
        quote.Property(q => q.TermsSnapshot).HasMaxLength(6000).IsRequired(); quote.Property(q => q.TermsVersion).HasMaxLength(100).IsRequired();
        quote.Property(q => q.AcceptedTermsVersion).HasMaxLength(100); quote.Property(q => q.AcceptanceTextSnapshot).HasMaxLength(2000).IsRequired();
        quote.HasMany(q => q.Lines).WithOne().HasForeignKey(l => l.QuotationId).OnDelete(DeleteBehavior.Cascade);
        quote.Navigation(q => q.Lines).HasField("lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        quote.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Quotation_Status", "\"Status\" IN ('Draft','Sent','Accepted','Declined','Expired','Superseded','Cancelled')");
            t.HasCheckConstraint("CK_Quotation_Money", "\"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" <= 10000000000");
            t.HasCheckConstraint("CK_Quotation_Version", "\"Version\" >= 1");
        });
        var line = model.Entity<QuotationLineItem>(); line.HasKey(l => l.Id);
        line.Property(l => l.Description).HasMaxLength(200).IsRequired(); line.HasIndex(l => new { l.QuotationId, l.SortOrder });
        line.ToTable(t => t.HasCheckConstraint("CK_QuotationLine_Money", "\"Quantity\" BETWEEN 1 AND 100 AND \"UnitPriceMinorUnits\" BETWEEN 0 AND 10000000000 AND \"TaxRateBasisPoints\" BETWEEN 0 AND 10000 AND \"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" <= 10000000000"));
        var token = model.Entity<QuotationAccessToken>(); token.HasKey(t => t.Id);
        token.Property(t => t.Hash).HasMaxLength(64).IsRequired(); token.HasIndex(t => t.Hash).IsUnique(); token.HasIndex(t => t.ExpiresAt);
        token.HasIndex(t => t.QuotationId).IsUnique(); token.HasOne<Quotation>().WithMany().HasForeignKey(t => t.QuotationId).OnDelete(DeleteBehavior.Cascade);
    }
}
