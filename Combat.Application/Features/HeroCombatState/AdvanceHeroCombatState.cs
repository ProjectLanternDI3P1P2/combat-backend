using Combat.Application.Ports;
using MediatR;

namespace Combat.Application.Features.HeroCombatState;

public sealed record AdvanceHeroCombatState(string SessionId) : IRequest<Models.HeroCombatState>;

public sealed class AdvanceHeroCombatStateHandler(IHeroCombatStateSource source)
    : IRequestHandler<AdvanceHeroCombatState, Models.HeroCombatState>
{
    public Task<Models.HeroCombatState> Handle(
        AdvanceHeroCombatState request,
        CancellationToken cancellationToken)
    {
        return source.AdvanceAsync(request.SessionId, cancellationToken);
    }
}
