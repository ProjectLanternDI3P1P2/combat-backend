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
    private readonly Mock<IMonsterTypeProvider> _monsterTypeProviderMock = new();
    private readonly Mock<IMonsterRepository> _monsterRepositoryMock = new();
    private readonly GenerateMonsterCommandHandler _handler;

    public GenerateMonsterCommandHandlerTests()
    {
        _handler = new GenerateMonsterCommandHandler(_monsterTypeProviderMock.Object, _monsterRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidType_GeneratesMonsterWithUniqueIdAndBaseStats()
    {
        // Arrange
        var combatId = Guid.NewGuid();
        var command = new GenerateMonsterCommand(combatId, "dragon");
        var monsterType = new MonsterTypeSummary
        {
            MonsterType = "dragon",
            IsBoss = true,
            BaseHp = 300,
            BaseAttack = 40,
            BaseDefense = 25,
            BaseSpeed = 15
        };
        Monster? capturedMonster = null;

        _monsterTypeProviderMock
            .Setup(provider => provider.GetMonsterTypeAsync("dragon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(monsterType);
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
        capturedMonster.BaseHp.Should().Be(monsterType.BaseHp);
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
        var command = new GenerateMonsterCommand(Guid.NewGuid(), "unknown-type");

        _monsterTypeProviderMock
            .Setup(provider => provider.GetMonsterTypeAsync("unknown-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonsterTypeSummary?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*unknown-type*");
        _monsterRepositoryMock.Verify(
            repository => repository.AddMonsterAsync(It.IsAny<Monster>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
