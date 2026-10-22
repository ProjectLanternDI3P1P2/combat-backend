using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Features.FightUseCase;

/// <summary>The state of a fight as the client sees it.</summary>
public sealed record FightResult(
    Guid FightId,
    Guid DungeonRunId,
    FightStatus Status,
    MonsterClass Encounter,
    int Floor,
    int Round,
    int Turn,
    Guid? ActiveCombatantId,
    bool CanFlee,
    IReadOnlyList<Guid> UpcomingTurns,
    IReadOnlyList<CombatantResult> Heroes,
    IReadOnlyList<CombatantResult> Monsters,
    IReadOnlyList<FightEventResult> Events
)
{
    /// <param name="events">Events to send: the whole log, or only those of the last action.</param>
    public static FightResult From(Fight fight, IEnumerable<FightEvent> events)
    {
        return new FightResult(
            fight.Id,
            fight.DungeonRunId,
            fight.Status,
            fight.Encounter,
            fight.Floor,
            fight.Round,
            fight.Turn,
            fight.ActiveCombatantId,
            fight.CanFlee,
            fight.UpcomingTurns(),
            fight
                .Combatants.Where(c => c.Side == FightSide.Hero)
                .Select(CombatantResult.From)
                .ToList(),
            fight
                .Combatants.Where(c => c.Side == FightSide.Monster)
                .Select(CombatantResult.From)
                .ToList(),
            events.Select(FightEventResult.From).ToList()
        );
    }
}

public sealed record CombatantResult(
    Guid CombatantId,
    FightSide Side,
    Guid ExternalId,
    string Name,
    int Level,
    string SpriteKey,
    int Hp,
    int MaxHp,
    int Mana,
    int MaxMana,
    int Attack,
    int Defense,
    int Speed,
    bool IsDefending,
    bool IsKnockedOut,
    IReadOnlyList<AbilityResult> Abilities,
    IReadOnlyList<ItemResult> Items
)
{
    public static CombatantResult From(Combatant combatant)
    {
        return new CombatantResult(
            combatant.Id,
            combatant.Side,
            combatant.ExternalId,
            combatant.Name,
            combatant.Level,
            combatant.SpriteKey,
            combatant.Hp,
            combatant.MaxHp,
            combatant.Mana,
            combatant.MaxMana,
            combatant.Attack,
            combatant.Defense,
            combatant.Speed,
            combatant.IsDefending,
            combatant.IsKnockedOut,
            combatant
                .Abilities.Select(a => new AbilityResult(a.AbilityId, a.Name, a.ManaCost, a.Target))
                .ToList(),
            combatant
                .Items.Select(i => new ItemResult(i.ItemId, i.Name, i.Effect, i.Amount, i.Quantity))
                .ToList()
        );
    }
}

public sealed record AbilityResult(Guid AbilityId, string Name, int ManaCost, AbilityTarget Target);

public sealed record ItemResult(
    Guid ItemId,
    string Name,
    ItemEffect Effect,
    int Amount,
    int Quantity
);

public sealed record FightEventResult(
    int Sequence,
    int Turn,
    FightEventType Type,
    Guid? ActorId,
    Guid? TargetId,
    int Amount,
    string? Label,
    bool IsCritical
)
{
    public static FightEventResult From(FightEvent fightEvent)
    {
        return new FightEventResult(
            fightEvent.Sequence,
            fightEvent.Turn,
            fightEvent.Type,
            fightEvent.ActorId,
            fightEvent.TargetId,
            fightEvent.Amount,
            fightEvent.Label,
            fightEvent.IsCritical
        );
    }
}
