using Combat.Application.Exceptions;
using Combat.Application.Ports;
using Combat.Domain.Enums;
using Combat.Infrastructure.Grpc.Configuration;
using Dungeon.Contracts.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ContractHeroFinalState = Dungeon.Contracts.V1.HeroFinalState;

namespace Combat.Infrastructure.Grpc.Clients;

/// <summary>Reports the result of a finished combat to the Dungeon service (internal gRPC).</summary>
public sealed class DungeonCombatResultGrpcClient(
    DungeonCombatService.DungeonCombatServiceClient client,
    IOptions<DungeonGrpcClientOptions> options
) : IDungeonCombatResultClient
{
    private const string ServiceName = "Dungeon";

    public async Task<string> ReportAsync(
        CombatResultReport report,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(report);

        var request = new ReportCombatResultRequest
        {
            CommandId = report.CommandId.ToString(),
            CombatId = report.CombatId.ToString(),
            RunId = report.RunId.ToString(),
            EncounterId = report.EncounterId.ToString(),
            Outcome = report.Result switch
            {
                CombatResult.Victory => CombatOutcome.Won,
                CombatResult.Defeat => CombatOutcome.Lost,
                CombatResult.Fled => CombatOutcome.Fled,
                _ => CombatOutcome.Unspecified,
            },
        };
        foreach (Application.Ports.HeroFinalState hero in report.Heroes)
        {
            request.Heroes.Add(
                new ContractHeroFinalState
                {
                    HeroId = hero.HeroId.ToString(),
                    CurrentHp = hero.CurrentHp,
                    CurrentMana = hero.CurrentMana,
                    KnockedOut = hero.KnockedOut,
                }
            );
        }

        request.DefeatedElementIds.AddRange(report.DefeatedElementIds.Select(id => id.ToString()));

        try
        {
            ReportCombatResultResponse response = await client.ReportCombatResultAsync(
                request,
                new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds),
                    cancellationToken: cancellationToken
                )
            );
            return response.RunStatus;
        }
        catch (RpcException exception)
            when (exception.StatusCode
                    is StatusCode.Unavailable
                        or StatusCode.DeadlineExceeded
                        or StatusCode.Internal
                        or StatusCode.Unknown
            )
        {
            throw new ExternalServiceUnavailableException(ServiceName, exception);
        }
    }
}
