namespace Combat.Application.Exceptions;

// Inherits KeyNotFoundException so the existing HTTP and gRPC error mappings return "not found".
public sealed class HeroNotFoundException(Guid heroId, Exception? innerException = null)
    : KeyNotFoundException($"Hero '{heroId}' was not found.", innerException)
{
    public Guid HeroId { get; } = heroId;
}
