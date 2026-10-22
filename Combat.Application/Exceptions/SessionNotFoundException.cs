namespace Combat.Application.Exceptions;

// Inherits KeyNotFoundException so the existing HTTP and gRPC error mappings return "not found".
public sealed class SessionNotFoundException(Guid sessionId, Exception? innerException = null)
    : KeyNotFoundException($"Session '{sessionId}' was not found.", innerException)
{
    public Guid SessionId { get; } = sessionId;
}
