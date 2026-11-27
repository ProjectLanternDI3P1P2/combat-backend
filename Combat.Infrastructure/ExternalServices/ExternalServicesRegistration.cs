using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.ExternalServices.Rewards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Combat.Infrastructure.ExternalServices;

public static class ExternalServicesRegistration
{
    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        PlayerServiceOptions playerOptions =
            configuration.GetSection(PlayerServiceOptions.SectionName).Get<PlayerServiceOptions>()
            ?? new PlayerServiceOptions();

        RewardsServiceOptions rewardsOptions =
            configuration.GetSection(RewardsServiceOptions.SectionName).Get<RewardsServiceOptions>()
            ?? new RewardsServiceOptions();

        services
            .AddSingleton(Options.Create(playerOptions))
            .AddSingleton(Options.Create(rewardsOptions))
            .AddSingleton<MockHeroSnapshotClient>()
            .AddSingleton<MockCombatInventoryClient>();

        // Heroes come from the real Player service (PlayerHeroSnapshotGrpcClient, registered
        // with the gRPC clients). The mock answers only when the fallback is explicitly
        // enabled; it is off in every committed configuration.
        if (playerOptions.IsMockFallbackEnabled)
        {
            services.AddHeroSnapshotClientWithMockFallback<Grpc.Clients.PlayerHeroSnapshotGrpcClient>();
        }

        // No consumable item in combat yet: an empty inventory, never invented items.
        services.AddScoped<ICombatInventoryClient, NoCombatInventoryClient>();

        return services;
    }

    public static IServiceCollection AddHeroSnapshotClientWithMockFallback<TRealClient>(
        this IServiceCollection services
    )
        where TRealClient : class, IHeroSnapshotClient
    {
        return services
            .AddScoped<TRealClient>()
            .AddScoped<IHeroSnapshotClient>(provider =>
                ActivatorUtilities.CreateInstance<FallbackHeroSnapshotClient>(
                    provider,
                    provider.GetRequiredService<TRealClient>()
                )
            );
    }

    public static IServiceCollection AddCombatInventoryClientWithMockFallback<TRealClient>(
        this IServiceCollection services
    )
        where TRealClient : class, ICombatInventoryClient
    {
        return services
            .AddScoped<TRealClient>()
            .AddScoped<ICombatInventoryClient>(provider =>
                ActivatorUtilities.CreateInstance<FallbackCombatInventoryClient>(
                    provider,
                    provider.GetRequiredService<TRealClient>()
                )
            );
    }
}
