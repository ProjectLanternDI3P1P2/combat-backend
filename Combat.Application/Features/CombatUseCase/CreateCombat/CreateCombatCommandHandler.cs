using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using MediatR;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatCommandHandler(
    ICombatInstanceRepository combatInstanceRepository,
    IHeroFighterInitializer heroFighterInitializer,
    IMonsterDefinitionRepository monsterDefinitionRepository,
    IOptions<MonsterGenerationOptions> monsterOptions,
    IOptions<CombatOptions> combatOptions,
    IDice dice,
    IClock clock,
    ILogger logger
) : IRequestHandler<CreateCombatCommand, CreateCombatResult>
{
    public async Task<CreateCombatResult> Handle(
        CreateCombatCommand request,
        CancellationToken cancellationToken
    )
    {
        CombatInstance? replayedCombat = await combatInstanceRepository.GetByCommandIdAsync(
            request.CommandId,
            cancellationToken
        );
        if (replayedCombat is not null)
        {
            EnsureSameRequest(replayedCombat, request);
            logger.Information(
                "Combat {CombatId} replayed for command {CommandId}.",
                replayedCombat.CombatId,
                request.CommandId
            );
            return CreateCombatResult.From(replayedCombat);
        }

        CombatInstance? combatOfEncounter = await combatInstanceRepository.GetByEncounterIdAsync(
            request.EncounterId,
            cancellationToken
        );
        if (combatOfEncounter is not null)
        {
            EnsureSameRequest(combatOfEncounter, request);
            logger.Information(
                "Encounter {EncounterId} already produced combat {CombatId}.",
                request.EncounterId,
                combatOfEncounter.CombatId
            );
            return CreateCombatResult.From(combatOfEncounter);
        }

        List<Fighter> fighters = [];
        foreach (EncounterParticipant participant in request.Participants)
        {
            fighters.Add(await InitializeHeroAsync(participant, cancellationToken));
        }

        fighters.AddRange(await CreateMonstersAsync(request, cancellationToken));

        DateTimeOffset now = clock.UtcNow;
        CombatInstance combat = CombatInstance.Create(
            Guid.NewGuid(),
            request.SessionId,
            request.EncounterId,
            request.CommandId,
            request.EncounterType,
            fighters,
            now,
            new CombatContext(request.RunId, request.Floor, request.RoomId)
        );
        combat.Start(dice, now, combatOptions.Value.TurnDurationSeconds);

        await combatInstanceRepository.AddAsync(combat, cancellationToken);

        logger.Information(
            "Combat {CombatId} created for run {RunId}, session {SessionId}, encounter {EncounterId} ({EncounterType}, floor {Floor}): {HeroCount} hero(s), {MonsterCount} monster(s), status {Status}.",
            combat.CombatId,
            combat.RunId,
            combat.SessionId,
            combat.EncounterId,
            combat.EncounterType,
            combat.Floor,
            request.Participants.Count,
            request.Monsters.Count,
            combat.Status
        );

        return CreateCombatResult.From(combat);
    }

    private async Task<List<Fighter>> CreateMonstersAsync(
        CreateCombatCommand request,
        CancellationToken cancellationToken
    )
    {
        Dictionary<MonsterClass, IReadOnlyList<MonsterDefinition>> definitionsByClass = [];
        List<Fighter> monsters = [];
        foreach (EncounterMonster monster in request.Monsters)
        {
            MonsterClass monsterClass = monster.IsBoss ? MonsterClass.Boss : MonsterClass.Ordinary;
            if (
                !definitionsByClass.TryGetValue(
                    monsterClass,
                    out IReadOnlyList<MonsterDefinition>? definitions
                )
            )
            {
                definitions = await monsterDefinitionRepository.GetByClassAsync(
                    monsterClass,
                    cancellationToken
                );
                if (definitions.Count == 0)
                {
                    throw new InvalidCombatEncounterException(
                        $"no monster definition is configured for the '{monsterClass}' class."
                    );
                }

                definitionsByClass[monsterClass] = definitions;
            }

            MonsterDefinition definition = MonsterFighterFactory.Pick(
                definitions,
                monster.ElementId
            );
            monsters.Add(
                MonsterFighterFactory.Create(
                    definition,
                    monster.ElementId,
                    monster.IsBoss,
                    request.Floor,
                    request.Participants.Count,
                    monsterOptions.Value
                )
            );
        }

        return monsters;
    }

    private async Task<Fighter> InitializeHeroAsync(
        EncounterParticipant participant,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await heroFighterInitializer.InitializeAsync(
                participant.HeroId,
                participant.CurrentHp,
                participant.CurrentMana,
                cancellationToken
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(
                exception,
                "Combat creation failed: hero {HeroId} could not be initialized.",
                participant.HeroId
            );
            throw;
        }
    }

    private static void EnsureSameRequest(CombatInstance combat, CreateCombatCommand request)
    {
        HashSet<Guid> requestedMobs = [.. request.Monsters.Select(monster => monster.ElementId)];
        HashSet<Guid> combatMobs =
        [
            .. combat
                .Fighters.Where(fighter => fighter.Type == FighterType.Monster)
                .Select(fighter => fighter.ExternalId),
        ];

        bool isSameRequest =
            combat.SessionId == request.SessionId
            && combat.EncounterId == request.EncounterId
            && combat.EncounterType == request.EncounterType
            && combat.RunId == request.RunId
            && requestedMobs.SetEquals(combatMobs);

        if (!isSameRequest)
        {
            throw new IdempotencyKeyReuseException(request.CommandId);
        }
    }
}
