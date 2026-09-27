using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices.Player;

/// <summary>Local stand-in for the Player service <c>GetCombatantSnapshot</c> operation.</summary>
public sealed class MockHeroSnapshotClient(ILogger logger) : IHeroSnapshotClient
{
    public Task<HeroCombatSnapshot> GetCombatantSnapshotAsync(Guid heroId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HeroCombatSnapshot hero = MockHeroCatalog.Find(heroId) ?? throw new HeroNotFoundException(heroId);

        logger.Debug("Mocked combatant snapshot served for hero {HeroId}.", heroId);
        return Task.FromResult(hero);
    }
}
