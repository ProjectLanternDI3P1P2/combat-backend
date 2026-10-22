using Combat.Application.Exceptions;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using MediatR;

namespace Combat.Application.Features.FightUseCase.GetFight;

/// <summary>The authoritative state of a fight, with its whole event log.</summary>
public sealed class GetFightQueryHandler(IFightRepository fightRepository)
    : IRequestHandler<GetFightQuery, FightResult>
{
    public async Task<FightResult> Handle(
        GetFightQuery request,
        CancellationToken cancellationToken
    )
    {
        Fight fight =
            await fightRepository.GetByIdAsync(request.FightId, cancellationToken)
            ?? throw new FightNotFoundException(request.FightId);

        lock (fight)
        {
            return FightResult.From(fight, fight.Events);
        }
    }
}
