using Combat.Application.Models;
using Combat.Application.Ports;

namespace Combat.Infrastructure.Monsters;

// Stands in for the real monster catalog source until one exists; the acceptance
// criteria require mocked data so generation can be validated without it.
public sealed class MockedMonsterTypeProvider : IMonsterTypeProvider
{
    private static readonly IReadOnlyDictionary<string, MonsterTypeSummary> MonsterTypes =
        new Dictionary<string, MonsterTypeSummary>(StringComparer.OrdinalIgnoreCase)
        {
            ["goblin"] = new() { MonsterType = "goblin", IsBoss = false, BaseHp = 30, BaseAttack = 8, BaseDefense = 4, BaseSpeed = 10 },
            ["orc"] = new() { MonsterType = "orc", IsBoss = false, BaseHp = 60, BaseAttack = 14, BaseDefense = 8, BaseSpeed = 6 },
            ["dragon"] = new() { MonsterType = "dragon", IsBoss = true, BaseHp = 300, BaseAttack = 40, BaseDefense = 25, BaseSpeed = 15 }
        };

    public Task<MonsterTypeSummary?> GetMonsterTypeAsync(string monsterType, CancellationToken cancellationToken)
    {
        MonsterTypes.TryGetValue(monsterType, out MonsterTypeSummary? summary);
        return Task.FromResult(summary);
    }
}
