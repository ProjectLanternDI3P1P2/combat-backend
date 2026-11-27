using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using MediatR;

namespace Combat.Application.Features.CombatUseCase.ReportCombatResult;

public sealed class MarkCombatResultReportedCommandHandler(
    ICombatInstanceRepository repository,
    IClock clock
) : IRequestHandler<MarkCombatResultReportedCommand>
{
    public async Task Handle(
        MarkCombatResultReportedCommand request,
        CancellationToken cancellationToken
    )
    {
        CombatInstance combat =
            await repository.GetByIdAsync(request.CombatId, cancellationToken)
            ?? throw new CombatNotFoundException(request.CombatId);
        combat.MarkResultReported(clock.UtcNow);
        await repository.UpdateAsync(combat, cancellationToken);
    }
}
