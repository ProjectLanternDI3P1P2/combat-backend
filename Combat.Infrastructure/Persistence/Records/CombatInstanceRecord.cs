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
    public List<CombatFighterRecord> Fighters { get; set; } = [];
}
