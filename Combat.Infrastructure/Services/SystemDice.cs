using Combat.Domain.Services;

namespace Combat.Infrastructure.Services;

public sealed class SystemDice : IDice
{
    public int Between(int minInclusive, int maxInclusive) =>
        Random.Shared.Next(minInclusive, maxInclusive + 1);
}
