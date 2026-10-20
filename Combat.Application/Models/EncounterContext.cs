using Combat.Domain.Enums;

namespace Combat.Application.Models;

/// <summary>Context of an encounter, provided by the Dungeon service.</summary>
public sealed record EncounterContext
{
    public EncounterContext(EncounterType encounterType, int monsterQuantity, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(monsterQuantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);

        EncounterType = encounterType;
        MonsterQuantity = monsterQuantity;
        Level = level;
    }

    public EncounterType EncounterType { get; }

    /// <summary>Number of monsters the encounter requires.</summary>
    public int MonsterQuantity { get; }

    /// <summary>Level applied to the generated monsters.</summary>
    public int Level { get; }
}
