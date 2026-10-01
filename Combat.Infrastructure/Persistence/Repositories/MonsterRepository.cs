using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class MonsterRepository(CombatDbContext dbContext) : IMonsterRepository
{
    public async Task<Monster?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Monsters.SingleOrDefaultAsync(
            monster => monster.IdempotencyKey == idempotencyKey,
            cancellationToken
        );
    }

    public async Task<Monster> AddOrGetByIdempotencyKeyAsync(
        Monster monster,
        CancellationToken cancellationToken
    )
    {
        await dbContext.Monsters.AddAsync(monster, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return monster;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                    is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            dbContext.ChangeTracker.Clear();
            Monster? existingMonster = await GetByIdempotencyKeyAsync(
                monster.IdempotencyKey,
                cancellationToken
            );
            if (existingMonster is null)
            {
                throw;
            }

            return existingMonster;
        }
    }
}
