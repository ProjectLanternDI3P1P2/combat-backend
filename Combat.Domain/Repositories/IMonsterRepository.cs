using Combat.Domain.Entities;

namespace Combat.Domain.Repositories;

public interface IMonsterRepository
{
    Task<Monster?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken
    );

    Task<Monster> AddOrGetByIdempotencyKeyAsync(
        Monster monster,
        CancellationToken cancellationToken
    );
}
