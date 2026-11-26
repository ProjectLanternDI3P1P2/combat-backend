using Combat.Domain.Entities;

namespace Combat.Application.Services;

/// <summary>
/// Creates the temporary combat state of a hero from the data retrieved at combat initialization.
/// Any exception means the combat must not start.
/// </summary>
public interface IHeroFighterInitializer
{
    /// <param name="currentHp">Hit points the hero still has in the run; null: full.</param>
    /// <param name="currentMana">Mana the hero still has in the run; null: full.</param>
    Task<Fighter> InitializeAsync(
        Guid heroId,
        int? currentHp,
        int? currentMana,
        CancellationToken cancellationToken
    );
}
