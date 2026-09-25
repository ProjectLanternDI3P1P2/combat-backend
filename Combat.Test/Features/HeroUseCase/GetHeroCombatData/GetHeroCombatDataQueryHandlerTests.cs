using Combat.Application.Exceptions;
using Combat.Application.Features.HeroUseCase.GetHeroCombatData;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Services;
using FluentAssertions;
using Moq;

namespace Combat.Test.Features.HeroUseCase.GetHeroCombatData;

public class GetHeroCombatDataQueryHandlerTests
{
    private readonly Mock<IHeroCombatDataProvider> _providerMock = new();
    private readonly GetHeroCombatDataQueryHandler _handler;

    public GetHeroCombatDataQueryHandlerTests()
    {
        _handler = new GetHeroCombatDataQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task Handle_HeroDataAvailable_ReturnsProviderData()
    {
        // Arrange
        var heroId = Guid.NewGuid();
        var data = new HeroCombatData
        {
            Hero = new HeroCombatSnapshot
            {
                HeroId = heroId,
                Name = "Test Hero",
                Level = 1,
                CurrentHp = 10,
                MaxHp = 10,
                CurrentMana = 0,
                MaxMana = 0,
                Stats = new HeroCombatStats { Attack = 1, Defense = 1, Speed = 1 },
                Abilities = []
            },
            Inventory = new CombatInventorySnapshot { HeroId = heroId, Items = [] }
        };

        _providerMock
            .Setup(provider => provider.GetAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        HeroCombatData result = await _handler.Handle(new GetHeroCombatDataQuery(heroId), TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(data);
    }

    [Fact]
    public async Task Handle_HeroDoesNotExist_ThrowsHeroNotFoundException()
    {
        // Arrange
        var heroId = Guid.NewGuid();
        _providerMock
            .Setup(provider => provider.GetAsync(heroId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HeroNotFoundException(heroId));

        // Act
        Func<Task> act = () => _handler.Handle(new GetHeroCombatDataQuery(heroId), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HeroNotFoundException>();
    }
}
