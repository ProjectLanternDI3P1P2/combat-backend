using Combat.Domain.Entities;

namespace Combat.Domain.Repositories;

public interface IFightRepository
{
    Task<Fight?> GetByIdAsync(Guid fightId, CancellationToken cancellationToken);

    Task<Fight?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken);

    /// <returns>The fight stored, or the one already stored under the same idempotency key.</returns>
    Task<Fight> AddOrGetByIdempotencyKeyAsync(Fight fight, CancellationToken cancellationToken);
}
