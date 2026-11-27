using Combat.Application.Exceptions;
using Combat.Application.Features.CombatUseCase.CreateCombat;
using Combat.Application.Models;
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
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICombatInstanceRepository> _combatRepository = new();
    private readonly Mock<IHeroFighterInitializer> _heroFighterInitializer = new();
    private readonly Mock<IMonsterDefinitionRepository> _monsterDefinitionRepository = new();
    private readonly CreateCombatCommandHandler _handler;
    private CombatInstance? _added;

    public CreateCombatCommandHandlerTests()
    {
        _handler = new CreateCombatCommandHandler(
            _combatRepository.Object,
            _heroFighterInitializer.Object,
            _monsterDefinitionRepository.Object,
            Options.Create(new MonsterGenerationOptions()),
            Options.Create(new CombatOptions { TurnDurationSeconds = 30 }),
            new MiddleDice(),
            new FixedClock(),
            Mock.Of<ILogger>()
        );
        _combatRepository
            .Setup(repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>())
            )
            .Callback<CombatInstance, CancellationToken>((combat, _) => _added = combat)
            .Returns(Task.CompletedTask);
        _heroFighterInitializer
            .Setup(initializer =>
                initializer.InitializeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (Guid heroId, int? hp, int? mana, CancellationToken _) => HeroFighter(heroId, hp)
            );
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Ordinary, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([
                Definition("Cave Goblin", MonsterClass.Ordinary),
                Definition("Moss Wolf", MonsterClass.Ordinary),
            ]);
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Boss, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([Definition("Crypt Sovereign", MonsterClass.Boss)]);
    }

    [Fact]
    public async Task Handle_PackOfThreeMobs_CreatesOneCombatWithThreeMonsters()
    {
        CreateCombatCommand command = Command(monsters: 3);

        CreateCombatResult result = await _handler.Handle(
            command,
            TestContext.Current.CancellationToken
        );

        result.RunId.Should().Be(command.RunId);
        result.Status.Should().Be(CombatStatus.Active);
        result.Participants.Count(p => p.Type == FighterType.Hero).Should().Be(1);
        result
            .Participants.Where(p => p.Type == FighterType.Monster)
            .Select(p => p.ExternalId)
            .Should()
            .BeEquivalentTo(command.Monsters.Select(m => m.ElementId));
        _added.Should().NotBeNull();
        _added!.Turn.Should().BeGreaterThan(0, "the combat starts on its first turn");
        _added.TurnDeadline.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SingleMob_CreatesACombatWithOneMonster()
    {
        CreateCombatResult result = await _handler.Handle(
            Command(monsters: 1),
            TestContext.Current.CancellationToken
        );

        result.Participants.Count(p => p.Type == FighterType.Monster).Should().Be(1);
    }

    [Fact]
    public async Task Handle_SameMobTwice_BecomesTheSameMonster()
    {
        Guid elementId = Guid.NewGuid();
        CreateCombatResult first = await _handler.Handle(
            Command(elementIds: [elementId]),
            TestContext.Current.CancellationToken
        );
        CreateCombatResult second = await _handler.Handle(
            Command(elementIds: [elementId]),
            TestContext.Current.CancellationToken
        );

        Monster(first).Name.Should().Be(Monster(second).Name);
        Monster(first).Visual.SpriteKey.Should().Be(Monster(second).Visual.SpriteKey);
    }

    [Fact]
    public async Task Handle_MonsterStats_FollowTheFloorAndPartyScaling()
    {
        CreateCombatResult result = await _handler.Handle(
            Command(monsters: 1, floor: 3),
            TestContext.Current.CancellationToken
        );

        // Base 20, +2 per floor above the first, one player.
        Monster(result).MaxHp.Should().Be(24);
    }

    [Fact]
    public async Task Handle_Boss_UsesABossDefinitionFlaggedAsBoss()
    {
        CreateCombatCommand command = Command(monsters: 0) with
        {
            EncounterType = EncounterType.Boss,
            Monsters = [new EncounterMonster(Guid.NewGuid(), IsBoss: true)],
        };

        CreateCombatResult result = await _handler.Handle(
            command,
            TestContext.Current.CancellationToken
        );

        Monster(result).Name.Should().Be("Crypt Sovereign");
        Monster(result).Visual.IsBoss.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RunHitPoints_AreGivenToTheHeroInitializer()
    {
        CreateCombatCommand command = Command(monsters: 1) with
        {
            Participants = [new EncounterParticipant(Guid.NewGuid(), 42, 7)],
        };

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _heroFighterInitializer.Verify(initializer =>
            initializer.InitializeAsync(
                command.Participants[0].HeroId,
                42,
                7,
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task Handle_ReplayedCommand_ReturnsTheExistingCombat()
    {
        CreateCombatCommand command = Command(monsters: 2);
        await _handler.Handle(command, TestContext.Current.CancellationToken);
        _combatRepository
            .Setup(repository =>
                repository.GetByCommandIdAsync(command.CommandId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(_added);

        CreateCombatResult replay = await _handler.Handle(
            command,
            TestContext.Current.CancellationToken
        );

        replay.CombatId.Should().Be(_added!.CombatId);
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReusedCommandIdWithAnotherPack_ThrowsConflict()
    {
        CreateCombatCommand command = Command(monsters: 2);
        await _handler.Handle(command, TestContext.Current.CancellationToken);
        _combatRepository
            .Setup(repository =>
                repository.GetByCommandIdAsync(command.CommandId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(_added);

        Func<Task> act = () =>
            _handler.Handle(
                command with
                {
                    Monsters = [new EncounterMonster(Guid.NewGuid(), false)],
                },
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
    }

    [Fact]
    public async Task Handle_HeroThatCannotBeInitialized_FailsTheWholeCombat()
    {
        _heroFighterInitializer
            .Setup(initializer =>
                initializer.InitializeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ExternalServiceUnavailableException("Player"));

        Func<Task> act = () =>
            _handler.Handle(Command(monsters: 1), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
        _combatRepository.Verify(
            repository =>
                repository.AddAsync(It.IsAny<CombatInstance>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_NoDefinitionForTheClass_Throws()
    {
        _monsterDefinitionRepository
            .Setup(repository =>
                repository.GetByClassAsync(MonsterClass.Ordinary, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        Func<Task> act = () =>
            _handler.Handle(Command(monsters: 1), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCombatEncounterException>();
    }

    private static CombatParticipantResult Monster(CreateCombatResult result) =>
        result.Participants.First(p => p.Type == FighterType.Monster);

    private static CreateCombatCommand Command(
        int monsters = 1,
        int floor = 1,
        IReadOnlyList<Guid>? elementIds = null
    ) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Enemy,
            Guid.NewGuid(),
            floor,
            2,
            [new EncounterParticipant(Guid.NewGuid(), null, null)],
            [
                .. (
                    elementIds ?? Enumerable.Range(0, monsters).Select(_ => Guid.NewGuid()).ToList()
                ).Select(id => new EncounterMonster(id, false)),
            ]
        );

    private static MonsterDefinition Definition(string name, MonsterClass monsterClass) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Class = monsterClass,
            ImageUrl = $"/assets/monsters/{name.ToLowerInvariant().Replace(' ', '-')}.png",
            BaseHealth = 20,
            BaseAttack = 10,
            BaseDefense = 5,
            BaseSpeed = 5,
        };

    private static Fighter HeroFighter(Guid heroId, int? hp) =>
        Fighter.CreateHero(
            heroId,
            "Aldric",
            2,
            new FighterInitialState(
                hp ?? 150,
                150,
                20,
                20,
                new FighterStats(18, 8, 9),
                [new FighterAbility("warrior-strike", "Strike", 4, "SingleEnemy", 150)],
                []
            ),
            isFromMockedData: false,
            FighterVisual.Hero("warrior", 1, "bob1", 1)
        );

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class MiddleDice : IDice
    {
        public int Between(int minInclusive, int maxInclusive) =>
            maxInclusive - minInclusive == 99 ? 50
            : minInclusive == 85 ? 100
            : minInclusive;
    }
}
