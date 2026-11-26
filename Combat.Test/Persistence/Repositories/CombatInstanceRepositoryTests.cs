using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Records;
using Combat.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Combat.Test.Persistence.Repositories;

public sealed class CombatInstanceRepositoryTests
{
    [Fact]
    public async Task AddAsync_NewCombat_PersistsTheCombatAndItsFighters()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new CombatInstanceRepository(context);
        CombatInstance combat = CreateCombat();

        await repository.AddAsync(combat, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        CombatInstanceRecord? stored = await context
            .CombatInstances.Include(instance => instance.Fighters)
            .SingleOrDefaultAsync(
                instance => instance.CombatId == combat.CombatId,
                TestContext.Current.CancellationToken
            );

        stored.Should().NotBeNull();
        stored!.Fighters.Should().HaveCount(2);
        stored.EncounterType.Should().Be(EncounterType.Enemy);
    }

    [Fact]
    public async Task GetByCommandIdAsync_ExistingCombat_ReturnsTheSameState()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new CombatInstanceRepository(context);
        CombatInstance combat = CreateCombat();
        await repository.AddAsync(combat, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        CombatInstance? result = await repository.GetByCommandIdAsync(
            combat.CommandId,
            TestContext.Current.CancellationToken
        );

        result.Should().NotBeNull();
        result!.CombatId.Should().Be(combat.CombatId);
        result.SessionId.Should().Be(combat.SessionId);
        result.EncounterId.Should().Be(combat.EncounterId);
        result.EncounterType.Should().Be(EncounterType.Enemy);
        result.Status.Should().Be(CombatStatus.Active);
        result.Fighters.Should().HaveCount(2);
        Fighter hero = result.Fighters.Single(fighter => fighter.Type == FighterType.Hero);
        hero.Name.Should().Be("Aldric");
        hero.CurrentHp.Should().Be(100);
        hero.MaxHp.Should().Be(100);
        hero.CurrentMana.Should().Be(50);
        hero.MaxMana.Should().Be(50);
        hero.Stats.Attack.Should().Be(10);
        hero.Abilities.Should().ContainSingle();
        hero.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByEncounterIdAsync_ExistingCombat_ReturnsIt()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new CombatInstanceRepository(context);
        CombatInstance combat = CreateCombat();
        await repository.AddAsync(combat, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        CombatInstance? result = await repository.GetByEncounterIdAsync(
            combat.EncounterId,
            TestContext.Current.CancellationToken
        );

        result.Should().NotBeNull();
        result!.CombatId.Should().Be(combat.CombatId);
    }

    [Fact]
    public async Task GetByCommandIdAsync_UnknownCommand_ReturnsNull()
    {
        await using CombatDbContext context = CreateInMemoryDbContext();
        var repository = new CombatInstanceRepository(context);

        CombatInstance? result = await repository.GetByCommandIdAsync(
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

    private static CombatInstance CreateCombat()
    {
        var heroState = new FighterInitialState(
            currentHp: 100,
            maxHp: 100,
            currentMana: 50,
            maxMana: 50,
            stats: new FighterStats(10, 5, 8),
            abilities: [new FighterAbility(Guid.NewGuid(), "Strike", 5, "SingleEnemy")],
            items: [new FighterItem(Guid.NewGuid(), "Health Potion", "Consumable", 2)]
        );
        var monsterState = new FighterInitialState(
            currentHp: 20,
            maxHp: 20,
            currentMana: 0,
            maxMana: 0,
            stats: new FighterStats(4, 2, 3),
            abilities: [new FighterAbility(Guid.NewGuid(), "Basic attack", 0, "SingleEnemy")],
            items: []
        );

        return CombatInstance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Enemy,
            [
                Fighter.CreateHero(Guid.NewGuid(), "Aldric", 3, heroState, false),
                Fighter.CreateMonster(Guid.NewGuid(), "Cave Goblin", 1, monsterState),
            ],
            DateTimeOffset.UtcNow
        );
    }
}
