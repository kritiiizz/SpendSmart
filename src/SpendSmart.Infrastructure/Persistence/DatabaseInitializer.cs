using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SpendSmart.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies migrations and seeds the demo account. Called once at startup.</summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<SpendSmartDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (db.Database.GetMigrations().Any())
        {
            logger.LogInformation("Applying EF Core migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // Fallback until the first migration has been generated (see README → "Database migrations").
            logger.LogWarning("No EF Core migrations found; creating the schema with EnsureCreated");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (provider.GetRequiredService<IOptions<SeedOptions>>().Value.Enabled)
        {
            await provider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
        }
    }
}
