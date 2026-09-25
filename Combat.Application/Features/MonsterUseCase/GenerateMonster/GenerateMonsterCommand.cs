using MediatR;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public record GenerateMonsterCommand(Guid CombatId, Guid MonsterTypeId) : IRequest<GenerateMonsterResult>;
