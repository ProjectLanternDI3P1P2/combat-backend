using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class MonsterRepository(CombatDbContext dbContext) : IMonsterRepository
{
    private const string SavepointName = "monster_idempotency";

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

        // The command transaction behavior owns the commit. A savepoint keeps a unique
        // violation from poisoning that transaction, so the existing monster stays reachable.
        IDbContextTransaction? transaction = dbContext.Database.CurrentTransaction;
        if (transaction is not null)
        {
            await transaction.CreateSavepointAsync(SavepointName, cancellationToken);
        }

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
            if (transaction is not null)
            {
                await transaction.RollbackToSavepointAsync(SavepointName, cancellationToken);
            }

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
