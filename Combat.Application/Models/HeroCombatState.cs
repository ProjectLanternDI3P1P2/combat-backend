namespace Combat.Application.Models;

public enum HeroCombatStateAvailability
{
    Loading,
    Ready
}

public sealed record HeroCombatState(
    long Sequence,
    string Scenario,
    HeroCombatStateAvailability Availability,
    HeroCombatant? Hero);

public sealed record HeroCombatant(
    Guid? HeroId,
    string? Name,
    int? CurrentHealth,
    int? MaxHealth,
    int? CurrentMana,
    int? MaxMana,
    int? Level,
    int? Attack,
    int? Defense,
    int? Speed,
    string? State,
    IReadOnlyList<HeroAbility>? Abilities,
    IReadOnlyList<HeroEquipmentItem>? Equipment,
    IReadOnlyList<HeroConsumable>? Consumables);

public sealed record HeroAbility(
    Guid AbilityId,
    string Name,
    int? ManaCost,
    string? TargetType,
    string? UsageState);

public sealed record HeroEquipmentItem(
    Guid ItemId,
    string Name,
    string? Slot);

public sealed record HeroConsumable(
    Guid ItemId,
    string Name,
    int? Quantity);
