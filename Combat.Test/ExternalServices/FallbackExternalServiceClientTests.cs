using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Domain.Enums;
using Combat.Infrastructure.ExternalServices;
using Combat.Infrastructure.ExternalServices.Dungeon;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.ExternalServices;

public sealed class FallbackExternalServiceClientTests
{
    [Fact]
    public async Task GetHeroIdsAsync_RealClientAnswers_ReturnsTheRealRoster()
    {
        var realClient = new Mock<ISessionHeroesClient>();
        realClient
            .Setup(client =>
                client.GetHeroIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([Guid.NewGuid()]);
        var client = CreateSessionHeroesClient(
            realClient.Object,
            new MockSessionHeroesClient(Mock.Of<ILogger>()),
            isMockFallbackEnabled: true
        );

        IReadOnlyList<Guid> heroIds = await client.GetHeroIdsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        heroIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHeroIdsAsync_RealClientIsDown_FallsBackToTheMock()
    {
        var realClient = new Mock<ISessionHeroesClient>();
        realClient
            .Setup(client =>
                client.GetHeroIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));
        var client = CreateSessionHeroesClient(
            realClient.Object,
            new MockSessionHeroesClient(Mock.Of<ILogger>()),
            isMockFallbackEnabled: true
        );

        IReadOnlyList<Guid> heroIds = await client.GetHeroIdsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        heroIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHeroIdsAsync_RealClientIsDownAndFallbackDisabled_Throws()
    {
        var realClient = new Mock<ISessionHeroesClient>();
        realClient
            .Setup(client =>
                client.GetHeroIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));
        var client = CreateSessionHeroesClient(
            realClient.Object,
            new MockSessionHeroesClient(Mock.Of<ILogger>()),
            isMockFallbackEnabled: false
        );

        Func<Task> act = () =>
            client.GetHeroIdsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    [Fact]
    public async Task GetAsync_RealClientAnswers_ReturnsTheRealEncounterContext()
    {
        var realClient = new Mock<IEncounterContextClient>();
        realClient
            .Setup(client =>
                client.GetAsync(It.IsAny<Guid>(), EncounterType.Boss, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new EncounterContext(EncounterType.Boss, 1, 5));
        var client = CreateEncounterContextClient(
            realClient.Object,
            new MockEncounterContextClient(
                Options.Create(new EncounterServiceOptions()),
                Mock.Of<ILogger>()
            ),
            isMockFallbackEnabled: true
        );

        EncounterContext context = await client.GetAsync(
            Guid.NewGuid(),
            EncounterType.Boss,
            TestContext.Current.CancellationToken
        );

        context.MonsterQuantity.Should().Be(1);
        context.Level.Should().Be(5);
    }

    [Fact]
    public async Task GetAsync_RealClientIsDown_FallsBackToTheMock()
    {
        var realClient = new Mock<IEncounterContextClient>();
        realClient
            .Setup(client =>
                client.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EncounterType>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ExternalServiceUnavailableException("Dungeon"));
        var client = CreateEncounterContextClient(
            realClient.Object,
            new MockEncounterContextClient(
                Options.Create(new EncounterServiceOptions()),
                Mock.Of<ILogger>()
            ),
            isMockFallbackEnabled: true
        );

        EncounterContext context = await client.GetAsync(
            Guid.NewGuid(),
            EncounterType.Enemy,
            TestContext.Current.CancellationToken
        );

        context.EncounterType.Should().Be(EncounterType.Enemy);
        context.MonsterQuantity.Should().BeGreaterThan(0);
    }

    private static FallbackSessionHeroesClient CreateSessionHeroesClient(
        ISessionHeroesClient realClient,
        MockSessionHeroesClient mockClient,
        bool isMockFallbackEnabled
    )
    {
        return new FallbackSessionHeroesClient(
            realClient,
            mockClient,
            Options.Create(
                new Combat.Infrastructure.ExternalServices.Player.PlayerServiceOptions
                {
                    IsMockFallbackEnabled = isMockFallbackEnabled,
                }
            ),
            Mock.Of<ILogger>()
        );
    }

    private static FallbackEncounterContextClient CreateEncounterContextClient(
        IEncounterContextClient realClient,
        MockEncounterContextClient mockClient,
        bool isMockFallbackEnabled
    )
    {
        return new FallbackEncounterContextClient(
            realClient,
            mockClient,
            Options.Create(
                new EncounterServiceOptions { IsMockFallbackEnabled = isMockFallbackEnabled }
            ),
            Mock.Of<ILogger>()
        );
    }
}
