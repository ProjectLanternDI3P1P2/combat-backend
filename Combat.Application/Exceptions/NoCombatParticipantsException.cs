namespace Combat.Application.Exceptions;

/// <summary>The session does not provide any hero able to fight.</summary>
public sealed class NoCombatParticipantsException(Guid sessionId)
    : Exception($"Session '{sessionId}' does not provide any hero able to start a combat.")
{
    public Guid SessionId { get; } = sessionId;
}
