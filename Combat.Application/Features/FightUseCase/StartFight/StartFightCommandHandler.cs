using Combat.Application.Models;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.FightUseCase.StartFight;

/// <summary>
/// Reads the heroes (Player, Rewards), generates the monsters of the encounter and opens the
/// fight. Replaying the same idempotency key returns the fight already opened.
/// </summary>
public sealed class StartFightCommandHandler(
    IFightRepository fightRepository,
    IMonsterDefinitionRepository monsterDefinitionRepository,
    IHeroCombatDataProvider heroCombatDataProvider,
    IDice dice,
    IClock clock,
    IOptions<FightOptions> fightOptions,
    IOptions<MonsterGenerationOptions> generationOptions,
    ILogger logger
) : IRequestHandler<StartFightCommand, FightResult>
{
    public async Task<FightResult> Handle(
        StartFightCommand request,
        CancellationToken cancellationToken
    )
    {
        Fight? existing = await fightRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            logger.Information(
                "Fight {FightId} replayed for idempotency key {IdempotencyKey}.",
                existing.Id,
                request.IdempotencyKey
            );
            return FightResult.From(existing, existing.Events);
        }

        List<Combatant> heroes = [];
        foreach (Guid heroId in request.HeroIds)
        {
            HeroCombatData data = await heroCombatDataProvider.GetAsync(heroId, cancellationToken);
            heroes.Add(ToCombatant(data));
        }

        List<Combatant> monsters = await GenerateMonstersAsync(
            request,
            heroes.Count,
            cancellationToken
        );

        Fight fight = Fight.Start(
            Guid.NewGuid(),
            request.DungeonRunId,
            request.IdempotencyKey,
            request.Encounter,
            request.Floor,
            heroes,
            monsters,
            dice,
            clock.UtcNow
        );
        Fight stored = await fightRepository.AddOrGetByIdempotencyKeyAsync(
            fight,
            cancellationToken
        );

        logger.Information(
            "Fight {FightId} started for run {DungeonRunId}: {Encounter} on floor {Floor}, {HeroCount} heroes against {MonsterCount} monsters.",
            stored.Id,
            stored.DungeonRunId,
            stored.Encounter,
            stored.Floor,
            heroes.Count,
            monsters.Count
        );

        return FightResult.From(stored, stored.Events);
    }

    private async Task<List<Combatant>> GenerateMonstersAsync(
        StartFightCommand request,
        int heroCount,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<MonsterDefinition> definitions =
            await monsterDefinitionRepository.GetByClassAsync(request.Encounter, cancellationToken);
        if (definitions.Count == 0)
        {
            throw new KeyNotFoundException(
                $"No monster definition is configured for class '{request.Encounter}'."
            );
        }

        int count =
            request.MonsterCount
            ?? (
                request.Encounter == MonsterClass.Ordinary
                    ? fightOptions.Value.DefaultOrdinaryMonsterCount
                    : 1
            );

        // Same scaling as GenerateMonster: +StatIncreasePerFloor per floor. A lone boss or
        // sub-boss faces the whole party, so its health grows with the number of heroes.
        int floorIncrease = (request.Floor - 1) * generationOptions.Value.StatIncreasePerFloor;
        int partyMultiplier = request.Encounter == MonsterClass.Ordinary ? 1 : heroCount;

        List<Combatant> monsters = [];
        for (int index = 0; index < count; index++)
        {
            MonsterDefinition definition = dice.Pick(definitions);
            int maxHp =
                (definition.BaseHealth + floorIncrease)
                * fightOptions.Value.MonsterHealthMultiplier
                * partyMultiplier;

            monsters.Add(
                new Combatant(
                    Guid.NewGuid(),
                    FightSide.Monster,
                    definition.Id,
                    count > 1 ? $"{definition.Name} {(char)('A' + index)}" : definition.Name,
                    request.Floor,
                    SpriteKeyOf(definition.ImageUrl),
                    maxHp,
                    maxHp,
                    0,
                    0,
                    definition.BaseAttack + floorIncrease,
                    definition.BaseDefense + floorIncrease,
                    definition.BaseSpeed + floorIncrease
                )
            );
        }

        return monsters;
    }

    private Combatant ToCombatant(HeroCombatData data)
    {
        HeroCombatSnapshot hero = data.Hero;
        return new Combatant(
            Guid.NewGuid(),
            FightSide.Hero,
            hero.HeroId,
            hero.Name,
            hero.Level,
            "hero",
            hero.CurrentHp,
            hero.MaxHp,
            hero.CurrentMana,
            hero.MaxMana,
            hero.Stats.Attack,
            hero.Stats.Defense,
            hero.Stats.Speed,
            hero.Abilities.Select(ToAbility).ToList(),
            data.Inventory.Items.Select(ToItem).OfType<CombatItem>().ToList()
        );
    }

    private static CombatAbility ToAbility(HeroAbility ability)
    {
        AbilityTarget target = Enum.TryParse(ability.TargetType, true, out AbilityTarget parsed)
            ? parsed
            : AbilityTarget.SingleEnemy;
        int power = target switch
        {
            AbilityTarget.SingleEnemy => 160,
            AbilityTarget.AllEnemies => 90,
            AbilityTarget.SingleAlly => 35,
            _ => 0,
        };
        return new CombatAbility(ability.AbilityId, ability.Name, ability.ManaCost, target, power);
    }

    /// <summary>Only potions are usable in a fight for now; anything else stays in the bag.</summary>
    private CombatItem? ToItem(CombatInventoryItem item)
    {
        if (
            !item.Category.Equals("Consumable", StringComparison.OrdinalIgnoreCase)
            || item.Quantity <= 0
        )
        {
            return null;
        }

        bool restoresMana = item.Name.Contains("mana", StringComparison.OrdinalIgnoreCase);
        return new CombatItem(
            item.ItemId,
            item.Name,
            restoresMana ? ItemEffect.RestoreMana : ItemEffect.RestoreHealth,
            restoresMana
                ? fightOptions.Value.ManaPotionAmount
                : fightOptions.Value.HealthPotionAmount,
            item.Quantity
        );
    }

    /// <summary>"/assets/monsters/cave-goblin.png" → "cave-goblin".</summary>
    private static string SpriteKeyOf(string imageUrl) =>
        Path.GetFileNameWithoutExtension(imageUrl);
}
