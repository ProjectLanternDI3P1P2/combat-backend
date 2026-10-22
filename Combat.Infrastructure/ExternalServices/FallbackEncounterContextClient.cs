using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Domain.Enums;
using Combat.Infrastructure.ExternalServices.Dungeon;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices;

/// <summary>
/// Calls the real Dungeon client first and uses the mock only when Dungeon is down.
/// </summary>
public sealed class FallbackEncounterContextClient(
    IEncounterContextClient realClient,
    MockEncounterContextClient mockClient,
    IOptions<EncounterServiceOptions> options,
    ILogger logger
) : IEncounterContextClient
{
    public async Task<EncounterContext> GetAsync(
        Guid sessionId,
        EncounterType encounterType,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await realClient.GetAsync(sessionId, encounterType, cancellationToken);
        }
        catch (ExternalServiceUnavailableException exception)
            when (options.Value.IsMockFallbackEnabled)
        {
            logger.Warning(
                exception,
                "Dungeon service unavailable, falling back to mocked encounter context for session {SessionId}.",
                sessionId
            );

            return await mockClient.GetAsync(sessionId, encounterType, cancellationToken);
        }
    }
}
