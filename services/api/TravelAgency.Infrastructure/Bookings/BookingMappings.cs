using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Requests;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Admin;

namespace TravelAgency.Infrastructure.Bookings;

public static class BookingMappings
{
    public static void ConfigureBookings(this ModelBuilder m)
    {
        m.HasSequence<long>("booking_reference");
        var b = m.Entity<TravelBooking>(); b.HasKey(x => x.Id); b.Property(x => x.VersionToken).IsConcurrencyToken(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Reference).HasMaxLength(40); b.HasIndex(x => x.Reference).IsUnique(); b.HasIndex(x => x.ProposalId).IsUnique(); b.HasIndex(x => x.RequestId).IsUnique(); b.HasIndex(x => new { x.Status, x.DepartureDate }); b.HasIndex(x => x.UpdatedAt);
        b.Property(x => x.Notes).HasMaxLength(4000); b.Property(x => x.AuthorizationJson).HasColumnType("jsonb");
        b.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TravelProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProposalAlternative>().WithMany().HasForeignKey(x => x.ApprovedAlternativeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CreatedByAdministratorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Reservations).WithOne().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Reservations).HasField("reservations").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.ToTable(t => t.HasCheckConstraint("CK_Booking_Status", "\"Status\" IN ('Pending','InProgress','PartiallyConfirmed','Confirmed','Failed','Cancelled')"));
        var r = m.Entity<SupplierReservation>(); r.HasKey(x => x.Id); r.HasIndex(x => new { x.BookingId, x.ProposalItemId }).IsUnique(); r.HasIndex(x => x.Status); r.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); r.Property(x => x.PaymentResponsibility).HasConversion<string>().HasMaxLength(32); r.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(24);
        r.Property(x => x.ApprovedSnapshotJson).HasColumnType("jsonb"); r.Property(x => x.CurrentSnapshotJson).HasColumnType("jsonb"); r.HasOne<ProposalItem>().WithMany().HasForeignKey(x => x.ProposalItemId).OnDelete(DeleteBehavior.Restrict);
        r.Property(x => x.SupplierReference).HasMaxLength(200); r.Property(x => x.ConfirmationEvidence).HasMaxLength(2000); r.Property(x => x.ConfirmationDocumentReference).HasMaxLength(1000); r.Property(x => x.FailureReason).HasMaxLength(2000); r.Property(x => x.CancellationReason).HasMaxLength(2000); r.Property(x => x.CancellationReference).HasMaxLength(1000); r.Property(x => x.PaymentArrangement).HasMaxLength(1000); r.Property(x => x.TicketingStatus).HasMaxLength(24); r.Property(x => x.ReservationType).HasMaxLength(24);
        r.ToTable(t => { t.HasCheckConstraint("CK_Reservation_Status", "\"Status\" IN ('NotStarted','InProgress','AwaitingConfirmation','Confirmed','Failed','Cancelled')"); t.HasCheckConstraint("CK_Reservation_Charge", "\"CancellationChargeMinorUnits\" IS NULL OR \"CancellationChargeMinorUnits\" BETWEEN 0 AND 10000000000"); });
        var e = m.Entity<BookingEvent>(); e.HasKey(x => x.Id); e.Property(x => x.CustomerStatus).HasMaxLength(24); e.Property(x => x.Type).HasMaxLength(32); e.Property(x => x.SnapshotJson).HasColumnType("jsonb"); e.HasIndex(x => new { x.BookingId, x.CreatedAt, x.Id }); e.HasOne<TravelBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); e.HasOne<SupplierReservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict); e.HasOne<Administrator>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
