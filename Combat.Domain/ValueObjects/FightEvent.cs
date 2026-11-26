using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <summary>
/// One resolved fact of the fight. The client replays events in sequence order to animate
/// the turn; it never computes an outcome itself.
/// </summary>
/// <param name="Amount">HP or mana changed, 0 when not relevant.</param>
/// <param name="Label">Name of the ability or item used, when there is one.</param>
/// <param name="IsCritical">A critical hit (damage x 1.5).</param>
public sealed record FightEvent(
    int Sequence,
    int Turn,
    FightEventType Type,
    Guid? ActorId,
    Guid? TargetId,
    int Amount,
    string? Label,
    bool IsCritical = false
);
