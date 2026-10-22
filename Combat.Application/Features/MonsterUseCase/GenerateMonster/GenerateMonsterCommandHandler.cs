using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public sealed class GenerateMonsterCommandHandler(
    IMonsterRepository monsterRepository,
    IMonsterDefinitionRepository monsterDefinitionRepository,
    IOptions<MonsterGenerationOptions> options
) : IRequestHandler<GenerateMonsterCommand, GenerateMonsterResult>
{
    public async Task<GenerateMonsterResult> Handle(
        GenerateMonsterCommand request,
        CancellationToken cancellationToken
    )
    {
        Monster? existingMonster = await monsterRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existingMonster is not null)
        {
            EnsureSameRequest(existingMonster, request);
            return GenerateMonsterResult.From(existingMonster);
        }

        IReadOnlyList<MonsterDefinition> definitions =
            await monsterDefinitionRepository.GetByClassAsync(
                request.MonsterClass,
                cancellationToken
            );
        MonsterDefinition definition =
            definitions.Count == 0
                ? throw new KeyNotFoundException(
                    $"No monster definition is configured for class '{request.MonsterClass}'."
                )
                : definitions[Random.Shared.Next(definitions.Count)];

        MonsterStats stats = MonsterStatsCalculator.Calculate(
            definition,
            request.Floor,
            request.PlayerCount,
            options.Value.StatIncreasePerFloor,
            options.Value.AdditionalPlayerStatMultiplier
        );

        var monster = new Monster
        {
            Id = Guid.NewGuid(),
            DungeonRunId = request.DungeonRunId,
            IdempotencyKey = request.IdempotencyKey,
            MonsterDefinitionId = definition.Id,
            Name = definition.Name,
            Class = definition.Class,
            Level = request.Floor,
            PlayerCount = request.PlayerCount,
            ImageUrl = definition.ImageUrl,
            BaseHealth = stats.Health,
            BaseAttack = stats.Attack,
            BaseDefense = stats.Defense,
            BaseSpeed = stats.Speed,
        };

        Monster persistedMonster = await monsterRepository.AddOrGetByIdempotencyKeyAsync(
            monster,
            cancellationToken
        );
        EnsureSameRequest(persistedMonster, request);
        return GenerateMonsterResult.From(persistedMonster);
    }

    private static void EnsureSameRequest(Monster monster, GenerateMonsterCommand request)
    {
        bool isSameRequest =
            monster.DungeonRunId == request.DungeonRunId
            && monster.Level == request.Floor
            && monster.PlayerCount == request.PlayerCount
            && monster.Class == request.MonsterClass;

        if (!isSameRequest)
        {
            throw new IdempotencyKeyReuseException(request.IdempotencyKey);
        }
    }
}
