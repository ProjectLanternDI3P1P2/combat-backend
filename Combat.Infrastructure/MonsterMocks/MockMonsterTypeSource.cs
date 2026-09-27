using Combat.Application.Models;
using Combat.Application.Ports;

namespace Combat.Infrastructure.MonsterMocks;

public sealed class MockMonsterTypeSource : IMonsterTypeSource
{
    private static readonly IReadOnlyCollection<MonsterTypeDefinition> MonsterTypes =
        Array.AsReadOnly(new[]
        {
            new MonsterTypeDefinition(
                Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                "Cave Rat",
                false,
                18,
                5,
                1,
                12),
            new MonsterTypeDefinition(
                Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                "Goblin Raider",
                false,
                42,
                11,
                5,
                9),
            new MonsterTypeDefinition(
                Guid.Parse("c3333333-3333-3333-3333-333333333333"),
                "Stone Guardian",
                true,
                180,
                22,
                18,
                4)
        });

    public Task<IReadOnlyCollection<MonsterTypeDefinition>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MonsterTypes);
    }
}
