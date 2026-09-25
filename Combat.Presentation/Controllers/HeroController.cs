using Combat.Application.Features.HeroUseCase.GetHeroCombatData;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Controllers;

[ApiController]
[Route("api/v1/heroes")]
public sealed class HeroController(IMediator mediator, ILogger logger) : ControllerBase
{
    [HttpGet("{heroId:guid}/combat-data")]
    public async Task<IActionResult> GetCombatData(Guid heroId, CancellationToken cancellationToken)
    {
        logger.Information("Received request to get combat data for hero {HeroId}.", heroId);

        var heroCombatData = await mediator.Send(new GetHeroCombatDataQuery(heroId), cancellationToken);

        return Ok(heroCombatData);
    }
}
