using Combat.Application.Abstractions;

namespace Combat.Application.Features.CombatUseCase.JoinCombat;

/// <summary>
/// Returns the current state of a combat to a client that joins or reconnects. A command,
/// not a query: the turns whose deadline is over are passed first (lazy timer).
/// </summary>
public sealed record JoinCombatCommand(Guid CombatId) : ICommand<CombatView>;
