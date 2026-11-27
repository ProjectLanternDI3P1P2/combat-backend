using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

/// <summary>Turn engine of a combat (rules ported from the fight loop).</summary>
public sealed class CombatInstanceEngineTests
{
    private const int TurnSeconds = 30;
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    // Always the middle of the range: no variance, first target, no miss, no critical,
    // flee succeeds (50 <= 60).
    private readonly IDice _dice = new MiddleDice();

    [Fact]
    public void Start_FasterHero_WaitsForTheHeroWithADeadline()
    {
        Fighter hero = Hero(speed: 20);
        CombatInstance combat = Start([hero], [Monster(speed: 5)]);

        combat.Status.Should().Be(CombatStatus.Active);
        combat.ActiveFighterId.Should().Be(hero.FighterId);
        combat.TurnDeadline.Should().Be(Now.AddSeconds(TurnSeconds));
        combat.Events.Should().ContainSingle(e => e.Type == FightEventType.TurnStarted);
    }

    [Fact]
    public void Start_FasterMonster_PlaysBeforeTheHero()
    {
        Fighter hero = Hero(speed: 5);
        CombatInstance combat = Start([hero], [Monster(speed: 20, attack: 50)]);

        combat.ActiveFighterId.Should().Be(hero.FighterId);
        hero.CurrentHp.Should().BeLessThan(hero.MaxHp);
        combat
            .Events.Select(e => e.Type)
            .Should()
            .ContainInOrder(
                FightEventType.TurnStarted,
                FightEventType.Attacked,
                FightEventType.Damaged,
                FightEventType.TurnStarted
            );
    }

    [Fact]
    public void Attack_KnocksOutTheLastMonster_WinsAndWaitsForTheReport()
    {
        Fighter hero = Hero(attack: 500);
        Fighter monster = Monster();
        CombatInstance combat = Start([hero], [monster]);

        IReadOnlyList<FightEvent> events = combat.Submit(
            Action(hero, FightActionType.Attack, monster.FighterId),
            _dice,
            Now
        );

        combat.Status.Should().Be(CombatStatus.Completed);
        combat.Result.Should().Be(CombatResult.Victory);
        combat.ActiveFighterId.Should().BeNull();
        combat.TurnDeadline.Should().BeNull();
        combat.EndedAt.Should().Be(Now);
        combat.IsResultReportPending.Should().BeTrue();
        monster.IsKnockedOut.Should().BeTrue();
        events.Select(e => e.Type).Should().EndWith(FightEventType.Victory);
    }

    [Fact]
    public void Attack_MonsterStillStanding_MonsterStrikesBackThenHeroPlaysAgain()
    {
        Fighter hero = Hero();
        Fighter monster = Monster(hp: 500);
        CombatInstance combat = Start([hero], [monster]);

        combat.Submit(Action(hero, FightActionType.Attack, monster.FighterId), _dice, Now);

        combat.ActiveFighterId.Should().Be(hero.FighterId);
        monster.CurrentHp.Should().BeLessThan(monster.MaxHp);
        hero.CurrentHp.Should().BeLessThan(hero.MaxHp);
        combat.Round.Should().Be(2);
    }

    [Fact]
    public void Submit_NotTheActorsTurn_IsRefusedAndChangesNothing()
    {
        Fighter hero = Hero();
        Fighter monster = Monster();
        CombatInstance combat = Start([hero], [monster]);
        int eventCount = combat.Events.Count;

        Action act = () =>
            combat.Submit(Action(monster, FightActionType.Attack, hero.FighterId), _dice, Now);

        act.Should().Throw<FightRuleViolationException>();
        combat.Events.Should().HaveCount(eventCount);
        monster.CurrentHp.Should().Be(monster.MaxHp);
    }

    [Fact]
    public void Submit_SameCommandTwice_IsAppliedOnceAndReplaysTheSameEvents()
    {
        Fighter hero = Hero();
        Fighter monster = Monster(hp: 500);
        CombatInstance combat = Start([hero], [monster]);
        FightAction action = Action(hero, FightActionType.Attack, monster.FighterId);

        IReadOnlyList<FightEvent> first = combat.Submit(action, _dice, Now);
        int hpAfterFirst = monster.CurrentHp;
        IReadOnlyList<FightEvent> replay = combat.Submit(action, _dice, Now);

        replay.Should().Equal(first);
        monster.CurrentHp.Should().Be(hpAfterFirst);
    }

    [Fact]
    public void Ability_NotEnoughMana_IsRefused()
    {
        Fighter hero = Hero(mana: 5);
        Fighter monster = Monster();
        CombatInstance combat = Start([hero], [monster]);

        Action act = () =>
            combat.Submit(
                Action(hero, FightActionType.Ability, monster.FighterId, "fireball"),
                _dice,
                Now
            );

        act.Should().Throw<FightRuleViolationException>().Which.Reason.Should().Contain("mana");
    }

    [Fact]
    public void Ability_SingleEnemy_SpendsManaAndHitsHarderThanAnAttack()
    {
        Fighter hero = Hero();
        Fighter first = Monster(hp: 500);
        Fighter second = Monster(hp: 500);
        CombatInstance combat = Start([hero], [first, second]);

        combat.Submit(Action(hero, FightActionType.Attack, first.FighterId), _dice, Now);
        combat.Submit(
            Action(hero, FightActionType.Ability, second.FighterId, "fireball"),
            _dice,
            Now
        );

        hero.CurrentMana.Should().Be(40);
        (second.MaxHp - second.CurrentHp).Should().BeGreaterThan(first.MaxHp - first.CurrentHp);
    }

    [Fact]
    public void Defend_HalvesTheNextHit()
    {
        Fighter guarded = Hero(speed: 20);
        Fighter exposed = Hero(speed: 20);
        CombatInstance defending = Start([guarded], [Monster(hp: 500, attack: 40)]);
        CombatInstance passive = Start([exposed], [Monster(hp: 500, attack: 40)]);

        defending.Submit(Action(guarded, FightActionType.Defend), _dice, Now);
        passive.Submit(Action(exposed, FightActionType.Pass), _dice, Now);

        (guarded.MaxHp - guarded.CurrentHp).Should().BeLessThan(exposed.MaxHp - exposed.CurrentHp);
    }

    [Fact]
    public void Flee_FromABoss_IsRefused()
    {
        Fighter hero = Hero();
        CombatInstance combat = Start([hero], [Monster()], EncounterType.Boss);

        combat.CanFlee.Should().BeFalse();
        Action act = () => combat.Submit(Action(hero, FightActionType.Flee), _dice, Now);

        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void Flee_Succeeds_EndsTheCombatAsFled()
    {
        Fighter hero = Hero();
        CombatInstance combat = Start([hero], [Monster()]);

        combat.Submit(Action(hero, FightActionType.Flee), _dice, Now);

        combat.Status.Should().Be(CombatStatus.Completed);
        combat.Result.Should().Be(CombatResult.Fled);
        combat.Events.Select(e => e.Type).Should().Contain(FightEventType.Fled);
    }

    [Fact]
    public void Flee_Fails_MonstersPlayAndTheHeroActsAgain()
    {
        Fighter hero = Hero();
        CombatInstance combat = Start([hero], [Monster()], dice: new PercentDice(90));

        combat.Submit(Action(hero, FightActionType.Flee), new PercentDice(90), Now);

        combat.Status.Should().Be(CombatStatus.Active);
        combat.Events.Select(e => e.Type).Should().Contain(FightEventType.FleeFailed);
        combat.ActiveFighterId.Should().Be(hero.FighterId);
    }

    [Fact]
    public void LastHeroKnockedOut_LosesTheCombat()
    {
        Fighter hero = Hero(hp: 5, speed: 20);
        CombatInstance combat = Start([hero], [Monster(hp: 500, attack: 200)]);

        combat.Submit(Action(hero, FightActionType.Pass), _dice, Now);

        combat.Status.Should().Be(CombatStatus.Completed);
        combat.Result.Should().Be(CombatResult.Defeat);
        hero.IsKnockedOut.Should().BeTrue();
    }

    [Fact]
    public void Item_IsRefusedWhileItemsAreOutOfScope()
    {
        Fighter hero = Hero();
        CombatInstance combat = Start([hero], [Monster()]);

        Action act = () => combat.Submit(Action(hero, FightActionType.Item), _dice, Now);

        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void Pass_RecordsItAndLetsTheMonstersPlay()
    {
        Fighter hero = Hero(speed: 20);
        CombatInstance combat = Start([hero], [Monster(hp: 500)]);

        IReadOnlyList<FightEvent> events = combat.Submit(
            Action(hero, FightActionType.Pass),
            _dice,
            Now
        );

        events.Select(e => e.Type).Should().StartWith(FightEventType.Passed);
        events.Select(e => e.Type).Should().Contain(FightEventType.Attacked);
        combat.ActiveFighterId.Should().Be(hero.FighterId);
    }

    [Fact]
    public void ApplyExpiredTurns_BeforeTheDeadline_ChangesNothing()
    {
        CombatInstance combat = Start([Hero(speed: 20)], [Monster()]);

        combat.ApplyExpiredTurns(_dice, Now.AddSeconds(TurnSeconds - 1)).Should().BeEmpty();
    }

    [Fact]
    public void ApplyExpiredTurns_AfterTheDeadline_PassesTheTurnFromTheDeadline()
    {
        Fighter hero = Hero(speed: 20);
        CombatInstance combat = Start([hero], [Monster(hp: 500)]);

        IReadOnlyList<FightEvent> events = combat.ApplyExpiredTurns(
            _dice,
            Now.AddSeconds(TurnSeconds + 5)
        );

        events.Select(e => e.Type).Should().StartWith(FightEventType.TurnTimedOut);
        combat.ActiveFighterId.Should().Be(hero.FighterId);
        combat.TurnDeadline.Should().Be(Now.AddSeconds(2 * TurnSeconds));
    }

    [Fact]
    public void Submit_AfterTheDeadline_IsRefused()
    {
        Fighter hero = Hero(speed: 20);
        Fighter monster = Monster();
        CombatInstance combat = Start([hero], [monster]);

        Action act = () =>
            combat.Submit(
                Action(hero, FightActionType.Attack, monster.FighterId),
                _dice,
                Now.AddSeconds(TurnSeconds)
            );

        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void MarkResultReported_BeforeTheEnd_IsRefused()
    {
        CombatInstance combat = Start([Hero()], [Monster()]);

        Action act = () => combat.MarkResultReported(Now);

        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void MarkResultReported_Twice_KeepsTheFirstAcknowledgement()
    {
        Fighter hero = Hero(attack: 500);
        Fighter monster = Monster();
        CombatInstance combat = Start([hero], [monster]);
        combat.Submit(Action(hero, FightActionType.Attack, monster.FighterId), _dice, Now);

        combat.MarkResultReported(Now);
        combat.MarkResultReported(Now.AddMinutes(1));

        combat.ResultReportedAt.Should().Be(Now);
        combat.IsResultReportPending.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_FromItsProgress_ContinuesAndReplaysProcessedCommands()
    {
        Fighter hero = Hero();
        Fighter monster = Monster(hp: 500);
        CombatInstance combat = Start([hero], [monster]);
        FightAction action = Action(hero, FightActionType.Attack, monster.FighterId);
        IReadOnlyList<FightEvent> first = combat.Submit(action, _dice, Now);

        CombatInstance reloaded = CombatInstance.Rehydrate(
            combat.CombatId,
            combat.SessionId,
            combat.EncounterId,
            combat.CommandId,
            combat.EncounterType,
            combat.Status,
            combat.StartedAt,
            combat.Fighters,
            new CombatProgress(
                new CombatContext(combat.RunId, combat.Floor, combat.RoomId),
                combat.Result,
                combat.Round,
                combat.Turn,
                combat.RoundIndex,
                combat.RoundOrder,
                combat.ActiveFighterId,
                combat.TurnDeadline,
                combat.TurnDurationSeconds,
                combat.EndedAt,
                combat.ResultReportedAt,
                combat.Events,
                combat.ProcessedCommands
            )
        );

        reloaded.Submit(action, _dice, Now).Should().Equal(first);
        reloaded.ActiveFighterId.Should().Be(hero.FighterId);
        reloaded.RunId.Should().Be(combat.RunId);
    }

    private CombatInstance Start(
        IReadOnlyList<Fighter> heroes,
        IReadOnlyList<Fighter> monsters,
        EncounterType encounter = EncounterType.Enemy,
        IDice? dice = null
    )
    {
        CombatInstance combat = CombatInstance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            encounter,
            heroes.Concat(monsters),
            Now,
            new CombatContext(Guid.NewGuid(), 1, 3)
        );
        combat.Start(dice ?? _dice, Now, TurnSeconds);
        return combat;
    }

    private static FightAction Action(
        Fighter actor,
        FightActionType type,
        Guid? targetId = null,
        string? abilityCode = null
    ) => new(Guid.NewGuid(), actor.FighterId, type, targetId, abilityCode);

    private static Fighter Hero(
        int hp = 100,
        int mana = 50,
        int attack = 20,
        int defense = 10,
        int speed = 10
    ) =>
        Fighter.CreateHero(
            Guid.NewGuid(),
            "Hero",
            1,
            new FighterInitialState(
                hp,
                100,
                mana,
                50,
                new FighterStats(attack, defense, speed),
                [
                    new FighterAbility("fireball", "Fireball", 10, "SingleEnemy", 160),
                    new FighterAbility("heal", "Heal", 5, "SingleAlly", 35),
                ],
                []
            ),
            isFromMockedData: false,
            FighterVisual.Hero("warrior", 1, "bob1", 1)
        );

    private static Fighter Monster(int hp = 30, int attack = 10, int defense = 10, int speed = 5) =>
        Fighter.CreateMonster(
            Guid.NewGuid(),
            "Goblin",
            1,
            new FighterInitialState(hp, hp, 0, 0, new FighterStats(attack, defense, speed), [], []),
            FighterVisual.Monster("cave-goblin", isBoss: false)
        );

    private sealed class MiddleDice : IDice
    {
        public int Between(int minInclusive, int maxInclusive) =>
            maxInclusive - minInclusive == 99 ? 50
            : minInclusive == 85 ? 100
            : minInclusive;
    }

    /// <summary>Like <see cref="MiddleDice"/>, but percent rolls come from a script, then 50.</summary>
    private sealed class PercentDice(params int[] percentRolls) : IDice
    {
        private readonly Queue<int> _rolls = new(percentRolls);
        private readonly MiddleDice _middle = new();

        public int Between(int minInclusive, int maxInclusive) =>
            maxInclusive - minInclusive == 99 && _rolls.Count > 0
                ? _rolls.Dequeue()
                : _middle.Between(minInclusive, maxInclusive);
    }
}
