namespace Combat.Domain.ValueObjects;

/// <summary>Combat statistics of a monster once the floor and the party size are applied.</summary>
public sealed record MonsterStats
{
    public MonsterStats(int health, int attack, int defense, int speed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(health);
        ArgumentOutOfRangeException.ThrowIfNegative(attack);
        ArgumentOutOfRangeException.ThrowIfNegative(defense);
        ArgumentOutOfRangeException.ThrowIfNegative(speed);

        Health = health;
        Attack = attack;
        Defense = defense;
        Speed = speed;
    }

    public int Health { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int Speed { get; }
}
