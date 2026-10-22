using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <param name="Power">
/// Percentage of the caster's attack dealt by an offensive ability, or of the target's
/// maximum HP restored by a healing one.
/// </param>
public sealed record CombatAbility(
    Guid AbilityId,
    string Name,
    int ManaCost,
    AbilityTarget Target,
    int Power
);
