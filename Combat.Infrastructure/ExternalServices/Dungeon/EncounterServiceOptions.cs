namespace Combat.Infrastructure.ExternalServices.Dungeon;

/// <summary>Values served by the mocked Dungeon encounter context.</summary>
public sealed class EncounterServiceOptions
{
    public const string SectionName = "ExternalServices:Dungeon";

    /// <summary>Serve the mocked encounter context only when the real Dungeon service is unavailable.</summary>
    public bool IsMockFallbackEnabled { get; init; } = true;

    public int EnemyMonsterQuantity { get; init; } = 2;
    public int EnemyLevel { get; init; } = 1;
    public int SubBossMonsterQuantity { get; init; } = 1;
    public int SubBossLevel { get; init; } = 3;
    public int BossMonsterQuantity { get; init; } = 1;
    public int BossLevel { get; init; } = 5;
}
