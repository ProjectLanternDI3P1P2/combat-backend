using Combat.Domain.Enums;

namespace Combat.Presentation.DTO;

/// <param name="Floor">1-based floor of the dungeon.</param>
/// <param name="MonsterCount">Optional: defaults to 3 for an ordinary encounter, 1 otherwise.</param>
public sealed record StartFightDto(
    Guid DungeonRunId,
    Guid IdempotencyKey,
    int Floor,
    MonsterClass Encounter,
    IReadOnlyList<Guid> HeroIds,
    int? MonsterCount
);
