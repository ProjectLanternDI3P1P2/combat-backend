using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <param name="Code">Ability code owned by Player, e.g. "warrior-strike".</param>
/// <param name="TargetType">"SingleEnemy", "AllEnemies", "Self" or "SingleAlly".</param>
/// <param name="Power">
/// Percentage of the caster's attack dealt by an offensive ability, or of the target's
/// maximum HP restored by a healing one.
/// </param>
public sealed record FighterAbility
{
    public FighterAbility(string code, string name, int manaCost, string targetType, int power)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(manaCost);
        ArgumentOutOfRangeException.ThrowIfNegative(power);

        Code = code;
        Name = name;
        ManaCost = manaCost;
        TargetType = targetType;
        Power = power;
    }

    public string Code { get; }
    public string Name { get; }
    public int ManaCost { get; }
    public string TargetType { get; }
    public int Power { get; }

    public AbilityTarget Target =>
        Enum.TryParse(TargetType, ignoreCase: true, out AbilityTarget target)
            ? target
            : AbilityTarget.SingleEnemy;
}
