namespace Combat.Application.Models.HeroCombat;

/// <summary>
/// Hero state owned by the Player service and read at combat initialization.
/// Transport contracts (real gRPC or mock) are mapped to this model so combat modules never depend on them.
/// </summary>
public sealed record HeroCombatSnapshot
{
    public required Guid HeroId { get; init; }
    public required string Name { get; init; }
    public required int Level { get; init; }
    public required int CurrentHp { get; init; }
    public required int MaxHp { get; init; }
    public required int CurrentMana { get; init; }
    public required int MaxMana { get; init; }
    public required HeroCombatStats Stats { get; init; }
    public required IReadOnlyCollection<HeroAbility> Abilities { get; init; }

    /// <summary>Player class code, e.g. "warrior"; drives the sprite and the headwear.</summary>
    public string ClassCode { get; init; } = string.Empty;

    /// <summary>Null when Player did not send it: the client then uses its default.</summary>
    public HeroAppearanceSnapshot? Appearance { get; init; }

    /// <summary>True when the data comes from the local mock instead of the Player service.</summary>
    public bool IsMocked { get; init; }
}
