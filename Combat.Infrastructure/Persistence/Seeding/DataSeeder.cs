using Bogus;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.Persistence.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(
        CombatDbContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!await context.Players.AnyAsync(cancellationToken))
        {
            var faker = new Faker<Player>()
                .RuleFor(player => player.Id, _ => Guid.NewGuid())
                .RuleFor(player => player.Name, faker => faker.Name.FirstName())
                .RuleFor(player => player.MaxHealth, faker => faker.Random.Int(100, 200))
                .RuleFor(player => player.Health, (_, player) => player.MaxHealth)
                .RuleFor(player => player.Attack, faker => faker.Random.Int(1, 20));

            await context.Players.AddRangeAsync(faker.Generate(10), cancellationToken);
        }

        if (!await context.MonsterDefinitions.AnyAsync(cancellationToken))
        {
            await context.MonsterDefinitions.AddRangeAsync(CreateDefinitions(), cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<MonsterDefinition> CreateDefinitions()
    {
        return
        [
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb201",
                "Cave Goblin",
                MonsterClass.Ordinary,
                10,
                "cave-goblin"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb202",
                "Moss Wolf",
                MonsterClass.Ordinary,
                10,
                "moss-wolf"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb203",
                "Skeleton Scout",
                MonsterClass.Ordinary,
                10,
                "skeleton-scout"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb204",
                "Ash Ogre",
                MonsterClass.SubBoss,
                15,
                "ash-ogre"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb205",
                "Crypt Warden",
                MonsterClass.SubBoss,
                15,
                "crypt-warden"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb206",
                "Thorn Matriarch",
                MonsterClass.SubBoss,
                15,
                "thorn-matriarch"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb207",
                "Magma Behemoth",
                MonsterClass.Boss,
                20,
                "magma-behemoth"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb208",
                "Crypt Sovereign",
                MonsterClass.Boss,
                20,
                "crypt-sovereign"
            ),
            CreateDefinition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb209",
                "Abyssal Tyrant",
                MonsterClass.Boss,
                20,
                "abyssal-tyrant"
            ),
        ];
    }

    private static MonsterDefinition CreateDefinition(
        string id,
        string name,
        MonsterClass monsterClass,
        int baseStat,
        string imageKey
    )
    {
        return new MonsterDefinition
        {
            Id = Guid.Parse(id),
            Name = name,
            Class = monsterClass,
            ImageUrl = $"/assets/monsters/{imageKey}.png",
            BaseHealth = baseStat,
            BaseAttack = baseStat,
            BaseDefense = baseStat,
            BaseSpeed = baseStat,
        };
    }
}
