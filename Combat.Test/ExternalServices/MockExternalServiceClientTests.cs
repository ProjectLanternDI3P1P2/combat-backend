using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Domain.Enums;
using Combat.Infrastructure.ExternalServices;
using Combat.Infrastructure.ExternalServices.Dungeon;
using Combat.Infrastructure.ExternalServices.Player;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.ExternalServices;

public sealed class MockExternalServiceClientTests
{
    [Fact]
    public async Task GetHeroIdsAsync_KnownSession_ReturnsTheMockedRoster()
    {
        var client = new MockSessionHeroesClient(Mock.Of<ILogger>());

        IReadOnlyList<Guid> heroIds = await client.GetHeroIdsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        heroIds.Should().HaveCount(MockHeroCatalog.HeroIds.Count);
        heroIds.Should().OnlyContain(heroId => MockHeroCatalog.HeroIds.Contains(heroId));
    }

    [Fact]
    public async Task GetHeroIdsAsync_UnknownSession_Throws()
    {
        var client = new MockSessionHeroesClient(Mock.Of<ILogger>());

        Func<Task> act = () =>
            client.GetHeroIdsAsync(Guid.Empty, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SessionNotFoundException>();
    }

    [Theory]
    [InlineData(EncounterType.Enemy, 2, 1)]
    [InlineData(EncounterType.SubBoss, 1, 3)]
    [InlineData(EncounterType.Boss, 1, 5)]
    public async Task GetAsync_KnownEncounterType_ReturnsTheConfiguredContext(
        EncounterType encounterType,
        int expectedQuantity,
        int expectedLevel
    )
    {
        var client = new MockEncounterContextClient(
            Options.Create(new EncounterServiceOptions()),
            Mock.Of<ILogger>()
        );

        EncounterContext context = await client.GetAsync(
            Guid.NewGuid(),
            encounterType,
            TestContext.Current.CancellationToken
        );

        context.EncounterType.Should().Be(encounterType);
        context.MonsterQuantity.Should().Be(expectedQuantity);
        context.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public async Task GetAsync_UnknownEncounterType_Throws()
    {
        var client = new MockEncounterContextClient(
            Options.Create(new EncounterServiceOptions()),
            Mock.Of<ILogger>()
        );

        Func<Task> act = () =>
            client.GetAsync(
                Guid.NewGuid(),
                (EncounterType)42,
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<InvalidCombatEncounterException>();
    }
}
