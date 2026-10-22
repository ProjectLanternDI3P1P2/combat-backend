using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;

namespace Combat.Domain.Entities;

/// <summary>
/// Temporary state of one participant inside a fight: a copy of the hero (Player, Rewards)
/// or of the generated monster, which evolves during the fight and is never written back.
/// </summary>
public sealed class Combatant
{
    private readonly List<CombatItem> _items;

    public Combatant(
        Guid id,
        FightSide side,
        Guid externalId,
        string name,
        int level,
        string spriteKey,
        int hp,
        int maxHp,
        int mana,
        int maxMana,
        int attack,
        int defense,
        int speed,
        IReadOnlyList<CombatAbility>? abilities = null,
        IReadOnlyList<CombatItem>? items = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMana);

        Id = id;
        Side = side;
        ExternalId = externalId;
        Name = name;
        Level = level;
        SpriteKey = spriteKey;
        MaxHp = maxHp;
        Hp = Math.Clamp(hp, 0, maxHp);
        MaxMana = maxMana;
        Mana = Math.Clamp(mana, 0, maxMana);
        Attack = attack;
        Defense = defense;
        Speed = speed;
        Abilities = abilities ?? [];
        _items = [.. items ?? []];
    }

    public Guid Id { get; }
    public FightSide Side { get; }

    /// <summary>Hero id in Player, or monster id in Combat.</summary>
    public Guid ExternalId { get; }

    public string Name { get; }
    public int Level { get; }

    /// <summary>Which sprite the client draws, e.g. "cave-goblin".</summary>
    public string SpriteKey { get; }

    public int Hp { get; private set; }
    public int MaxHp { get; }
    public int Mana { get; private set; }
    public int MaxMana { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int Speed { get; }

    /// <summary>Halves the damage taken until the combatant's next turn.</summary>
    public bool IsDefending { get; private set; }

    public IReadOnlyList<CombatAbility> Abilities { get; }
    public IReadOnlyList<CombatItem> Items => _items;
    public bool IsKnockedOut => Hp == 0;

    internal int TakeDamage(int amount)
    {
        int lost = Math.Min(Math.Max(amount, 0), Hp);
        Hp -= lost;
        if (IsKnockedOut)
        {
            IsDefending = false;
        }

        return lost;
    }

    internal int Heal(int amount)
    {
        int recovered = Math.Min(Math.Max(amount, 0), MaxHp - Hp);
        Hp += recovered;
        return recovered;
    }

    internal int RestoreMana(int amount)
    {
        int recovered = Math.Min(Math.Max(amount, 0), MaxMana - Mana);
        Mana += recovered;
        return recovered;
    }

    internal void SpendMana(int amount) => Mana -= amount;

    internal void StartDefending() => IsDefending = true;

    internal void StopDefending() => IsDefending = false;

    internal CombatItem? FindItem(Guid itemId) => _items.Find(item => item.ItemId == itemId);

    internal void ConsumeItem(Guid itemId)
    {
        int index = _items.FindIndex(item => item.ItemId == itemId);
        _items[index] = _items[index] with { Quantity = _items[index].Quantity - 1 };
    }
}
