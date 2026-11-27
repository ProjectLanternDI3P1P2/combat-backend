using Combat.Domain.Entities;
using Combat.Domain.Enums;

namespace Combat.Infrastructure.Persistence.Records;

/// <summary>Persisted representation of a combat instance.</summary>
public sealed class CombatInstanceRecord
{
    public Guid CombatId { get; set; }
    public Guid SessionId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid CommandId { get; set; }
    public EncounterType EncounterType { get; set; }
    public CombatStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public Guid RunId { get; set; }
    public int Floor { get; set; }
    public int RoomId { get; set; }
    public CombatResult? Result { get; set; }
    public int Round { get; set; }
    public int Turn { get; set; }
    public int RoundIndex { get; set; }
    public string RoundOrderJson { get; set; } = "[]";
    public Guid? ActiveFighterId { get; set; }
    public DateTimeOffset? TurnDeadline { get; set; }
    public int TurnDurationSeconds { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? ResultReportedAt { get; set; }
    public string EventsJson { get; set; } = "[]";
    public string ProcessedCommandsJson { get; set; } = "{}";

    /// <summary>PostgreSQL row version: two concurrent commands on one combat cannot both win.</summary>
    public uint Version { get; set; }
    public List<CombatFighterRecord> Fighters { get; set; } = [];
}
