namespace Combat.Application.Models.HeroCombat;

public sealed record CombatInventoryItem
{
    public required Guid ItemId { get; init; }
    public required string Name { get; init; }

    // Kept as text until the item vocabulary is agreed with the Rewards service.
    public required string Category { get; init; }
    public required int Quantity { get; init; }
}
