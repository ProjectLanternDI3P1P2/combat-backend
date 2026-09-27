using Combat.Application.Models;
using MediatR;

namespace Combat.Application.Features.MonsterTypes.GetMonsterTypeById;

public sealed record GetMonsterTypeByIdQuery(string MonsterTypeId) : IRequest<MonsterTypeDefinition>;
