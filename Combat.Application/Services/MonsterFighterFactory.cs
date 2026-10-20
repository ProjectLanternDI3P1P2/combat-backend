using Combat.Application.Models;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Services;

/// <summary>Maps a monster definition to the temporary state it receives in a combat.</summary>
public static class MonsterFighterFactory
{
    public const string DefaultAbilityName = "Basic attack";
    public const string DefaultAbilityTargetType = "SingleEnemy";

    public static Fighter Create(
        MonsterDefinition definition,
        int level,
        int playerCount,
        MonsterGenerationOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);

        MonsterStats stats = MonsterStatsCalculator.Calculate(
            definition,
            level,
            playerCount,
            options.StatIncreasePerFloor,
            options.AdditionalPlayerStatMultiplier
        );

        var initialState = new FighterInitialState(
            currentHp: stats.Health,
            maxHp: stats.Health,
            currentMana: 0,
            maxMana: 0,
            stats: new FighterStats(stats.Attack, stats.Defense, stats.Speed),
            abilities:
            [
                new FighterAbility(Guid.NewGuid(), DefaultAbilityName, 0, DefaultAbilityTargetType),
            ],
            items: [],
            effects: []
        );

        return Fighter.CreateMonster(Guid.NewGuid(), definition.Name, level, initialState);
    }
}
