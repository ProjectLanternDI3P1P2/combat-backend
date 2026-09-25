namespace Combat.Application.Models.HeroCombat;

public sealed record HeroCombatStats
{
    public required int Attack { get; init; }
    public required int Defense { get; init; }
    public required int Speed { get; init; }
}
