using MediatR;

namespace Combat.Application.Features.FightUseCase.GetFight;

public sealed record GetFightQuery(Guid FightId) : IRequest<FightResult>;
