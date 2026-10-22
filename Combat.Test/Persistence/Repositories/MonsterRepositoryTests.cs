using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.Persistence.Repositories;

public sealed class MonsterRepositoryTests
{
    [Fact]
    public async Task AddOrGetByIdempotencyKeyAsync_NewMonster_PersistsAndReturnsMonster()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new MonsterRepository(context);
        Monster monster = CreateMonster();

        Monster result = await repository.AddOrGetByIdempotencyKeyAsync(
            monster,
            TestContext.Current.CancellationToken
        );

        result.Should().BeSameAs(monster);
        (await context.Monsters.SingleAsync(TestContext.Current.CancellationToken))
            .Id.Should()
            .Be(monster.Id);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_WhenMonsterExists_ReturnsIt()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        Monster monster = CreateMonster();
        context.Monsters.Add(monster);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new MonsterRepository(context);

        Monster? result = await repository.GetByIdempotencyKeyAsync(
            monster.IdempotencyKey,
            TestContext.Current.CancellationToken
        );

        result.Should().NotBeNull();
        result!.Id.Should().Be(monster.Id);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_WhenMonsterDoesNotExist_ReturnsNull()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new MonsterRepository(context);

        Monster? result = await repository.GetByIdempotencyKeyAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        result.Should().BeNull();
    }

    private static CombatDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CombatDbContext(options);
    }

    private static Monster CreateMonster() =>
        new()
        {
            Id = Guid.NewGuid(),
            DungeonRunId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid(),
            MonsterDefinitionId = Guid.NewGuid(),
            Name = "Test monster",
            Class = MonsterClass.Ordinary,
            Level = 1,
            PlayerCount = 1,
            ImageUrl = "/assets/monsters/test.png",
            BaseHealth = 10,
            BaseAttack = 10,
            BaseDefense = 10,
            BaseSpeed = 10,
        };
}
