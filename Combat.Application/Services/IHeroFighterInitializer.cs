using Combat.Domain.Entities;

namespace Combat.Application.Services;

/// <summary>
/// Creates the temporary combat state of a hero from the data retrieved at combat initialization.
/// Any exception means the combat must not start.
/// </summary>
public interface IHeroFighterInitializer
{
    Task<Fighter> InitializeAsync(Guid heroId, CancellationToken cancellationToken);
}
