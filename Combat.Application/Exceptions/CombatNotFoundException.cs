namespace Combat.Application.Exceptions;

// Inherits KeyNotFoundException so the existing HTTP and gRPC error mappings return "not found".
public sealed class CombatNotFoundException(Guid combatId)
    : KeyNotFoundException($"Combat '{combatId}' was not found.")
{
    public Guid CombatId { get; } = combatId;
}
