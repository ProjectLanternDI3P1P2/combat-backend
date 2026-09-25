using Combat.Application.Models;
using Combat.Application.Ports;

namespace Combat.Infrastructure.HeroMocks;

public sealed class UnavailableHeroCombatStateSource : IHeroCombatStateSource
{
    private const string ErrorMessage = "The hero combat state preview is not enabled.";

    public Task<HeroCombatState> StartSessionAsync(
        string sessionId,
        string scenario,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<HeroCombatState>(new InvalidOperationException(ErrorMessage));
    }

    public Task<HeroCombatState> GetCurrentAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<HeroCombatState>(new InvalidOperationException(ErrorMessage));
    }

    public Task<HeroCombatState> AdvanceAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<HeroCombatState>(new InvalidOperationException(ErrorMessage));
    }

    public void EndSession(string sessionId)
    {
    }
}
