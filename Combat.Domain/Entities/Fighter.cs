using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using Combat.Domain.ValueObjects;

namespace Combat.Domain.Entities;

/// <summary>
/// Temporary combat state of a participant. It starts as a copy of the data received
/// from the owning service and evolves during the fight without ever writing back to it.
/// </summary>
public sealed class Fighter
{
    private readonly List<FighterItem> _items;
    private readonly IReadOnlyList<FighterItem> _readOnlyItems;

    private Fighter(
        Guid fighterId,
        FighterType type,
        Guid externalId,
        string name,
        int level,
        FighterInitialState initialState,
        bool isFromMockedData)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        ArgumentNullException.ThrowIfNull(initialState);

        FighterId = fighterId;
        Type = type;
        ExternalId = externalId;
        Name = name;
        Level = level;
        InitialState = initialState;
        IsFromMockedData = isFromMockedData;

        CurrentHp = initialState.CurrentHp;
        CurrentMana = initialState.CurrentMana;
        Stats = initialState.Stats;
        Abilities = initialState.Abilities;
        _items = [.. initialState.Items];
        _readOnlyItems = _items.AsReadOnly();
        State = CurrentHp == 0 ? FighterState.KnockedOut : FighterState.Active;
    }

    public Guid FighterId { get; }
    public FighterType Type { get; }

    /// <summary>Identifier of the participant in its owning service, e.g. the Player hero id.</summary>
    public Guid ExternalId { get; }

    public string Name { get; }
    public int Level { get; }
    public FighterInitialState InitialState { get; }
    public bool IsFromMockedData { get; }

    public int CurrentHp { get; private set; }
    public int MaxHp => InitialState.MaxHp;
    public int CurrentMana { get; private set; }
    public int MaxMana => InitialState.MaxMana;
    public FighterStats Stats { get; private set; }
    public IReadOnlyList<FighterAbility> Abilities { get; }
    public IReadOnlyList<FighterItem> Items => _readOnlyItems;
    public FighterState State { get; private set; }
    public bool IsKnockedOut => State == FighterState.KnockedOut;

    public static Fighter CreateHero(
        Guid heroId,
        string name,
        int level,
        FighterInitialState initialState,
        bool isFromMockedData)
    {
        return new Fighter(Guid.NewGuid(), FighterType.Hero, heroId, name, level, initialState, isFromMockedData);
    }

    /// <returns>The HP actually lost.</returns>
    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        EnsureActive();

        int lostHp = Math.Min(amount, CurrentHp);
        CurrentHp -= lostHp;

        if (CurrentHp == 0)
        {
            State = FighterState.KnockedOut;
        }

        return lostHp;
    }

    /// <returns>The HP actually recovered, never above MaxHp.</returns>
    public int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        EnsureActive();

        int recoveredHp = Math.Min(amount, MaxHp - CurrentHp);
        CurrentHp += recoveredHp;

        return recoveredHp;
    }

    public void SpendMana(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        EnsureActive();

        if (amount > CurrentMana)
        {
            throw new InvalidFighterOperationException(FighterId, "not enough mana.");
        }

        CurrentMana -= amount;
    }

    /// <returns>The mana actually recovered, never above MaxMana.</returns>
    public int RestoreMana(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        EnsureActive();

        int recoveredMana = Math.Min(amount, MaxMana - CurrentMana);
        CurrentMana += recoveredMana;

        return recoveredMana;
    }

    public void ChangeStats(FighterStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        EnsureActive();

        Stats = stats;
    }

    public void ConsumeItem(Guid itemId)
    {
        EnsureActive();

        int index = _items.FindIndex(item => item.ItemId == itemId);
        if (index < 0)
        {
            throw new InvalidFighterOperationException(FighterId, $"item '{itemId}' is not in the combat inventory.");
        }

        FighterItem item = _items[index];
        if (item.Quantity == 0)
        {
            throw new InvalidFighterOperationException(FighterId, $"item '{itemId}' is depleted.");
        }

        _items[index] = new FighterItem(item.ItemId, item.Name, item.Category, item.Quantity - 1);
    }

    private void EnsureActive()
    {
        if (IsKnockedOut)
        {
            throw new InvalidFighterOperationException(FighterId, "the fighter is knocked out.");
        }
    }
}
