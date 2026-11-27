namespace Combat.Application.Models;

/// <summary>Combat rules read from the "Combat" configuration section.</summary>
public sealed class CombatOptions
{
    public const string SectionName = "Combat";

    /// <summary>Seconds a hero has to act before the turn is passed.</summary>
    public int TurnDurationSeconds { get; init; } = 30;

    /// <summary>Heroes accepted in one combat.</summary>
    public int MaxHeroes { get; init; } = 4;
}
