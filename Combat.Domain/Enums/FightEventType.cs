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

    /// <summary>The active hero passed its turn.</summary>
    Passed,

    /// <summary>The active hero did not act before the deadline: the turn is passed.</summary>
    TurnTimedOut,
    Fled,
    KnockedOut,
    Victory,
    Defeat,
}
