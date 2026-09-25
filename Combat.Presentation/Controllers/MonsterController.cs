using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Presentation.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Controllers;

[ApiController]
[Route("api/v1/monsters")]
public sealed class MonsterController(IMediator mediator, ILogger logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] MonsterDto monsterDto, CancellationToken cancellationToken)
    {
        logger.Information(
            "Received request to generate monster of type {MonsterType} for combat {CombatId}.",
            monsterDto.MonsterType,
            monsterDto.CombatId);

        GenerateMonsterResult result = await mediator.Send(
            new GenerateMonsterCommand(monsterDto.CombatId, monsterDto.MonsterType),
            cancellationToken);

        logger.Information("Monster {MonsterId} generated successfully for combat {CombatId}.", result.MonsterId, result.CombatId);
        return Ok(result);
    }
}
