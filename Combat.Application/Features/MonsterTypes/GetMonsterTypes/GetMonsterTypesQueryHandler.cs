using Combat.Application.Models;
using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.MonsterTypes.GetMonsterTypes;

public sealed class GetMonsterTypesQueryHandler(IMonsterTypeCatalog catalog)
    : IRequestHandler<GetMonsterTypesQuery, IReadOnlyList<MonsterTypeDefinition>>
{
    public Task<IReadOnlyList<MonsterTypeDefinition>> Handle(
        GetMonsterTypesQuery request,
        CancellationToken cancellationToken)
    {
        return catalog.GetAllAsync(cancellationToken);
    }
}
