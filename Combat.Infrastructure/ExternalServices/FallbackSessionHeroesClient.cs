using Combat.Application.Exceptions;
using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices;

/// <summary>
/// Calls the real Player client first and uses the mock only when Player is down.
/// A business answer such as "session not found" is never replaced by mocked data.
/// </summary>
public sealed class FallbackSessionHeroesClient(
    ISessionHeroesClient realClient,
    MockSessionHeroesClient mockClient,
    IOptions<PlayerServiceOptions> options,
    ILogger logger
) : ISessionHeroesClient
{
    public async Task<IReadOnlyList<Guid>> GetHeroIdsAsync(
        Guid sessionId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await realClient.GetHeroIdsAsync(sessionId, cancellationToken);
        }
        catch (ExternalServiceUnavailableException exception)
            when (options.Value.IsMockFallbackEnabled)
        {
            logger.Warning(
                exception,
                "Player service unavailable, falling back to mocked session roster for session {SessionId}.",
                sessionId
            );

            return await mockClient.GetHeroIdsAsync(sessionId, cancellationToken);
        }
    }
}
