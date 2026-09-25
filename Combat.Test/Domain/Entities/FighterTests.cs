using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.ValueObjects;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

public class FighterTests
{
    private static readonly Guid PotionId = Guid.Parse("b0000000-0000-0000-0000-000000000001");

    private static Fighter CreateFighter(int currentHp = 50, int maxHp = 100, int currentMana = 20, int maxMana = 40, int potions = 1)
    {
        var initialState = new FighterInitialState(
            currentHp,
            maxHp,
            currentMana,
            maxMana,
            new FighterStats(10, 5, 7),
            [new FighterAbility(Guid.NewGuid(), "Strike", 5, "SingleEnemy")],
            [new FighterItem(PotionId, "Health Potion", "Consumable", potions)]);

        return Fighter.CreateHero(Guid.NewGuid(), "Test Hero", 1, initialState, isFromMockedData: false);
    }

    [Fact]
    public void CreateHero_ValidInitialState_StartsWithInitialValues()
    {
        // Act
        Fighter fighter = CreateFighter();

        // Assert
        fighter.Type.Should().Be(FighterType.Hero);
        fighter.FighterId.Should().NotBe(fighter.ExternalId);
        fighter.State.Should().Be(FighterState.Active);
        fighter.CurrentHp.Should().Be(fighter.InitialState.CurrentHp);
        fighter.CurrentMana.Should().Be(fighter.InitialState.CurrentMana);
        fighter.Stats.Should().Be(fighter.InitialState.Stats);
        fighter.Abilities.Should().Equal(fighter.InitialState.Abilities);
        fighter.Items.Should().Equal(fighter.InitialState.Items);
    }

    [Fact]
    public void TakeDamage_LessThanCurrentHp_ReducesHpAndKeepsInitialState()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentHp: 50);

        // Act
        int lostHp = fighter.TakeDamage(20);

        // Assert
        lostHp.Should().Be(20);
        fighter.CurrentHp.Should().Be(30);
        fighter.InitialState.CurrentHp.Should().Be(50);
        fighter.IsKnockedOut.Should().BeFalse();
    }

    [Fact]
    public void TakeDamage_MoreThanCurrentHp_StopsAtZeroAndKnocksOut()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentHp: 10);

        // Act
        int lostHp = fighter.TakeDamage(25);

        // Assert
        lostHp.Should().Be(10);
        fighter.CurrentHp.Should().Be(0);
        fighter.State.Should().Be(FighterState.KnockedOut);
    }

    [Fact]
    public void Heal_AboveMaxHp_CapsAtMaxHp()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentHp: 90, maxHp: 100);

        // Act
        int recoveredHp = fighter.Heal(30);

        // Assert
        recoveredHp.Should().Be(10);
        fighter.CurrentHp.Should().Be(100);
    }

    [Fact]
    public void SpendMana_EnoughMana_ReducesMana()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentMana: 20);

        // Act
        fighter.SpendMana(15);

        // Assert
        fighter.CurrentMana.Should().Be(5);
        fighter.InitialState.CurrentMana.Should().Be(20);
    }

    [Fact]
    public void SpendMana_NotEnoughMana_ThrowsAndKeepsMana()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentMana: 5);

        // Act
        Action act = () => fighter.SpendMana(10);

        // Assert
        act.Should().Throw<InvalidFighterOperationException>().WithMessage("*not enough mana*");
        fighter.CurrentMana.Should().Be(5);
    }

    [Fact]
    public void RestoreMana_AboveMaxMana_CapsAtMaxMana()
    {
        // Arrange
        Fighter fighter = CreateFighter(currentMana: 35, maxMana: 40);

        // Act
        int recoveredMana = fighter.RestoreMana(20);

        // Assert
        recoveredMana.Should().Be(5);
        fighter.CurrentMana.Should().Be(40);
    }

    [Fact]
    public void ChangeStats_ActiveFighter_ReplacesCurrentStatsOnly()
    {
        // Arrange
        Fighter fighter = CreateFighter();
        var boostedStats = new FighterStats(20, 5, 7);

        // Act
        fighter.ChangeStats(boostedStats);

        // Assert
        fighter.Stats.Should().Be(boostedStats);
        fighter.InitialState.Stats.Attack.Should().Be(10);
    }

    [Fact]
    public void ConsumeItem_AvailableItem_DecrementsCurrentQuantityOnly()
    {
        // Arrange
        Fighter fighter = CreateFighter(potions: 2);

        // Act
        fighter.ConsumeItem(PotionId);

        // Assert
        fighter.Items.Single().Quantity.Should().Be(1);
        fighter.InitialState.Items.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public void ConsumeItem_DepletedItem_Throws()
    {
        // Arrange
        Fighter fighter = CreateFighter(potions: 1);
        fighter.ConsumeItem(PotionId);

        // Act
        Action act = () => fighter.ConsumeItem(PotionId);

        // Assert
        act.Should().Throw<InvalidFighterOperationException>().WithMessage("*depleted*");
    }

    [Fact]
    public void ConsumeItem_UnknownItem_Throws()
    {
        // Arrange
        Fighter fighter = CreateFighter();

        // Act
        Action act = () => fighter.ConsumeItem(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidFighterOperationException>().WithMessage("*not in the combat inventory*");
    }

    public static TheoryData<Action<Fighter>> Operations() => new(
        fighter => fighter.TakeDamage(1),
        fighter => fighter.Heal(1),
        fighter => fighter.SpendMana(1),
        fighter => fighter.RestoreMana(1),
        fighter => fighter.ChangeStats(new FighterStats(1, 1, 1)),
        fighter => fighter.ConsumeItem(PotionId));

    [Theory]
    [MemberData(nameof(Operations))]
    public void Operation_KnockedOutFighter_Throws(Action<Fighter> operation)
    {
        // Arrange
        Fighter fighter = CreateFighter();
        fighter.TakeDamage(fighter.CurrentHp);

        // Act
        Action act = () => operation(fighter);

        // Assert
        act.Should().Throw<InvalidFighterOperationException>().WithMessage("*knocked out*");
    }

    [Fact]
    public void Collections_CastToMutableList_CannotBeModified()
    {
        // Arrange
        Fighter fighter = CreateFighter();
        var extraItem = new FighterItem(Guid.NewGuid(), "Bomb", "Consumable", 1);
        var extraAbility = new FighterAbility(Guid.NewGuid(), "Cheat", 0, "Self");

        // Act
        Action[] mutations =
        [
            () => ((IList<FighterItem>)fighter.Items).Add(extraItem),
            () => ((IList<FighterItem>)fighter.InitialState.Items).Add(extraItem),
            () => ((IList<FighterAbility>)fighter.Abilities).Add(extraAbility),
            () => ((IList<FighterAbility>)fighter.InitialState.Abilities).Add(extraAbility)
        ];

        // Assert
        mutations.Should().AllSatisfy(mutation => mutation.Should().Throw<NotSupportedException>());
        fighter.Items.Should().ContainSingle();
        fighter.InitialState.Abilities.Should().ContainSingle();
    }

    [Fact]
    public void FighterInitialState_SourceCollectionChangedAfterCreation_KeepsInitialValues()
    {
        // Arrange
        List<FighterItem> items = [new FighterItem(PotionId, "Health Potion", "Consumable", 1)];
        var initialState = new FighterInitialState(10, 10, 0, 0, new FighterStats(1, 1, 1), [], items);

        // Act
        items.Clear();

        // Assert
        initialState.Items.Should().ContainSingle();
    }

    [Fact]
    public void TakeDamage_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        Fighter fighter = CreateFighter();

        // Act
        Action act = () => fighter.TakeDamage(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(101, 100, 10, 40)]
    [InlineData(-1, 100, 10, 40)]
    [InlineData(0, 0, 10, 40)]
    [InlineData(50, 100, 41, 40)]
    [InlineData(50, 100, -1, 40)]
    public void FighterInitialState_InconsistentValues_ThrowsArgumentOutOfRangeException(int currentHp, int maxHp, int currentMana, int maxMana)
    {
        // Act
        Action act = () => _ = new FighterInitialState(currentHp, maxHp, currentMana, maxMana, new FighterStats(1, 1, 1), [], []);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
