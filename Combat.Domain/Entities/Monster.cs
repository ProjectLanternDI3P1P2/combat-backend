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

    public static Monster Create(
        MonsterDefinition definition,
        Guid dungeonRunId,
        Guid idempotencyKey,
        int floor,
        int playerCount,
        int statIncreasePerFloor,
        int additionalPlayerStatMultiplier
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfLessThan(floor, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(playerCount, 1);

        int statMultiplier = 1 + ((playerCount - 1) * additionalPlayerStatMultiplier);
        int floorIncrease = (floor - 1) * statIncreasePerFloor;

        return new Monster
        {
            Id = Guid.NewGuid(),
            DungeonRunId = dungeonRunId,
            IdempotencyKey = idempotencyKey,
            MonsterDefinitionId = definition.Id,
            Name = definition.Name,
            Class = definition.Class,
            Level = floor,
            PlayerCount = playerCount,
            ImageUrl = definition.ImageUrl,
            BaseHealth = checked((definition.BaseHealth + floorIncrease) * statMultiplier),
            BaseAttack = checked((definition.BaseAttack + floorIncrease) * statMultiplier),
            BaseDefense = checked((definition.BaseDefense + floorIncrease) * statMultiplier),
            BaseSpeed = checked((definition.BaseSpeed + floorIncrease) * statMultiplier),
        };
    }
}
