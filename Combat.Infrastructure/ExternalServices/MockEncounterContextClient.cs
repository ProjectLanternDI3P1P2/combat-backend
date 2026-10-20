using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Domain.Enums;
using Combat.Infrastructure.ExternalServices.Dungeon;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Infrastructure.ExternalServices;

/// <summary>
/// Local stand-in for the Dungeon service <c>GetEncounterContext</c> operation.
/// The Dungeon service owns the encounter: it knows how many monsters it requires
/// and at which level they appear.
/// </summary>
public sealed class MockEncounterContextClient(
    IOptions<EncounterServiceOptions> options,
    ILogger logger
) : IEncounterContextClient
{
    public Task<EncounterContext> GetAsync(
        Guid sessionId,
        EncounterType encounterType,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sessionId == Guid.Empty)
        {
            throw new SessionNotFoundException(sessionId);
        }

        EncounterServiceOptions encounterOptions = options.Value;
        EncounterContext context = encounterType switch
        {
            EncounterType.Enemy => new EncounterContext(
                encounterType,
                encounterOptions.EnemyMonsterQuantity,
                encounterOptions.EnemyLevel
            ),
            EncounterType.SubBoss => new EncounterContext(
                encounterType,
                encounterOptions.SubBossMonsterQuantity,
                encounterOptions.SubBossLevel
            ),
            EncounterType.Boss => new EncounterContext(
                encounterType,
                encounterOptions.BossMonsterQuantity,
                encounterOptions.BossLevel
            ),
            _ => throw new InvalidCombatEncounterException(
                $"'{encounterType}' is not a known encounter type."
            ),
        };

        logger.Debug(
            "Mocked encounter context served for session {SessionId}: {MonsterQuantity} monster(s) at level {Level}.",
            sessionId,
            context.MonsterQuantity,
            context.Level
        );

        return Task.FromResult(context);
    }
}
