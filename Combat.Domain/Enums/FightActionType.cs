namespace Combat.Domain.Enums;

public enum FightActionType
{
    Attack,
    Ability,
    Item,
    Defend,
    Flee,

    /// <summary>The hero does nothing this turn; also applied when the turn times out.</summary>
    Pass,
}
