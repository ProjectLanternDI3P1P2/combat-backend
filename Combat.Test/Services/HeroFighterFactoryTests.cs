using Combat.Application.Models.HeroCombat;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using FluentAssertions;

namespace Combat.Test.Services;

public class HeroFighterFactoryTests
{
    private static HeroCombatData CreateData(bool isMocked = false)
    {
        var heroId = Guid.NewGuid();

        return new HeroCombatData
        {
            Hero = new HeroCombatSnapshot
            {
                HeroId = heroId,
                Name = "Kael",
                Level = 2,
                CurrentHp = 25,
                MaxHp = 85,
                CurrentMana = 10,
                MaxMana = 40,
                Stats = new HeroCombatStats { Attack = 14, Defense = 7, Speed = 14 },
                Abilities =
                [
                    new HeroAbility { AbilityId = Guid.NewGuid(), Name = "Backstab", ManaCost = 10, TargetType = "SingleEnemy" },
                    new HeroAbility { AbilityId = Guid.NewGuid(), Name = "Vanish", ManaCost = 0, TargetType = "Self" }
                ],
                IsMocked = isMocked
            },
            Inventory = new CombatInventorySnapshot
            {
                HeroId = heroId,
                Items =
                [
                    new CombatInventoryItem { ItemId = Guid.NewGuid(), Name = "Health Potion", Category = "Consumable", Quantity = 2 }
                ]
            }
        };
    }

    [Fact]
    public void Create_RetrievedData_CopiesValuesIdenticallyIntoInitialAndCurrentState()
    {
        // Arrange
        HeroCombatData data = CreateData();
        HeroCombatSnapshot hero = data.Hero;

        // Act
        Fighter fighter = HeroFighterFactory.Create(data);

        // Assert
        fighter.Type.Should().Be(FighterType.Hero);
        fighter.ExternalId.Should().Be(hero.HeroId);
        fighter.FighterId.Should().NotBeEmpty().And.NotBe(hero.HeroId);
        fighter.Name.Should().Be(hero.Name);
        fighter.Level.Should().Be(hero.Level);

        foreach (var (currentHp, maxHp, currentMana, maxMana) in new[]
        {
            (fighter.InitialState.CurrentHp, fighter.InitialState.MaxHp, fighter.InitialState.CurrentMana, fighter.InitialState.MaxMana),
            (fighter.CurrentHp, fighter.MaxHp, fighter.CurrentMana, fighter.MaxMana)
        })
        {
            currentHp.Should().Be(hero.CurrentHp);
            maxHp.Should().Be(hero.MaxHp);
            currentMana.Should().Be(hero.CurrentMana);
            maxMana.Should().Be(hero.MaxMana);
        }

        fighter.Stats.Attack.Should().Be(hero.Stats.Attack);
        fighter.Stats.Defense.Should().Be(hero.Stats.Defense);
        fighter.Stats.Speed.Should().Be(hero.Stats.Speed);

        fighter.Abilities.Should().BeEquivalentTo(hero.Abilities, options => options.WithStrictOrdering());
        fighter.Items.Should().BeEquivalentTo(data.Inventory.Items, options => options.WithStrictOrdering());
        fighter.IsFromMockedData.Should().BeFalse();
    }

    [Fact]
    public void Create_ThenFighterIsModified_RetrievedDataIsUnchanged()
    {
        // Arrange
        HeroCombatData data = CreateData();
        Fighter fighter = HeroFighterFactory.Create(data);

        // Act
        fighter.TakeDamage(10);
        fighter.SpendMana(10);
        fighter.ConsumeItem(data.Inventory.Items.Single().ItemId);

        // Assert
        data.Hero.CurrentHp.Should().Be(25);
        data.Hero.CurrentMana.Should().Be(10);
        data.Inventory.Items.Single().Quantity.Should().Be(2);
        fighter.InitialState.CurrentHp.Should().Be(25);
        fighter.InitialState.Items.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public void Create_MockedData_FlagsFighterAsFromMockedData()
    {
        // Act
        Fighter fighter = HeroFighterFactory.Create(CreateData(isMocked: true));

        // Assert
        fighter.IsFromMockedData.Should().BeTrue();
    }

    [Fact]
    public void Create_SameDataTwice_CreatesIndependentFighters()
    {
        // Arrange
        HeroCombatData data = CreateData();

        // Act
        Fighter first = HeroFighterFactory.Create(data);
        Fighter second = HeroFighterFactory.Create(data);
        first.TakeDamage(5);

        // Assert
        first.FighterId.Should().NotBe(second.FighterId);
        second.CurrentHp.Should().Be(data.Hero.CurrentHp);
    }
}
