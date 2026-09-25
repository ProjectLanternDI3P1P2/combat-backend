namespace Combat.Application.Models.HeroCombat;

/// <summary>Inventory data owned by the Rewards service and strictly needed during a combat.</summary>
public sealed record CombatInventorySnapshot
{
    public required Guid HeroId { get; init; }
    public required IReadOnlyCollection<CombatInventoryItem> Items { get; init; }

    /// <summary>True when the data comes from the local mock instead of the Rewards service.</summary>
    public bool IsMocked { get; init; }
}
