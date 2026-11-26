using Combat.Domain.Enums;

namespace Combat.Infrastructure.Persistence.Records;

/// <summary>Persisted representation of the temporary state of a combat participant.</summary>
public sealed class CombatFighterRecord
{
    public Guid FighterId { get; set; }
    public Guid CombatId { get; set; }
    public FighterType Type { get; set; }
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsFromMockedData { get; set; }

    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public FighterState State { get; set; }

    public int InitialCurrentHp { get; set; }
    public int InitialMaxHp { get; set; }
    public int InitialCurrentMana { get; set; }
    public int InitialMaxMana { get; set; }
    public int InitialAttack { get; set; }
    public int InitialDefense { get; set; }
    public int InitialSpeed { get; set; }

    public string AbilitiesJson { get; set; } = "[]";
    public string ItemsJson { get; set; } = "[]";
    public string EffectsJson { get; set; } = "[]";
}
