using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Persistence;

public sealed class TravelAgencyDbContext(DbContextOptions<TravelAgencyDbContext> options) : IdentityDbContext<Administrator>(options)
{
    public DbSet<TravelInquiry> TravelRequests => Set<TravelInquiry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<NotificationRecord> NotificationOutbox => Set<NotificationRecord>();
    public DbSet<RequestNote> RequestNotes => Set<RequestNote>();
    public DbSet<RequestActivity> RequestActivities => Set<RequestActivity>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Administrator>().Property(a => a.DisplayName).HasMaxLength(100);
        var note = model.Entity<RequestNote>();
        note.HasKey(n => n.Id);
        note.Property(n => n.Content).HasMaxLength(4000).IsRequired();
        note.HasIndex(n => new { n.RequestId, n.CreatedAt, n.Id });
        note.HasOne<TravelInquiry>().WithMany().HasForeignKey(n => n.RequestId).OnDelete(DeleteBehavior.Cascade);
        note.HasOne<Administrator>().WithMany().HasForeignKey(n => n.AdministratorId).OnDelete(DeleteBehavior.Restrict);
        var activity = model.Entity<RequestActivity>();
        activity.HasKey(a => a.Id);
        activity.Property(a => a.Type).HasMaxLength(20).IsRequired();
        activity.Property(a => a.PreviousValue).HasMaxLength(24);
        activity.Property(a => a.NewValue).HasMaxLength(24);
        activity.HasIndex(a => new { a.RequestId, a.CreatedAt, a.Id });
        activity.HasOne<TravelInquiry>().WithMany().HasForeignKey(a => a.RequestId).OnDelete(DeleteBehavior.Cascade);
        activity.HasOne<Administrator>().WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);
        model.HasSequence<long>("travel_request_reference");
        var request = model.Entity<TravelInquiry>();
        request.HasKey(r => r.Id);
        request.Property(r => r.Version).IsConcurrencyToken();
        request.HasIndex(r => r.UpdatedAt);
        request.HasIndex(r => new { r.Service, r.CreatedAt });
        request.Property(r => r.ReferenceNumber).HasMaxLength(40).IsRequired();
        request.HasIndex(r => r.ReferenceNumber).IsUnique();
        request.Property(r => r.Service).HasConversion<string>().HasMaxLength(32);
        request.Property(r => r.Status).HasConversion<string>().HasMaxLength(24);
        request.Property(r => r.Language).HasMaxLength(2).IsRequired();
        request.HasIndex(r => r.CreatedAt);
        request.HasIndex(r => new { r.Status, r.CreatedAt });
        request.OwnsOne(r => r.Contact, contact =>
        {
            contact.Property(c => c.Name).HasMaxLength(100).IsRequired();
            contact.Property(c => c.Email).HasMaxLength(254).IsRequired();
            contact.Property(c => c.Phone).HasMaxLength(40);
            contact.Property(c => c.PreferredMethod).HasMaxLength(8).IsRequired();
        });
        request.Navigation(r => r.Contact).IsRequired();
        request.OwnsOne(r => r.Plan, plan =>
        {
            plan.Property(p => p.Origin).HasMaxLength(150);
            plan.Property(p => p.Destination).HasMaxLength(150);
            plan.Property(p => p.DateMode).HasMaxLength(8).IsRequired();
            plan.Property(p => p.BudgetAmount).HasPrecision(12, 2);
            plan.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            plan.Property(p => p.BudgetScope).HasMaxLength(16).IsRequired();
            plan.Property(p => p.SpecialRequests).HasMaxLength(2000);
            plan.Property(p => p.PreferencesJson).HasColumnType("jsonb").IsRequired();
        });
        request.Navigation(r => r.Plan).IsRequired();
        request.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Request_Budget", "\"Plan_BudgetAmount\" > 0 AND \"Plan_BudgetAmount\" <= 100000000");
            t.HasCheckConstraint("CK_Request_Travelers", "\"Plan_Adults\" BETWEEN 1 AND 20 AND cardinality(\"Plan_ChildrenAges\") <= 10 AND \"Plan_Adults\" + cardinality(\"Plan_ChildrenAges\") <= 20");
            t.HasCheckConstraint("CK_Request_Status", "\"Status\" IN ('Submitted', 'UnderReview', 'AwaitingCustomer', 'Closed')");
        });
        var key = model.Entity<IdempotencyRecord>();
        key.HasKey(k => k.KeyHash);
        key.Property(k => k.KeyHash).HasMaxLength(64);
        key.Property(k => k.PayloadHash).HasMaxLength(64).IsRequired();
        key.HasIndex(k => k.ExpiresAt);
        key.HasOne<TravelInquiry>().WithMany().HasForeignKey(k => k.RequestId).OnDelete(DeleteBehavior.Cascade);
        var notification = model.Entity<NotificationRecord>();
        notification.HasKey(n => n.Id);
        notification.Property(n => n.Kind).HasMaxLength(12).IsRequired();
        notification.HasIndex(n => new { n.RequestId, n.Kind }).IsUnique();
        notification.HasIndex(n => n.NextAttemptAt).HasFilter("\"SentAt\" IS NULL AND \"DeadLetterAt\" IS NULL");
        notification.HasOne<TravelInquiry>().WithMany().HasForeignKey(n => n.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
