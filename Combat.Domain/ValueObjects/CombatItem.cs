using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <summary>A consumable the hero carries into the fight, copied from the Rewards inventory.</summary>
public sealed record CombatItem(
    Guid ItemId,
    string Name,
    ItemEffect Effect,
    int Amount,
    int Quantity
);
