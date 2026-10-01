using Combat.Domain.Enums;

namespace Combat.Domain.Entities;

public class Monster
{
    public Guid Id { get; set; }

    public Guid DungeonRunId { get; set; }

    public Guid IdempotencyKey { get; set; }

    public Guid MonsterDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public MonsterClass Class { get; set; }

    public int Level { get; set; }

    public int PlayerCount { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int BaseHealth { get; set; }

    public int BaseAttack { get; set; }

    public int BaseDefense { get; set; }

    public int BaseSpeed { get; set; }

    public MonsterDefinition MonsterDefinition { get; set; } = null!;
}
