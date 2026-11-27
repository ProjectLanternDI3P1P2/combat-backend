using Combat.Application.Models;
using Combat.Domain.Entities;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;

namespace Combat.Application.Services;

/// <summary>Maps a monster definition to the temporary state it receives in a combat.</summary>
public static class MonsterFighterFactory
{
    /// <param name="elementId">Id of the Dungeon mob the monster stands for.</param>
    public static Fighter Create(
        MonsterDefinition definition,
        Guid elementId,
        bool isBoss,
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
            abilities: [],
            items: [],
            effects: []
        );

        return Fighter.CreateMonster(
            elementId,
            definition.Name,
            level,
            initialState,
            FighterVisual.Monster(SpriteKeyOf(definition), isBoss)
        );
    }

    /// <summary>Sprite code of a definition: "/assets/monsters/cave-goblin.png" gives "cave-goblin".</summary>
    public static string SpriteKeyOf(MonsterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        string key = Path.GetFileNameWithoutExtension(definition.ImageUrl);
        return string.IsNullOrWhiteSpace(key) ? "unknown" : key;
    }

    /// <summary>
    /// Picks the definition a mob becomes. The same mob always becomes the same monster:
    /// the choice is a stable hash of its id over the definitions ordered by id.
    /// </summary>
    public static MonsterDefinition Pick(
        IReadOnlyList<MonsterDefinition> definitions,
        Guid elementId
    )
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one definition is required.",
                nameof(definitions)
            );
        }

        List<MonsterDefinition> ordered = [.. definitions.OrderBy(definition => definition.Id)];
        byte[] bytes = elementId.ToByteArray();
        uint hash = 2166136261;
        foreach (byte value in bytes)
        {
            hash = (hash ^ value) * 16777619;
        }

        return ordered[(int)(hash % (uint)ordered.Count)];
    }
}
