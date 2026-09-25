using Combat.Infrastructure.HeroMocks;
using FluentAssertions;

namespace Combat.Test.HeroMocks;

public class MockHeroCombatStateSourceTests
{
    private readonly MockHeroCombatStateSource _source = new();

    [Fact]
    public async Task StartSessionAsync_CompleteScenario_ReturnsAllDisplayData()
    {
        // Act
        var state = await _source.StartSessionAsync(
            "connection-1",
            HeroMockScenarios.Complete,
            TestContext.Current.CancellationToken);

        // Assert
        state.Sequence.Should().Be(1);
        state.Hero.Should().NotBeNull();
        state.Hero!.CurrentHealth.Should().Be(84);
        state.Hero.MaxHealth.Should().Be(120);
        state.Hero.CurrentMana.Should().Be(31);
        state.Hero.MaxMana.Should().Be(45);
        state.Hero.Level.Should().Be(8);
        state.Hero.Attack.Should().Be(19);
        state.Hero.Defense.Should().Be(14);
        state.Hero.Speed.Should().Be(17);
        state.Hero.State.Should().Be("ready");
        state.Hero.Abilities.Should().HaveCount(2);
        state.Hero.Abilities![0].UsageState.Should().Be("available");
        state.Hero.Abilities[1].UsageState.Should().BeNull();
        state.Hero.Equipment.Should().ContainSingle();
        state.Hero.Consumables.Should().ContainSingle();
    }

    [Fact]
    public async Task StartSessionAsync_ZeroScenario_PreservesZeroValues()
    {
        // Act
        var state = await _source.StartSessionAsync(
            "connection-zero",
            HeroMockScenarios.Zero,
            TestContext.Current.CancellationToken);

        // Assert
        state.Hero.Should().NotBeNull();
        state.Hero!.CurrentHealth.Should().Be(0);
        state.Hero.MaxHealth.Should().Be(0);
        state.Hero.CurrentMana.Should().Be(0);
        state.Hero.MaxMana.Should().Be(0);
        state.Hero.Level.Should().Be(0);
        state.Hero.Attack.Should().Be(0);
        state.Hero.Defense.Should().Be(0);
        state.Hero.Speed.Should().Be(0);
        state.Hero.Consumables.Should().ContainSingle().Which.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task StartSessionAsync_EmptyScenario_ReturnsPresentEmptyCollections()
    {
        // Act
        var state = await _source.StartSessionAsync(
            "connection-empty",
            HeroMockScenarios.Empty,
            TestContext.Current.CancellationToken);

        // Assert
        state.Hero.Should().NotBeNull();
        state.Hero!.Abilities.Should().NotBeNull().And.BeEmpty();
        state.Hero.Equipment.Should().NotBeNull().And.BeEmpty();
        state.Hero.Consumables.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task StartSessionAsync_PartialScenario_PreservesAbsentValues()
    {
        // Act
        var state = await _source.StartSessionAsync(
            "connection-partial",
            HeroMockScenarios.Partial,
            TestContext.Current.CancellationToken);

        // Assert
        state.Hero.Should().NotBeNull();
        state.Hero!.CurrentHealth.Should().Be(38);
        state.Hero.MaxHealth.Should().BeNull();
        state.Hero.CurrentMana.Should().BeNull();
        state.Hero.MaxMana.Should().Be(20);
        state.Hero.State.Should().BeNull();
        state.Hero.Equipment.Should().BeNull();
        state.Hero.Abilities.Should().ContainSingle().Which.UsageState.Should().BeNull();
        state.Hero.Consumables.Should().ContainSingle().Which.Quantity.Should().BeNull();
    }

    [Fact]
    public async Task AdvanceAsync_ChangingScenario_UpdatesOnlyRequestedSession()
    {
        // Arrange
        await _source.StartSessionAsync(
            "changing-connection",
            HeroMockScenarios.Changing,
            TestContext.Current.CancellationToken);
        await _source.StartSessionAsync(
            "other-connection",
            HeroMockScenarios.Changing,
            TestContext.Current.CancellationToken);

        // Act
        var changed = await _source.AdvanceAsync(
            "changing-connection",
            TestContext.Current.CancellationToken);
        var resynchronized = await _source.GetCurrentAsync(
            "changing-connection",
            TestContext.Current.CancellationToken);
        var other = await _source.GetCurrentAsync(
            "other-connection",
            TestContext.Current.CancellationToken);

        // Assert
        changed.Sequence.Should().Be(2);
        changed.Hero!.CurrentHealth.Should().Be(57);
        changed.Hero.CurrentMana.Should().Be(18);
        resynchronized.Should().Be(changed);
        other.Sequence.Should().Be(1);
        other.Hero!.CurrentHealth.Should().Be(84);
    }

    [Fact]
    public async Task EndSession_ExistingSession_RemovesSession()
    {
        // Arrange
        await _source.StartSessionAsync(
            "closed-connection",
            HeroMockScenarios.Complete,
            TestContext.Current.CancellationToken);

        // Act
        _source.EndSession("closed-connection");
        Func<Task> act = async () => await _source.GetCurrentAsync(
            "closed-connection",
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
