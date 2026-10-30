using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelAgency.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TravelAgencyDbContext>
{
    public TravelAgencyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TravelAgency");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Set ConnectionStrings__TravelAgency before running EF commands.");
        }

        return new TravelAgencyDbContext(
            new DbContextOptionsBuilder<TravelAgencyDbContext>().UseNpgsql(connectionString).Options);
    }
}
