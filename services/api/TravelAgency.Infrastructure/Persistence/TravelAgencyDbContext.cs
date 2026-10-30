using Microsoft.EntityFrameworkCore;

namespace TravelAgency.Infrastructure.Persistence;

public sealed class TravelAgencyDbContext(DbContextOptions<TravelAgencyDbContext> options)
    : DbContext(options);
