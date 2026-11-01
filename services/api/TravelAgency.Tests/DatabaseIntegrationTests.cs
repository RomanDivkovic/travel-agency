using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed class DatabaseIntegrationTests
{
    public static bool HasTestDatabase => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION"));

    [Fact(Skip = "Set TEST_DATABASE_CONNECTION to an isolated database ending in _tests.", SkipUnless = nameof(HasTestDatabase))]
    public async Task BaselineMigrationAppliesIdempotentlyAndReadinessSucceeds()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")!;
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.EndsWith("_tests", settings.Database);

        await using var factory = new ApiFactory(connectionString: connectionString);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();
            await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.Empty(await database.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, (await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Count());
        }

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
