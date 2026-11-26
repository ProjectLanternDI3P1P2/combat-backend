using Combat.Application.Exceptions;
using Combat.Application.Features.CombatUseCase;
using Combat.Application.Features.CombatUseCase.JoinCombat;
using Combat.Application.Features.CombatUseCase.SubmitCombatAction;
using Combat.Application.Services;
using Combat.Domain.Enums;
using Combat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Hubs;

/// <summary>
/// Gameplay of a combat over SignalR (ADR-GLOB-010). Every call answers with an explicit
/// <see cref="CombatCommandResult"/>; every accepted change sends the full snapshot to the
/// group <c>combat:{CombatId}</c>, which the service creates (a client never picks a group).
/// </summary>
/// <remarks>
/// Authorization MVP (solo): the actor must be the active hero of the combat. Restricting
/// the connection to the heroes the X-User-Id owns comes with multiplayer.
/// </remarks>
public sealed class CombatHub(IMediator mediator, ICombatResultReporter reporter, ILogger logger)
    : Hub<ICombatClient>
{
    public const string Route = "/hubs/combats";

    /// <summary>Set by the API Gateway from the Keycloak token (ADR-GLOB-004).</summary>
    private const string UserIdHeader = "X-User-Id";

    public static string GroupName(Guid combatId) => $"combat:{combatId}";

    public override Task OnConnectedAsync()
    {
        logger.Information(
            "Connection {ConnectionId} of user {UserId} opened on the Combat Hub.",
            Context.ConnectionId,
            Context.GetHttpContext()?.Request.Headers[UserIdHeader].ToString()
        );
        return base.OnConnectedAsync();
    }

    /// <summary>Joins the combat group and returns the current state (also after a reload).</summary>
    public async Task<CombatCommandResult> JoinCombat(JoinCombatRequest request)
    {
        logger.Information(
            "Connection {ConnectionId} joins combat {CombatId}.",
            Context.ConnectionId,
            request.CombatId
        );

        return await RefuseOnErrorAsync(
            null,
            async () =>
            {
                CombatView view = await mediator.Send(
                    new JoinCombatCommand(request.CombatId),
                    Context.ConnectionAborted
                );
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    GroupName(request.CombatId),
                    Context.ConnectionAborted
                );
                await ReportWhenFinishedAsync(view);
                return CombatCommandResult.Accept(null, CombatSnapshotDto.From(view));
            }
        );
    }

    public Task<CombatCommandResult> SubmitAction(SubmitActionRequest request)
    {
        if (!TryParseAction(request.Type, out FightActionType type))
        {
            return Task.FromResult(
                CombatCommandResult.Refuse(
                    request.CommandId,
                    CombatCommandErrorCodes.Validation,
                    $"Unknown action '{request.Type}'."
                )
            );
        }

        return ExecuteAsync(
            new SubmitCombatActionCommand(
                request.CombatId,
                request.CommandId,
                request.ActorId,
                type,
                request.TargetId,
                request.AbilityCode
            )
        );
    }

    public Task<CombatCommandResult> PassTurn(PassTurnRequest request) =>
        ExecuteAsync(
            new SubmitCombatActionCommand(
                request.CombatId,
                request.CommandId,
                request.ActorId,
                FightActionType.Pass,
                null,
                null
            )
        );

    private async Task<CombatCommandResult> ExecuteAsync(SubmitCombatActionCommand command)
    {
        if (command.CommandId == Guid.Empty)
        {
            return CombatCommandResult.Refuse(
                null,
                CombatCommandErrorCodes.Validation,
                "CommandId is required."
            );
        }

        return await RefuseOnErrorAsync(
            command.CommandId,
            async () =>
            {
                CombatActionResult result = await mediator.Send(command, Context.ConnectionAborted);
                CombatSnapshotDto snapshot = CombatSnapshotDto.From(result.Combat);
                await Clients.Group(GroupName(command.CombatId)).CombatStateChanged(snapshot);
                await ReportWhenFinishedAsync(result.Combat);
                return CombatCommandResult.Accept(
                    command.CommandId,
                    snapshot,
                    [.. result.Events.Select(CombatEventDto.From)]
                );
            }
        );
    }

    /// <summary>
    /// The command is committed: report a finished combat to Dungeon now. A failure leaves
    /// the report pending; the background worker retries it with the same command id.
    /// </summary>
    private async Task ReportWhenFinishedAsync(CombatView view)
    {
        if (view.Status != CombatStatus.Completed)
        {
            return;
        }

        try
        {
            await reporter.ReportAsync(view.CombatId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Combat {CombatId} result report failed.", view.CombatId);
        }
    }

    private static bool TryParseAction(string? value, out FightActionType type)
    {
        type = default;
        return !string.IsNullOrWhiteSpace(value)
            && !value.Equals(nameof(FightActionType.Item), StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse(value, ignoreCase: true, out type)
            && Enum.IsDefined(type);
    }

    private async Task<CombatCommandResult> RefuseOnErrorAsync(
        Guid? commandId,
        Func<Task<CombatCommandResult>> action
    )
    {
        try
        {
            return await action();
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "Combat hub command {CommandId} is invalid.", commandId);
            return CombatCommandResult.Refuse(
                commandId,
                CombatCommandErrorCodes.Validation,
                string.Join(" ", exception.Errors.Select(error => error.ErrorMessage))
            );
        }
        catch (CombatNotFoundException exception)
        {
            logger.Warning(
                exception,
                "Combat hub command {CommandId}: combat not found.",
                commandId
            );
            return CombatCommandResult.Refuse(
                commandId,
                CombatCommandErrorCodes.NotFound,
                exception.Message
            );
        }
        catch (FightRuleViolationException exception)
        {
            return CombatCommandResult.Refuse(
                commandId,
                CombatCommandErrorCodes.Refused,
                exception.Reason
            );
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.Warning(
                exception,
                "Concurrent update on combat hub command {CommandId}.",
                commandId
            );
            return CombatCommandResult.Refuse(
                commandId,
                CombatCommandErrorCodes.Conflict,
                "The combat changed meanwhile; join it again and retry."
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Combat hub command {CommandId} failed.", commandId);
            return CombatCommandResult.Refuse(
                commandId,
                CombatCommandErrorCodes.Internal,
                "The combat service could not process the command."
            );
        }
    }
}
