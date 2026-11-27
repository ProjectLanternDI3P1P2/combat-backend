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

    Task<CombatInstance?> GetByIdAsync(Guid combatId, CancellationToken cancellationToken);

    Task AddAsync(CombatInstance combat, CancellationToken cancellationToken);

    /// <summary>Copies the state of a loaded combat onto its stored row (saved by the command).</summary>
    Task UpdateAsync(CombatInstance combat, CancellationToken cancellationToken);

    /// <summary>Finished combats whose result Dungeon has not acknowledged yet, oldest first.</summary>
    Task<IReadOnlyList<Guid>> ListPendingResultReportsAsync(
        int limit,
        CancellationToken cancellationToken
    );
}
