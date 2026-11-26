using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;

namespace Combat.Infrastructure.ExternalServices.Rewards;

/// <summary>
/// Items are not available in combat yet (decision Q13 of the combat-on-collision work):
/// heroes enter a fight with an empty inventory. This is real data, not a mock: no item
/// is invented. Replace it by a Rewards gRPC client when consumable items are in scope.
/// </summary>
public sealed class NoCombatInventoryClient : ICombatInventoryClient
{
    public Task<CombatInventorySnapshot> GetCombatInventorySnapshotAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CombatInventorySnapshot { HeroId = heroId, Items = [] });
    }
}
