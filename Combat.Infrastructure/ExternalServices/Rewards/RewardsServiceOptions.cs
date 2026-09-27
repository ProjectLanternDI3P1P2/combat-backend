namespace Combat.Infrastructure.ExternalServices.Rewards;

public sealed class RewardsServiceOptions
{
    public const string SectionName = "ExternalServices:Rewards";

    /// <summary>Serve mocked inventories only when the real Rewards service is unavailable.</summary>
    public bool IsMockFallbackEnabled { get; init; } = true;
}
