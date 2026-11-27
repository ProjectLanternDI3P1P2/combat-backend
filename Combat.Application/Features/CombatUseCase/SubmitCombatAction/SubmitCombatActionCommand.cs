using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.CombatUseCase.SubmitCombatAction;

/// <summary>The active hero acts; the monsters' turns that follow are resolved too.</summary>
/// <param name="CommandId">Idempotency key: a replay returns the first outcome, applied once.</param>
public sealed record SubmitCombatActionCommand(
    Guid CombatId,
    Guid CommandId,
    Guid ActorId,
    FightActionType Type,
    Guid? TargetId,
    string? AbilityCode
) : ICommand<CombatActionResult>;
