using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class MonsterDefinitionRepository(CombatDbContext dbContext)
    : IMonsterDefinitionRepository
{
    public async Task<IReadOnlyList<MonsterDefinition>> GetByClassAsync(
        MonsterClass monsterClass,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .MonsterDefinitions.Where(definition => definition.Class == monsterClass)
            .OrderBy(definition => definition.Name)
            .ToListAsync(cancellationToken);
    }
}
