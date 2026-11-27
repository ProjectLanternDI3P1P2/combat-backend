using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;

namespace Combat.Domain.Entities;

/// <summary>
/// A combat instance. It owns the temporary state of every participant for the duration
/// of the confrontation and never writes that state back to the owning services.
///
/// Turn rules (from the fight loop):
/// - initiative: every round, the fighters still standing play by speed, fastest first,
///   heroes before monsters on a tie, then in creation order;
/// - one action per turn; a refused attempt changes nothing and keeps the turn;
/// - monsters play on their own, right after a hero's action, until a hero must act;
/// - a hero's turn has a server deadline; once it is past, the turn is passed;
/// - the combat ends when one side is knocked out, or when the party flees.
/// </summary>
public sealed class CombatInstance
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

    private readonly List<Fighter> _fighters;
    private readonly IReadOnlyList<Fighter> _readOnlyFighters;
    private readonly List<FightEvent> _events;
    private readonly Dictionary<Guid, ProcessedCommand> _processedCommands;
    private readonly List<Guid> _roundOrder;

    private CombatInstance(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        CombatStatus status,
        DateTimeOffset startedAt,
        IEnumerable<Fighter> fighters,
        CombatContext context
    )
    {
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentNullException.ThrowIfNull(context);

        _fighters = [.. fighters];
        _readOnlyFighters = _fighters.AsReadOnly();
        _events = [];
        _processedCommands = [];
        _roundOrder = [];

        CombatId = combatId;
        SessionId = sessionId;
        EncounterId = encounterId;
        CommandId = commandId;
        EncounterType = encounterType;
        Status = status;
        StartedAt = startedAt;
        RunId = context.RunId;
        Floor = context.Floor;
        RoomId = context.RoomId;
    }

    public Guid CombatId { get; }

    /// <summary>Game session the combat belongs to. One combat belongs to exactly one session.</summary>
    public Guid SessionId { get; }

    /// <summary>Encounter that caused the combat. The same encounter never creates two combats.</summary>
    public Guid EncounterId { get; }

    /// <summary>Identifier of the command that created the combat. It guarantees the replay safety.</summary>
    public Guid CommandId { get; }

    public EncounterType EncounterType { get; }
    public CombatStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public IReadOnlyList<Fighter> Fighters => _readOnlyFighters;

    /// <summary>Dungeon run the encounter happened in.</summary>
    public Guid RunId { get; }

    /// <summary>1-based floor of the encounter.</summary>
    public int Floor { get; }

    public int RoomId { get; }

    public CombatResult? Result { get; private set; }
    public int Round { get; private set; }

    /// <summary>Number of turns started since the beginning of the combat.</summary>
    public int Turn { get; private set; }

    public Guid? ActiveFighterId { get; private set; }

    /// <summary>Server deadline of the active hero's turn; null on a monster turn or once over.</summary>
    public DateTimeOffset? TurnDeadline { get; private set; }

    /// <summary>Length of a hero's turn, fixed when the combat starts.</summary>
    public int TurnDurationSeconds { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>When Dungeon acknowledged the result; null while the report is pending.</summary>
    public DateTimeOffset? ResultReportedAt { get; private set; }

    public IReadOnlyList<FightEvent> Events => _events;
    public IReadOnlyList<Guid> RoundOrder => _roundOrder;
    public int RoundIndex { get; private set; }
    public IReadOnlyDictionary<Guid, ProcessedCommand> ProcessedCommands => _processedCommands;

    public bool IsOver => Status == CombatStatus.Completed;
    public bool CanFlee => EncounterType != EncounterType.Boss;
    public bool IsResultReportPending => IsOver && ResultReportedAt is null;

    public static CombatInstance Create(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        IEnumerable<Fighter> fighters,
        DateTimeOffset startedAt,
        CombatContext? context = null
    )
    {
        EnsureIdentifiers(combatId, sessionId, encounterId, commandId);

        List<Fighter> participants = [.. fighters];

        if (participants.Count == 0)
        {
            throw new ArgumentException(
                "A combat requires at least one participant.",
                nameof(fighters)
            );
        }

        if (participants.All(fighter => fighter.Type != FighterType.Hero))
        {
            throw new ArgumentException("A combat requires at least one hero.", nameof(fighters));
        }

        if (participants.All(fighter => fighter.Type != FighterType.Monster))
        {
            throw new ArgumentException(
                "A combat requires at least one monster.",
                nameof(fighters)
            );
        }

        if (
            participants.Select(fighter => fighter.FighterId).Distinct().Count()
            != participants.Count
        )
        {
            throw new ArgumentException(
                "A combat cannot contain the same fighter twice.",
                nameof(fighters)
            );
        }

        return new CombatInstance(
            combatId,
            sessionId,
            encounterId,
            commandId,
            encounterType,
            CombatStatus.Active,
            startedAt,
            participants,
            context ?? CombatContext.None
        );
    }

    /// <summary>Rebuilds a combat from its persisted state.</summary>
    public static CombatInstance Rehydrate(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        CombatStatus status,
        DateTimeOffset startedAt,
        IEnumerable<Fighter> fighters,
        CombatProgress? progress = null
    )
    {
        EnsureIdentifiers(combatId, sessionId, encounterId, commandId);
        progress ??= CombatProgress.Empty;

        var combat = new CombatInstance(
            combatId,
            sessionId,
            encounterId,
            commandId,
            encounterType,
            status,
            startedAt,
            fighters,
            progress.Context
        )
        {
            Result = progress.Result,
            Round = progress.Round,
            Turn = progress.Turn,
            RoundIndex = progress.RoundIndex,
            ActiveFighterId = progress.ActiveFighterId,
            TurnDeadline = progress.TurnDeadline,
            TurnDurationSeconds = progress.TurnDurationSeconds,
            EndedAt = progress.EndedAt,
            ResultReportedAt = progress.ResultReportedAt,
        };
        combat._events.AddRange(progress.Events);
        combat._roundOrder.AddRange(progress.RoundOrder);
        foreach (KeyValuePair<Guid, ProcessedCommand> command in progress.ProcessedCommands)
        {
            combat._processedCommands[command.Key] = command.Value;
        }

        return combat;
    }

    public Fighter? Find(Guid fighterId) =>
        _fighters.Find(fighter => fighter.FighterId == fighterId);

    /// <summary>Remaining turn order of the current round, active fighter first.</summary>
    public IReadOnlyList<Guid> UpcomingTurns() =>
        _roundOrder.Skip(RoundIndex).Where(id => Find(id) is { IsKnockedOut: false }).ToList();

    /// <summary>
    /// Opens the first turn: the monsters faster than every hero play at once, so the
    /// combat is left on a hero's turn (or already over).
    /// </summary>
    public IReadOnlyList<FightEvent> Start(IDice dice, DateTimeOffset now, int turnDurationSeconds)
    {
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(turnDurationSeconds);
        if (Turn > 0)
        {
            return [];
        }

        TurnDurationSeconds = turnDurationSeconds;
        List<FightEvent> produced = [];
        AdvanceTurn(produced, dice, now);
        return produced;
    }

    /// <summary>
    /// Passes every hero turn whose deadline is over (lazy timer: applied on the next
    /// read or command). Each passed turn starts the next one at the previous deadline.
    /// </summary>
    public IReadOnlyList<FightEvent> ApplyExpiredTurns(IDice dice, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(dice);
        List<FightEvent> produced = [];
        while (!IsOver && TurnDeadline is { } deadline && deadline <= now)
        {
            Fighter actor = Find(ActiveFighterId!.Value)!;
            Record(produced, FightEventType.TurnTimedOut, actor, null);
            AdvanceTurn(produced, dice, deadline);
        }

        return produced;
    }

    /// <summary>Resolves a hero's action, then the monsters' turns that follow it.</summary>
    /// <returns>The events produced, in order. A replayed command returns the same events.</returns>
    /// <exception cref="FightRuleViolationException">The attempt is refused; nothing changed.</exception>
    public IReadOnlyList<FightEvent> Submit(FightAction action, IDice dice, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dice);

        if (_processedCommands.TryGetValue(action.CommandId, out ProcessedCommand? replay))
        {
            return EventsOf(replay);
        }

        if (TurnDeadline is { } deadline && deadline <= now)
        {
            throw Refuse("the turn is over: its deadline has passed.");
        }

        Fighter actor = Validate(action);
        List<FightEvent> produced = [];

        Resolve(actor, action, produced, dice);
        if (!IsOver)
        {
            AdvanceTurn(produced, dice, now);
        }
        else
        {
            Finish(now);
        }

        _processedCommands[action.CommandId] =
            produced.Count == 0
                ? new ProcessedCommand(0, -1)
                : new ProcessedCommand(produced[0].Sequence, produced[^1].Sequence);
        return produced;
    }

    /// <summary>Records that Dungeon applied the result. Idempotent.</summary>
    public void MarkResultReported(DateTimeOffset now)
    {
        if (!IsOver)
        {
            throw Refuse("the combat is not over yet.");
        }

        ResultReportedAt ??= now;
    }

    /// <summary>attack x power x 50 / (50 + defense), +/- 15%, at least 1.</summary>
    public static int CalculateDamage(int attack, int defense, int powerPercent, IDice dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        double raw =
            attack * (powerPercent / 100.0) * DefenseScale / (DefenseScale + Math.Max(0, defense));
        double variance = dice.Between(85, 115) / 100.0;
        return Math.Max(1, (int)Math.Round(raw * variance));
    }

    private IReadOnlyList<FightEvent> EventsOf(ProcessedCommand command) =>
        _events
            .Where(fightEvent =>
                fightEvent.Sequence >= command.FirstSequence
                && fightEvent.Sequence <= command.LastSequence
            )
            .ToList();

    private Fighter Validate(FightAction action)
    {
        if (IsOver)
        {
            throw Refuse($"the combat is over ({Result}).");
        }

        if (action.ActorId != ActiveFighterId)
        {
            throw Refuse("it is not this fighter's turn.");
        }

        Fighter actor = Find(action.ActorId)!;
        if (!actor.IsHero)
        {
            throw Refuse("monsters play on their own.");
        }

        switch (action.Type)
        {
            case FightActionType.Attack:
                RequireTarget(action.TargetId, FighterType.Monster);
                break;
            case FightActionType.Ability:
                FighterAbility ability =
                    actor.Abilities.FirstOrDefault(a =>
                        string.Equals(a.Code, action.AbilityCode, StringComparison.Ordinal)
                    ) ?? throw Refuse("this hero does not know that ability.");
                if (ability.ManaCost > actor.CurrentMana)
                {
                    throw Refuse($"not enough mana for {ability.Name}.");
                }

                if (ability.Target == AbilityTarget.SingleEnemy)
                {
                    RequireTarget(action.TargetId, FighterType.Monster);
                }
                else if (ability.Target == AbilityTarget.SingleAlly)
                {
                    RequireTarget(action.TargetId, FighterType.Hero);
                }

                break;
            case FightActionType.Item:
                throw Refuse("items are not available in combat yet.");
            case FightActionType.Flee when !CanFlee:
                throw Refuse("there is no running away from a boss.");
            case FightActionType.Defend:
            case FightActionType.Flee:
            case FightActionType.Pass:
                break;
            default:
                throw Refuse($"unknown action '{action.Type}'.");
        }

        return actor;
    }

    private void RequireTarget(Guid? targetId, FighterType side)
    {
        Fighter? target = targetId is { } id ? Find(id) : null;
        if (target is null || target.Type != side)
        {
            throw Refuse(side == FighterType.Monster ? "choose a monster." : "choose a hero.");
        }

        if (target.IsKnockedOut)
        {
            throw Refuse($"{target.Name} is knocked out.");
        }
    }

    private void Resolve(Fighter actor, FightAction action, List<FightEvent> produced, IDice dice)
    {
        switch (action.Type)
        {
            case FightActionType.Attack:
                Fighter target = Find(action.TargetId!.Value)!;
                Record(produced, FightEventType.Attacked, actor, target);
                Strike(actor, target, 100, produced, dice);
                break;

            case FightActionType.Ability:
                UseAbility(actor, action, produced, dice);
                break;

            case FightActionType.Defend:
                actor.StartDefending();
                Record(produced, FightEventType.Defended, actor, actor);
                break;

            case FightActionType.Pass:
                Record(produced, FightEventType.Passed, actor, null);
                break;

            case FightActionType.Flee:
                if (dice.Chance(FleeChancePercent))
                {
                    Result = CombatResult.Fled;
                    Status = CombatStatus.Completed;
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
        Fighter actor,
        FightAction action,
        List<FightEvent> produced,
        IDice dice
    )
    {
        FighterAbility ability = actor.Abilities.First(a =>
            string.Equals(a.Code, action.AbilityCode, StringComparison.Ordinal)
        );
        actor.SpendMana(ability.ManaCost);

        switch (ability.Target)
        {
            case AbilityTarget.SingleEnemy:
                Fighter enemy = Find(action.TargetId!.Value)!;
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
                foreach (Fighter monster in Standing(FighterType.Monster))
                {
                    Strike(actor, monster, ability.Power, produced, dice);
                }

                break;

            case AbilityTarget.SingleAlly:
                Fighter ally = Find(action.TargetId!.Value)!;
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
        Fighter attacker,
        Fighter target,
        int powerPercent,
        List<FightEvent> produced,
        IDice dice
    )
    {
        // A faster target sidesteps more often; a guarding one does not try.
        int missChance = Math.Clamp(
            BaseMissChancePercent + target.Stats.Speed - attacker.Stats.Speed,
            MinMissChancePercent,
            MaxMissChancePercent
        );
        if (!target.IsDefending && dice.Chance(missChance))
        {
            Record(produced, FightEventType.Missed, attacker, target);
            return;
        }

        int damage = CalculateDamage(
            attacker.Stats.Attack,
            target.Stats.Defense,
            powerPercent,
            dice
        );
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

    /// <summary>Moves to the next fighter still standing, playing the monsters on the way.</summary>
    private void AdvanceTurn(List<FightEvent> produced, IDice dice, DateTimeOffset now)
    {
        TurnDeadline = null;
        while (!IsOver)
        {
            Fighter next = NextInOrder();
            Turn++;
            ActiveFighterId = next.FighterId;
            next.StopDefending();
            Record(produced, FightEventType.TurnStarted, next, null);

            if (next.IsHero)
            {
                TurnDeadline = now.AddSeconds(Math.Max(1, TurnDurationSeconds));
                return;
            }

            PlayMonster(next, produced, dice);
            CheckEnd(produced);
        }

        Finish(now);
    }

    private void Finish(DateTimeOffset now)
    {
        ActiveFighterId = null;
        TurnDeadline = null;
        EndedAt ??= now;
    }

    private Fighter NextInOrder()
    {
        while (true)
        {
            if (RoundIndex >= _roundOrder.Count)
            {
                StartRound();
            }

            Fighter candidate = Find(_roundOrder[RoundIndex++])!;
            if (!candidate.IsKnockedOut)
            {
                return candidate;
            }
        }
    }

    private void StartRound()
    {
        Round++;
        RoundIndex = 0;
        _roundOrder.Clear();
        _roundOrder.AddRange(
            _fighters
                .Select((fighter, index) => (fighter, index))
                .Where(entry => !entry.fighter.IsKnockedOut)
                .OrderByDescending(entry => entry.fighter.Stats.Speed)
                .ThenBy(entry => entry.fighter.IsHero ? 0 : 1)
                .ThenBy(entry => entry.index)
                .Select(entry => entry.fighter.FighterId)
        );
    }

    /// <summary>A monster strikes a hero still standing, at random.</summary>
    private void PlayMonster(Fighter monster, List<FightEvent> produced, IDice dice)
    {
        List<Fighter> heroes = Standing(FighterType.Hero);
        Fighter target = dice.Pick(heroes);
        Record(produced, FightEventType.Attacked, monster, target);
        Strike(monster, target, 100, produced, dice);
    }

    private void CheckEnd(List<FightEvent> produced)
    {
        if (Result == CombatResult.Fled)
        {
            return;
        }

        if (Standing(FighterType.Monster).Count == 0)
        {
            Result = CombatResult.Victory;
            Status = CombatStatus.Completed;
            Record(produced, FightEventType.Victory, null, null);
        }
        else if (Standing(FighterType.Hero).Count == 0)
        {
            Result = CombatResult.Defeat;
            Status = CombatStatus.Completed;
            Record(produced, FightEventType.Defeat, null, null);
        }
    }

    private List<Fighter> Standing(FighterType side) =>
        _fighters.Where(fighter => fighter.Type == side && !fighter.IsKnockedOut).ToList();

    private void Record(
        List<FightEvent> produced,
        FightEventType type,
        Fighter? actor,
        Fighter? target,
        int amount = 0,
        string? label = null,
        bool critical = false
    )
    {
        var fightEvent = new FightEvent(
            _events.Count + 1,
            Turn,
            type,
            actor?.FighterId,
            target?.FighterId,
            amount,
            label,
            critical
        );
        _events.Add(fightEvent);
        produced.Add(fightEvent);
    }

    private FightRuleViolationException Refuse(string reason) => new(CombatId, reason);

    private static void EnsureIdentifiers(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId
    )
    {
        if (combatId == Guid.Empty)
        {
            throw new ArgumentException("The combat identifier is required.", nameof(combatId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("The session identifier is required.", nameof(sessionId));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException(
                "The encounter identifier is required.",
                nameof(encounterId)
            );
        }

        if (commandId == Guid.Empty)
        {
            throw new ArgumentException("The command identifier is required.", nameof(commandId));
        }
    }
}

/// <summary>Where the encounter happened in the dungeon.</summary>
public sealed record CombatContext(Guid RunId, int Floor, int RoomId)
{
    public static CombatContext None { get; } = new(Guid.Empty, 1, 0);
}

/// <summary>Range of events a hero command produced, to replay it without a second effect.</summary>
public sealed record ProcessedCommand(int FirstSequence, int LastSequence);

/// <summary>Persisted progress of a combat, used to rehydrate it.</summary>
public sealed record CombatProgress(
    CombatContext Context,
    CombatResult? Result,
    int Round,
    int Turn,
    int RoundIndex,
    IReadOnlyList<Guid> RoundOrder,
    Guid? ActiveFighterId,
    DateTimeOffset? TurnDeadline,
    int TurnDurationSeconds,
    DateTimeOffset? EndedAt,
    DateTimeOffset? ResultReportedAt,
    IReadOnlyList<FightEvent> Events,
    IReadOnlyDictionary<Guid, ProcessedCommand> ProcessedCommands
)
{
    public static CombatProgress Empty { get; } =
        new(
            CombatContext.None,
            null,
            0,
            0,
            0,
            [],
            null,
            null,
            0,
            null,
            null,
            [],
            new Dictionary<Guid, ProcessedCommand>()
        );
}
