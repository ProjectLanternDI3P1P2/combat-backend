using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Combat.Test.Features.MonsterUseCase.GenerateMonster;

public class GenerateMonsterCommandHandlerTests
{
    private readonly Mock<IMonsterTypeCatalog> _monsterTypeCatalogMock = new();
    private readonly Mock<IMonsterRepository> _monsterRepositoryMock = new();
    private readonly GenerateMonsterCommandHandler _handler;

    public GenerateMonsterCommandHandlerTests()
    {
        _handler = new GenerateMonsterCommandHandler(_monsterTypeCatalogMock.Object, _monsterRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidType_GeneratesMonsterWithUniqueIdAndBaseStats()
    {
        // Arrange
        var combatId = Guid.NewGuid();
        var monsterTypeId = Guid.NewGuid();
        var command = new GenerateMonsterCommand(combatId, monsterTypeId);
        var monsterType = new MonsterTypeDefinition(monsterTypeId, "Dragon", true, 300, 40, 25, 15);
        Monster? capturedMonster = null;

        _monsterTypeCatalogMock
            .Setup(catalog => catalog.ResolveRequiredAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == monsterTypeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([monsterType]);
        _monsterRepositoryMock
            .Setup(repository => repository.AddMonsterAsync(It.IsAny<Monster>(), It.IsAny<CancellationToken>()))
            .Callback<Monster, CancellationToken>((monster, _) => capturedMonster = monster)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedMonster.Should().NotBeNull();
        capturedMonster!.MonsterId.Should().NotBeEmpty();
        capturedMonster.CombatId.Should().Be(combatId);
        capturedMonster.IsBoss.Should().BeTrue();
        capturedMonster.BaseHp.Should().Be(monsterType.BaseHealth);
        capturedMonster.BaseAttack.Should().Be(monsterType.BaseAttack);
        capturedMonster.BaseDefense.Should().Be(monsterType.BaseDefense);
        capturedMonster.BaseSpeed.Should().Be(monsterType.BaseSpeed);
        capturedMonster.State.Should().Be(MonsterState.Alive);

        result.MonsterId.Should().Be(capturedMonster.MonsterId);
        result.CombatId.Should().Be(combatId);
        result.IsBoss.Should().BeTrue();
        result.State.Should().Be(MonsterState.Alive);
    }

    [Fact]
    public async Task Handle_UnknownType_ThrowsKeyNotFoundException()
    {
        // Arrange
        var monsterTypeId = Guid.NewGuid();
        var command = new GenerateMonsterCommand(Guid.NewGuid(), monsterTypeId);

        _monsterTypeCatalogMock
            .Setup(catalog => catalog.ResolveRequiredAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Unknown monster type IDs: {monsterTypeId}."));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{monsterTypeId}*");
        _monsterRepositoryMock.Verify(
            repository => repository.AddMonsterAsync(It.IsAny<Monster>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
