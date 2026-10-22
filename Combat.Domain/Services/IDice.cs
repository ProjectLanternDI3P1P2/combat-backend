namespace Combat.Domain.Services;

/// <summary>Every random draw of a fight goes through this seam, so tests can fix the dice.</summary>
public interface IDice
{
    /// <returns>An integer between <paramref name="minInclusive"/> and <paramref name="maxInclusive"/>.</returns>
    int Between(int minInclusive, int maxInclusive);
}

public static class DiceExtensions
{
    public static bool Chance(this IDice dice, int percent) => dice.Between(1, 100) <= percent;

    public static T Pick<T>(this IDice dice, IReadOnlyList<T> values) =>
        values[dice.Between(0, values.Count - 1)];
}
