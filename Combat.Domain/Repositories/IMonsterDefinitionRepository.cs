using Combat.Domain.Entities;
using Combat.Domain.Enums;

namespace Combat.Domain.Repositories;

public interface IMonsterDefinitionRepository
{
    Task<IReadOnlyList<MonsterDefinition>> GetByClassAsync(
        MonsterClass monsterClass,
        CancellationToken cancellationToken
    );
}
