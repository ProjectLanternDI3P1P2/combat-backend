using Combat.Application.Models.HeroCombat;
using MediatR;

namespace Combat.Application.Features.HeroUseCase.GetHeroCombatData;

public record GetHeroCombatDataQuery(Guid HeroId) : IRequest<HeroCombatData>;
