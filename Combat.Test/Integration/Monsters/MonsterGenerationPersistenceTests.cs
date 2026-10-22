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

/// <summary>Exercises generation through MediatR, the real repositories and PostgreSQL.</summary>
public sealed class MonsterGenerationPersistenceTests : IAsyncLifetime
{
    private TestDatabase? database;
    private CombatWebApplicationFactory? factory;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"combat_test_monsters_{Guid.NewGuid():N}");
        await database.ResetAsync();
        factory = new CombatWebApplicationFactory(database.ConnectionString);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        context.MonsterDefinitions.Add(
            new MonsterDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Test monster",
                Class = MonsterClass.Ordinary,
                ImageUrl = "/assets/monsters/test.png",
                BaseHealth = 10,
                BaseAttack = 12,
                BaseDefense = 8,
                BaseSpeed = 6,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_NewRequest_CommitsScaledMonster()
    {
        // Arrange
        GenerateMonsterCommand command = NewCommand();

        // Act
        GenerateMonsterResult result = await SendAsync(command);

        // Assert: a fresh context sees the command's committed result.
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        Monster stored = await scope
            .ServiceProvider.GetRequiredService<CombatDbContext>()
            .Monsters.SingleAsync(TestContext.Current.CancellationToken);
        stored.Id.Should().Be(result.MonsterId);
        stored.DungeonRunId.Should().Be(command.DungeonRunId);
        stored.IdempotencyKey.Should().Be(command.IdempotencyKey);
        stored.Level.Should().Be(5);
        stored.PlayerCount.Should().Be(3);
        stored.BaseHealth.Should().Be(54);
        stored.BaseAttack.Should().Be(60);
        stored.BaseDefense.Should().Be(48);
        stored.BaseSpeed.Should().Be(42);
    }

    [Fact]
    public async Task Generate_RepeatedRequest_ReturnsSameMonsterWithoutDuplicates()
    {
        // Arrange
        GenerateMonsterCommand command = NewCommand();
        GenerateMonsterResult first = await SendAsync(command);

        // Act
        GenerateMonsterResult repeated = await SendAsync(command);

        // Assert
        repeated.Should().Be(first);
        (await CountMonstersAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("run")]
    [InlineData("floor")]
    [InlineData("players")]
    [InlineData("class")]
    public async Task Generate_KeyReusedWithDifferentParameters_RejectsWithoutChanges(string field)
    {
        // Arrange
        GenerateMonsterCommand command = NewCommand();
        GenerateMonsterResult first = await SendAsync(command);
        GenerateMonsterCommand changed = field switch
        {
            "run" => command with { DungeonRunId = Guid.NewGuid() },
            "floor" => command with { Floor = 6 },
            "players" => command with { PlayerCount = 4 },
            _ => command with { MonsterClass = MonsterClass.Boss },
        };

        // Act
        Func<Task> act = async () => await SendAsync(changed);

        // Assert
        await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
        (await SendAsync(command)).Should().Be(first);
        (await CountMonstersAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Generate_ConcurrentRepeatedRequests_CommitOnlyOneMonster()
    {
        // Arrange
        GenerateMonsterCommand command = NewCommand();

        // Act: each request has its own scope, connection and transaction.
        GenerateMonsterResult[] results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SendAsync(command))
        );

        // Assert
        results.Should().AllBeEquivalentTo(results[0]);
        (await CountMonstersAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Generate_ConcurrentConflictingRequests_RejectsOneWithoutDuplicates()
    {
        // Arrange
        GenerateMonsterCommand command = NewCommand();
        GenerateMonsterCommand changed = command with { Floor = 6 };

        // Act
        Exception?[] errors = await Task.WhenAll(CaptureAsync(command), CaptureAsync(changed));

        // Assert
        errors.Count(error => error is null).Should().Be(1);
        errors.OfType<IdempotencyKeyReuseException>().Should().ContainSingle();
        (await CountMonstersAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Generate_InsideOuterTransaction_ReusesPendingMonsterAndRollsBackTogether()
    {
        // Arrange: represents a command run during combat initialization.
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        GenerateMonsterCommand command = NewCommand();

        // Act
        await using (
            var transaction = await context.Database.BeginTransactionAsync(
                TestContext.Current.CancellationToken
            )
        )
        {
            GenerateMonsterResult first = await mediator.Send(
                command,
                TestContext.Current.CancellationToken
            );
            GenerateMonsterResult repeated = await mediator.Send(
                command,
                TestContext.Current.CancellationToken
            );

            // Assert: the internal command leaves saving to the outer transaction.
            repeated.Should().Be(first);
            context.ChangeTracker.Entries<Monster>().Should().ContainSingle();
            (await context.Monsters.CountAsync(TestContext.Current.CancellationToken))
                .Should()
                .Be(0);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }
        (await CountMonstersAsync()).Should().Be(0);
    }

    private async Task<GenerateMonsterResult> SendAsync(GenerateMonsterCommand command)
    {
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        return await scope
            .ServiceProvider.GetRequiredService<IMediator>()
            .Send(command, TestContext.Current.CancellationToken);
    }

    private async Task<Exception?> CaptureAsync(GenerateMonsterCommand command)
    {
        try
        {
            await SendAsync(command);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task<int> CountMonstersAsync()
    {
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        return await scope
            .ServiceProvider.GetRequiredService<CombatDbContext>()
            .Monsters.CountAsync(TestContext.Current.CancellationToken);
    }

    private static GenerateMonsterCommand NewCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 5, 3, MonsterClass.Ordinary);

    public async ValueTask DisposeAsync()
    {
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
