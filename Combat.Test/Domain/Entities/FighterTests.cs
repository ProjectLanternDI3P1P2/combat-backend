using Combat.Domain.Entities;
using Combat.Domain.Exceptions;
using Combat.Domain.ValueObjects;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

public sealed class FighterTests
{
    [Fact]
    public void CreateHero_ValidState_CopiesTheHeroData()
    {
        Guid heroId = Guid.NewGuid();
        var initialState = CreateInitialState();

        Fighter fighter = Fighter.CreateHero(heroId, "Aldric", 3, initialState, false);

        fighter.Type.Should().Be(Combat.Domain.Enums.FighterType.Hero);
        fighter.ExternalId.Should().Be(heroId);
        fighter.Name.Should().Be("Aldric");
        fighter.Level.Should().Be(3);
        fighter.CurrentHp.Should().Be(initialState.CurrentHp);
        fighter.CurrentMana.Should().Be(initialState.CurrentMana);
        fighter.Stats.Should().Be(initialState.Stats);
        fighter.Abilities.Should().BeEquivalentTo(initialState.Abilities);
        fighter.Items.Should().BeEquivalentTo(initialState.Items);
        fighter.IsFromMockedData.Should().BeFalse();
        fighter.State.Should().Be(Combat.Domain.Enums.FighterState.Active);
    }

    [Fact]
    public void CreateMonster_ValidState_CreatesAMonsterFighter()
    {
        Guid monsterId = Guid.NewGuid();

        Fighter fighter = Fighter.CreateMonster(monsterId, "Cave Goblin", 1, CreateInitialState());

        fighter.Type.Should().Be(Combat.Domain.Enums.FighterType.Monster);
        fighter.ExternalId.Should().Be(monsterId);
        fighter.Name.Should().Be("Cave Goblin");
    }

    [Fact]
    public void TakeDamage_KillsTheFighter_MarksItKnockedOut()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );

        int lostHp = fighter.TakeDamage(fighter.CurrentHp);

        lostHp.Should().Be(fighter.MaxHp);
        fighter.CurrentHp.Should().Be(0);
        fighter.IsKnockedOut.Should().BeTrue();
    }

    [Fact]
    public void Heal_KnockedOutFighter_Throws()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );
        fighter.TakeDamage(fighter.CurrentHp);

        Action act = () => fighter.Heal(10);

        act.Should().Throw<InvalidFighterOperationException>();
    }

    [Fact]
    public void Heal_MoreThanMissingHp_NeverExceedsMaxHp()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );
        fighter.TakeDamage(10);

        int recovered = fighter.Heal(1000);

        recovered.Should().Be(10);
        fighter.CurrentHp.Should().Be(fighter.MaxHp);
    }

    [Fact]
    public void SpendMana_NotEnoughMana_Throws()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );

        Action act = () => fighter.SpendMana(fighter.CurrentMana + 1);

        act.Should().Throw<InvalidFighterOperationException>();
    }

    [Fact]
    public void RestoreMana_KeepsTheManaUnderTheMaximum()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );
        fighter.SpendMana(20);

        int restored = fighter.RestoreMana(1000);

        restored.Should().Be(20);
        fighter.CurrentMana.Should().Be(fighter.MaxMana);
    }

    [Fact]
    public void ConsumeItem_UnknownItem_Throws()
    {
        Fighter fighter = Fighter.CreateHero(
            Guid.NewGuid(),
            "Aldric",
            3,
            CreateInitialState(),
            false
        );

        Action act = () => fighter.ConsumeItem(Guid.NewGuid());

        act.Should().Throw<InvalidFighterOperationException>();
    }

    private static FighterInitialState CreateInitialState()
    {
        return new FighterInitialState(
            currentHp: 100,
            maxHp: 100,
            currentMana: 50,
            maxMana: 50,
            stats: new FighterStats(10, 5, 8),
            abilities: [new FighterAbility(Guid.NewGuid(), "Strike", 5, "SingleEnemy")],
            items: [new FighterItem(Guid.NewGuid(), "Health Potion", "Consumable", 2)]
        );
    }
}
