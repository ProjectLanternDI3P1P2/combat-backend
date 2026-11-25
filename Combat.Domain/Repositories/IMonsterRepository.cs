using Combat.Domain.Entities;

namespace Combat.Domain.Repositories;

public interface IMonsterRepository
{
    Task<Monster?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken
    );

    Task AddAsync(Monster monster, CancellationToken cancellationToken);
}
