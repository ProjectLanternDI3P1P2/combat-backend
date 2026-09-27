using Combat.Application.Models;
using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.MonsterTypes.GetMonsterTypeById;

public sealed class GetMonsterTypeByIdQueryHandler(IMonsterTypeCatalog catalog)
    : IRequestHandler<GetMonsterTypeByIdQuery, MonsterTypeDefinition>
{
    public async Task<MonsterTypeDefinition> Handle(
        GetMonsterTypeByIdQuery request,
        CancellationToken cancellationToken)
    {
        Guid monsterTypeId = Guid.Parse(request.MonsterTypeId);
        IReadOnlyList<MonsterTypeDefinition> resolved =
            await catalog.ResolveRequiredAsync([monsterTypeId], cancellationToken);

        return resolved[0];
    }
}
