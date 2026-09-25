using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.HeroCombatState;

public sealed record EndHeroCombatStateSession(string SessionId) : IRequest;

public sealed class EndHeroCombatStateSessionHandler(IHeroCombatStateSource source)
    : IRequestHandler<EndHeroCombatStateSession>
{
    public Task Handle(EndHeroCombatStateSession request, CancellationToken cancellationToken)
    {
        source.EndSession(request.SessionId);
        return Task.CompletedTask;
    }
}
