using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

/// <summary>
/// Creates a combat instance from an encounter reported by the Dungeon service.
/// The same command never creates two combats.
/// </summary>
/// <param name="Floor">1-based floor of the encounter.</param>
/// <param name="Participants">Heroes of the party, at least one.</param>
/// <param name="Monsters">One entry per engaged Dungeon mob, at least one.</param>
public sealed record CreateCombatCommand(
    Guid SessionId,
    Guid EncounterId,
    Guid CommandId,
    EncounterType EncounterType,
    Guid RunId,
    int Floor,
    int RoomId,
    IReadOnlyList<EncounterParticipant> Participants,
    IReadOnlyList<EncounterMonster> Monsters
) : ICommand<CreateCombatResult>;

/// <param name="CurrentHp">Hit points the hero still has in the run; null: full.</param>
public sealed record EncounterParticipant(Guid HeroId, int? CurrentHp, int? CurrentMana);

/// <param name="ElementId">Id of the Dungeon mob; it seeds the monster it becomes.</param>
public sealed record EncounterMonster(Guid ElementId, bool IsBoss);
