namespace Combat.Application.Exceptions;

// Inherits KeyNotFoundException so the existing HTTP mapping returns "not found".
public sealed class FightNotFoundException(Guid fightId)
    : KeyNotFoundException($"Fight '{fightId}' was not found.")
{
    public Guid FightId { get; } = fightId;
}
