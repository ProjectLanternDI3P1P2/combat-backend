namespace Combat.Domain.Enums;

/// <summary>What happened during a turn, in the order the client must replay it.</summary>
public enum FightEventType
{
    TurnStarted,
    Attacked,
    AbilityUsed,
    ItemUsed,
    Damaged,

    /// <summary>The target sidestepped the blow: no damage.</summary>
    Missed,
    Healed,
    ManaRestored,
    Defended,
    FleeFailed,
    Fled,
    KnockedOut,
    Victory,
    Defeat,
}
