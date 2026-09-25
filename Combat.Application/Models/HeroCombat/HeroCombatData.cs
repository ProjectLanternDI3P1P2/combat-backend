namespace Combat.Application.Models.HeroCombat;

/// <summary>Validated initial hero state shared with the combat modules that need it.</summary>
public sealed record HeroCombatData
{
    public required HeroCombatSnapshot Hero { get; init; }
    public required CombatInventorySnapshot Inventory { get; init; }

    public Guid HeroId => Hero.HeroId;
    public bool IsMocked => Hero.IsMocked || Inventory.IsMocked;
}
