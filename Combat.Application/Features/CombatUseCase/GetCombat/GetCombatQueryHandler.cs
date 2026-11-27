using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using MediatR;

namespace Combat.Application.Features.CombatUseCase.GetCombat;

public sealed class GetCombatQueryHandler(ICombatInstanceRepository repository)
    : IRequestHandler<GetCombatQuery, CombatView>
{
    public async Task<CombatView> Handle(
        GetCombatQuery request,
        CancellationToken cancellationToken
    )
    {
        CombatInstance combat =
            await repository.GetByIdAsync(request.CombatId, cancellationToken)
            ?? throw new CombatNotFoundException(request.CombatId);
        return CombatView.From(combat);
    }
}
