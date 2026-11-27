extern alias PlayerContracts;

using Combat.Contracts.V1;
using Combat.Infrastructure.Grpc.Clients;
using Combat.Infrastructure.Grpc.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DungeonCombatServiceClient = Dungeon.Contracts.V1.DungeonCombatService.DungeonCombatServiceClient;
using PlayerHeroServiceClient = PlayerContracts::Player.Contracts.V1.PlayerHeroService.PlayerHeroServiceClient;

namespace Combat.Infrastructure.Grpc;

public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        PlayerGrpcClientOptions playerGrpcClientOptions =
            configuration
                .GetSection(PlayerGrpcClientOptions.SectionName)
                .Get<PlayerGrpcClientOptions>()
            ?? new PlayerGrpcClientOptions();
        DungeonGrpcClientOptions dungeonGrpcClientOptions =
            configuration
                .GetSection(DungeonGrpcClientOptions.SectionName)
                .Get<DungeonGrpcClientOptions>()
            ?? new DungeonGrpcClientOptions();

        ValidatePlayerGrpcClientOptions(playerGrpcClientOptions);
        if (!Uri.TryCreate(dungeonGrpcClientOptions.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Dungeon:Address must be an absolute URI.");
        }

        services
            .AddSingleton(Options.Create(playerGrpcClientOptions))
            .AddSingleton(Options.Create(dungeonGrpcClientOptions))
            .AddGrpcClient<CombatPlayerService.CombatPlayerServiceClient>(options =>
                options.Address = new Uri(playerGrpcClientOptions.Address)
            );

        // Player: the combatant snapshot of each hero (GetCombatantSnapshot).
        services.AddGrpcClient<PlayerHeroServiceClient>(options =>
            options.Address = new Uri(playerGrpcClientOptions.Address)
        );

        // Dungeon: the result of a finished fight (ReportCombatResult).
        services.AddGrpcClient<DungeonCombatServiceClient>(options =>
            options.Address = new Uri(dungeonGrpcClientOptions.Address)
        );

        return services.Scan(scan =>
            scan.FromAssembliesOf(typeof(GrpcServiceRegistration))
                .AddClasses(classes => classes.Where(c => c.Name.EndsWith("GrpcClient")))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );
    }

    private static void ValidatePlayerGrpcClientOptions(PlayerGrpcClientOptions options)
    {
        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Player:Address must be an absolute URI.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Grpc:Player:TimeoutSeconds must be greater than zero."
            );
        }
    }
}
