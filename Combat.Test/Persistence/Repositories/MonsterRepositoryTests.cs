using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.Persistence.Repositories;

public class MonsterRepositoryTests
{
    private static CombatDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CombatDbContext(options);
    }

    [Fact]
    public async Task AddMonsterAsync_ValidMonster_PersistsMonster()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new MonsterRepository(dbContext);
        var monster = CreateMonster();

        // Act
        await repository.AddMonsterAsync(monster, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var storedMonster = await dbContext.Monsters.FindAsync([monster.MonsterId], TestContext.Current.CancellationToken);
        storedMonster.Should().NotBeNull();
        storedMonster!.CombatId.Should().Be(monster.CombatId);
        storedMonster.IsBoss.Should().Be(monster.IsBoss);
        storedMonster.BaseHp.Should().Be(monster.BaseHp);
        storedMonster.State.Should().Be(monster.State);
    }

    private static Monster CreateMonster()
    {
        return new Monster
        {
            MonsterId = Guid.NewGuid(),
            CombatId = Guid.NewGuid(),
            IsBoss = false,
            BaseHp = 30,
            BaseAttack = 8,
            BaseDefense = 4,
            BaseSpeed = 10,
            State = MonsterState.Alive
        };
    }
}
