using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.ExternalServices.Rewards;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.ExternalServices.Rewards;

public class FallbackCombatInventoryClientTests
{
    private readonly Mock<ICombatInventoryClient> _realClientMock = new();

    private FallbackCombatInventoryClient CreateClient(bool isMockFallbackEnabled = true) => new(
        _realClientMock.Object,
        new MockCombatInventoryClient(new Mock<ILogger>().Object),
        Options.Create(new RewardsServiceOptions { IsMockFallbackEnabled = isMockFallbackEnabled }),
        new Mock<ILogger>().Object);

    [Fact]
    public async Task GetCombatInventorySnapshotAsync_RealServiceUnavailable_ReturnsMockedData()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.MageHeroId;
        _realClientMock
            .Setup(client => client.GetCombatInventorySnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Rewards"));

        // Act
        CombatInventorySnapshot result = await CreateClient()
            .GetCombatInventorySnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        result.HeroId.Should().Be(heroId);
        result.IsMocked.Should().BeTrue();
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCombatInventorySnapshotAsync_FallbackDisabled_ThrowsExternalServiceUnavailableException()
    {
        // Arrange
        Guid heroId = MockHeroCatalog.MageHeroId;
        _realClientMock
            .Setup(client => client.GetCombatInventorySnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Rewards"));

        // Act
        Func<Task> act = () => CreateClient(isMockFallbackEnabled: false)
            .GetCombatInventorySnapshotAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }
}
