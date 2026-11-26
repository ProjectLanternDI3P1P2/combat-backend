namespace Combat.Application.Exceptions;

/// <summary>The retrieved hero data cannot be used to initialize a combat safely.</summary>
public sealed class InvalidHeroCombatDataException(Guid heroId, IReadOnlyCollection<string> errors)
    : Exception($"Hero '{heroId}' combat data is invalid: {string.Join(" ", errors)}")
{
    public Guid HeroId { get; } = heroId;
    public IReadOnlyCollection<string> Errors { get; } = errors;
}
