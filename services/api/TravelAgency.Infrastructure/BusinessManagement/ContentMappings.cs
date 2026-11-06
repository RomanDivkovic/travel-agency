using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.BusinessManagement;
using TravelAgency.Infrastructure.Admin;
namespace TravelAgency.Infrastructure.BusinessManagement;

public static class ContentMappings
{
    public static void ConfigureContent(this ModelBuilder m)
    {
        var d = m.Entity<ManagedContent>(); d.HasKey(x => x.Key); d.Property(x => x.Key).HasMaxLength(100); d.Property(x => x.DraftJson).HasColumnType("jsonb"); d.Property(x => x.VersionToken).IsConcurrencyToken(); d.HasOne<Administrator>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
        var v = m.Entity<ContentRevision>(); v.HasKey(x => new { x.Key, x.Version }); v.Property(x => x.DataJson).HasColumnType("jsonb"); v.HasOne<ManagedContent>().WithMany().HasForeignKey(x => x.Key).OnDelete(DeleteBehavior.Restrict); v.HasOne<Administrator>().WithMany().HasForeignKey(x => x.PublishedBy).OnDelete(DeleteBehavior.SetNull); v.HasIndex(x => x.PublishedAt);
        var a = m.Entity<ContentAudit>(); a.HasKey(x => x.Id); a.Property(x => x.Key).HasMaxLength(100); a.Property(x => x.Action).HasMaxLength(32); a.HasIndex(x => new { x.Key, x.CreatedAt }); a.HasOne<Administrator>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.SetNull);
        var media = m.Entity<WebsiteMedia>(); media.HasKey(x => x.Id); media.Property(x => x.FileName).HasMaxLength(120); media.Property(x => x.ContentType).HasMaxLength(30); media.Property(x => x.AltSv).HasMaxLength(500); media.Property(x => x.AltEn).HasMaxLength(500); media.Property(x => x.VersionToken).IsConcurrencyToken(); media.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull); media.HasIndex(x => new { x.Archived, x.CreatedAt });
        var r = m.Entity<PublishedMediaReference>(); r.HasKey(x => new { x.Key, x.Version, x.MediaId }); r.HasOne<ContentRevision>().WithMany().HasForeignKey(x => new { x.Key, x.Version }).OnDelete(DeleteBehavior.Restrict); r.HasOne<WebsiteMedia>().WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class PublishedMediaReference { public string Key { get; set; } = ""; public int Version { get; set; } public Guid MediaId { get; set; } }
