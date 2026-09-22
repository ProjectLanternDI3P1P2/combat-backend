using Combat.Application.Models;

namespace Combat.Application.Ports;

/// <summary>Application port for retrieving data owned by the Player service.</summary>
public interface IPlayerClient
{
    Task<PlayerSummary> GetPlayerAsync(Guid playerId, CancellationToken cancellationToken);
}
