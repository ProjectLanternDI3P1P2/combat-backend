namespace Combat.Application.Models.HeroCombat;

public sealed record HeroAbility
{
    /// <summary>Ability code owned by Player, e.g. "warrior-strike".</summary>
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int ManaCost { get; init; }

    /// <summary>"SingleEnemy", "AllEnemies", "Self" or "SingleAlly".</summary>
    public required string TargetType { get; init; }

    /// <summary>Percentage of the attack (offensive) or of the maximum HP (healing).</summary>
    public int Power { get; init; } = 100;
}
