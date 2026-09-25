using Combat.Application.Models.HeroCombat;
using Combat.Domain.Entities;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Services;

public sealed class HeroFighterInitializer(
    IHeroCombatDataProvider heroCombatDataProvider,
    ILogger logger) : IHeroFighterInitializer
{
    public async Task<Fighter> InitializeAsync(Guid heroId, CancellationToken cancellationToken)
    {
        HeroCombatData data = await heroCombatDataProvider.GetAsync(heroId, cancellationToken);

        Fighter fighter = HeroFighterFactory.Create(data);

        logger.Information(
            "Temporary combat state {FighterId} created for hero {HeroId}: Hp {CurrentHp}/{MaxHp}, Mana {CurrentMana}/{MaxMana}, {AbilityCount} abilities, {ItemCount} items, IsFromMockedData {IsFromMockedData}.",
            fighter.FighterId,
            heroId,
            fighter.CurrentHp,
            fighter.MaxHp,
            fighter.CurrentMana,
            fighter.MaxMana,
            fighter.Abilities.Count,
            fighter.Items.Count,
            fighter.IsFromMockedData);

        return fighter;
    }
}
