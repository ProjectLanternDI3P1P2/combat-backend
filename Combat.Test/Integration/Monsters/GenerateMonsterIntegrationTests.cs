using Combat.Application.Exceptions;
using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Combat.Test.Integration.Monsters;

public sealed class GenerateMonsterIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private CombatWebApplicationFactory? factory;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"combat_test_monsters_{Guid.NewGuid():N}");
        factory = new CombatWebApplicationFactory(database.ConnectionString);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        context.MonsterDefinitions.Add(
            new MonsterDefinition
            {
                Id = Guid.Parse("0d89a169-cfa8-4c0f-8e27-59e1cedbb201"),
                Name = "Cave Goblin",
                Class = MonsterClass.Ordinary,
                BaseHealth = 10,
                BaseAttack = 10,
                BaseDefense = 10,
                BaseSpeed = 10,
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_NewIdempotencyKey_PersistsTheMonster()
    {
        Guid idempotencyKey = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();

        GenerateMonsterResult result = await SendAsync(dungeonRunId, idempotencyKey);

        result.MonsterId.Should().NotBeEmpty();
        result.MonsterClass.Should().Be(MonsterClass.Ordinary);

        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        (await context.Monsters.SingleAsync(TestContext.Current.CancellationToken))
            .IdempotencyKey.Should()
            .Be(idempotencyKey);
    }

    [Fact]
    public async Task Handle_ReplayedIdempotencyKey_ReturnsTheSameMonster()
    {
        Guid idempotencyKey = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();

        GenerateMonsterResult first = await SendAsync(dungeonRunId, idempotencyKey);
        GenerateMonsterResult second = await SendAsync(dungeonRunId, idempotencyKey);

        second.MonsterId.Should().Be(first.MonsterId);
    }

    [Fact]
    public async Task Handle_ReusedIdempotencyKeyWithDifferentParameters_ThrowsConflict()
    {
        Guid idempotencyKey = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        await SendAsync(dungeonRunId, idempotencyKey);

        Func<Task> act = () => SendAsync(dungeonRunId, idempotencyKey, floor: 5);

        await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
    }

    private async Task<GenerateMonsterResult> SendAsync(
        Guid dungeonRunId,
        Guid idempotencyKey,
        int floor = 1
    )
    {
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        return await sender.Send(
            new GenerateMonsterCommand(
                dungeonRunId,
                idempotencyKey,
                floor,
                PlayerCount: 1,
                MonsterClass: MonsterClass.Ordinary
            ),
            TestContext.Current.CancellationToken
        );
    }

    public async ValueTask DisposeAsync()
    {
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
