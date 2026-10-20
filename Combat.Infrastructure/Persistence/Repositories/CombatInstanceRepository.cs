using System.Text.Json;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Combat.Domain.ValueObjects;
using Combat.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class CombatInstanceRepository(CombatDbContext dbContext) : ICombatInstanceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CombatInstance?> GetByCommandIdAsync(
        Guid commandId,
        CancellationToken cancellationToken
    )
    {
        CombatInstanceRecord? record = await dbContext
            .CombatInstances.Include(instance => instance.Fighters)
            .SingleOrDefaultAsync(instance => instance.CommandId == commandId, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<CombatInstance?> GetByEncounterIdAsync(
        Guid encounterId,
        CancellationToken cancellationToken
    )
    {
        CombatInstanceRecord? record = await dbContext
            .CombatInstances.Include(instance => instance.Fighters)
            .SingleOrDefaultAsync(
                instance => instance.EncounterId == encounterId,
                cancellationToken
            );

        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(CombatInstance combat, CancellationToken cancellationToken)
    {
        await dbContext.CombatInstances.AddAsync(ToRecord(combat), cancellationToken);
    }

    private static CombatInstance ToDomain(CombatInstanceRecord record)
    {
        return CombatInstance.Rehydrate(
            record.CombatId,
            record.SessionId,
            record.EncounterId,
            record.CommandId,
            record.EncounterType,
            record.Status,
            record.StartedAt,
            [.. record.Fighters.Select(ToDomain)]
        );
    }

    private static Fighter ToDomain(CombatFighterRecord record)
    {
        var initialState = new FighterInitialState(
            record.InitialCurrentHp,
            record.InitialMaxHp,
            record.InitialCurrentMana,
            record.InitialMaxMana,
            new FighterStats(record.InitialAttack, record.InitialDefense, record.InitialSpeed),
            Deserialize<FighterAbility>(record.AbilitiesJson),
            Deserialize<FighterItem>(record.ItemsJson),
            Deserialize<FighterEffect>(record.EffectsJson)
        );

        return Fighter.Rehydrate(
            record.FighterId,
            record.Type,
            record.ExternalId,
            record.Name,
            record.Level,
            initialState,
            record.IsFromMockedData,
            record.CurrentHp,
            record.CurrentMana,
            new FighterStats(record.Attack, record.Defense, record.Speed),
            record.State,
            Deserialize<FighterEffect>(record.EffectsJson)
        );
    }

    private static CombatInstanceRecord ToRecord(CombatInstance combat)
    {
        var record = new CombatInstanceRecord
        {
            CombatId = combat.CombatId,
            SessionId = combat.SessionId,
            EncounterId = combat.EncounterId,
            CommandId = combat.CommandId,
            EncounterType = combat.EncounterType,
            Status = combat.Status,
            StartedAt = combat.StartedAt,
        };

        foreach (Fighter fighter in combat.Fighters)
        {
            record.Fighters.Add(ToRecord(fighter, combat.CombatId));
        }

        return record;
    }

    private static CombatFighterRecord ToRecord(Fighter fighter, Guid combatId)
    {
        return new CombatFighterRecord
        {
            FighterId = fighter.FighterId,
            CombatId = combatId,
            Type = fighter.Type,
            ExternalId = fighter.ExternalId,
            Name = fighter.Name,
            Level = fighter.Level,
            IsFromMockedData = fighter.IsFromMockedData,
            CurrentHp = fighter.CurrentHp,
            MaxHp = fighter.MaxHp,
            CurrentMana = fighter.CurrentMana,
            MaxMana = fighter.MaxMana,
            Attack = fighter.Stats.Attack,
            Defense = fighter.Stats.Defense,
            Speed = fighter.Stats.Speed,
            State = fighter.State,
            InitialCurrentHp = fighter.InitialState.CurrentHp,
            InitialMaxHp = fighter.InitialState.MaxHp,
            InitialCurrentMana = fighter.InitialState.CurrentMana,
            InitialMaxMana = fighter.InitialState.MaxMana,
            InitialAttack = fighter.InitialState.Stats.Attack,
            InitialDefense = fighter.InitialState.Stats.Defense,
            InitialSpeed = fighter.InitialState.Stats.Speed,
            AbilitiesJson = JsonSerializer.Serialize(fighter.Abilities, JsonOptions),
            ItemsJson = JsonSerializer.Serialize(fighter.Items, JsonOptions),
            EffectsJson = JsonSerializer.Serialize(fighter.Effects, JsonOptions),
        };
    }

    private static List<T> Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
    }
}
