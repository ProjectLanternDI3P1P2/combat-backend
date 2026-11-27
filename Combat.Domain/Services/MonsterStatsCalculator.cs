using Combat.Domain.Entities;
using Combat.Domain.ValueObjects;

namespace Combat.Domain.Services;

/// <summary>
/// Applies the floor and the party size to the base statistics of a monster definition.
/// The rule belongs to the business model, so both monster generation use cases share it.
/// </summary>
public static class MonsterStatsCalculator
{
    public static MonsterStats Calculate(
        MonsterDefinition definition,
        int floor,
        int playerCount,
        int statIncreasePerFloor,
        int additionalPlayerStatMultiplier
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerCount);
        ArgumentOutOfRangeException.ThrowIfNegative(statIncreasePerFloor);
        ArgumentOutOfRangeException.ThrowIfNegative(additionalPlayerStatMultiplier);

        int statMultiplier = 1 + ((playerCount - 1) * additionalPlayerStatMultiplier);
        int floorIncrease = (floor - 1) * statIncreasePerFloor;

        return new MonsterStats(
            checked((definition.BaseHealth + floorIncrease) * statMultiplier),
            checked((definition.BaseAttack + floorIncrease) * statMultiplier),
            checked((definition.BaseDefense + floorIncrease) * statMultiplier),
            checked((definition.BaseSpeed + floorIncrease) * statMultiplier)
        );
    }
}
