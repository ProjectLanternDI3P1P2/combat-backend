namespace Combat.Domain.ValueObjects;

public sealed record FighterAbility
{
    public FighterAbility(Guid abilityId, string name, int manaCost, string targetType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(manaCost);

        AbilityId = abilityId;
        Name = name;
        ManaCost = manaCost;
        TargetType = targetType;
    }

    public Guid AbilityId { get; }
    public string Name { get; }
    public int ManaCost { get; }
    public string TargetType { get; }
}
