using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public sealed record GenerateMonsterCommand(
    Guid DungeonRunId,
    Guid IdempotencyKey,
    int Floor,
    int PlayerCount,
    MonsterClass MonsterClass
) : ICommand<GenerateMonsterResult>;
