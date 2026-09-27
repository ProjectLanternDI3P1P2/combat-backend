using Combat.Application.Models;

namespace Combat.Application.Ports;

public interface IMonsterTypeCatalog
{
    Task<IReadOnlyList<MonsterTypeDefinition>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MonsterTypeDefinition>> ResolveRequiredAsync(
        IReadOnlyCollection<Guid> monsterTypeIds,
        CancellationToken cancellationToken);
}
