using Combat.Application.Abstractions;
using Combat.Domain.Entities;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.PipelineBehavior;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.PipelineBehavior;

public sealed class CommandTransactionBehaviorTests
{
    [Fact]
    public async Task Handle_CommandWithResponse_CommitsChangesAndReturnsResult()
    {
        string databaseName = Guid.NewGuid().ToString();
        await using var context = CreateInMemoryDbContext(databaseName);
        var behavior = new CommandTransactionBehavior<CreatePlayerWithResponseCommand, Guid>(
            context
        );
        var player = new Player { Id = Guid.NewGuid(), Name = "Committed with response" };

        Guid result = await behavior.Handle(
            new CreatePlayerWithResponseCommand(),
            _ =>
            {
                context.Players.Add(player);
                return Task.FromResult(player.Id);
            },
            TestContext.Current.CancellationToken
        );

        result.Should().Be(player.Id);
        await using var reading = CreateInMemoryDbContext(databaseName);
        (await reading.Players.FindAsync([result], TestContext.Current.CancellationToken))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task Handle_CommandSucceeds_CommitsChanges()
    {
        string databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateInMemoryDbContext(databaseName);
        var behavior = new CommandTransactionBehavior<CreatePlayerCommand, Unit>(dbContext);
        var player = new Player { Id = Guid.NewGuid(), Name = "Committed" };

        var result = await behavior.Handle(
            new CreatePlayerCommand(player),
            _ =>
            {
                dbContext.Players.Add(player);
                return Task.FromResult(Unit.Value);
            },
            TestContext.Current.CancellationToken
        );

        result.Should().Be(Unit.Value);
        await using var verificationContext = CreateInMemoryDbContext(databaseName);
        (
            await verificationContext.Players.FindAsync(
                [player.Id],
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task Handle_CommandFails_DoesNotCommitChanges()
    {
        string databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateInMemoryDbContext(databaseName);
        var behavior = new CommandTransactionBehavior<CreatePlayerCommand, Unit>(dbContext);
        var player = new Player { Id = Guid.NewGuid(), Name = "Not committed" };

        Func<Task> action = async () =>
            await behavior.Handle(
                new CreatePlayerCommand(player),
                _ =>
                {
                    dbContext.Players.Add(player);
                    return Task.FromException<Unit>(
                        new InvalidOperationException("Handler failed.")
                    );
                },
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var verificationContext = CreateInMemoryDbContext(databaseName);
        (
            await verificationContext.Players.FindAsync(
                [player.Id],
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .BeNull();
    }

    private static CombatDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new CombatDbContext(options);
    }

    private sealed record CreatePlayerCommand(Player Player) : ICommand;

    private sealed record CreatePlayerWithResponseCommand : ICommand<Guid>;
}
