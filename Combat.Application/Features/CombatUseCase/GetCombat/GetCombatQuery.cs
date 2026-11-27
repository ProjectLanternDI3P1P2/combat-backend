using MediatR;

namespace Combat.Application.Features.CombatUseCase.GetCombat;

/// <summary>Read-only state of a combat (no timer applied).</summary>
public sealed record GetCombatQuery(Guid CombatId) : IRequest<CombatView>;
