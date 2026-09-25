using Combat.Application.Exceptions;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.ExternalServices.Rewards;
using FluentAssertions;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Services;

public class HeroFighterInitializerTests
{
    private readonly ILogger _logger = new Mock<ILogger>().Object;

    [Fact]
    public async Task InitializeAsync_RetrievalFails_ThrowsAndCreatesNoFighter()
    {
        // Arrange
        var heroId = Guid.NewGuid();
        var providerMock = new Mock<IHeroCombatDataProvider>();
        providerMock
            .Setup(provider => provider.GetAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));
        var initializer = new HeroFighterInitializer(providerMock.Object, _logger);

        // Act
        Func<Task> act = () => initializer.InitializeAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    // End to end with the Player and Rewards mocks used while the real services are not available.
    [Fact]
    public async Task InitializeAsync_MockedHero_CreatesFighterFromMockedData()
    {
        // Arrange
        var provider = new HeroCombatDataProvider(
            new MockHeroSnapshotClient(_logger),
            new MockCombatInventoryClient(_logger),
            _logger);
        var initializer = new HeroFighterInitializer(provider, _logger);

        // Act
        Fighter fighter = await initializer.InitializeAsync(MockHeroCatalog.MageHeroId, TestContext.Current.CancellationToken);

        // Assert
        fighter.ExternalId.Should().Be(MockHeroCatalog.MageHeroId);
        fighter.IsFromMockedData.Should().BeTrue();
        fighter.CurrentHp.Should().Be(70);
        fighter.CurrentMana.Should().Be(80);
        fighter.Abilities.Should().HaveCount(3);
        fighter.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task InitializeAsync_UnknownHero_ThrowsHeroNotFoundException()
    {
        // Arrange
        var provider = new HeroCombatDataProvider(
            new MockHeroSnapshotClient(_logger),
            new MockCombatInventoryClient(_logger),
            _logger);
        var initializer = new HeroFighterInitializer(provider, _logger);

        // Act
        Func<Task> act = () => initializer.InitializeAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>();
    }
}
