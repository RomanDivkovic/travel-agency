using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Abstractions;

namespace TravelAgency.Infrastructure.Persistence;

public sealed class DatabaseReadiness(TravelAgencyDbContext database) : IDatabaseReadiness
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        database.Database.CanConnectAsync(cancellationToken);
}
