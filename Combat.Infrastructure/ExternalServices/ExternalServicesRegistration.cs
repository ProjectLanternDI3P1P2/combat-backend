using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.ExternalServices.Rewards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Combat.Infrastructure.ExternalServices;

public static class ExternalServicesRegistration
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        PlayerServiceOptions playerOptions = configuration
            .GetSection(PlayerServiceOptions.SectionName)
            .Get<PlayerServiceOptions>() ?? new PlayerServiceOptions();

        RewardsServiceOptions rewardsOptions = configuration
            .GetSection(RewardsServiceOptions.SectionName)
            .Get<RewardsServiceOptions>() ?? new RewardsServiceOptions();

        services
            .AddSingleton(Options.Create(playerOptions))
            .AddSingleton(Options.Create(rewardsOptions))
            .AddSingleton<MockHeroSnapshotClient>()
            .AddSingleton<MockCombatInventoryClient>();

        // Player and Rewards have not published their contracts yet: the mocks are the only source.
        // Once a real gRPC client exists, replace the matching line with
        // AddHeroSnapshotClientWithMockFallback<TRealClient>() or AddCombatInventoryClientWithMockFallback<TRealClient>().
        services.AddScoped<IHeroSnapshotClient>(provider => provider.GetRequiredService<MockHeroSnapshotClient>());
        services.AddScoped<ICombatInventoryClient>(provider => provider.GetRequiredService<MockCombatInventoryClient>());

        return services;
    }

    public static IServiceCollection AddHeroSnapshotClientWithMockFallback<TRealClient>(this IServiceCollection services)
        where TRealClient : class, IHeroSnapshotClient
    {
        return services
            .AddScoped<TRealClient>()
            .AddScoped<IHeroSnapshotClient>(provider => ActivatorUtilities.CreateInstance<FallbackHeroSnapshotClient>(
                provider,
                provider.GetRequiredService<TRealClient>()));
    }

    public static IServiceCollection AddCombatInventoryClientWithMockFallback<TRealClient>(this IServiceCollection services)
        where TRealClient : class, ICombatInventoryClient
    {
        return services
            .AddScoped<TRealClient>()
            .AddScoped<ICombatInventoryClient>(provider => ActivatorUtilities.CreateInstance<FallbackCombatInventoryClient>(
                provider,
                provider.GetRequiredService<TRealClient>()));
    }
}
