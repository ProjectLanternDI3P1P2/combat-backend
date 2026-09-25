namespace Combat.Application.Models;

public sealed record MonsterTypeDefinition(
    Guid Id,
    string Name,
    bool IsBoss,
    int BaseHealth,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed);
