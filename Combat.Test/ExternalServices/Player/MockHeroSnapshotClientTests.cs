using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Combat.Application.Services;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.ExternalServices.Rewards;
using FluentAssertions;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.ExternalServices.Player;

public class MockHeroSnapshotClientTests
{
    private readonly MockHeroSnapshotClient _client = new(new Mock<ILogger>().Object);

    [Fact]
    public async Task GetCombatantSnapshotAsync_KnownHero_ReturnsMockedSnapshot()
    {
        // Act
        HeroCombatSnapshot hero = await _client.GetCombatantSnapshotAsync(
            MockHeroCatalog.WarriorHeroId,
            TestContext.Current.CancellationToken);

        // Assert
        hero.HeroId.Should().Be(MockHeroCatalog.WarriorHeroId);
        hero.IsMocked.Should().BeTrue();
        hero.Abilities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCombatantSnapshotAsync_UnknownHero_ThrowsHeroNotFoundException()
    {
        // Act
        Func<Task> act = () => _client.GetCombatantSnapshotAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>();
    }

    public static TheoryData<Guid> MockedHeroIds() => new(
        MockHeroCatalog.WarriorHeroId,
        MockHeroCatalog.MageHeroId,
        MockHeroCatalog.WoundedRogueHeroId);

    // Mocks must go through the same validation as real service data.
    [Theory]
    [MemberData(nameof(MockedHeroIds))]
    public async Task GetAsync_MockedHero_PassesCombatDataValidation(Guid heroId)
    {
        // Arrange
        ILogger logger = new Mock<ILogger>().Object;
        IHeroSnapshotClient heroClient = new MockHeroSnapshotClient(logger);
        ICombatInventoryClient inventoryClient = new MockCombatInventoryClient(logger);
        var provider = new HeroCombatDataProvider(heroClient, inventoryClient, logger);

        // Act
        HeroCombatData data = await provider.GetAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        data.HeroId.Should().Be(heroId);
        data.IsMocked.Should().BeTrue();
    }
}
