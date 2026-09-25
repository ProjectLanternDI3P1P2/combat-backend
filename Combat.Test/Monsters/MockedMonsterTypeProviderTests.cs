using Combat.Infrastructure.Monsters;
using FluentAssertions;

namespace Combat.Test.Monsters;

public class MockedMonsterTypeProviderTests
{
    private readonly MockedMonsterTypeProvider _provider = new();

    [Theory]
    [InlineData("goblin", false)]
    [InlineData("orc", false)]
    [InlineData("dragon", true)]
    [InlineData("DRAGON", true)]
    public async Task GetMonsterTypeAsync_KnownType_ReturnsMonsterType(string monsterType, bool expectedIsBoss)
    {
        // Act
        var result = await _provider.GetMonsterTypeAsync(monsterType, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.IsBoss.Should().Be(expectedIsBoss);
        result.BaseHp.Should().BePositive();
        result.BaseAttack.Should().BePositive();
        result.BaseDefense.Should().BePositive();
        result.BaseSpeed.Should().BePositive();
    }

    [Fact]
    public async Task GetMonsterTypeAsync_UnknownType_ReturnsNull()
    {
        // Act
        var result = await _provider.GetMonsterTypeAsync("not-a-real-type", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }
}
