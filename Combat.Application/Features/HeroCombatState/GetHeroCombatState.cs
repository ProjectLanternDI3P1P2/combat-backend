using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.HeroCombatState;

public sealed record GetHeroCombatState(string SessionId) : IRequest<Models.HeroCombatState>;

public sealed class GetHeroCombatStateHandler(IHeroCombatStateSource source)
    : IRequestHandler<GetHeroCombatState, Models.HeroCombatState>
{
    public Task<Models.HeroCombatState> Handle(
        GetHeroCombatState request,
        CancellationToken cancellationToken)
    {
        return source.GetCurrentAsync(request.SessionId, cancellationToken);
    }
}
