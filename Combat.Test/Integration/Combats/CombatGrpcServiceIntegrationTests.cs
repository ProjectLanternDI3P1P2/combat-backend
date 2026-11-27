using Combat.Application.Features.CombatUseCase;
using Combat.Application.Features.CombatUseCase.JoinCombat;
using Combat.Application.Features.CombatUseCase.SubmitCombatAction;
using Combat.Application.Services;
using Combat.Contracts.V1;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.Persistence;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using DomainActionType = Combat.Domain.Enums.FightActionType;
using DomainResult = Combat.Domain.Enums.CombatResult;
using DomainStatus = Combat.Domain.Enums.CombatStatus;

namespace Combat.Test.Integration.Combats;

/// <summary>Combat opened by Dungeon over gRPC, played and reported, on PostgreSQL.</summary>
public sealed class CombatGrpcServiceIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private CombatWebApplicationFactory? factory;
    private GrpcChannel? channel;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"combat_test_combat_{Guid.NewGuid():N}");
        factory = new CombatWebApplicationFactory(database.ConnectionString);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        CombatDbContext context = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        context.MonsterDefinitions.AddRange(
            Definition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb201",
                "Cave Goblin",
                global::Combat.Domain.Enums.MonsterClass.Ordinary,
                12
            ),
            Definition(
                "0d89a169-cfa8-4c0f-8e27-59e1cedbb207",
                "Magma Behemoth",
                global::Combat.Domain.Enums.MonsterClass.Boss,
                60
            )
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateCombat_PackOfThreeMobs_ReturnsOneCombatWithThreeMonsters()
    {
        CreateCombatRequest request = Request(monsters: 3);

        CreateCombatResponse response = await CreateClient()
            .CreateCombatAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        response.CombatId.Should().NotBeEmpty();
        response.RunId.Should().Be(request.RunId);
        response.Status.Should().Be("active");
        response.Participants.Should().ContainSingle(participant => participant.Type == "Hero");
        response
            .Participants.Where(participant => participant.Type == "Monster")
            .Select(participant => participant.ExternalId)
            .Should()
            .BeEquivalentTo(request.Monsters.Select(monster => monster.ElementId));
        response
            .Participants.Where(participant => participant.Type == "Monster")
            .Should()
            .OnlyContain(participant => participant.SpriteKey == "cave-goblin");
    }

    [Fact]
    public async Task CreateCombat_TwoEncounters_UsesMaximaThenCarriesRunResourcesIncludingZero()
    {
        CombatService.CombatServiceClient client = CreateClient();
        string heroId = MockHeroCatalog.WoundedRogueHeroId.ToString();
        CreateCombatRequest first = Request(monsters: 1);
        first.Participants.Add(new EncounterParticipant { HeroId = heroId });

        CreateCombatResponse firstResponse = await client.CreateCombatAsync(
            first,
            cancellationToken: TestContext.Current.CancellationToken
        );
        CombatParticipant initialHero = firstResponse.Participants.Single(p =>
            p.ExternalId == heroId
        );
        initialHero.CurrentHp.Should().Be(initialHero.MaxHp);
        initialHero.CurrentMana.Should().Be(initialHero.MaxMana);

        CreateCombatRequest second = Request(monsters: 1);
        second.RunId = first.RunId;
        second.SessionId = first.SessionId;
        second.Participants.Add(
            new EncounterParticipant
            {
                HeroId = heroId,
                CurrentHp = 42,
                CurrentMana = 0,
            }
        );

        CreateCombatResponse secondResponse = await client.CreateCombatAsync(
            second,
            cancellationToken: TestContext.Current.CancellationToken
        );
        secondResponse.Participants.Count(p => p.Type == "Hero").Should().Be(2);
        CombatParticipant carriedHero = secondResponse.Participants.Single(p =>
            p.ExternalId == heroId
        );
        carriedHero.CurrentHp.Should().Be(42);
        carriedHero.CurrentMana.Should().Be(0);
        carriedHero.MaxMana.Should().Be(initialHero.MaxMana);
    }

    [Fact]
    public async Task CreateCombat_ReplayedCommand_ReturnsTheSameCombat()
    {
        CombatService.CombatServiceClient client = CreateClient();
        CreateCombatRequest request = Request(monsters: 2);

        CreateCombatResponse first = await client.CreateCombatAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );
        CreateCombatResponse second = await client.CreateCombatAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );

        second.CombatId.Should().Be(first.CombatId);
        second.Participants.Should().HaveCount(first.Participants.Count);
    }

    [Fact]
    public async Task CreateCombat_ReusedCommandIdWithAnotherPack_ReturnsAlreadyExists()
    {
        CombatService.CombatServiceClient client = CreateClient();
        CreateCombatRequest request = Request(monsters: 1);
        await client.CreateCombatAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );
        CreateCombatRequest other = request.Clone();
        other.Monsters.Clear();
        other.Monsters.Add(new EncounterMonster { ElementId = Guid.NewGuid().ToString() });

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateCombatAsync(
                other,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.Should().Be(StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task CreateCombat_NoMonster_ReturnsInvalidArgument()
    {
        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await CreateClient()
                .CreateCombatAsync(
                    Request(monsters: 0),
                    cancellationToken: TestContext.Current.CancellationToken
                )
        );

        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCombat_UnknownHero_ReturnsNotFound()
    {
        CreateCombatRequest request = Request(monsters: 1);
        request.Participants[0].HeroId = Guid.NewGuid().ToString();

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await CreateClient()
                .CreateCombatAsync(
                    request,
                    cancellationToken: TestContext.Current.CancellationToken
                )
        );

        exception.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCombat_InvalidSessionId_ReturnsInvalidArgument()
    {
        CreateCombatRequest request = Request(monsters: 1);
        request.SessionId = "not-a-uuid";

        RpcException exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await CreateClient()
                .CreateCombatAsync(
                    request,
                    cancellationToken: TestContext.Current.CancellationToken
                )
        );

        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Combat_PlayedUntilTheEnd_IsReportedToDungeonOnce()
    {
        CreateCombatRequest request = Request(monsters: 1);
        CreateCombatResponse created = await CreateClient()
            .CreateCombatAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Guid combatId = Guid.Parse(created.CombatId);

        CombatView view = await PlayUntilTheEndAsync(combatId);

        view.Status.Should().Be(DomainStatus.Completed);
        view.Result.Should().BeOneOf(DomainResult.Victory, DomainResult.Defeat);
        await using AsyncServiceScope scope = factory!.Services.CreateAsyncScope();
        ICombatResultReporter reporter =
            scope.ServiceProvider.GetRequiredService<ICombatResultReporter>();
        (await reporter.ReportAsync(combatId, TestContext.Current.CancellationToken))
            .Should()
            .BeTrue();
        (await reporter.ReportAsync(combatId, TestContext.Current.CancellationToken))
            .Should()
            .BeTrue();
        factory
            .DungeonResults.Reports.Where(report => report.CombatId == combatId)
            .Should()
            .ContainSingle()
            .Which.EncounterId.Should()
            .Be(Guid.Parse(request.EncounterId));
    }

    private async Task<CombatView> PlayUntilTheEndAsync(Guid combatId)
    {
        await using AsyncServiceScope joinScope = factory!.Services.CreateAsyncScope();
        CombatView view = await joinScope
            .ServiceProvider.GetRequiredService<ISender>()
            .Send(new JoinCombatCommand(combatId), TestContext.Current.CancellationToken);
        for (int turn = 0; turn < 100 && view.Status == DomainStatus.Active; turn++)
        {
            CombatFighterView monster = view.Fighters.First(fighter =>
                fighter.Type == global::Combat.Domain.Enums.FighterType.Monster
                && fighter.State == global::Combat.Domain.Enums.FighterState.Active
            );
            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
            CombatActionResult result = await scope
                .ServiceProvider.GetRequiredService<ISender>()
                .Send(
                    new SubmitCombatActionCommand(
                        combatId,
                        Guid.NewGuid(),
                        view.ActiveFighterId!.Value,
                        DomainActionType.Attack,
                        monster.FighterId,
                        null
                    ),
                    TestContext.Current.CancellationToken
                );
            view = result.Combat;
        }

        return view;
    }

    private static CreateCombatRequest Request(int monsters)
    {
        var request = new CreateCombatRequest
        {
            SessionId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(),
            CommandId = Guid.NewGuid().ToString(),
            EncounterType = EncounterType.Enemy,
            RunId = Guid.NewGuid().ToString(),
            Floor = 1,
            RoomId = 4,
        };
        request.Participants.Add(
            new EncounterParticipant { HeroId = MockHeroCatalog.WarriorHeroId.ToString() }
        );
        for (int index = 0; index < monsters; index++)
        {
            request.Monsters.Add(new EncounterMonster { ElementId = Guid.NewGuid().ToString() });
        }

        return request;
    }

    private static global::Combat.Domain.Entities.MonsterDefinition Definition(
        string id,
        string name,
        global::Combat.Domain.Enums.MonsterClass monsterClass,
        int stat
    ) =>
        new()
        {
            Id = Guid.Parse(id),
            Name = name,
            Class = monsterClass,
            ImageUrl = $"/assets/monsters/{name.ToLowerInvariant().Replace(' ', '-')}.png",
            BaseHealth = stat,
            BaseAttack = stat,
            BaseDefense = 2,
            BaseSpeed = 5,
        };

    private CombatService.CombatServiceClient CreateClient()
    {
        TestServer server =
            factory?.Server ?? throw new InvalidOperationException("Fixture not initialized.");
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = server.CreateHandler() }
        );
        return new CombatService.CombatServiceClient(channel);
    }

    public async ValueTask DisposeAsync()
    {
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
