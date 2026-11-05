using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Admin;
namespace TravelAgency.Infrastructure.Documents;

public sealed class DocumentBlob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BookingId { get; set; }
    public string Key { get; set; } = "";
    public DateTimeOffset? DeleteAfter { get; set; }
    public DateTimeOffset? PurgedAt { get; set; }
    public int Attempts { get; set; }
}
public static class DocumentMappings
{
    public static void ConfigureDocuments(this ModelBuilder m)
    {
        var blob = m.Entity<DocumentBlob>(); blob.HasKey(b => b.Id); blob.Property(b => b.Key).HasMaxLength(43); blob.HasIndex(b => b.Key).IsUnique(); blob.HasIndex(b => b.DeleteAfter); blob.HasOne<TravelBooking>().WithMany().HasForeignKey(b => b.BookingId).OnDelete(DeleteBehavior.SetNull);
        var d = m.Entity<TravelDocument>(); d.HasKey(d => d.Id); d.Property(d => d.VersionToken).IsConcurrencyToken(); d.Property(d => d.Type).HasConversion<string>().HasMaxLength(32); d.HasIndex(d => new { d.BookingId, d.CreatedAt, d.Id }); d.HasOne<TravelBooking>().WithMany().HasForeignKey(d => d.BookingId).OnDelete(DeleteBehavior.Cascade); d.HasOne<SupplierReservation>().WithMany().HasForeignKey(d => d.ReservationId).OnDelete(DeleteBehavior.SetNull); d.HasMany(d => d.Versions).WithOne().HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
        var v = m.Entity<TravelDocumentVersion>(); v.HasKey(v => v.Id); v.HasIndex(v => new { v.DocumentId, v.Version }).IsUnique(); v.HasIndex(v => new { v.Status, v.UpdatedAt }); v.Property(v => v.Status).HasConversion<string>().HasMaxLength(16); v.Property(v => v.Scan).HasConversion<string>().HasMaxLength(16); v.Property(v => v.Name).HasMaxLength(150); v.Property(v => v.Description).HasMaxLength(2000); v.Property(v => v.OriginalFileName).HasMaxLength(120); v.Property(v => v.ContentType).HasMaxLength(32); v.HasIndex(v => v.BlobId).IsUnique(); v.HasOne<DocumentBlob>().WithMany().HasForeignKey(v => v.BlobId).OnDelete(DeleteBehavior.Restrict); v.HasOne<Administrator>().WithMany().HasForeignKey(v => v.UploadedById).OnDelete(DeleteBehavior.Restrict);
        var s = m.Entity<ItinerarySeries>(); s.HasKey(s => s.Id); s.HasIndex(s => s.BookingId).IsUnique(); s.Property(s => s.VersionToken).IsConcurrencyToken(); s.HasOne<TravelBooking>().WithMany().HasForeignKey(s => s.BookingId).OnDelete(DeleteBehavior.Cascade); s.HasMany(s => s.Versions).WithOne().HasForeignKey(v => v.SeriesId).OnDelete(DeleteBehavior.Cascade);
        var i = m.Entity<TravelItinerary>(); i.HasKey(i => i.Id); i.HasIndex(i => new { i.SeriesId, i.Version }).IsUnique(); i.Property(i => i.Status).HasConversion<string>().HasMaxLength(16); i.Property(i => i.Title).HasMaxLength(150); i.Property(i => i.Description).HasMaxLength(3000); i.Property(i => i.EntriesJson).HasColumnType("jsonb"); i.Property(i => i.TripJson).HasColumnType("jsonb"); i.HasOne<Administrator>().WithMany().HasForeignKey(i => i.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
