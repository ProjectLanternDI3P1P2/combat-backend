using Combat.Application.Models;

namespace Combat.Application.Ports;

public interface IMonsterTypeSource
{
    Task<IReadOnlyCollection<MonsterTypeDefinition>> GetAllAsync(CancellationToken cancellationToken);
}
