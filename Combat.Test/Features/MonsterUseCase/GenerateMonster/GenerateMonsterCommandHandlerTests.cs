using Combat.Application.Exceptions;
using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Application.Models;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Combat.Test.Features.MonsterUseCase.GenerateMonster;

public sealed class GenerateMonsterCommandHandlerTests
{
    private readonly Mock<IMonsterRepository> _monsterRepository = new();
    private readonly Mock<IMonsterDefinitionRepository> _definitionRepository = new();
    private readonly GenerateMonsterCommandHandler _handler;

    public GenerateMonsterCommandHandlerTests()
    {
        _handler = new GenerateMonsterCommandHandler(
            _monsterRepository.Object,
            _definitionRepository.Object,
            Options.Create(new MonsterGenerationOptions())
        );
    }

    [Fact]
    public async Task Handle_NewRequest_AddsMonsterFromRequestedClass()
    {
        Guid dungeonRunId = Guid.NewGuid();
        Guid idempotencyKey = Guid.NewGuid();
        MonsterDefinition definition = CreateDefinition(MonsterClass.Ordinary, 10);
        _monsterRepository
            .Setup(repository =>
                repository.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Monster?)null);
        _definitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Ordinary, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([definition]);
        _monsterRepository
            .Setup(repository =>
                repository.AddAsync(It.IsAny<Monster>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);

        GenerateMonsterResult result = await _handler.Handle(
            new GenerateMonsterCommand(
                dungeonRunId,
                idempotencyKey,
                Floor: 5,
                PlayerCount: 3,
                MonsterClass: MonsterClass.Ordinary
            ),
            CancellationToken.None
        );

        result.MonsterClass.Should().Be(MonsterClass.Ordinary);
        result.Level.Should().Be(5);
        result.MonsterId.Should().NotBeEmpty();
        _monsterRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.Is<Monster>(monster =>
                        monster.Id == result.MonsterId
                        && monster.MonsterDefinitionId == definition.Id
                        && monster.DungeonRunId == dungeonRunId
                        && monster.IdempotencyKey == idempotencyKey
                        && monster.PlayerCount == 3
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReplayedRequest_ReturnsTheExistingMonster()
    {
        Guid idempotencyKey = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        var existingMonster = new Monster
        {
            Id = Guid.NewGuid(),
            DungeonRunId = dungeonRunId,
            IdempotencyKey = idempotencyKey,
            Class = MonsterClass.Boss,
            Level = 10,
            PlayerCount = 2,
            Name = "Crypt Sovereign",
            ImageUrl = "/assets/monsters/crypt-sovereign.png",
            BaseHealth = 76,
            BaseAttack = 76,
            BaseDefense = 76,
            BaseSpeed = 76,
        };
        _monsterRepository
            .Setup(repository =>
                repository.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existingMonster);

        GenerateMonsterResult result = await _handler.Handle(
            new GenerateMonsterCommand(
                dungeonRunId,
                idempotencyKey,
                existingMonster.Level,
                existingMonster.PlayerCount,
                existingMonster.Class
            ),
            CancellationToken.None
        );

        result.MonsterId.Should().Be(existingMonster.Id);
        _definitionRepository.Verify(
            repository =>
                repository.GetByClassAsync(It.IsAny<MonsterClass>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReusedIdempotencyKeyWithDifferentRequest_ThrowsConflict()
    {
        Guid idempotencyKey = Guid.NewGuid();
        var existingMonster = new Monster
        {
            Id = Guid.NewGuid(),
            DungeonRunId = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            Class = MonsterClass.Ordinary,
            Level = 1,
            PlayerCount = 1,
        };
        _monsterRepository
            .Setup(repository =>
                repository.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existingMonster);

        Func<Task> act = () =>
            _handler.Handle(
                new GenerateMonsterCommand(
                    existingMonster.DungeonRunId,
                    idempotencyKey,
                    Floor: 2,
                    PlayerCount: 1,
                    MonsterClass: MonsterClass.Ordinary
                ),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
    }

    private static MonsterDefinition CreateDefinition(MonsterClass monsterClass, int baseStat)
    {
        return new MonsterDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Test monster",
            Class = monsterClass,
            ImageUrl = "/assets/monsters/test-monster.png",
            BaseHealth = baseStat,
            BaseAttack = baseStat,
            BaseDefense = baseStat,
            BaseSpeed = baseStat,
        };
    }
}
