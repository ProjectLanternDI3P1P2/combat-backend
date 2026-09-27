using Combat.Application.Models;

namespace Combat.Presentation.DTO;

public sealed record MonsterTypeDto(
    Guid Id,
    string Name,
    bool IsBoss,
    int BaseHealth,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed)
{
    public static MonsterTypeDto From(MonsterTypeDefinition monsterType)
    {
        return new MonsterTypeDto(
            monsterType.Id,
            monsterType.Name,
            monsterType.IsBoss,
            monsterType.BaseHealth,
            monsterType.BaseAttack,
            monsterType.BaseDefense,
            monsterType.BaseSpeed);
    }
}
