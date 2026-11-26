using Combat.Contracts.V1;
using Combat.Infrastructure.Persistence;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Combat.Test.Integration.Combats;

public sealed class CombatGrpcServiceIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private CombatWebApplicationFactory? factory;
    private GrpcChannel? channel;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"combat_test_combat_{Guid.NewGuid():N}");
        factory = new CombatWebApplicationFactory(database.ConnectionString);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        context.MonsterDefinitions.Add(
            new Combat.Domain.Entities.MonsterDefinition
            {
                Id = Guid.Parse("0d89a169-cfa8-4c0f-8e27-59e1cedbb201"),
                Name = "Cave Goblin",
                Class = Combat.Domain.Enums.MonsterClass.Ordinary,
                BaseHealth = 10,
                BaseAttack = 10,
                BaseDefense = 10,
                BaseSpeed = 10,
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateCombat_ValidRequest_ReturnsTheCombatAndItsParticipants()
    {
        CombatService.CombatServiceClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();

        CreateCombatResponse response = await client.CreateCombatAsync(
            new CreateCombatRequest
            {
                SessionId = sessionId.ToString(),
                EncounterId = encounterId.ToString(),
                CommandId = commandId.ToString(),
                EncounterType = EncounterType.Enemy,
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.CombatId.Should().NotBeEmpty();
        response.SessionId.Should().Be(sessionId.ToString());
        response.EncounterId.Should().Be(encounterId.ToString());
        response.EncounterType.Should().Be(EncounterType.Enemy);
        response.Status.Should().Be("Active");
        response.Participants.Should().Contain(participant => participant.Type == "Hero");
        response.Participants.Should().Contain(participant => participant.Type == "Monster");
    }

    [Fact]
    public async Task CreateCombat_ReplayedCommand_ReturnsTheSameCombat()
    {
        CombatService.CombatServiceClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        var request = new CreateCombatRequest
        {
            SessionId = sessionId.ToString(),
            EncounterId = encounterId.ToString(),
            CommandId = commandId.ToString(),
            EncounterType = EncounterType.Enemy,
        };

        CreateCombatResponse first = await client.CreateCombatAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );
        CreateCombatResponse second = await client.CreateCombatAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );

        second.CombatId.Should().Be(first.CombatId);
    }

    [Fact]
    public async Task CreateCombat_InvalidSessionId_ReturnsInvalidArgument()
    {
        CombatService.CombatServiceClient client = CreateClient();

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateCombatAsync(
                new CreateCombatRequest
                {
                    SessionId = "not-a-uuid",
                    EncounterId = Guid.NewGuid().ToString(),
                    CommandId = Guid.NewGuid().ToString(),
                    EncounterType = EncounterType.Enemy,
                },
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCombat_UnknownEncounterType_ReturnsInvalidArgument()
    {
        CombatService.CombatServiceClient client = CreateClient();

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateCombatAsync(
                new CreateCombatRequest
                {
                    SessionId = Guid.NewGuid().ToString(),
                    EncounterId = Guid.NewGuid().ToString(),
                    CommandId = Guid.NewGuid().ToString(),
                    EncounterType = EncounterType.Unspecified,
                },
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCombat_ReusedCommandIdWithOtherParameters_ReturnsAlreadyExists()
    {
        CombatService.CombatServiceClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        await client.CreateCombatAsync(
            new CreateCombatRequest
            {
                SessionId = sessionId.ToString(),
                EncounterId = Guid.NewGuid().ToString(),
                CommandId = commandId.ToString(),
                EncounterType = EncounterType.Enemy,
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateCombatAsync(
                new CreateCombatRequest
                {
                    SessionId = sessionId.ToString(),
                    EncounterId = Guid.NewGuid().ToString(),
                    CommandId = commandId.ToString(),
                    EncounterType = EncounterType.Enemy,
                },
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.Should().Be(StatusCode.AlreadyExists);
    }

    private CombatService.CombatServiceClient CreateClient()
    {
        TestServer server =
            factory?.Server ?? throw new InvalidOperationException("Fixture not initialized.");
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = server.CreateHandler() }
        );
        return new CombatService.CombatServiceClient(channel);
    }

    public async ValueTask DisposeAsync()
    {
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
