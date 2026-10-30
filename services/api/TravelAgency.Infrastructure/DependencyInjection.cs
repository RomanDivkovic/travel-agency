using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Abstractions;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<TravelAgencyDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDatabaseReadiness, DatabaseReadiness>();
        return services;
    }
}
