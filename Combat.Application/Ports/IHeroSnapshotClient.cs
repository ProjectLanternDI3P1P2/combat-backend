using Combat.Application.Models.HeroCombat;

namespace Combat.Application.Ports;

/// <summary>Application port for the Player service operation <c>GetCombatantSnapshot</c>.</summary>
public interface IHeroSnapshotClient
{
    /// <exception cref="Exceptions.HeroNotFoundException">The hero does not exist.</exception>
    /// <exception cref="Exceptions.ExternalServiceUnavailableException">The Player service cannot be reached.</exception>
    Task<HeroCombatSnapshot> GetCombatantSnapshotAsync(Guid heroId, CancellationToken cancellationToken);
}
