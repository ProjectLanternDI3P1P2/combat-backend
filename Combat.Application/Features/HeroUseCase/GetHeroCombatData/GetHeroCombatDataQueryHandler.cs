using Combat.Application.Models.HeroCombat;
using Combat.Application.Services;
using MediatR;

namespace Combat.Application.Features.HeroUseCase.GetHeroCombatData;

public sealed class GetHeroCombatDataQueryHandler(IHeroCombatDataProvider heroCombatDataProvider)
    : IRequestHandler<GetHeroCombatDataQuery, HeroCombatData>
{
    public Task<HeroCombatData> Handle(GetHeroCombatDataQuery request, CancellationToken cancellationToken)
    {
        return heroCombatDataProvider.GetAsync(request.HeroId, cancellationToken);
    }
}
