namespace Combat.Application.Models;

/// <summary>Monster type configuration required to generate a monster, independent of its source.</summary>
public sealed class MonsterTypeSummary
{
    public string MonsterType { get; init; } = string.Empty;
    public bool IsBoss { get; init; }
    public int BaseHp { get; init; }
    public int BaseAttack { get; init; }
    public int BaseDefense { get; init; }
    public int BaseSpeed { get; init; }
}
