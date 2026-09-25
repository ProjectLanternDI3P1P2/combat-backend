using Combat.Application.Models;
using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.HeroCombatState;

public sealed record StartHeroCombatStateSession(string SessionId, string Scenario) : IRequest<Models.HeroCombatState>;

public sealed class StartHeroCombatStateSessionHandler(IHeroCombatStateSource source)
    : IRequestHandler<StartHeroCombatStateSession, Models.HeroCombatState>
{
    public Task<Models.HeroCombatState> Handle(
        StartHeroCombatStateSession request,
        CancellationToken cancellationToken)
    {
        return source.StartSessionAsync(request.SessionId, request.Scenario, cancellationToken);
    }
}
