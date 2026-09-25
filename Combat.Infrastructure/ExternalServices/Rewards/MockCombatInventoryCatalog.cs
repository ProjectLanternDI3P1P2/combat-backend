using Combat.Application.Models.HeroCombat;
using Combat.Infrastructure.ExternalServices.Player;

namespace Combat.Infrastructure.ExternalServices.Rewards;

/// <summary>Combat inventories served while the Rewards service contract is not available.</summary>
public static class MockCombatInventoryCatalog
{
    private static readonly Dictionary<Guid, CombatInventoryItem[]> Items = new()
    {
        [MockHeroCatalog.WarriorHeroId] =
        [
            Item("b0000000-0000-0000-0000-000000000001", "Health Potion", "Consumable", 2)
        ],
        [MockHeroCatalog.MageHeroId] =
        [
            Item("b0000000-0000-0000-0000-000000000001", "Health Potion", "Consumable", 1),
            Item("b0000000-0000-0000-0000-000000000002", "Mana Potion", "Consumable", 3)
        ]
    };

    // Rewards does not own heroes: an unknown hero simply has nothing usable in combat.
    public static CombatInventorySnapshot Get(Guid heroId) => new()
    {
        HeroId = heroId,
        Items = Items.GetValueOrDefault(heroId) ?? [],
        IsMocked = true
    };

    private static CombatInventoryItem Item(string itemId, string name, string category, int quantity) => new()
    {
        ItemId = Guid.Parse(itemId),
        Name = name,
        Category = category,
        Quantity = quantity
    };
}
