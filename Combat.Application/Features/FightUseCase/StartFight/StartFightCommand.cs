using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.FightUseCase.StartFight;

/// <param name="Floor">1-based floor, which scales the monsters.</param>
/// <param name="MonsterCount">Monsters to generate; defaults by encounter when null.</param>
public sealed record StartFightCommand(
    Guid DungeonRunId,
    Guid IdempotencyKey,
    int Floor,
    MonsterClass Encounter,
    IReadOnlyList<Guid> HeroIds,
    int? MonsterCount
) : ICommand<FightResult>;
