using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;

namespace Combat.Infrastructure.MonsterMocks;

public sealed class UnavailableMonsterTypeSource : IMonsterTypeSource
{
    public Task<IReadOnlyCollection<MonsterTypeDefinition>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<IReadOnlyCollection<MonsterTypeDefinition>>(
            new MonsterTypeSourceUnavailableException());
    }
}
