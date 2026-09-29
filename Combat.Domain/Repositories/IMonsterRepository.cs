using Combat.Domain.Entities;

namespace Combat.Domain.Repositories;

public interface IMonsterRepository
{
    Task AddMonsterAsync(Monster monster, CancellationToken cancellationToken);
}
