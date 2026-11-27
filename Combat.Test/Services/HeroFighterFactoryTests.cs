using Combat.Application.Models.HeroCombat;
using Combat.Application.Services;
using Combat.Domain.Entities;
using FluentAssertions;

namespace Combat.Test.Services;

public sealed class HeroFighterFactoryTests
{
    [Theory]
    [InlineData(null, null, 100, 30)]
    [InlineData(42, 0, 42, 0)]
    [InlineData(null, 0, 100, 0)]
    [InlineData(42, null, 42, 30)]
    public void Create_UsesRunValuesWhenPresentAndHeroMaximaOtherwise(
        int? currentHp,
        int? currentMana,
        int expectedHp,
        int expectedMana
    )
    {
        Guid heroId = Guid.NewGuid();
        var snapshot = new HeroCombatSnapshot
        {
            HeroId = heroId,
            Name = "Test Hero",
            Level = 3,
            CurrentHp = 50,
            MaxHp = 100,
            CurrentMana = 10,
            MaxMana = 30,
            Stats = new HeroCombatStats
            {
                Attack = 10,
                Defense = 5,
                Speed = 7,
            },
            Abilities = [],
        };
        var data = new HeroCombatData
        {
            Hero = snapshot,
            Inventory = new CombatInventorySnapshot { HeroId = heroId, Items = [] },
        };

        Fighter fighter = HeroFighterFactory.Create(data, currentHp, currentMana);

        fighter.ExternalId.Should().Be(heroId);
        fighter.Level.Should().Be(3);
        fighter.CurrentHp.Should().Be(expectedHp);
        fighter.MaxHp.Should().Be(100);
        fighter.CurrentMana.Should().Be(expectedMana);
        fighter.MaxMana.Should().Be(30);
        fighter.InitialState.Stats.Attack.Should().Be(10);
        fighter.InitialState.Stats.Defense.Should().Be(5);
        fighter.InitialState.Stats.Speed.Should().Be(7);
    }
}
