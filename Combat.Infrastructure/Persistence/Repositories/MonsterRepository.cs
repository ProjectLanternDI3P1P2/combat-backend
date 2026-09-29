using Combat.Domain.Entities;
using Combat.Domain.Repositories;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class MonsterRepository(CombatDbContext dbContext) : IMonsterRepository
{
    public async Task AddMonsterAsync(Monster monster, CancellationToken cancellationToken)
    {
        await dbContext.Monsters.AddAsync(monster, cancellationToken);
    }
}
