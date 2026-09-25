using Combat.Application.Features.HeroCombatState;
using Combat.Infrastructure.HeroMocks;
using Combat.Presentation.DTO;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Combat.Presentation.Hubs;

public sealed class CombatHub(ISender sender, ILogger<CombatHub> logger) : Hub<ICombatHubClient>
{
    public override async Task OnConnectedAsync()
    {
        string scenario = (Context.GetHttpContext()?.Request.Query["scenario"].FirstOrDefault()
            ?? HeroMockScenarios.Complete).Trim().ToLowerInvariant();

        await Clients.Caller.CombatStateChanged(HeroCombatStateDto.Loading(scenario));

        var state = await sender.Send(
            new StartHeroCombatStateSession(Context.ConnectionId, scenario),
            Context.ConnectionAborted);

        await Clients.Caller.CombatStateChanged(HeroCombatStateDto.FromApplication(state));

        logger.LogInformation(
            "Hero mock session started for connection {ConnectionId} with scenario {Scenario}.",
            Context.ConnectionId,
            scenario);

        await base.OnConnectedAsync();
    }

    public async Task<HeroCombatStateDto> Resynchronize()
    {
        var state = await sender.Send(
            new GetHeroCombatState(Context.ConnectionId),
            Context.ConnectionAborted);
        var dto = HeroCombatStateDto.FromApplication(state);

        await Clients.Caller.CombatStateChanged(dto);

        logger.LogInformation(
            "Hero mock session resynchronized for connection {ConnectionId} at sequence {Sequence}.",
            Context.ConnectionId,
            dto.Sequence);

        return dto;
    }

    public async Task<HeroCombatStateDto> AdvanceMockState()
    {
        var state = await sender.Send(
            new AdvanceHeroCombatState(Context.ConnectionId),
            Context.ConnectionAborted);
        var dto = HeroCombatStateDto.FromApplication(state);

        await Clients.Caller.CombatStateChanged(dto);

        logger.LogInformation(
            "Hero mock session advanced for connection {ConnectionId} to sequence {Sequence}.",
            Context.ConnectionId,
            dto.Sequence);

        return dto;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await sender.Send(new EndHeroCombatStateSession(Context.ConnectionId), CancellationToken.None);

        logger.LogInformation(
            exception,
            "Hero mock session ended for connection {ConnectionId}.",
            Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}
