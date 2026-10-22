namespace Combat.Application.Models;

public sealed class MonsterGenerationOptions
{
    public const string SectionName = "MonsterGeneration";

    public int MaxFloor { get; init; } = 40;

    public int StatIncreasePerFloor { get; init; } = 2;

    public int AdditionalPlayerStatMultiplier { get; init; } = 1;
}
