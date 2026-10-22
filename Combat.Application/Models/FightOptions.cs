namespace Combat.Application.Models;

/// <summary>Balancing knobs of the fight, read from the "Fight" configuration section.</summary>
public sealed class FightOptions
{
    public const string SectionName = "Fight";

    /// <summary>Monster HP = generated base health x this multiplier.</summary>
    public int MonsterHealthMultiplier { get; init; } = 4;

    /// <summary>Monsters of an ordinary encounter when the caller does not say.</summary>
    public int DefaultOrdinaryMonsterCount { get; init; } = 3;

    public int MaxMonsters { get; init; } = 4;

    public int MaxHeroes { get; init; } = 4;

    public int HealthPotionAmount { get; init; } = 40;

    public int ManaPotionAmount { get; init; } = 30;
}
