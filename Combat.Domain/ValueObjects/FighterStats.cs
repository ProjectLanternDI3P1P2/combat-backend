namespace Combat.Domain.ValueObjects;

public sealed record FighterStats
{
    public FighterStats(int attack, int defense, int speed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attack);
        ArgumentOutOfRangeException.ThrowIfNegative(defense);
        ArgumentOutOfRangeException.ThrowIfNegative(speed);

        Attack = attack;
        Defense = defense;
        Speed = speed;
    }

    public int Attack { get; }
    public int Defense { get; }
    public int Speed { get; }
}
