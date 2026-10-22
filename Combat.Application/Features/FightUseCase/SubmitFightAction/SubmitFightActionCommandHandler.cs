using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Exceptions;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.FightUseCase.SubmitFightAction;

/// <summary>
/// Plays a hero's action and the monsters' turns that follow it. The result carries only
/// the events of this action, for the client to animate.
/// </summary>
public sealed class SubmitFightActionCommandHandler(
    IFightRepository fightRepository,
    IDice dice,
    IClock clock,
    ILogger logger
) : IRequestHandler<SubmitFightActionCommand, FightResult>
{
    public async Task<FightResult> Handle(
        SubmitFightActionCommand request,
        CancellationToken cancellationToken
    )
    {
        Fight fight =
            await fightRepository.GetByIdAsync(request.FightId, cancellationToken)
            ?? throw new FightNotFoundException(request.FightId);

        var action = new FightAction(
            request.CommandId,
            request.ActorId,
            request.Type,
            request.TargetId,
            request.AbilityId,
            request.ItemId
        );

        IReadOnlyList<FightEvent> events;
        // The fight lives in memory: one action at a time, whatever the number of requests.
        lock (fight)
        {
            try
            {
                events = fight.Submit(action, dice, clock.UtcNow);
            }
            catch (FightRuleViolationException exception)
            {
                logger.Information(
                    "Fight {FightId}: {ActionType} of {ActorId} refused ({Reason}). CommandId {CommandId}.",
                    fight.Id,
                    request.Type,
                    request.ActorId,
                    exception.Reason,
                    request.CommandId
                );
                throw;
            }
        }

        logger.Information(
            "Fight {FightId}: {ActionType} of {ActorId} resolved into {EventCount} events, turn {Turn}, status {Status}. CommandId {CommandId}.",
            fight.Id,
            request.Type,
            request.ActorId,
            events.Count,
            fight.Turn,
            fight.Status,
            request.CommandId
        );

        return FightResult.From(fight, events);
    }
}
