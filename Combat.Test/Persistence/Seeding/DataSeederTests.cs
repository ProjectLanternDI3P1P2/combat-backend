using Combat.Domain.Enums;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.Persistence.Seeding;

public sealed class DataSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsThreeDefinitionsForEachMonsterClassOnlyOnce()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();

        await DataSeeder.SeedAsync(context, TestContext.Current.CancellationToken);
        await DataSeeder.SeedAsync(context, TestContext.Current.CancellationToken);

        context.MonsterDefinitions.Should().HaveCount(9);
        context
            .MonsterDefinitions.Count(definition => definition.Class == MonsterClass.Ordinary)
            .Should()
            .Be(3);
        context
            .MonsterDefinitions.Count(definition => definition.Class == MonsterClass.SubBoss)
            .Should()
            .Be(3);
        context
            .MonsterDefinitions.Count(definition => definition.Class == MonsterClass.Boss)
            .Should()
            .Be(3);
        context
            .MonsterDefinitions.Should()
            .OnlyContain(definition =>
                definition.BaseHealth == definition.BaseAttack
                && definition.BaseAttack == definition.BaseDefense
                && definition.BaseDefense == definition.BaseSpeed
            );
    }

    private static CombatDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CombatDbContext(options);
    }
}
