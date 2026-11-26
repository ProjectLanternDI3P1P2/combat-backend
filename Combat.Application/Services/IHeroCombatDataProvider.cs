using Combat.Application.Models.HeroCombat;

namespace Combat.Application.Services;

/// <summary>
/// Retrieves and validates everything a combat needs about a hero before it starts.
/// Any exception means the combat must not start.
/// </summary>
public interface IHeroCombatDataProvider
{
    Task<HeroCombatData> GetAsync(Guid heroId, CancellationToken cancellationToken);
}
