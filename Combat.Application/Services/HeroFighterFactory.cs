using Combat.Application.Models.HeroCombat;
using Combat.Domain.Entities;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Services;

/// <summary>Maps the hero data retrieved at combat initialization to its temporary combat state.</summary>
public static class HeroFighterFactory
{
    public static Fighter Create(HeroCombatData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        HeroCombatSnapshot hero = data.Hero;

        var initialState = new FighterInitialState(
            hero.CurrentHp,
            hero.MaxHp,
            hero.CurrentMana,
            hero.MaxMana,
            new FighterStats(hero.Stats.Attack, hero.Stats.Defense, hero.Stats.Speed),
            hero.Abilities.Select(ability => new FighterAbility(
                ability.AbilityId,
                ability.Name,
                ability.ManaCost,
                ability.TargetType)),
            data.Inventory.Items.Select(item => new FighterItem(
                item.ItemId,
                item.Name,
                item.Category,
                item.Quantity)));

        return Fighter.CreateHero(hero.HeroId, hero.Name, hero.Level, initialState, data.IsMocked);
    }
}
