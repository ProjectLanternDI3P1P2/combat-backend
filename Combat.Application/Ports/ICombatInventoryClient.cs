using Combat.Application.Models.HeroCombat;

namespace Combat.Application.Ports;

/// <summary>Application port for the Rewards service operation <c>GetCombatInventorySnapshot</c>.</summary>
public interface ICombatInventoryClient
{
    /// <exception cref="Exceptions.ExternalServiceUnavailableException">The Rewards service cannot be reached.</exception>
    Task<CombatInventorySnapshot> GetCombatInventorySnapshotAsync(Guid heroId, CancellationToken cancellationToken);
}
