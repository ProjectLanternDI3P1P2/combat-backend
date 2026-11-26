using Combat.Domain.Entities;
using Combat.Domain.Enums;
using FluentAssertions;

namespace Combat.Test.Domain.Entities;

public sealed class MonsterTests
{
    [Theory]
    [InlineData(1, 1, 2, 1, 10, 12, 8, 6)]
    [InlineData(5, 3, 2, 1, 54, 60, 48, 42)]
    [InlineData(40, 1, 2, 1, 88, 90, 86, 84)]
    [InlineData(1, 4, 2, 1, 40, 48, 32, 24)]
    [InlineData(3, 2, 3, 2, 48, 54, 42, 36)]
    public void Create_AppliesFloorAndPartyScalingToEachStat(
        int floor,
        int playerCount,
        int statIncreasePerFloor,
        int additionalPlayerStatMultiplier,
        int health,
        int attack,
        int defense,
        int speed
    )
    {
        Monster monster = Monster.Create(
            Definition(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            floor,
            playerCount,
            statIncreasePerFloor,
            additionalPlayerStatMultiplier
        );

        monster.Level.Should().Be(floor);
        monster.PlayerCount.Should().Be(playerCount);
        monster.BaseHealth.Should().Be(health);
        monster.BaseAttack.Should().Be(attack);
        monster.BaseDefense.Should().Be(defense);
        monster.BaseSpeed.Should().Be(speed);
    }

    [Fact]
    public void Create_CopiesDefinitionWithoutChangingItsBaseStats()
    {
        MonsterDefinition definition = Definition();
        Guid runId = Guid.NewGuid();
        Guid key = Guid.NewGuid();

        Monster monster = Monster.Create(definition, runId, key, 5, 3, 2, 1);

        monster.Id.Should().NotBeEmpty();
        monster.DungeonRunId.Should().Be(runId);
        monster.IdempotencyKey.Should().Be(key);
        monster.MonsterDefinitionId.Should().Be(definition.Id);
        monster.Name.Should().Be(definition.Name);
        monster.Class.Should().Be(definition.Class);
        monster.ImageUrl.Should().Be(definition.ImageUrl);
        definition.BaseHealth.Should().Be(10);
        definition.BaseAttack.Should().Be(12);
        definition.BaseDefense.Should().Be(8);
        definition.BaseSpeed.Should().Be(6);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Create_InvalidFloorOrPlayerCount_RejectsTheRequest(int floor, int playerCount)
    {
        Action act = () =>
            Monster.Create(Definition(), Guid.NewGuid(), Guid.NewGuid(), floor, playerCount, 2, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_StatsExceedIntegerRange_ThrowsInsteadOfWrapping()
    {
        MonsterDefinition definition = Definition();
        definition.BaseHealth = int.MaxValue;

        Action act = () => Monster.Create(definition, Guid.NewGuid(), Guid.NewGuid(), 1, 2, 2, 1);

        act.Should().Throw<OverflowException>();
    }

    private static MonsterDefinition Definition() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test monster",
            Class = MonsterClass.Ordinary,
            ImageUrl = "/assets/monsters/test.png",
            BaseHealth = 10,
            BaseAttack = 12,
            BaseDefense = 8,
            BaseSpeed = 6,
        };
}
