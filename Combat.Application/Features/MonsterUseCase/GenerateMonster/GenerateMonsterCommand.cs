using MediatR;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public record GenerateMonsterCommand(Guid CombatId, string MonsterType) : IRequest<GenerateMonsterResult>;
