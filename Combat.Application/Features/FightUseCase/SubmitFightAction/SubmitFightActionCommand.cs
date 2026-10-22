using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.FightUseCase.SubmitFightAction;

/// <param name="CommandId">Idempotency key: a double click or a retry is applied once.</param>
public sealed record SubmitFightActionCommand(
    Guid FightId,
    Guid CommandId,
    Guid ActorId,
    FightActionType Type,
    Guid? TargetId,
    Guid? AbilityId,
    Guid? ItemId
) : ICommand<FightResult>;
