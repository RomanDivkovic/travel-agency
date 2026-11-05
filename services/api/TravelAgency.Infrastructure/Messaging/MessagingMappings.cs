using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Messaging;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;
namespace TravelAgency.Infrastructure.Messaging;

public static class MessagingMappings
{
    public static void ConfigureMessaging(this ModelBuilder m)
    {
        var c = m.Entity<CustomerConversation>(); c.HasKey(x => x.Id); c.Property(x => x.Subject).HasMaxLength(150); c.Property(x => x.CreationHash).HasMaxLength(64); c.Property(x => x.VersionToken).IsConcurrencyToken(); c.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); c.Property(x => x.Priority).HasConversion<string>().HasMaxLength(12);
        c.HasIndex(x => new { x.CustomerId, x.ClientId }).IsUnique(); c.HasIndex(x => new { x.CustomerId, x.LastMessageAt, x.Id }); c.HasIndex(x => new { x.Status, x.LastMessageAt, x.Id }); c.HasIndex(x => new { x.Priority, x.LastMessageAt }); c.HasIndex(x => x.ClosedAt); c.HasIndex(x => x.RequestId); c.HasIndex(x => x.BookingId);
        c.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade); c.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull); c.HasOne<TravelBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.SetNull);
        var msg = m.Entity<ConversationMessage>(); msg.HasKey(x => x.Id); msg.Property(x => x.Body).HasMaxLength(20000); msg.Property(x => x.SenderRole).HasConversion<string>().HasMaxLength(16); msg.HasIndex(x => new { x.ConversationId, x.Sequence }).IsUnique(); msg.HasIndex(x => new { x.ConversationId, x.SenderId, x.ClientId }).IsUnique(); msg.HasIndex(x => new { x.ConversationId, x.SenderRole, x.Sequence }); msg.HasOne<CustomerConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade); msg.HasOne<Administrator>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.SetNull);
        var r = m.Entity<ConversationReadState>(); r.HasKey(x => new { x.ConversationId, x.ParticipantId }); r.Property(x => x.Role).HasConversion<string>().HasMaxLength(16); r.HasOne<CustomerConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade); r.HasOne<Administrator>().WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Cascade);
        var e = m.Entity<ConversationEvent>(); e.HasKey(x => x.Id); e.Property(x => x.Type).HasMaxLength(32); e.Property(x => x.Previous).HasMaxLength(24); e.Property(x => x.Current).HasMaxLength(24); e.HasIndex(x => new { x.ConversationId, x.CreatedAt, x.Id }); e.HasOne<CustomerConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade); e.HasOne<Administrator>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.SetNull);
        var n = m.Entity<TravelAgency.Infrastructure.Requests.NotificationRecord>(); n.HasOne<CustomerConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade); n.HasIndex(x => new { x.ConversationId, x.Kind }).IsUnique().HasFilter("\"ConversationId\" IS NOT NULL");
    }
}
