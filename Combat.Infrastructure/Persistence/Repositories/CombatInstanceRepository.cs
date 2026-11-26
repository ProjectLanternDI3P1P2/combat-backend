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

    public async Task<CombatInstance?> GetByIdAsync(
        Guid combatId,
        CancellationToken cancellationToken
    )
    {
        CombatInstanceRecord? record = await dbContext
            .CombatInstances.Include(instance => instance.Fighters)
            .SingleOrDefaultAsync(instance => instance.CombatId == combatId, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(CombatInstance combat, CancellationToken cancellationToken)
    {
        await dbContext.CombatInstances.AddAsync(ToRecord(combat), cancellationToken);
    }

    public async Task UpdateAsync(CombatInstance combat, CancellationToken cancellationToken)
    {
        CombatInstanceRecord record =
            dbContext.CombatInstances.Local.SingleOrDefault(instance =>
                instance.CombatId == combat.CombatId
            )
            ?? await dbContext
                .CombatInstances.Include(instance => instance.Fighters)
                .SingleAsync(instance => instance.CombatId == combat.CombatId, cancellationToken);

        CopyProgress(combat, record);
        foreach (Fighter fighter in combat.Fighters)
        {
            CombatFighterRecord fighterRecord = record.Fighters.Single(stored =>
                stored.FighterId == fighter.FighterId
            );
            CopyState(fighter, fighterRecord);
        }
    }

    public async Task<IReadOnlyList<Guid>> ListPendingResultReportsAsync(
        int limit,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .CombatInstances.AsNoTracking()
            .Where(instance => instance.EndedAt != null && instance.ResultReportedAt == null)
            .OrderBy(instance => instance.EndedAt)
            .Take(limit)
            .Select(instance => instance.CombatId)
            .ToListAsync(cancellationToken);
    }

    private static CombatInstance ToDomain(CombatInstanceRecord record)
    {
        var progress = new CombatProgress(
            new CombatContext(record.RunId, Math.Max(1, record.Floor), record.RoomId),
            record.Result,
            record.Round,
            record.Turn,
            record.RoundIndex,
            Deserialize<Guid>(record.RoundOrderJson),
            record.ActiveFighterId,
            record.TurnDeadline,
            record.TurnDurationSeconds,
            record.EndedAt,
            record.ResultReportedAt,
            Deserialize<FightEvent>(record.EventsJson),
            DeserializeMap(record.ProcessedCommandsJson)
        );

        return CombatInstance.Rehydrate(
            record.CombatId,
            record.SessionId,
            record.EncounterId,
            record.CommandId,
            record.EncounterType,
            record.Status,
            record.StartedAt,
            [.. record.Fighters.OrderBy(fighter => fighter.Order).Select(ToDomain)],
            progress
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
            Deserialize<FighterEffect>(record.EffectsJson),
            DeserializeVisual(record.VisualJson),
            record.IsDefending,
            Deserialize<FighterItem>(record.ItemsJson)
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
            StartedAt = combat.StartedAt,
        };
        CopyProgress(combat, record);

        int order = 0;
        foreach (Fighter fighter in combat.Fighters)
        {
            var fighterRecord = new CombatFighterRecord
            {
                FighterId = fighter.FighterId,
                CombatId = combat.CombatId,
                Order = order++,
                Type = fighter.Type,
                ExternalId = fighter.ExternalId,
                Name = fighter.Name,
                Level = fighter.Level,
                IsFromMockedData = fighter.IsFromMockedData,
                MaxHp = fighter.MaxHp,
                MaxMana = fighter.MaxMana,
                InitialCurrentHp = fighter.InitialState.CurrentHp,
                InitialMaxHp = fighter.InitialState.MaxHp,
                InitialCurrentMana = fighter.InitialState.CurrentMana,
                InitialMaxMana = fighter.InitialState.MaxMana,
                InitialAttack = fighter.InitialState.Stats.Attack,
                InitialDefense = fighter.InitialState.Stats.Defense,
                InitialSpeed = fighter.InitialState.Stats.Speed,
                AbilitiesJson = JsonSerializer.Serialize(fighter.Abilities, JsonOptions),
                VisualJson = JsonSerializer.Serialize(fighter.Visual, JsonOptions),
            };
            CopyState(fighter, fighterRecord);
            record.Fighters.Add(fighterRecord);
        }

        return record;
    }

    private static void CopyProgress(CombatInstance combat, CombatInstanceRecord record)
    {
        record.Status = combat.Status;
        record.RunId = combat.RunId;
        record.Floor = combat.Floor;
        record.RoomId = combat.RoomId;
        record.Result = combat.Result;
        record.Round = combat.Round;
        record.Turn = combat.Turn;
        record.RoundIndex = combat.RoundIndex;
        record.RoundOrderJson = JsonSerializer.Serialize(combat.RoundOrder, JsonOptions);
        record.ActiveFighterId = combat.ActiveFighterId;
        record.TurnDeadline = combat.TurnDeadline;
        record.TurnDurationSeconds = combat.TurnDurationSeconds;
        record.EndedAt = combat.EndedAt;
        record.ResultReportedAt = combat.ResultReportedAt;
        record.EventsJson = JsonSerializer.Serialize(combat.Events, JsonOptions);
        record.ProcessedCommandsJson = JsonSerializer.Serialize(
            combat.ProcessedCommands.ToDictionary(
                command => command.Key.ToString(),
                command => command.Value
            ),
            JsonOptions
        );
    }

    private static void CopyState(Fighter fighter, CombatFighterRecord record)
    {
        record.CurrentHp = fighter.CurrentHp;
        record.CurrentMana = fighter.CurrentMana;
        record.Attack = fighter.Stats.Attack;
        record.Defense = fighter.Stats.Defense;
        record.Speed = fighter.Stats.Speed;
        record.State = fighter.State;
        record.IsDefending = fighter.IsDefending;
        record.ItemsJson = JsonSerializer.Serialize(fighter.Items, JsonOptions);
        record.EffectsJson = JsonSerializer.Serialize(fighter.Effects, JsonOptions);
    }

    private static FighterVisual? DeserializeVisual(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return null;
        }

        FighterVisual? visual = JsonSerializer.Deserialize<FighterVisual>(json, JsonOptions);
        return string.IsNullOrEmpty(visual?.Kind) ? null : visual;
    }

    private static Dictionary<Guid, ProcessedCommand> DeserializeMap(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        Dictionary<string, ProcessedCommand> stored =
            JsonSerializer.Deserialize<Dictionary<string, ProcessedCommand>>(json, JsonOptions)
            ?? [];
        return stored.ToDictionary(entry => Guid.Parse(entry.Key), entry => entry.Value);
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
