namespace Combat.Domain.Exceptions;

/// <summary>An operation is not allowed in the fighter's current state.</summary>
public sealed class InvalidFighterOperationException(Guid fighterId, string reason)
    : Exception($"Fighter '{fighterId}' cannot perform this operation: {reason}")
{
    public Guid FighterId { get; } = fighterId;
    public string Reason { get; } = reason;
}
