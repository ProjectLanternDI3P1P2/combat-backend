using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

public sealed class FightTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Fireball = Guid.NewGuid();
    private static readonly Guid Heal = Guid.NewGuid();
    private static readonly Guid Potion = Guid.NewGuid();

    // Always the middle of the range: no variance, first target, flee succeeds (50 <= 60).
    private readonly IDice _dice = new MiddleDice();

    [Fact]
    public void Start_FasterHero_WaitsForTheHero()
    {
        Combatant hero = Hero(speed: 20);
        Fight fight = StartFight([hero], [Monster(speed: 5)]);

        fight.Status.Should().Be(FightStatus.Active);
        fight.ActiveCombatantId.Should().Be(hero.Id);
        fight.Events.Should().ContainSingle(e => e.Type == FightEventType.TurnStarted);
    }

    [Fact]
    public void Start_FasterMonster_PlaysBeforeTheHero()
    {
        Combatant hero = Hero(speed: 5);
        Fight fight = StartFight([hero], [Monster(speed: 20, attack: 50)]);

        fight.ActiveCombatantId.Should().Be(hero.Id);
        hero.Hp.Should().BeLessThan(hero.MaxHp);
        fight
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
    public void Attack_KnocksOutTheLastMonster_WinsTheFight()
    {
        Combatant hero = Hero(attack: 500);
        Combatant monster = Monster();
        Fight fight = StartFight([hero], [monster]);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id),
            _dice,
            Now
        );

        monster.IsKnockedOut.Should().BeTrue();
        fight.Status.Should().Be(FightStatus.Victory);
        fight.ActiveCombatantId.Should().BeNull();
        events
            .Select(e => e.Type)
            .Should()
            .EndWith([FightEventType.KnockedOut, FightEventType.Victory]);
    }

    [Fact]
    public void Attack_MonsterStillStanding_MonsterStrikesBackThenHeroPlaysAgain()
    {
        Combatant hero = Hero();
        Combatant monster = Monster(hp: 1000);
        Fight fight = StartFight([hero], [monster]);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id),
            _dice,
            Now
        );

        events.Should().Contain(e => e.Type == FightEventType.Attacked && e.ActorId == monster.Id);
        fight.ActiveCombatantId.Should().Be(hero.Id);
        fight.Round.Should().Be(2);
    }

    [Fact]
    public void Submit_NotTheActorsTurn_IsRefusedAndChangesNothing()
    {
        Combatant first = Hero(speed: 20);
        Combatant second = Hero(speed: 10);
        Combatant monster = Monster();
        Fight fight = StartFight([first, second], [monster]);
        int eventCount = fight.Events.Count;

        Action act = () =>
            fight.Submit(
                new FightAction(Guid.NewGuid(), second.Id, FightActionType.Attack, monster.Id),
                _dice,
                Now
            );

        act.Should().Throw<FightRuleViolationException>();
        fight.Events.Should().HaveCount(eventCount);
        fight.ActiveCombatantId.Should().Be(first.Id);
        monster.Hp.Should().Be(monster.MaxHp);
    }

    [Fact]
    public void Submit_SameCommandTwice_IsAppliedOnce()
    {
        Combatant hero = Hero();
        Combatant monster = Monster(hp: 1000);
        Fight fight = StartFight([hero], [monster]);
        var action = new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id);

        IReadOnlyList<FightEvent> first = fight.Submit(action, _dice, Now);
        int hpAfterFirst = monster.Hp;
        IReadOnlyList<FightEvent> replay = fight.Submit(action, _dice, Now);

        replay.Should().BeSameAs(first);
        monster.Hp.Should().Be(hpAfterFirst);
    }

    [Fact]
    public void Ability_NotEnoughMana_IsRefused()
    {
        Combatant hero = Hero(mana: 5);
        Combatant monster = Monster();
        Fight fight = StartFight([hero], [monster]);

        Action act = () =>
            fight.Submit(
                new FightAction(
                    Guid.NewGuid(),
                    hero.Id,
                    FightActionType.Ability,
                    monster.Id,
                    AbilityId: Fireball
                ),
                _dice,
                Now
            );

        act.Should().Throw<FightRuleViolationException>().Which.Reason.Should().Contain("mana");
        hero.Mana.Should().Be(5);
    }

    [Fact]
    public void Ability_SingleEnemy_SpendsManaAndHitsHarderThanAnAttack()
    {
        Combatant hero = Hero(attack: 20);
        Combatant monster = Monster(hp: 1000, defense: 0);
        Fight fight = StartFight([hero], [monster]);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Ability, monster.Id, Fireball),
            _dice,
            Now
        );

        hero.Mana.Should().Be(50 - 10);
        events
            .First(e => e.Type == FightEventType.Damaged && e.TargetId == monster.Id)
            .Amount.Should()
            .Be(Fight.CalculateDamage(20, 0, 160, _dice));
    }

    [Fact]
    public void Item_HealthPotion_HealsAndIsConsumed()
    {
        Combatant hero = Hero(hp: 10);
        Fight fight = StartFight([hero], [Monster(hp: 1000)]);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Item, ItemId: Potion),
            _dice,
            Now
        );

        events.Should().Contain(e => e.Type == FightEventType.Healed && e.Amount == 40);
        hero.Items.Single().Quantity.Should().Be(0);
    }

    [Fact]
    public void Defend_HalvesTheNextHit()
    {
        Combatant hero = Hero(defense: 0);
        Combatant monster = Monster(hp: 1000, attack: 40);
        Fight fight = StartFight([hero], [monster]);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Defend),
            _dice,
            Now
        );

        events
            .First(e => e.Type == FightEventType.Damaged && e.TargetId == hero.Id)
            .Amount.Should()
            .Be(Fight.CalculateDamage(40, 0, 100, _dice) / 2);
    }

    [Fact]
    public void Flee_FromABoss_IsRefused()
    {
        Combatant hero = Hero();
        Fight fight = StartFight([hero], [Monster()], MonsterClass.Boss);

        Action act = () =>
            fight.Submit(
                new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Flee),
                _dice,
                Now
            );

        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void Flee_Succeeds_EndsTheFight()
    {
        Combatant hero = Hero();
        Fight fight = StartFight([hero], [Monster()]);

        fight.Submit(new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Flee), _dice, Now);

        fight.Status.Should().Be(FightStatus.Fled);
        fight.EndedAt.Should().Be(Now);
    }

    [Fact]
    public void LastHeroKnockedOut_LosesTheFight()
    {
        Combatant hero = Hero(hp: 1);
        Combatant monster = Monster(hp: 1000);
        Fight fight = StartFight([hero], [monster]);

        fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id),
            _dice,
            Now
        );

        fight.Status.Should().Be(FightStatus.Defeat);
        Action act = () =>
            fight.Submit(
                new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Defend),
                _dice,
                Now
            );
        act.Should().Throw<FightRuleViolationException>();
    }

    [Fact]
    public void KnockedOutHero_NoLongerGetsATurn()
    {
        Combatant fast = Hero(hp: 1, speed: 30);
        Combatant slow = Hero(speed: 1);
        Combatant monster = Monster(hp: 1000, speed: 10);
        // Middle dice picks the first standing hero: the fast one, who has 1 HP.
        Fight fight = StartFight([fast, slow], [monster]);

        fight.Submit(new FightAction(Guid.NewGuid(), fast.Id, FightActionType.Defend), _dice, Now);
        fight.Submit(
            new FightAction(Guid.NewGuid(), slow.Id, FightActionType.Attack, monster.Id),
            _dice,
            Now
        );

        fast.IsKnockedOut.Should().BeTrue();
        fight.ActiveCombatantId.Should().Be(slow.Id);
        fight.UpcomingTurns().Should().NotContain(fast.Id);
    }

    [Fact]
    public void Attack_TargetSidesteps_MissesWithoutDamage()
    {
        Combatant hero = Hero();
        Combatant monster = Monster(hp: 1000);
        Fight fight = StartFight([hero], [monster]);
        // First percent roll: the miss check. 1 is under any miss chance.
        var dice = new PercentDice(1, 50, 50, 50);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id),
            dice,
            Now
        );

        events.Should().Contain(e => e.Type == FightEventType.Missed && e.TargetId == monster.Id);
        events
            .Should()
            .NotContain(e => e.Type == FightEventType.Damaged && e.TargetId == monster.Id);
        monster.Hp.Should().Be(monster.MaxHp);
    }

    [Fact]
    public void Attack_CriticalHit_DealsHalfAsMuchAgainAndIsFlagged()
    {
        Combatant hero = Hero(attack: 40);
        Combatant monster = Monster(hp: 1000, defense: 0);
        Fight fight = StartFight([hero], [monster]);
        // Miss check 50 (no miss), then critical check 1 (critical).
        var dice = new PercentDice(50, 1, 50, 50);

        IReadOnlyList<FightEvent> events = fight.Submit(
            new FightAction(Guid.NewGuid(), hero.Id, FightActionType.Attack, monster.Id),
            dice,
            Now
        );

        FightEvent hit = events.First(e =>
            e.Type == FightEventType.Damaged && e.TargetId == monster.Id
        );
        hit.IsCritical.Should().BeTrue();
        hit.Amount.Should().Be(Fight.CalculateDamage(40, 0, 100, _dice) * 3 / 2);
    }

    private Fight StartFight(
        IReadOnlyCollection<Combatant> heroes,
        IReadOnlyCollection<Combatant> monsters,
        MonsterClass encounter = MonsterClass.Ordinary
    ) =>
        Fight.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            encounter,
            1,
            heroes,
            monsters,
            _dice,
            Now
        );

    private static Combatant Hero(
        int hp = 100,
        int mana = 50,
        int attack = 20,
        int defense = 10,
        int speed = 10
    ) =>
        new(
            Guid.NewGuid(),
            FightSide.Hero,
            Guid.NewGuid(),
            "Hero",
            1,
            "hero",
            hp,
            100,
            mana,
            50,
            attack,
            defense,
            speed,
            [
                new CombatAbility(Fireball, "Fireball", 10, AbilityTarget.SingleEnemy, 160),
                new CombatAbility(Heal, "Heal", 5, AbilityTarget.SingleAlly, 35),
            ],
            [new CombatItem(Potion, "Health Potion", ItemEffect.RestoreHealth, 40, 1)]
        );

    private static Combatant Monster(
        int hp = 30,
        int attack = 10,
        int defense = 10,
        int speed = 5
    ) =>
        new(
            Guid.NewGuid(),
            FightSide.Monster,
            Guid.NewGuid(),
            "Goblin",
            1,
            "cave-goblin",
            hp,
            hp,
            0,
            0,
            attack,
            defense,
            speed
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
