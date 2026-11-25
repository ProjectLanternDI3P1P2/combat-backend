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
    public async Task AddAsync_NewMonster_StagesWithoutSaving()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new MonsterRepository(context);
        Monster monster = CreateMonster();

        await repository.AddAsync(monster, TestContext.Current.CancellationToken);

        context.Entry(monster).State.Should().Be(EntityState.Added);
        (await context.Monsters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (
            await repository.GetByIdempotencyKeyAsync(
                monster.IdempotencyKey,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .BeSameAs(monster);
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
