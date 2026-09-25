using Combat.Application.Models;
using MediatR;

namespace Combat.Application.Features.MonsterTypes.GetMonsterTypes;

public sealed record GetMonsterTypesQuery : IRequest<IReadOnlyList<MonsterTypeDefinition>>;
