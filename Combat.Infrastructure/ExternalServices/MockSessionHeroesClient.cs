using Combat.Application.Exceptions;
using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices;

/// <summary>
/// Local stand-in for the Player service <c>GetSessionHeroes</c> operation.
/// The Player service remains the source of truth for the session membership.
/// </summary>
public sealed class MockSessionHeroesClient(ILogger logger) : ISessionHeroesClient
{
    public Task<IReadOnlyList<Guid>> GetHeroIdsAsync(
        Guid sessionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sessionId == Guid.Empty)
        {
            throw new SessionNotFoundException(sessionId);
        }

        logger.Debug(
            "Mocked session roster served for session {SessionId}: {HeroCount} hero(s).",
            sessionId,
            MockHeroCatalog.HeroIds.Count
        );

        return Task.FromResult<IReadOnlyList<Guid>>(MockHeroCatalog.HeroIds);
    }
}
