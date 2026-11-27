using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Exceptions;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.CombatUseCase.SubmitCombatAction;

/// <summary>
/// Plays a hero's action and the monsters' turns that follow it. Expired turns are passed
/// first, so a late action is refused like any action out of turn.
/// </summary>
public sealed class SubmitCombatActionCommandHandler(
    ICombatInstanceRepository repository,
    IDice dice,
    IClock clock,
    ILogger logger
) : IRequestHandler<SubmitCombatActionCommand, CombatActionResult>
{
    public async Task<CombatActionResult> Handle(
        SubmitCombatActionCommand request,
        CancellationToken cancellationToken
    )
    {
        CombatInstance combat =
            await repository.GetByIdAsync(request.CombatId, cancellationToken)
            ?? throw new CombatNotFoundException(request.CombatId);

        logger.Information(
            "Combat {CombatId}: {ActionType} received from {ActorId}, command {CommandId}.",
            combat.CombatId,
            request.Type,
            request.ActorId,
            request.CommandId
        );

        DateTimeOffset now = clock.UtcNow;
        bool isReplay = combat.ProcessedCommands.ContainsKey(request.CommandId);
        IReadOnlyList<FightEvent> expired = isReplay ? [] : combat.ApplyExpiredTurns(dice, now);
        if (expired.Count > 0)
        {
            // Saved with the action. If the action is refused, the transaction rolls back
            // and the next join or command passes the expired turns again.
            logger.Information(
                "Combat {CombatId}: turn timed out before command {CommandId}.",
                combat.CombatId,
                request.CommandId
            );
        }

        var action = new FightAction(
            request.CommandId,
            request.ActorId,
            request.Type,
            request.TargetId,
            request.AbilityCode
        );

        IReadOnlyList<FightEvent> events;
        try
        {
            events = combat.Submit(action, dice, now);
        }
        catch (FightRuleViolationException exception)
        {
            logger.Information(
                "Combat {CombatId}: {ActionType} of {ActorId} refused ({Reason}), command {CommandId}.",
                combat.CombatId,
                request.Type,
                request.ActorId,
                exception.Reason,
                request.CommandId
            );
            throw;
        }

        if (!isReplay)
        {
            await repository.UpdateAsync(combat, cancellationToken);
        }

        logger.Information(
            "Combat {CombatId}: {ActionType} of {ActorId} accepted, {EventCount} events, turn {Turn}, status {Status}, result {Result}, command {CommandId}.",
            combat.CombatId,
            request.Type,
            request.ActorId,
            events.Count,
            combat.Turn,
            combat.Status,
            combat.Result,
            request.CommandId
        );

        return new CombatActionResult(CombatView.From(combat), events);
    }
}
