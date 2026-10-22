using System.Collections.Concurrent;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;

namespace Combat.Infrastructure.Persistence.InMemory;

/// <summary>
/// Fights in progress, kept in the memory of the instance. A fight is a temporary snapshot
/// (CONTEXTE_METIER_COMBAT): this store is enough for one instance, but fights are lost on
/// restart and are not shared between replicas. Persisting them (EF Core, Redis) only means
/// another implementation of <see cref="IFightRepository"/>.
/// Registered as a singleton; its name deliberately does not end with "Repository" so the
/// scoped repository scan leaves it alone.
/// </summary>
public sealed class InMemoryFightStore : IFightRepository
{
    private readonly ConcurrentDictionary<Guid, Fight> _fights = new();
    private readonly ConcurrentDictionary<Guid, Guid> _fightIdsByIdempotencyKey = new();

    public Task<Fight?> GetByIdAsync(Guid fightId, CancellationToken cancellationToken) =>
        Task.FromResult(_fights.GetValueOrDefault(fightId));

    public Task<Fight?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            _fightIdsByIdempotencyKey.TryGetValue(idempotencyKey, out Guid fightId)
                ? _fights.GetValueOrDefault(fightId)
                : null
        );

    public Task<Fight> AddOrGetByIdempotencyKeyAsync(
        Fight fight,
        CancellationToken cancellationToken
    )
    {
        Guid storedId = _fightIdsByIdempotencyKey.GetOrAdd(fight.IdempotencyKey, fight.Id);
        Fight stored = _fights.GetOrAdd(storedId, fight);
        return Task.FromResult(stored);
    }
}
