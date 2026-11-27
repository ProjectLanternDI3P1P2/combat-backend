namespace Combat.Application.Services;

/// <summary>Sends the result of finished combats to Dungeon, outside any transaction.</summary>
public interface ICombatResultReporter
{
    /// <returns>True when Dungeon acknowledged it (now or earlier), false when it must be retried.</returns>
    Task<bool> ReportAsync(Guid combatId, CancellationToken cancellationToken);

    /// <summary>Retries every finished combat whose report is still pending.</summary>
    Task ReportPendingAsync(CancellationToken cancellationToken);
}
