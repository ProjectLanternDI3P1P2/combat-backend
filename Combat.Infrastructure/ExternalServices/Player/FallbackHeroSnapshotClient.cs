using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices.Player;

/// <summary>
/// Calls the real Player client first and uses the mock only when Player is down.
/// A business answer such as "hero not found" is never replaced by mocked data.
/// </summary>
public sealed class FallbackHeroSnapshotClient(
    IHeroSnapshotClient realClient,
    MockHeroSnapshotClient mockClient,
    IOptions<PlayerServiceOptions> options,
    ILogger logger) : IHeroSnapshotClient
{
    public async Task<HeroCombatSnapshot> GetCombatantSnapshotAsync(Guid heroId, CancellationToken cancellationToken)
    {
        try
        {
            return await realClient.GetCombatantSnapshotAsync(heroId, cancellationToken);
        }
        catch (ExternalServiceUnavailableException exception) when (options.Value.IsMockFallbackEnabled)
        {
            logger.Warning(
                exception,
                "Player service unavailable, falling back to mocked combatant snapshot for hero {HeroId}.",
                heroId);

            return await mockClient.GetCombatantSnapshotAsync(heroId, cancellationToken);
        }
    }
}
