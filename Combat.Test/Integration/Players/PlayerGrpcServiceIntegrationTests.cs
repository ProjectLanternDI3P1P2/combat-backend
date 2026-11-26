using Combat.Contracts.V1;
using Combat.Domain.Entities;
using Combat.Infrastructure.Persistence;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Combat.Test.Integration.Players;

public sealed class PlayerGrpcServiceIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private CombatWebApplicationFactory? factory;
    private GrpcChannel? channel;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"combat_test_grpc_{Guid.NewGuid():N}");
        factory = new CombatWebApplicationFactory(database.ConnectionString);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        context.Players.Add(
            new Player
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Ari",
                Health = 100,
                MaxHealth = 100,
                Attack = 12,
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPlayer_ExistingPlayer_ReturnsContractResponse()
    {
        TestServer server =
            factory?.Server ?? throw new InvalidOperationException("Fixture not initialized.");
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = server.CreateHandler() }
        );
        var client = new CombatPlayerService.CombatPlayerServiceClient(channel);

        GetPlayerResponse response = await client.GetPlayerAsync(
            new GetPlayerRequest { PlayerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.Name.Should().Be("Ari");
        response.Attack.Should().Be(12);
    }

    public async ValueTask DisposeAsync()
    {
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
