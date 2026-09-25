using Combat.Application.Models;

namespace Combat.Application.Ports;

public interface IHeroCombatStateSource
{
    Task<HeroCombatState> StartSessionAsync(
        string sessionId,
        string scenario,
        CancellationToken cancellationToken);

    Task<HeroCombatState> GetCurrentAsync(
        string sessionId,
        CancellationToken cancellationToken);

    Task<HeroCombatState> AdvanceAsync(
        string sessionId,
        CancellationToken cancellationToken);

    void EndSession(string sessionId);
}
