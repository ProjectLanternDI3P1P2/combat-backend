using Combat.Application.Services;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.BackgroundServices;

/// <summary>
/// Retries the result reports Dungeon has not acknowledged yet (Dungeon down, deadline
/// exceeded...). A combat is never considered reported without Dungeon's answer.
/// </summary>
public sealed class PendingCombatReportsWorker(IServiceScopeFactory scopeFactory, ILogger logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                ICombatResultReporter reporter =
                    scope.ServiceProvider.GetRequiredService<ICombatResultReporter>();
                await reporter.ReportPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Error(exception, "Pending combat result reports could not be retried.");
            }
        }
    }
}
