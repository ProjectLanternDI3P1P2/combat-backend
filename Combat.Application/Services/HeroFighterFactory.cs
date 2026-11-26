using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Domain.Entities;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Services;

/// <summary>Maps the hero data retrieved at combat initialization to its temporary combat state.</summary>
public static class HeroFighterFactory
{
    /// <param name="currentHp">
    /// Hit points carried by the run; they replace Player's (full) value when present.
    /// </param>
    /// <param name="currentMana">Mana carried by the run, same rule.</param>
    public static Fighter Create(
        HeroCombatData data,
        int? currentHp = null,
        int? currentMana = null
    )
    {
        ArgumentNullException.ThrowIfNull(data);

        HeroCombatSnapshot hero = data.Hero;
        int hp = currentHp ?? hero.CurrentHp;
        int mana = currentMana ?? hero.CurrentMana;
        if (hp <= 0 || hp > hero.MaxHp || mana < 0 || mana > hero.MaxMana)
        {
            throw new InvalidHeroCombatDataException(
                hero.HeroId,
                [
                    $"The run state of the hero is out of range (hp {hp}/{hero.MaxHp}, mana {mana}/{hero.MaxMana}).",
                ]
            );
        }

        var initialState = new FighterInitialState(
            hp,
            hero.MaxHp,
            mana,
            hero.MaxMana,
            new FighterStats(hero.Stats.Attack, hero.Stats.Defense, hero.Stats.Speed),
            hero.Abilities.Select(ability => new FighterAbility(
                ability.Code,
                ability.Name,
                ability.ManaCost,
                ability.TargetType,
                ability.Power
            )),
            data.Inventory.Items.Select(item => new FighterItem(
                item.ItemId,
                item.Name,
                item.Category,
                item.Quantity
            ))
        );

        FighterVisual visual = FighterVisual.Hero(
            hero.ClassCode,
            hero.Appearance?.SkinTone,
            hero.Appearance?.HairStyle,
            hero.Appearance?.HairColor
        );

        return Fighter.CreateHero(
            hero.HeroId,
            hero.Name,
            hero.Level,
            initialState,
            data.IsMocked,
            visual
        );
    }
}
