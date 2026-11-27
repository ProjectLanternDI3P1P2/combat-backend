using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Features.CombatUseCase;

/// <summary>Authoritative state of a combat sent to clients (CombatHub snapshot, REST read).</summary>
public sealed record CombatView(
    Guid CombatId,
    Guid RunId,
    Guid EncounterId,
    int Floor,
    EncounterType EncounterType,
    CombatStatus Status,
    CombatResult? Result,
    int Round,
    int Turn,
    Guid? ActiveFighterId,
    DateTimeOffset? TurnDeadline,
    int TurnDurationSeconds,
    bool CanFlee,
    IReadOnlyList<Guid> UpcomingTurns,
    IReadOnlyList<CombatFighterView> Fighters,
    IReadOnlyList<FightEvent> Events
)
{
    public static CombatView From(CombatInstance combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        return new CombatView(
            combat.CombatId,
            combat.RunId,
            combat.EncounterId,
            combat.Floor,
            combat.EncounterType,
            combat.Status,
            combat.Result,
            combat.Round,
            combat.Turn,
            combat.ActiveFighterId,
            combat.TurnDeadline,
            combat.TurnDurationSeconds,
            combat.CanFlee,
            combat.UpcomingTurns(),
            [.. combat.Fighters.Select(CombatFighterView.From)],
            combat.Events
        );
    }
}

public sealed record CombatFighterView(
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
    bool IsDefending,
    IReadOnlyList<FighterAbility> Abilities,
    FighterVisual Visual
)
{
    public static CombatFighterView From(Fighter fighter) =>
        new(
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
            fighter.IsDefending,
            fighter.Abilities,
            fighter.Visual
        );
}
