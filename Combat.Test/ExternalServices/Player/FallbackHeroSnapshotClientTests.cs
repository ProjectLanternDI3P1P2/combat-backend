using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.ExternalServices.Player;

public class FallbackHeroSnapshotClientTests
{
    private readonly Mock<IHeroSnapshotClient> _realClientMock = new();
    private readonly MockHeroSnapshotClient _mockClient = new(new Mock<ILogger>().Object);

    private FallbackHeroSnapshotClient CreateClient(bool isMockFallbackEnabled = true) => new(
        _realClientMock.Object,
        _mockClient,
        Options.Create(new PlayerServiceOptions { IsMockFallbackEnabled = isMockFallbackEnabled }),
        new Mock<ILogger>().Object);

    [Fact]
    public async Task GetCombatantSnapshotAsync_RealServiceAnswers_ReturnsRealData()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.WarriorHeroId;
        HeroCombatSnapshot realHero = MockHeroCatalog.Find(heroId)! with { Name = "Real Aldric", IsMocked = false };
        _realClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(realHero);

        // Act
        HeroCombatSnapshot result = await CreateClient().GetCombatantSnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(realHero);
        result.IsMocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetCombatantSnapshotAsync_RealServiceUnavailable_ReturnsMockedData()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.WarriorHeroId;
        _realClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));

        // Act
        HeroCombatSnapshot result = await CreateClient().GetCombatantSnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        result.HeroId.Should().Be(heroId);
        result.IsMocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetCombatantSnapshotAsync_RealServiceReturnsNotFound_DoesNotFallBack()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.WarriorHeroId;
        _realClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HeroNotFoundException(heroId));

        // Act
        Func<Task> act = () => CreateClient().GetCombatantSnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>();
    }

    [Fact]
    public async Task GetCombatantSnapshotAsync_FallbackDisabled_ThrowsExternalServiceUnavailableException()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.WarriorHeroId;
        _realClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));

        // Act
        Func<Task> act = () => CreateClient(isMockFallbackEnabled: false)
            .GetCombatantSnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    [Fact]
    public async Task GetCombatantSnapshotAsync_ServiceUnavailableAndHeroUnknownToMock_ThrowsHeroNotFoundException()
    {
        // Arrange
        var heroId = Guid.NewGuid();
        _realClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));

        // Act
        Func<Task> act = () => CreateClient().GetCombatantSnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>();
    }
}
