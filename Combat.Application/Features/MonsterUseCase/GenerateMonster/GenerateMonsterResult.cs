using Combat.Domain.Entities;
using Combat.Domain.Enums;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public sealed record GenerateMonsterResult(
    Guid MonsterId,
    string Name,
    MonsterClass MonsterClass,
    int Level,
    int BaseHealth,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string ImageUrl
)
{
    public static GenerateMonsterResult From(Monster monster)
    {
        return new GenerateMonsterResult(
            monster.Id,
            monster.Name,
            monster.Class,
            monster.Level,
            monster.BaseHealth,
            monster.BaseAttack,
            monster.BaseDefense,
            monster.BaseSpeed,
            monster.ImageUrl
        );
    }
}
