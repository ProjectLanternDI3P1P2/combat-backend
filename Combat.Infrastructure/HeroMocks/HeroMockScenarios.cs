namespace Combat.Infrastructure.HeroMocks;

public static class HeroMockScenarios
{
    public const string Complete = "complete";
    public const string Zero = "zero";
    public const string Empty = "empty";
    public const string Partial = "partial";
    public const string Changing = "changing";

    public static IReadOnlyCollection<string> All { get; } =
        [Complete, Zero, Empty, Partial, Changing];
}
