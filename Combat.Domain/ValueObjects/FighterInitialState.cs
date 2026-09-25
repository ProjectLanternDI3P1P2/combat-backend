namespace Combat.Domain.ValueObjects;

/// <summary>
/// Values received when the fighter entered the combat. Never modified afterwards,
/// so the combat can always compare the current state with its starting point.
/// </summary>
public sealed record FighterInitialState
{
    public FighterInitialState(
        int currentHp,
        int maxHp,
        int currentMana,
        int maxMana,
        FighterStats stats,
        IEnumerable<FighterAbility> abilities,
        IEnumerable<FighterItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(currentHp);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(currentHp, maxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMana);
        ArgumentOutOfRangeException.ThrowIfNegative(currentMana);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(currentMana, maxMana);
        ArgumentNullException.ThrowIfNull(stats);

        CurrentHp = currentHp;
        MaxHp = maxHp;
        CurrentMana = currentMana;
        MaxMana = maxMana;
        Stats = stats;
        // Copied and wrapped so that neither the caller nor a consumer can alter the initial values.
        Abilities = abilities.ToList().AsReadOnly();
        Items = items.ToList().AsReadOnly();
    }

    public int CurrentHp { get; }
    public int MaxHp { get; }
    public int CurrentMana { get; }
    public int MaxMana { get; }
    public FighterStats Stats { get; }
    public IReadOnlyList<FighterAbility> Abilities { get; }
    public IReadOnlyList<FighterItem> Items { get; }
}
