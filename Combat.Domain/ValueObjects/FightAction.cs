using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <param name="CommandId">Idempotency key: a replayed command returns its first outcome.</param>
public sealed record FightAction(
    Guid CommandId,
    Guid ActorId,
    FightActionType Type,
    Guid? TargetId = null,
    Guid? AbilityId = null,
    Guid? ItemId = null
);
