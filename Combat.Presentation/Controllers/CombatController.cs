using Combat.Application.Features.CombatUseCase;
using Combat.Application.Features.CombatUseCase.GetCombat;
using Combat.Presentation.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Combat.Presentation.Controllers;

/// <summary>
/// Read-only REST view of a combat. Gameplay (join, actions) goes through the Combat Hub
/// (ADR-GLOB-010).
/// </summary>
[ApiController]
[Route("api/v1/combats")]
public sealed class CombatController(IMediator mediator) : ControllerBase
{
    [HttpGet("{combatId:guid}")]
    [ProducesResponseType<CombatSnapshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid combatId, CancellationToken cancellationToken)
    {
        CombatView view = await mediator.Send(new GetCombatQuery(combatId), cancellationToken);
        return Ok(CombatSnapshotDto.From(view));
    }
}
