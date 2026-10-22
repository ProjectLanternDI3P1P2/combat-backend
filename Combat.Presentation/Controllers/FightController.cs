using Combat.Application.Features.FightUseCase;
using Combat.Application.Features.FightUseCase.GetFight;
using Combat.Application.Features.FightUseCase.StartFight;
using Combat.Application.Features.FightUseCase.SubmitFightAction;
using Combat.Presentation.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Controllers;

[ApiController]
[Route("api/v1/combats")]
public sealed class FightController(IMediator mediator, ILogger logger) : ControllerBase
{
    /// <summary>Opens a fight. Replaying the same idempotency key returns the same fight.</summary>
    [HttpPost]
    [ProducesResponseType<FightResult>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(
        [FromBody] StartFightDto dto,
        CancellationToken cancellationToken
    )
    {
        logger.Information(
            "Received request to start a {Encounter} fight for run {DungeonRunId}.",
            dto.Encounter,
            dto.DungeonRunId
        );

        FightResult fight = await mediator.Send(
            new StartFightCommand(
                dto.DungeonRunId,
                dto.IdempotencyKey,
                dto.Floor,
                dto.Encounter,
                dto.HeroIds ?? [],
                dto.MonsterCount
            ),
            cancellationToken
        );

        return CreatedAtAction(nameof(GetById), new { fightId = fight.FightId }, fight);
    }

    /// <summary>The authoritative state of the fight, with its whole event log.</summary>
    [HttpGet("{fightId:guid}")]
    [ProducesResponseType<FightResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid fightId, CancellationToken cancellationToken)
    {
        FightResult fight = await mediator.Send(new GetFightQuery(fightId), cancellationToken);
        return Ok(fight);
    }

    /// <summary>
    /// The active hero acts; the monsters' turns that follow are resolved too. The response
    /// carries the new state and only the events of this action. 409 when the rules refuse it.
    /// </summary>
    [HttpPost("{fightId:guid}/actions")]
    [ProducesResponseType<FightResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Act(
        Guid fightId,
        [FromBody] FightActionDto dto,
        CancellationToken cancellationToken
    )
    {
        logger.Information(
            "Received {ActionType} from {ActorId} in fight {FightId}, command {CommandId}.",
            dto.Type,
            dto.ActorId,
            fightId,
            dto.CommandId
        );

        FightResult fight = await mediator.Send(
            new SubmitFightActionCommand(
                fightId,
                dto.CommandId,
                dto.ActorId,
                dto.Type,
                dto.TargetId,
                dto.AbilityId,
                dto.ItemId
            ),
            cancellationToken
        );

        return Ok(fight);
    }
}
