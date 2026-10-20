using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

public sealed class CombatInstanceTests
{
    [Fact]
    public void Create_ValidParticipants_ReturnsAnActiveCombat()
    {
        CombatInstance combat = CombatInstance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Enemy,
            [CreateHeroFighter(), CreateMonsterFighter()],
            DateTimeOffset.UtcNow
        );

        combat.Status.Should().Be(CombatStatus.Active);
        combat.Fighters.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithoutHero_Throws()
    {
        Action act = () =>
            CombatInstance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy,
                [CreateMonsterFighter()],
                DateTimeOffset.UtcNow
            );

        act.Should().Throw<ArgumentException>().WithMessage("*at least one hero*");
    }

    [Fact]
    public void Create_WithoutMonster_Throws()
    {
        Action act = () =>
            CombatInstance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy,
                [CreateHeroFighter()],
                DateTimeOffset.UtcNow
            );

        act.Should().Throw<ArgumentException>().WithMessage("*at least one monster*");
    }

    [Fact]
    public void Create_WithoutParticipant_Throws()
    {
        Action act = () =>
            CombatInstance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy,
                [],
                DateTimeOffset.UtcNow
            );

        act.Should().Throw<ArgumentException>().WithMessage("*at least one participant*");
    }

    [Fact]
    public void Create_WithoutSessionId_Throws()
    {
        Action act = () =>
            CombatInstance.Create(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy,
                [CreateHeroFighter(), CreateMonsterFighter()],
                DateTimeOffset.UtcNow
            );

        act.Should().Throw<ArgumentException>().WithMessage("*session identifier*");
    }

    [Fact]
    public void Create_WithTheSameFighterTwice_Throws()
    {
        Fighter fighter = CreateHeroFighter();

        Action act = () =>
            CombatInstance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy,
                [fighter, fighter, CreateMonsterFighter()],
                DateTimeOffset.UtcNow
            );

        act.Should().Throw<ArgumentException>().WithMessage("*same fighter twice*");
    }

    [Fact]
    public void Rehydrate_PersistedCombat_ReturnsTheSameState()
    {
        Guid combatId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        var startedAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        Fighter fighter = CreateHeroFighter();

        CombatInstance combat = CombatInstance.Rehydrate(
            combatId,
            sessionId,
            encounterId,
            commandId,
            EncounterType.Boss,
            CombatStatus.Active,
            startedAt,
            [fighter]
        );

        combat.CombatId.Should().Be(combatId);
        combat.SessionId.Should().Be(sessionId);
        combat.EncounterId.Should().Be(encounterId);
        combat.CommandId.Should().Be(commandId);
        combat.EncounterType.Should().Be(EncounterType.Boss);
        combat.StartedAt.Should().Be(startedAt);
        combat.Fighters.Should().ContainSingle();
    }

    private static Fighter CreateHeroFighter()
    {
        var initialState = new FighterInitialState(
            currentHp: 100,
            maxHp: 100,
            currentMana: 50,
            maxMana: 50,
            stats: new FighterStats(10, 5, 8),
            abilities: [new FighterAbility(Guid.NewGuid(), "Strike", 5, "SingleEnemy")],
            items: [new FighterItem(Guid.NewGuid(), "Health Potion", "Consumable", 2)]
        );

        return Fighter.CreateHero(Guid.NewGuid(), "Aldric", 3, initialState, false);
    }

    private static Fighter CreateMonsterFighter()
    {
        var initialState = new FighterInitialState(
            currentHp: 40,
            maxHp: 40,
            currentMana: 0,
            maxMana: 0,
            stats: new FighterStats(6, 3, 4),
            abilities: [new FighterAbility(Guid.NewGuid(), "Basic attack", 0, "SingleEnemy")],
            items: []
        );

        return Fighter.CreateMonster(Guid.NewGuid(), "Cave Goblin", 1, initialState);
    }
}
