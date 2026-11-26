using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class MonsterRepository(CombatDbContext dbContext) : IMonsterRepository
{
    public async Task<Monster?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        if (dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is not null)
        {
            // A row lock cannot protect a key that has not been inserted yet. Serialize
            // generation for this key until the command transaction commits or rolls back.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({idempotencyKey.ToString()}, 0))",
                cancellationToken
            );
        }

        Monster? pendingMonster = dbContext.Monsters.Local.SingleOrDefault(monster =>
            monster.IdempotencyKey == idempotencyKey
        );
        if (pendingMonster is not null)
        {
            return pendingMonster;
        }

        return await dbContext.Monsters.SingleOrDefaultAsync(
            monster => monster.IdempotencyKey == idempotencyKey,
            cancellationToken
        );
    }

    public async Task AddAsync(Monster monster, CancellationToken cancellationToken)
    {
        await dbContext.Monsters.AddAsync(monster, cancellationToken);
    }
}
