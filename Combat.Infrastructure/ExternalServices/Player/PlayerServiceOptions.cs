namespace Combat.Infrastructure.ExternalServices.Player;

public sealed class PlayerServiceOptions
{
    public const string SectionName = "ExternalServices:Player";

    /// <summary>Serve mocked heroes only when the real Player service is unavailable.</summary>
    public bool IsMockFallbackEnabled { get; init; } = true;
}
