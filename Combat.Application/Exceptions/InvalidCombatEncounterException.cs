namespace Combat.Application.Exceptions;

/// <summary>The encounter cannot produce a valid combat.</summary>
public sealed class InvalidCombatEncounterException(string reason, Exception? innerException = null)
    : Exception($"The encounter cannot start a combat: {reason}", innerException)
{
    public string Reason { get; } = reason;
}
