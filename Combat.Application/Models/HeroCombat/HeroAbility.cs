namespace Combat.Application.Models.HeroCombat;

public sealed record HeroAbility
{
    public required Guid AbilityId { get; init; }
    public required string Name { get; init; }
    public required int ManaCost { get; init; }

    // Kept as text until the target vocabulary is agreed with the Player service.
    public required string TargetType { get; init; }
}
