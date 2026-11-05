using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Customers;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;

namespace TravelAgency.Infrastructure.Customers;

public static class CustomerMappings
{
    public static void ConfigureCustomers(this ModelBuilder m)
    {
        var u = m.Entity<Administrator>(); u.Property(x => x.FirstName).HasMaxLength(100); u.Property(x => x.LastName).HasMaxLength(100); u.Property(x => x.PreferredLanguage).HasMaxLength(2);
        u.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique().HasFilter("\"NormalizedEmail\" IS NOT NULL");
        var o = m.Entity<CustomerRequestOwnership>(); o.HasKey(x => x.RequestId); o.HasIndex(x => new { x.CustomerId, x.LinkedAt }); o.Property(x => x.Method).HasMaxLength(24); o.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); o.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        var a = m.Entity<CustomerAccountAction>(); a.HasKey(x => x.Id); a.Property(x => x.VersionToken).IsConcurrencyToken(); a.Property(x => x.Hash).HasMaxLength(64); a.HasIndex(x => x.Hash).IsUnique(); a.HasIndex(x => x.ExpiresAt); a.HasIndex(x => new { x.CustomerId, x.Purpose, x.CreatedAt }); a.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(24); a.Property(x => x.SecurityStamp).HasMaxLength(100); a.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade); a.HasOne<TravelInquiry>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        var e = m.Entity<CustomerSecurityEvent>(); e.HasKey(x => x.Id); e.Property(x => x.Type).HasMaxLength(32); e.HasIndex(x => new { x.CustomerId, x.CreatedAt }); e.HasOne<Administrator>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
