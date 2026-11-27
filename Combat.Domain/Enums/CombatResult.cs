namespace Combat.Domain.Enums;

/// <summary>How a finished combat ended.</summary>
public enum CombatResult
{
    Victory,
    Defeat,

    /// <summary>The party ran away; the monsters survive.</summary>
    Fled,
}
