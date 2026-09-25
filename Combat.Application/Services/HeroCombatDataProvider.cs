using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Services;

public sealed class HeroCombatDataProvider(
    IHeroSnapshotClient heroSnapshotClient,
    ICombatInventoryClient combatInventoryClient,
    ILogger logger) : IHeroCombatDataProvider
{
    public async Task<HeroCombatData> GetAsync(Guid heroId, CancellationToken cancellationToken)
    {
        logger.Information("Retrieving combat data for hero {HeroId}.", heroId);

        HeroCombatSnapshot hero;
        CombatInventorySnapshot inventory;

        try
        {
            // Player and Rewards are independent dependencies: query them concurrently.
            Task<HeroCombatSnapshot> heroTask = heroSnapshotClient.GetCombatantSnapshotAsync(heroId, cancellationToken);
            Task<CombatInventorySnapshot> inventoryTask = combatInventoryClient.GetCombatInventorySnapshotAsync(heroId, cancellationToken);

            await Task.WhenAll(heroTask, inventoryTask);

            hero = await heroTask;
            inventory = await inventoryTask;
        }
        catch (HeroNotFoundException exception)
        {
            logger.Warning(exception, "Combat data retrieval failed: hero {HeroId} was not found.", heroId);
            throw;
        }
        catch (ExternalServiceUnavailableException exception)
        {
            logger.Error(
                exception,
                "Combat data retrieval failed for hero {HeroId}: {ServiceName} service unavailable.",
                heroId,
                exception.ServiceName);
            throw;
        }

        HeroCombatData data = new()
        {
            Hero = hero,
            Inventory = inventory
        };

        EnsureUsable(heroId, data);

        logger.Information(
            "Combat data retrieved for hero {HeroId}: Hp {CurrentHp}/{MaxHp}, Mana {CurrentMana}/{MaxMana}, {AbilityCount} abilities, {ItemCount} items, IsMocked {IsMocked}.",
            heroId,
            data.Hero.CurrentHp,
            data.Hero.MaxHp,
            data.Hero.CurrentMana,
            data.Hero.MaxMana,
            data.Hero.Abilities.Count,
            data.Inventory.Items.Count,
            data.IsMocked);

        return data;
    }

    private void EnsureUsable(Guid heroId, HeroCombatData data)
    {
        List<string> errors = [];
        HeroCombatSnapshot hero = data.Hero;

        if (hero.HeroId != heroId || data.Inventory.HeroId != heroId)
        {
            errors.Add("Retrieved data does not belong to the requested hero.");
        }

        if (hero.MaxHp <= 0)
        {
            errors.Add("MaxHp must be greater than 0.");
        }

        if (hero.CurrentHp <= 0 || hero.CurrentHp > hero.MaxHp)
        {
            errors.Add("CurrentHp must be between 1 and MaxHp.");
        }

        if (hero.MaxMana < 0 || hero.CurrentMana < 0 || hero.CurrentMana > hero.MaxMana)
        {
            errors.Add("CurrentMana must be between 0 and MaxMana.");
        }

        if (hero.Stats.Attack < 0 || hero.Stats.Defense < 0 || hero.Stats.Speed < 0)
        {
            errors.Add("Combat stats cannot be negative.");
        }

        if (hero.Abilities.Any(ability => ability.ManaCost < 0))
        {
            errors.Add("Ability mana cost cannot be negative.");
        }

        if (data.Inventory.Items.Any(item => item.Quantity < 0))
        {
            errors.Add("Inventory item quantity cannot be negative.");
        }

        if (errors.Count == 0)
        {
            return;
        }

        var exception = new InvalidHeroCombatDataException(heroId, errors);
        logger.Error(exception, "Combat data retrieved for hero {HeroId} is invalid.", heroId);
        throw exception;
    }
}
