using Combat.Application.Models;

namespace Combat.Presentation.DTO;

public enum HeroCombatStateAvailabilityDto
{
    Loading,
    Ready
}

public sealed record HeroCombatStateDto(
    long Sequence,
    string Scenario,
    HeroCombatStateAvailabilityDto Availability,
    HeroCombatantDto? Hero)
{
    public static HeroCombatStateDto Loading(string scenario)
    {
        return new HeroCombatStateDto(0, scenario, HeroCombatStateAvailabilityDto.Loading, null);
    }

    public static HeroCombatStateDto FromApplication(HeroCombatState state)
    {
        return new HeroCombatStateDto(
            state.Sequence,
            state.Scenario,
            state.Availability switch
            {
                HeroCombatStateAvailability.Loading => HeroCombatStateAvailabilityDto.Loading,
                HeroCombatStateAvailability.Ready => HeroCombatStateAvailabilityDto.Ready,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state.Availability, "Unsupported availability.")
            },
            state.Hero is null ? null : HeroCombatantDto.FromApplication(state.Hero));
    }
}

public sealed record HeroCombatantDto(
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
    IReadOnlyList<HeroAbilityDto>? Abilities,
    IReadOnlyList<HeroEquipmentItemDto>? Equipment,
    IReadOnlyList<HeroConsumableDto>? Consumables)
{
    public static HeroCombatantDto FromApplication(HeroCombatant hero)
    {
        return new HeroCombatantDto(
            hero.HeroId,
            hero.Name,
            hero.CurrentHealth,
            hero.MaxHealth,
            hero.CurrentMana,
            hero.MaxMana,
            hero.Level,
            hero.Attack,
            hero.Defense,
            hero.Speed,
            hero.State,
            hero.Abilities?.Select(HeroAbilityDto.FromApplication).ToArray(),
            hero.Equipment?.Select(HeroEquipmentItemDto.FromApplication).ToArray(),
            hero.Consumables?.Select(HeroConsumableDto.FromApplication).ToArray());
    }
}

public sealed record HeroAbilityDto(
    Guid AbilityId,
    string Name,
    int? ManaCost,
    string? TargetType,
    string? UsageState)
{
    public static HeroAbilityDto FromApplication(HeroAbility ability)
    {
        return new HeroAbilityDto(
            ability.AbilityId,
            ability.Name,
            ability.ManaCost,
            ability.TargetType,
            ability.UsageState);
    }
}

public sealed record HeroEquipmentItemDto(Guid ItemId, string Name, string? Slot)
{
    public static HeroEquipmentItemDto FromApplication(HeroEquipmentItem item)
    {
        return new HeroEquipmentItemDto(item.ItemId, item.Name, item.Slot);
    }
}

public sealed record HeroConsumableDto(Guid ItemId, string Name, int? Quantity)
{
    public static HeroConsumableDto FromApplication(HeroConsumable item)
    {
        return new HeroConsumableDto(item.ItemId, item.Name, item.Quantity);
    }
}
