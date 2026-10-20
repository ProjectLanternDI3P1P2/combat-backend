using Combat.Domain.Enums;

namespace Combat.Application.Models;

public static class EncounterTypeExtensions
{
    /// <summary>Maps an encounter type to the class of the monsters it produces.</summary>
    public static MonsterClass ToMonsterClass(this EncounterType encounterType)
    {
        return encounterType switch
        {
            EncounterType.Enemy => MonsterClass.Ordinary,
            EncounterType.SubBoss => MonsterClass.SubBoss,
            EncounterType.Boss => MonsterClass.Boss,
            _ => throw new ArgumentOutOfRangeException(
                nameof(encounterType),
                encounterType,
                "Unknown encounter type."
            ),
        };
    }
}
