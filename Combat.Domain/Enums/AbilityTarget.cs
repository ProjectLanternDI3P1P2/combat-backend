namespace Combat.Domain.Enums;

/// <summary>Target vocabulary of the Player service abilities ("SingleEnemy", "Self"...).</summary>
public enum AbilityTarget
{
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    Self,
}
