using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices.Rewards;

/// <summary>Calls the real Rewards client first and uses the mock only when Rewards is down.</summary>
public sealed class FallbackCombatInventoryClient(
    ICombatInventoryClient realClient,
    MockCombatInventoryClient mockClient,
    IOptions<RewardsServiceOptions> options,
    ILogger logger) : ICombatInventoryClient
{
    public async Task<CombatInventorySnapshot> GetCombatInventorySnapshotAsync(Guid heroId, CancellationToken cancellationToken)
    {
        try
        {
            return await realClient.GetCombatInventorySnapshotAsync(heroId, cancellationToken);
        }
        catch (ExternalServiceUnavailableException exception) when (options.Value.IsMockFallbackEnabled)
        {
            logger.Warning(
                exception,
                "Rewards service unavailable, falling back to mocked combat inventory for hero {HeroId}.",
                heroId);

            return await mockClient.GetCombatInventorySnapshotAsync(heroId, cancellationToken);
        }
    }
}
