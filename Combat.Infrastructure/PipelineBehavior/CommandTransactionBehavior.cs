using Combat.Application.Abstractions;
using Combat.Infrastructure.Persistence;
using MediatR;

namespace Combat.Infrastructure.PipelineBehavior;

public sealed class CommandTransactionBehavior<TRequest, TResponse>(CombatDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        TResponse response = await next(cancellationToken);

        if (request is ICommand)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
