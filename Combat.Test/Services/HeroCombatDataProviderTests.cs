using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Combat.Application.Services;
using FluentAssertions;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Services;

public class HeroCombatDataProviderTests
{
    private readonly Mock<IHeroSnapshotClient> _heroSnapshotClientMock = new();
    private readonly Mock<ICombatInventoryClient> _combatInventoryClientMock = new();
    private readonly HeroCombatDataProvider _provider;
    private readonly Guid _heroId = Guid.NewGuid();

    public HeroCombatDataProviderTests()
    {
        _provider = new HeroCombatDataProvider(
            _heroSnapshotClientMock.Object,
            _combatInventoryClientMock.Object,
            new Mock<ILogger>().Object);
    }

    [Fact]
    public async Task GetAsync_BothServicesAnswer_ReturnsCombinedData()
    {
        // Arrange
        HeroCombatSnapshot hero = CreateHero(_heroId);
        CombatInventorySnapshot inventory = CreateInventory(_heroId);
        SetupHero(hero);
        SetupInventory(inventory);

        // Act
        HeroCombatData result = await _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        result.HeroId.Should().Be(_heroId);
        result.Hero.Should().Be(hero);
        result.Inventory.Should().Be(inventory);
        result.IsMocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_HeroDoesNotExist_ThrowsHeroNotFoundException()
    {
        // Arrange
        _heroSnapshotClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(_heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HeroNotFoundException(_heroId));
        SetupInventory(CreateInventory(_heroId));

        // Act
        Func<Task> act = () => _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>().WithMessage($"*{_heroId}*");
    }

    [Fact]
    public async Task GetAsync_PlayerServiceUnavailable_ThrowsExternalServiceUnavailableException()
    {
        // Arrange
        _heroSnapshotClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(_heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));
        SetupInventory(CreateInventory(_heroId));

        // Act
        Func<Task> act = () => _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ExternalServiceUnavailableException>())
            .Which.ServiceName.Should().Be("Player");
    }

    [Fact]
    public async Task GetAsync_RewardsServiceUnavailable_ThrowsExternalServiceUnavailableException()
    {
        // Arrange
        SetupHero(CreateHero(_heroId));
        _combatInventoryClientMock
            .Setup(client => client.GetCombatInventorySnapshotAsync(_heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceUnavailableException("Rewards"));

        // Act
        Func<Task> act = () => _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ExternalServiceUnavailableException>())
            .Which.ServiceName.Should().Be("Rewards");
    }

    public static TheoryData<HeroCombatSnapshot> InvalidHeroes()
    {
        Guid heroId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        HeroCombatSnapshot valid = CreateHero(heroId);

        return new TheoryData<HeroCombatSnapshot>(
            valid with { MaxHp = 0, CurrentHp = 0 },
            valid with { CurrentHp = valid.MaxHp + 1 },
            valid with { CurrentHp = 0 },
            valid with { CurrentMana = valid.MaxMana + 1 },
            valid with { CurrentMana = -1 },
            valid with { Stats = valid.Stats with { Defense = -1 } },
            valid with { Abilities = [valid.Abilities.First() with { ManaCost = -5 }] },
            valid with { HeroId = Guid.NewGuid() });
    }

    [Theory]
    [MemberData(nameof(InvalidHeroes))]
    public async Task GetAsync_InvalidHeroData_ThrowsInvalidHeroCombatDataException(HeroCombatSnapshot hero)
    {
        // Arrange
        Guid heroId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _heroSnapshotClientMock
            .Setup(client => client.GetCombatantSnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hero);
        _combatInventoryClientMock
            .Setup(client => client.GetCombatInventorySnapshotAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInventory(heroId));

        // Act
        Func<Task> act = () => _provider.GetAsync(heroId, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<InvalidHeroCombatDataException>())
            .Which.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAsync_NegativeItemQuantity_ThrowsInvalidHeroCombatDataException()
    {
        // Arrange
        SetupHero(CreateHero(_heroId));
        CombatInventorySnapshot inventory = CreateInventory(_heroId);
        SetupInventory(inventory with { Items = [inventory.Items.First() with { Quantity = -1 }] });

        // Act
        Func<Task> act = () => _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidHeroCombatDataException>();
    }

    [Fact]
    public async Task GetAsync_MockedHero_FlagsDataAsMocked()
    {
        // Arrange
        SetupHero(CreateHero(_heroId) with { IsMocked = true });
        SetupInventory(CreateInventory(_heroId));

        // Act
        HeroCombatData result = await _provider.GetAsync(_heroId, TestContext.Current.CancellationToken);

        // Assert
        result.IsMocked.Should().BeTrue();
    }

    private void SetupHero(HeroCombatSnapshot hero) => _heroSnapshotClientMock
        .Setup(client => client.GetCombatantSnapshotAsync(_heroId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(hero);

    private void SetupInventory(CombatInventorySnapshot inventory) => _combatInventoryClientMock
        .Setup(client => client.GetCombatInventorySnapshotAsync(_heroId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(inventory);

    private static HeroCombatSnapshot CreateHero(Guid heroId) => new()
    {
        HeroId = heroId,
        Name = "Test Hero",
        Level = 1,
        CurrentHp = 50,
        MaxHp = 100,
        CurrentMana = 10,
        MaxMana = 30,
        Stats = new HeroCombatStats { Attack = 10, Defense = 5, Speed = 7 },
        Abilities =
        [
            new HeroAbility { AbilityId = Guid.NewGuid(), Name = "Strike", ManaCost = 5, TargetType = "SingleEnemy" }
        ]
    };

    private static CombatInventorySnapshot CreateInventory(Guid heroId) => new()
    {
        HeroId = heroId,
        Items =
        [
            new CombatInventoryItem { ItemId = Guid.NewGuid(), Name = "Health Potion", Category = "Consumable", Quantity = 2 }
        ]
    };
}
