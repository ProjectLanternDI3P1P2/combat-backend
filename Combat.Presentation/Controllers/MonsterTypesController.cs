using Combat.Application.Features.MonsterTypes.GetMonsterTypeById;
using Combat.Application.Features.MonsterTypes.GetMonsterTypes;
using Combat.Application.Models;
using Combat.Presentation.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Controllers;

[ApiController]
[Route("api/v1/monster-types")]
public sealed class MonsterTypesController(IMediator mediator, ILogger logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MonsterTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        logger.Information("Received request to get monster types.");

        IReadOnlyList<MonsterTypeDefinition> monsterTypes =
            await mediator.Send(new GetMonsterTypesQuery(), cancellationToken);
        MonsterTypeDto[] response = [.. monsterTypes.Select(MonsterTypeDto.From)];

        logger.Information("Retrieved {MonsterTypeCount} monster types.", response.Length);
        return Ok(response);
    }

    [HttpGet("{monsterTypeId}")]
    public async Task<ActionResult<MonsterTypeDto>> GetById(
        string monsterTypeId,
        CancellationToken cancellationToken)
    {
        logger.Information("Received request to get monster type {MonsterTypeId}.", monsterTypeId);

        MonsterTypeDefinition monsterType =
            await mediator.Send(new GetMonsterTypeByIdQuery(monsterTypeId), cancellationToken);

        logger.Information("Retrieved monster type {MonsterTypeId}.", monsterType.Id);
        return Ok(MonsterTypeDto.From(monsterType));
    }
}
