using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Combat.Infrastructure.Persistence.Seeding;

public static class DatabaseSeedingExtensions
{
    public static async Task MigrateAndSeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default
    )
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
        await DataSeeder.SeedAsync(context, cancellationToken);
    }
}
