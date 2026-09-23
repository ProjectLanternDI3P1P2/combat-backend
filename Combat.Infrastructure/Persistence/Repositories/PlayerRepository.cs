using Combat.Domain.Entities;
using Combat.Domain.Repositories;

namespace Combat.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository(CombatDbContext dbContext) : IPlayerRepository
{
    public async Task AddPlayerAsync(Player player, CancellationToken cancellationToken)
    {
        await dbContext.Players.AddAsync(player, cancellationToken);
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        return await dbContext.Players.FindAsync([playerId], cancellationToken);
    }
}
