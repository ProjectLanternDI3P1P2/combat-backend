using Combat.Domain.ValueObjects;

namespace Combat.Application.Features.CombatUseCase.SubmitCombatAction;

/// <param name="Events">Events produced by this action only, for the client to animate.</param>
public sealed record CombatActionResult(CombatView Combat, IReadOnlyList<FightEvent> Events);
