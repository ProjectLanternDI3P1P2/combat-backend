using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.Persistence.Repositories;

public sealed class MonsterDefinitionRepositoryTests
{
    [Fact]
    public async Task GetByClassAsync_ReturnsOnlyRequestedClassOrderedByName()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        context.MonsterDefinitions.AddRange(
            CreateDefinition("Zulu", MonsterClass.Ordinary),
            CreateDefinition("Alpha", MonsterClass.Ordinary),
            CreateDefinition("Boss", MonsterClass.Boss)
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new MonsterDefinitionRepository(context);

        IReadOnlyList<MonsterDefinition> result = await repository.GetByClassAsync(
            MonsterClass.Ordinary,
            TestContext.Current.CancellationToken
        );

        result.Select(definition => definition.Name).Should().Equal("Alpha", "Zulu");
    }

    private static CombatDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CombatDbContext(options);
    }

    private static MonsterDefinition CreateDefinition(string name, MonsterClass monsterClass) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Class = monsterClass,
            ImageUrl = "/assets/monsters/test.png",
            BaseHealth = 10,
            BaseAttack = 10,
            BaseDefense = 10,
            BaseSpeed = 10,
        };
}
