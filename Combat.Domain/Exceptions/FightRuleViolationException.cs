namespace Combat.Domain.Exceptions;

/// <summary>
/// An attempt the rules refuse (not the actor's turn, unknown target, not enough mana...).
/// A refused attempt changes nothing and does not consume the turn.
/// </summary>
public sealed class FightRuleViolationException(Guid fightId, string reason)
    : Exception($"Fight '{fightId}' refused the action: {reason}")
{
    public Guid FightId { get; } = fightId;
    public string Reason { get; } = reason;
}
