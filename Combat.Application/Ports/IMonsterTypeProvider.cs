using Combat.Application.Models;

namespace Combat.Application.Ports;

/// <summary>Application port for retrieving monster type configuration owned outside Combat.</summary>
public interface IMonsterTypeProvider
{
    Task<MonsterTypeSummary?> GetMonsterTypeAsync(string monsterType, CancellationToken cancellationToken);
}
