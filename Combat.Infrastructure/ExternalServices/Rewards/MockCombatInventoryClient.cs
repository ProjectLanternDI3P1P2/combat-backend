using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices.Rewards;

/// <summary>Local stand-in for the Rewards service <c>GetCombatInventorySnapshot</c> operation.</summary>
public sealed class MockCombatInventoryClient(ILogger logger) : ICombatInventoryClient
{
    public Task<CombatInventorySnapshot> GetCombatInventorySnapshotAsync(Guid heroId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.Debug("Mocked combat inventory snapshot served for hero {HeroId}.", heroId);
        return Task.FromResult(MockCombatInventoryCatalog.Get(heroId));
    }
}
