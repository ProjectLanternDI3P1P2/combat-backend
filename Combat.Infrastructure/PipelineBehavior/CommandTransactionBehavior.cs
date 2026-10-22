using Combat.Application.Abstractions;
using Combat.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.PipelineBehavior;

public sealed class CommandTransactionBehavior<TRequest, TResponse>(CombatDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        if (request is not ICommandRequest)
        {
            return await next(cancellationToken);
        }

        if (!dbContext.Database.IsRelational())
        {
            TResponse response = await next(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return response;
        }

        // An internal command called during combat initialization shares its caller's
        // transaction. Only the outer command saves and commits the complete operation.
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await next(cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        TResponse transactionalResponse = await next(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return transactionalResponse;
    }
}
