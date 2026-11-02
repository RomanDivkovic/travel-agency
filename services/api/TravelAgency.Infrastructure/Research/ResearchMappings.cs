using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Requests;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Admin;

namespace TravelAgency.Infrastructure.Research;

public static class ResearchMappings
{
    public static void ConfigureResearch(this ModelBuilder m)
    {
        var w = m.Entity<ResearchWorkspace>(); w.HasKey(x => x.RequestId); w.Property(x => x.VersionToken).IsConcurrencyToken(); w.Property(x => x.Notes).HasMaxLength(4000); w.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        var o = m.Entity<ResearchOption>(); o.HasKey(x => x.Id); o.Property(x => x.VersionToken).IsConcurrencyToken(); o.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16); o.Property(x => x.Notes).HasMaxLength(4000); o.Property(x => x.SnapshotJson).HasColumnType("jsonb");
        o.HasIndex(x => new { x.RequestId, x.Kind }); o.HasOne<ResearchWorkspace>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        m.HasSequence<long>("proposal_reference");
        var s = m.Entity<ProposalSeries>(); s.HasKey(x => x.Id); s.Property(x => x.Number).HasMaxLength(40); s.HasIndex(x => x.Number).IsUnique(); s.HasIndex(x => x.RequestId).IsUnique(); s.HasOne<ResearchWorkspace>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        var p = m.Entity<TravelProposal>(); p.HasKey(x => x.Id); p.Property(x => x.VersionToken).IsConcurrencyToken(); p.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        p.HasIndex(x => new { x.SeriesId, x.Version }).IsUnique(); p.HasIndex(x => x.SeriesId).IsUnique().HasDatabaseName("IX_Proposal_Current").HasFilter("\"Status\" IN ('Draft','Sent','Approved')"); p.HasIndex(x => new { x.Status, x.ValidUntil }); p.HasIndex(x => x.UpdatedAt);
        p.HasOne<ProposalSeries>().WithMany().HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade); p.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CreatedByAdministratorId).OnDelete(DeleteBehavior.Restrict);
        p.Property(x => x.Title).HasMaxLength(200); p.Property(x => x.Introduction).HasMaxLength(3000); p.Property(x => x.Summary).HasMaxLength(3000); p.Property(x => x.TermsSnapshot).HasMaxLength(6000); p.Property(x => x.TermsVersion).HasMaxLength(100); p.Property(x => x.AcceptanceText).HasMaxLength(2000);
        p.Property(x => x.Language).HasMaxLength(2); p.Property(x => x.CustomerName).HasMaxLength(100); p.Property(x => x.CustomerEmail).HasMaxLength(254); p.Property(x => x.TripJson).HasColumnType("jsonb"); p.Property(x => x.PaidServiceJson).HasColumnType("jsonb");
        p.HasMany(x => x.Alternatives).WithOne().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Cascade); p.Navigation(x => x.Alternatives).HasField("alternatives").UsePropertyAccessMode(PropertyAccessMode.Field);
        p.ToTable(t => { t.HasCheckConstraint("CK_Proposal_Status", "\"Status\" IN ('Draft','Sent','Approved','Rejected','Expired','Superseded','Cancelled')"); t.HasCheckConstraint("CK_Proposal_Version", "\"Version\" >= 1"); });
        var a = m.Entity<ProposalAlternative>(); a.HasKey(x => x.Id); a.Property(x => x.Title).HasMaxLength(200); a.Property(x => x.Description).HasMaxLength(2000); a.HasIndex(x => new { x.ProposalId, x.SortOrder }).IsUnique(); a.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.AlternativeId).OnDelete(DeleteBehavior.Cascade);
        a.Navigation(x => x.Items).HasField("items").UsePropertyAccessMode(PropertyAccessMode.Field);
        var i = m.Entity<ProposalItem>(); i.HasKey(x => x.Id); i.Property(x => x.SnapshotJson).HasColumnType("jsonb"); i.HasIndex(x => new { x.AlternativeId, x.ResearchOptionId }).IsUnique(); i.HasOne<ResearchOption>().WithMany().HasForeignKey(x => x.ResearchOptionId).OnDelete(DeleteBehavior.Restrict);
        var t = m.Entity<ProposalAccessToken>(); t.HasKey(x => x.Id); t.Property(x => x.Hash).HasMaxLength(64); t.HasIndex(x => x.Hash).IsUnique(); t.HasIndex(x => x.ProposalId).IsUnique(); t.HasIndex(x => x.ExpiresAt); t.HasOne<TravelProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Cascade);
        var d = m.Entity<ProposalDecision>(); d.HasKey(x => x.Id); d.HasIndex(x => x.ProposalId).IsUnique(); d.Property(x => x.TermsVersion).HasMaxLength(100); d.Property(x => x.Acknowledgement).HasMaxLength(2000); d.Property(x => x.Feedback).HasMaxLength(1000); d.HasOne<TravelProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Cascade); d.HasOne<ProposalAlternative>().WithMany().HasForeignKey(x => x.AlternativeId).OnDelete(DeleteBehavior.Restrict);
    }
}
