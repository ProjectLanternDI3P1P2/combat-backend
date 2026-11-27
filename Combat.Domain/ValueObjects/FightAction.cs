using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <param name="CommandId">Idempotency key: a replayed command returns its first outcome.</param>
/// <param name="AbilityCode">Code of the ability used, for <see cref="FightActionType.Ability"/>.</param>
public sealed record FightAction(
    Guid CommandId,
    Guid ActorId,
    FightActionType Type,
    Guid? TargetId = null,
    string? AbilityCode = null,
    Guid? ItemId = null
);
