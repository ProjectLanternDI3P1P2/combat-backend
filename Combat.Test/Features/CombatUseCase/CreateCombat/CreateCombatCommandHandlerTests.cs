using Combat.Application.Exceptions;
using Combat.Application.Features.CombatUseCase.CreateCombat;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using Combat.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatCommandHandlerTests
{
    private readonly Mock<ICombatInstanceRepository> _combatRepository = new();
    private readonly Mock<ISessionHeroesClient> _sessionHeroesClient = new();
    private readonly Mock<IEncounterContextClient> _encounterContextClient = new();
    private readonly Mock<IHeroFighterInitializer> _heroFighterInitializer = new();
    private readonly Mock<IMonsterDefinitionRepository> _monsterDefinitionRepository = new();
    private readonly CreateCombatCommandHandler _handler;

    public CreateCombatCommandHandlerTests()
    {
        _handler = new CreateCombatCommandHandler(
            _combatRepository.Object,
            _sessionHeroesClient.Object,
            _encounterContextClient.Object,
            _heroFighterInitializer.Object,
            _monsterDefinitionRepository.Object,
            Options.Create(new MonsterGenerationOptions()),
            new FixedClock(),
            Mock.Of<ILogger>()
        );
    }

    [Fact]
    public async Task Handle_NewRequest_CreatesACombatWithHeroesAndMonsters()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        SetupHappyPath(
            sessionId,
            encounterId,
            commandId,
            [MockHeroCatalogIds.Warrior, MockHeroCatalogIds.Mage]
        );

        CreateCombatResult result = await _handler.Handle(
            new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
            TestContext.Current.CancellationToken
        );

        result.CombatId.Should().NotBeEmpty();
        result.SessionId.Should().Be(sessionId);
        result.EncounterId.Should().Be(encounterId);
        result.EncounterType.Should().Be(EncounterType.Enemy);
        result.Status.Should().Be(CombatStatus.Active);
        result.Participants.Should().HaveCount(4);
        result.Participants.Count(p => p.Type == FighterType.Hero).Should().Be(2);
        result.Participants.Count(p => p.Type == FighterType.Monster).Should().Be(2);
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_NewRequest_InitializesEveryParticipantWithItsCombatState()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        SetupHappyPath(sessionId, encounterId, commandId, [MockHeroCatalogIds.Warrior]);

        CreateCombatResult result = await _handler.Handle(
            new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
            TestContext.Current.CancellationToken
        );

        CombatParticipantResult hero = result.Participants.Single(p => p.Type == FighterType.Hero);
        hero.CurrentHp.Should().Be(100);
        hero.MaxHp.Should().Be(100);
        hero.CurrentMana.Should().Be(50);
        hero.MaxMana.Should().Be(50);
        hero.Attack.Should().Be(10);
        hero.Defense.Should().Be(5);
        hero.Speed.Should().Be(8);
        hero.Abilities.Should().ContainSingle();
        hero.Effects.Should().BeEmpty();
        hero.State.Should().Be(FighterState.Active);
    }

    [Fact]
    public async Task Handle_ReplayedCommand_ReturnsTheExistingCombat()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        CombatInstance existing = CreateExistingCombat(sessionId, encounterId, commandId);
        _combatRepository
            .Setup(repository =>
                repository.GetByCommandIdAsync(commandId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);

        CreateCombatResult result = await _handler.Handle(
            new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
            TestContext.Current.CancellationToken
        );

        result.CombatId.Should().Be(existing.CombatId);
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _sessionHeroesClient.Verify(
            client => client.GetHeroIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReusedCommandIdWithDifferentParameters_ThrowsConflict()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        CombatInstance existing = CreateExistingCombat(sessionId, encounterId, commandId);
        _combatRepository
            .Setup(repository =>
                repository.GetByCommandIdAsync(commandId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);

        Func<Task> act = () =>
            _handler.Handle(
                new CreateCombatCommand(sessionId, Guid.NewGuid(), commandId, EncounterType.Enemy),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
    }

    [Fact]
    public async Task Handle_SameEncounterWithAnotherCommand_ReturnsTheExistingCombat()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        CombatInstance existing = CreateExistingCombat(sessionId, encounterId, Guid.NewGuid());
        _combatRepository
            .Setup(repository =>
                repository.GetByEncounterIdAsync(encounterId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);

        CreateCombatResult result = await _handler.Handle(
            new CreateCombatCommand(sessionId, encounterId, Guid.NewGuid(), EncounterType.Enemy),
            TestContext.Current.CancellationToken
        );

        result.CombatId.Should().Be(existing.CombatId);
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_SessionWithoutHero_Throws()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        _sessionHeroesClient
            .Setup(client => client.GetHeroIdsAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Func<Task> act = () =>
            _handler.Handle(
                new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<NoCombatParticipantsException>();
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_HeroThatCannotBeInitialized_FailsTheWholeCombat()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        SetupHappyPath(
            sessionId,
            encounterId,
            commandId,
            [MockHeroCatalogIds.Warrior, MockHeroCatalogIds.Mage]
        );
        _heroFighterInitializer
            .Setup(initializer =>
                initializer.InitializeAsync(MockHeroCatalogIds.Mage, It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new HeroNotFoundException(MockHeroCatalogIds.Mage));

        Func<Task> act = () =>
            _handler.Handle(
                new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<HeroNotFoundException>();
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_EncounterWithoutMonsterDefinition_Throws()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        SetupHappyPath(sessionId, encounterId, commandId, [MockHeroCatalogIds.Warrior]);
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Ordinary, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        Func<Task> act = () =>
            _handler.Handle(
                new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Enemy),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<InvalidCombatEncounterException>();
    }

    [Fact]
    public async Task Handle_BossEncounter_GeneratesABossMonster()
    {
        Guid sessionId = Guid.NewGuid();
        Guid encounterId = Guid.NewGuid();
        Guid commandId = Guid.NewGuid();
        SetupHappyPath(sessionId, encounterId, commandId, [MockHeroCatalogIds.Warrior]);
        _encounterContextClient
            .Setup(client =>
                client.GetAsync(sessionId, EncounterType.Boss, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new EncounterContext(EncounterType.Boss, 1, 5));
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Boss, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([
                new MonsterDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Abyssal Tyrant",
                    Class = MonsterClass.Boss,
                    BaseHealth = 20,
                    BaseAttack = 20,
                    BaseDefense = 20,
                    BaseSpeed = 20,
                },
            ]);

        CreateCombatResult result = await _handler.Handle(
            new CreateCombatCommand(sessionId, encounterId, commandId, EncounterType.Boss),
            TestContext.Current.CancellationToken
        );

        CombatParticipantResult monster = result.Participants.Single(p =>
            p.Type == FighterType.Monster
        );
        monster.Name.Should().Be("Abyssal Tyrant");
        monster.Level.Should().Be(5);
        // Base health 20 plus 4 floors of +2, for a single player.
        monster.MaxHp.Should().Be(28);
    }

    private void SetupHappyPath(
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        IReadOnlyList<Guid> heroIds
    )
    {
        _sessionHeroesClient
            .Setup(client => client.GetHeroIdsAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(heroIds);
        _encounterContextClient
            .Setup(client =>
                client.GetAsync(sessionId, EncounterType.Enemy, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new EncounterContext(EncounterType.Enemy, 2, 1));
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Ordinary, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([
                new MonsterDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Cave Goblin",
                    Class = MonsterClass.Ordinary,
                    BaseHealth = 10,
                    BaseAttack = 10,
                    BaseDefense = 10,
                    BaseSpeed = 10,
                },
            ]);

        foreach (Guid heroId in heroIds)
        {
            _heroFighterInitializer
                .Setup(initializer =>
                    initializer.InitializeAsync(heroId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(CreateHeroFighter(heroId));
        }
    }

    private static Fighter CreateHeroFighter(Guid heroId)
    {
        var initialState = new FighterInitialState(
            currentHp: 100,
            maxHp: 100,
            currentMana: 50,
            maxMana: 50,
            stats: new FighterStats(10, 5, 8),
            abilities: [new FighterAbility(Guid.NewGuid(), "Strike", 5, "SingleEnemy")],
            items: []
        );

        return Fighter.CreateHero(heroId, "Hero", 1, initialState, false);
    }

    private static CombatInstance CreateExistingCombat(
        Guid sessionId,
        Guid encounterId,
        Guid commandId
    )
    {
        var initialState = new FighterInitialState(
            currentHp: 10,
            maxHp: 10,
            currentMana: 0,
            maxMana: 0,
            stats: new FighterStats(1, 1, 1),
            abilities: [],
            items: []
        );

        return CombatInstance.Create(
            Guid.NewGuid(),
            sessionId,
            encounterId,
            commandId,
            EncounterType.Enemy,
            [
                Fighter.CreateHero(Guid.NewGuid(), "Hero", 1, initialState, false),
                Fighter.CreateMonster(Guid.NewGuid(), "Goblin", 1, initialState),
            ],
            DateTimeOffset.UtcNow
        );
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    }

    private static class MockHeroCatalogIds
    {
        public static readonly Guid Warrior = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Mage = Guid.Parse("22222222-2222-2222-2222-222222222222");
    }
}
