using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;

namespace Combat.Domain.Entities;

/// <summary>
/// One encounter between the party and the monsters, resolved turn by turn.
///
/// Rules of this first version (to refine with the team):
/// - initiative: every round, the combatants still standing play by speed, fastest first,
///   heroes before monsters on a tie;
/// - one action per turn; a refused attempt changes nothing and keeps the turn;
/// - monsters play on their own, right after the heroes' action, until a hero must act;
/// - the fight ends when one side is knocked out, or when the party flees.
/// </summary>
public sealed class Fight
{
    /// <summary>Damage taken while defending.</summary>
    private const int DefendingDamagePercent = 50;

    /// <summary>Defense that halves the damage of an attack: damage x 50 / (50 + defense).</summary>
    private const int DefenseScale = 50;

    private const int FleeChancePercent = 60;

    private const int CriticalChancePercent = 10;
    private const int CriticalDamagePercent = 150;

    /// <summary>Chance to miss between equally fast fighters; each point of speed moves it by 1%.</summary>
    private const int BaseMissChancePercent = 5;
    private const int MinMissChancePercent = 3;
    private const int MaxMissChancePercent = 20;

    private readonly List<Combatant> _combatants;
    private readonly List<FightEvent> _events = [];
    private readonly Dictionary<Guid, IReadOnlyList<FightEvent>> _processedCommands = [];
    private readonly List<Guid> _roundOrder = [];
    private int _roundIndex;

    private Fight(
        Guid id,
        Guid dungeonRunId,
        Guid idempotencyKey,
        MonsterClass encounter,
        int floor,
        IEnumerable<Combatant> combatants,
        DateTimeOffset startedAt
    )
    {
        Id = id;
        DungeonRunId = dungeonRunId;
        IdempotencyKey = idempotencyKey;
        Encounter = encounter;
        Floor = floor;
        _combatants = [.. combatants];
        StartedAt = startedAt;
    }

    public Guid Id { get; }
    public Guid DungeonRunId { get; }
    public Guid IdempotencyKey { get; }
    public MonsterClass Encounter { get; }
    public int Floor { get; }
    public FightStatus Status { get; private set; } = FightStatus.Active;
    public int Round { get; private set; }

    /// <summary>Number of turns started since the beginning of the fight.</summary>
    public int Turn { get; private set; }

    public Guid? ActiveCombatantId { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? EndedAt { get; private set; }

    public IReadOnlyList<Combatant> Combatants => _combatants;
    public IReadOnlyList<FightEvent> Events => _events;
    public bool IsOver => Status != FightStatus.Active;
    public bool CanFlee => Encounter != MonsterClass.Boss;

    /// <summary>
    /// Builds the fight and plays the monsters faster than every hero, so that the fight is
    /// returned on a hero's turn (or already over).
    /// </summary>
    public static Fight Start(
        Guid id,
        Guid dungeonRunId,
        Guid idempotencyKey,
        MonsterClass encounter,
        int floor,
        IReadOnlyCollection<Combatant> heroes,
        IReadOnlyCollection<Combatant> monsters,
        IDice dice,
        DateTimeOffset now
    )
    {
        if (heroes.Count == 0 || heroes.Any(hero => hero.Side != FightSide.Hero))
        {
            throw new ArgumentException("A fight needs at least one hero.", nameof(heroes));
        }

        if (monsters.Count == 0 || monsters.Any(monster => monster.Side != FightSide.Monster))
        {
            throw new ArgumentException("A fight needs at least one monster.", nameof(monsters));
        }

        var fight = new Fight(
            id,
            dungeonRunId,
            idempotencyKey,
            encounter,
            floor,
            heroes.Concat(monsters),
            now
        );
        List<FightEvent> opening = [];
        fight.AdvanceTurn(opening, dice, now);
        return fight;
    }

    public Combatant? Find(Guid combatantId) =>
        _combatants.Find(combatant => combatant.Id == combatantId);

    /// <summary>Remaining turn order of the current round, active combatant first.</summary>
    public IReadOnlyList<Guid> UpcomingTurns() =>
        _roundOrder.Skip(_roundIndex).Where(id => Find(id) is { IsKnockedOut: false }).ToList();

    /// <summary>
    /// Resolves a hero's action, then the monsters' turns that follow it.
    /// </summary>
    /// <returns>The events produced, in order. A replayed command returns the same events.</returns>
    /// <exception cref="FightRuleViolationException">The attempt is refused; nothing changed.</exception>
    public IReadOnlyList<FightEvent> Submit(FightAction action, IDice dice, DateTimeOffset now)
    {
        if (_processedCommands.TryGetValue(action.CommandId, out IReadOnlyList<FightEvent>? replay))
        {
            return replay;
        }

        Combatant actor = Validate(action);
        List<FightEvent> produced = [];

        Resolve(actor, action, produced, dice);
        if (!IsOver)
        {
            AdvanceTurn(produced, dice, now);
        }
        else
        {
            ActiveCombatantId = null;
            EndedAt = now;
        }

        _processedCommands[action.CommandId] = produced;
        return produced;
    }

    private Combatant Validate(FightAction action)
    {
        if (IsOver)
        {
            throw Refuse($"the fight is over ({Status}).");
        }

        if (action.ActorId != ActiveCombatantId)
        {
            throw Refuse("it is not this combatant's turn.");
        }

        Combatant actor = Find(action.ActorId)!;
        if (actor.Side != FightSide.Hero)
        {
            throw Refuse("monsters play on their own.");
        }

        switch (action.Type)
        {
            case FightActionType.Attack:
                RequireTarget(action.TargetId, FightSide.Monster);
                break;
            case FightActionType.Ability:
                CombatAbility ability =
                    actor.Abilities.FirstOrDefault(a => a.AbilityId == action.AbilityId)
                    ?? throw Refuse("this hero does not know that ability.");
                if (ability.ManaCost > actor.Mana)
                {
                    throw Refuse($"not enough mana for {ability.Name}.");
                }

                if (ability.Target == AbilityTarget.SingleEnemy)
                {
                    RequireTarget(action.TargetId, FightSide.Monster);
                }
                else if (ability.Target == AbilityTarget.SingleAlly)
                {
                    RequireTarget(action.TargetId, FightSide.Hero);
                }

                break;
            case FightActionType.Item:
                CombatItem item = action.ItemId is { } itemId
                    ? actor.FindItem(itemId) ?? throw Refuse("this hero does not carry that item.")
                    : throw Refuse("an item is required.");
                if (item.Quantity <= 0)
                {
                    throw Refuse($"no {item.Name} left.");
                }

                RequireTarget(action.TargetId ?? actor.Id, FightSide.Hero);
                break;
            case FightActionType.Flee when !CanFlee:
                throw Refuse("there is no running away from a boss.");
            case FightActionType.Defend:
            case FightActionType.Flee:
                break;
            default:
                throw Refuse($"unknown action '{action.Type}'.");
        }

        return actor;
    }

    private void RequireTarget(Guid? targetId, FightSide side)
    {
        Combatant? target = targetId is { } id ? Find(id) : null;
        if (target is null || target.Side != side)
        {
            throw Refuse(side == FightSide.Monster ? "choose a monster." : "choose a hero.");
        }

        if (target.IsKnockedOut)
        {
            throw Refuse($"{target.Name} is knocked out.");
        }
    }

    private void Resolve(Combatant actor, FightAction action, List<FightEvent> produced, IDice dice)
    {
        switch (action.Type)
        {
            case FightActionType.Attack:
                Combatant target = Find(action.TargetId!.Value)!;
                Record(produced, FightEventType.Attacked, actor, target);
                Strike(actor, target, 100, produced, dice);
                break;

            case FightActionType.Ability:
                UseAbility(actor, action, produced, dice);
                break;

            case FightActionType.Item:
                CombatItem item = actor.FindItem(action.ItemId!.Value)!;
                Combatant receiver = Find(action.TargetId ?? actor.Id)!;
                actor.ConsumeItem(item.ItemId);
                Record(produced, FightEventType.ItemUsed, actor, receiver, label: item.Name);
                if (item.Effect == ItemEffect.RestoreMana)
                {
                    int mana = receiver.RestoreMana(item.Amount);
                    Record(produced, FightEventType.ManaRestored, actor, receiver, mana, item.Name);
                }
                else
                {
                    int hp = receiver.Heal(item.Amount);
                    Record(produced, FightEventType.Healed, actor, receiver, hp, item.Name);
                }

                break;

            case FightActionType.Defend:
                actor.StartDefending();
                Record(produced, FightEventType.Defended, actor, actor);
                break;

            case FightActionType.Flee:
                if (dice.Chance(FleeChancePercent))
                {
                    Status = FightStatus.Fled;
                    Record(produced, FightEventType.Fled, actor, null);
                }
                else
                {
                    Record(produced, FightEventType.FleeFailed, actor, null);
                }

                break;
        }

        CheckEnd(produced);
    }

    private void UseAbility(
        Combatant actor,
        FightAction action,
        List<FightEvent> produced,
        IDice dice
    )
    {
        CombatAbility ability = actor.Abilities.First(a => a.AbilityId == action.AbilityId);
        actor.SpendMana(ability.ManaCost);

        switch (ability.Target)
        {
            case AbilityTarget.SingleEnemy:
                Combatant enemy = Find(action.TargetId!.Value)!;
                Record(
                    produced,
                    FightEventType.AbilityUsed,
                    actor,
                    enemy,
                    ability.ManaCost,
                    ability.Name
                );
                Strike(actor, enemy, ability.Power, produced, dice);
                break;

            case AbilityTarget.AllEnemies:
                Record(
                    produced,
                    FightEventType.AbilityUsed,
                    actor,
                    null,
                    ability.ManaCost,
                    ability.Name
                );
                foreach (Combatant monster in Standing(FightSide.Monster))
                {
                    Strike(actor, monster, ability.Power, produced, dice);
                }

                break;

            case AbilityTarget.SingleAlly:
                Combatant ally = Find(action.TargetId!.Value)!;
                Record(
                    produced,
                    FightEventType.AbilityUsed,
                    actor,
                    ally,
                    ability.ManaCost,
                    ability.Name
                );
                int healed = ally.Heal(ally.MaxHp * ability.Power / 100);
                Record(produced, FightEventType.Healed, actor, ally, healed, ability.Name);
                break;

            case AbilityTarget.Self:
                Record(
                    produced,
                    FightEventType.AbilityUsed,
                    actor,
                    actor,
                    ability.ManaCost,
                    ability.Name
                );
                actor.StartDefending();
                Record(produced, FightEventType.Defended, actor, actor, label: ability.Name);
                break;
        }
    }

    private void Strike(
        Combatant attacker,
        Combatant target,
        int powerPercent,
        List<FightEvent> produced,
        IDice dice
    )
    {
        // A faster target sidesteps more often; a guarding one does not try.
        int missChance = Math.Clamp(
            BaseMissChancePercent + target.Speed - attacker.Speed,
            MinMissChancePercent,
            MaxMissChancePercent
        );
        if (!target.IsDefending && dice.Chance(missChance))
        {
            Record(produced, FightEventType.Missed, attacker, target);
            return;
        }

        int damage = CalculateDamage(attacker.Attack, target.Defense, powerPercent, dice);
        bool isCritical = dice.Chance(CriticalChancePercent);
        if (isCritical)
        {
            damage = damage * CriticalDamagePercent / 100;
        }

        if (target.IsDefending)
        {
            damage = Math.Max(1, damage * DefendingDamagePercent / 100);
        }

        int lost = target.TakeDamage(damage);
        Record(produced, FightEventType.Damaged, attacker, target, lost, critical: isCritical);
        if (target.IsKnockedOut)
        {
            Record(produced, FightEventType.KnockedOut, attacker, target);
        }
    }

    /// <summary>attack x power x 50 / (50 + defense), +/- 15%, at least 1.</summary>
    public static int CalculateDamage(int attack, int defense, int powerPercent, IDice dice)
    {
        double raw =
            attack * (powerPercent / 100.0) * DefenseScale / (DefenseScale + Math.Max(0, defense));
        double variance = dice.Between(85, 115) / 100.0;
        return Math.Max(1, (int)Math.Round(raw * variance));
    }

    /// <summary>Moves to the next combatant still standing, playing the monsters on the way.</summary>
    private void AdvanceTurn(List<FightEvent> produced, IDice dice, DateTimeOffset now)
    {
        while (!IsOver)
        {
            Combatant next = NextInOrder();
            Turn++;
            ActiveCombatantId = next.Id;
            next.StopDefending();
            Record(produced, FightEventType.TurnStarted, next, null);

            if (next.Side == FightSide.Hero)
            {
                return;
            }

            PlayMonster(next, produced, dice);
            CheckEnd(produced);
        }

        ActiveCombatantId = null;
        EndedAt = now;
    }

    private Combatant NextInOrder()
    {
        while (true)
        {
            if (_roundIndex >= _roundOrder.Count)
            {
                StartRound();
            }

            Combatant candidate = Find(_roundOrder[_roundIndex++])!;
            if (!candidate.IsKnockedOut)
            {
                return candidate;
            }
        }
    }

    private void StartRound()
    {
        Round++;
        _roundIndex = 0;
        _roundOrder.Clear();
        _roundOrder.AddRange(
            _combatants
                .Select((combatant, index) => (combatant, index))
                .Where(entry => !entry.combatant.IsKnockedOut)
                .OrderByDescending(entry => entry.combatant.Speed)
                .ThenBy(entry => entry.combatant.Side)
                .ThenBy(entry => entry.index)
                .Select(entry => entry.combatant.Id)
        );
    }

    /// <summary>A monster strikes a hero still standing, at random.</summary>
    private void PlayMonster(Combatant monster, List<FightEvent> produced, IDice dice)
    {
        List<Combatant> heroes = Standing(FightSide.Hero);
        Combatant target = dice.Pick(heroes);
        Record(produced, FightEventType.Attacked, monster, target);
        Strike(monster, target, 100, produced, dice);
    }

    private void CheckEnd(List<FightEvent> produced)
    {
        if (Status == FightStatus.Fled)
        {
            return;
        }

        if (Standing(FightSide.Monster).Count == 0)
        {
            Status = FightStatus.Victory;
            Record(produced, FightEventType.Victory, null, null);
        }
        else if (Standing(FightSide.Hero).Count == 0)
        {
            Status = FightStatus.Defeat;
            Record(produced, FightEventType.Defeat, null, null);
        }
    }

    private List<Combatant> Standing(FightSide side) =>
        _combatants.Where(c => c.Side == side && !c.IsKnockedOut).ToList();

    private void Record(
        List<FightEvent> produced,
        FightEventType type,
        Combatant? actor,
        Combatant? target,
        int amount = 0,
        string? label = null,
        bool critical = false
    )
    {
        var fightEvent = new FightEvent(
            _events.Count + 1,
            Turn,
            type,
            actor?.Id,
            target?.Id,
            amount,
            label,
            critical
        );
        _events.Add(fightEvent);
        produced.Add(fightEvent);
    }

    private FightRuleViolationException Refuse(string reason) => new(Id, reason);
}
