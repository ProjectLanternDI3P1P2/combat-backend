using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.CombatUseCase.JoinCombat;

public sealed class JoinCombatCommandHandler(
    ICombatInstanceRepository repository,
    IDice dice,
    IClock clock,
    ILogger logger
) : IRequestHandler<JoinCombatCommand, CombatView>
{
    public async Task<CombatView> Handle(
        JoinCombatCommand request,
        CancellationToken cancellationToken
    )
    {
        CombatInstance combat =
            await repository.GetByIdAsync(request.CombatId, cancellationToken)
            ?? throw new CombatNotFoundException(request.CombatId);

        IReadOnlyList<FightEvent> expired = combat.ApplyExpiredTurns(dice, clock.UtcNow);
        if (expired.Count > 0)
        {
            await repository.UpdateAsync(combat, cancellationToken);
            logger.Information(
                "Combat {CombatId}: expired turns passed, {EventCount} events, turn {Turn}, status {Status}.",
                combat.CombatId,
                expired.Count,
                combat.Turn,
                combat.Status
            );
        }

        return CombatView.From(combat);
    }
}
