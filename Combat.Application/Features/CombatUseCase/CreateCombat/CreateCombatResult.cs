using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

public sealed record CreateCombatResult(
    Guid CombatId,
    Guid SessionId,
    Guid EncounterId,
    EncounterType EncounterType,
    CombatStatus Status,
    IReadOnlyList<CombatParticipantResult> Participants
)
{
    public static CreateCombatResult From(CombatInstance combat)
    {
        return new CreateCombatResult(
            combat.CombatId,
            combat.SessionId,
            combat.EncounterId,
            combat.EncounterType,
            combat.Status,
            [.. combat.Fighters.Select(CombatParticipantResult.From)]
        );
    }
}

public sealed record CombatParticipantResult(
    Guid FighterId,
    FighterType Type,
    Guid ExternalId,
    string Name,
    int Level,
    int CurrentHp,
    int MaxHp,
    int CurrentMana,
    int MaxMana,
    int Attack,
    int Defense,
    int Speed,
    FighterState State,
    IReadOnlyList<FighterAbility> Abilities,
    IReadOnlyList<FighterEffect> Effects
)
{
    public static CombatParticipantResult From(Fighter fighter)
    {
        return new CombatParticipantResult(
            fighter.FighterId,
            fighter.Type,
            fighter.ExternalId,
            fighter.Name,
            fighter.Level,
            fighter.CurrentHp,
            fighter.MaxHp,
            fighter.CurrentMana,
            fighter.MaxMana,
            fighter.Stats.Attack,
            fighter.Stats.Defense,
            fighter.Stats.Speed,
            fighter.State,
            fighter.Abilities,
            fighter.Effects
        );
    }
}
