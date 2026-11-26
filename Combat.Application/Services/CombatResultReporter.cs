using Combat.Application.Exceptions;
using Combat.Application.Features.CombatUseCase.ReportCombatResult;
using Combat.Application.Ports;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Services;

/// <summary>
/// Reports a finished combat to Dungeon, then records the acknowledgement in its own
/// command. The network call never runs inside a transaction; a failed call leaves the
/// report pending and the retry uses the same command id (the combat id), so Dungeon
/// applies the result once.
/// </summary>
public sealed class CombatResultReporter(
    ICombatInstanceRepository repository,
    IDungeonCombatResultClient dungeonClient,
    ISender sender,
    ILogger logger
) : ICombatResultReporter
{
    private const int PendingBatchSize = 20;

    public async Task<bool> ReportAsync(Guid combatId, CancellationToken cancellationToken)
    {
        CombatInstance combat =
            await repository.GetByIdAsync(combatId, cancellationToken)
            ?? throw new CombatNotFoundException(combatId);
        if (!combat.IsOver)
        {
            return false;
        }

        if (!combat.IsResultReportPending)
        {
            return true;
        }

        if (combat.RunId == Guid.Empty)
        {
            logger.Warning(
                "Combat {CombatId} has no dungeon run: nothing to report.",
                combat.CombatId
            );
            await sender.Send(new MarkCombatResultReportedCommand(combatId), cancellationToken);
            return true;
        }

        CombatResultReport report = BuildReport(combat);
        try
        {
            string runStatus = await dungeonClient.ReportAsync(report, cancellationToken);
            await sender.Send(new MarkCombatResultReportedCommand(combatId), cancellationToken);
            logger.Information(
                "Combat {CombatId} result {Result} reported to Dungeon for run {RunId}, encounter {EncounterId}: run status {RunStatus}, {DefeatedCount} defeated mob(s).",
                combat.CombatId,
                combat.Result,
                combat.RunId,
                combat.EncounterId,
                runStatus,
                report.DefeatedElementIds.Count
            );
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(
                exception,
                "Combat {CombatId} result could not be reported to Dungeon for run {RunId}; it stays pending and will be retried.",
                combat.CombatId,
                combat.RunId
            );
            return false;
        }
    }

    public async Task ReportPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> pending = await repository.ListPendingResultReportsAsync(
            PendingBatchSize,
            cancellationToken
        );
        foreach (Guid combatId in pending)
        {
            await ReportAsync(combatId, cancellationToken);
        }
    }

    private static CombatResultReport BuildReport(CombatInstance combat) =>
        new(
            combat.CombatId,
            combat.CombatId,
            combat.RunId,
            combat.EncounterId,
            combat.Result!.Value,
            [
                .. combat
                    .Fighters.Where(fighter => fighter.Type == FighterType.Hero)
                    .Select(fighter => new HeroFinalState(
                        fighter.ExternalId,
                        fighter.CurrentHp,
                        fighter.CurrentMana,
                        fighter.IsKnockedOut
                    )),
            ],
            [
                .. combat
                    .Fighters.Where(fighter =>
                        fighter.Type == FighterType.Monster && fighter.IsKnockedOut
                    )
                    .Select(fighter => fighter.ExternalId),
            ]
        );
}
