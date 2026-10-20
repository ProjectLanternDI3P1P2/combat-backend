using Combat.Domain.Entities;

namespace Combat.Domain.Repositories;

public interface ICombatInstanceRepository
{
    /// <summary>Returns the combat created by the given command, or null when it does not exist yet.</summary>
    Task<CombatInstance?> GetByCommandIdAsync(Guid commandId, CancellationToken cancellationToken);

    /// <summary>Returns the combat created for the given encounter, or null when it does not exist yet.</summary>
    Task<CombatInstance?> GetByEncounterIdAsync(
        Guid encounterId,
        CancellationToken cancellationToken
    );

    Task AddAsync(CombatInstance combat, CancellationToken cancellationToken);
}
