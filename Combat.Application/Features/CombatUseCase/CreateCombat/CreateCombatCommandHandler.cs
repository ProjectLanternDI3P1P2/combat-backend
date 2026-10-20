using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Application.Services;
using Combat.Domain.Entities;
using Combat.Domain.Repositories;
using Combat.Domain.Services;
using MediatR;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatCommandHandler(
    ICombatInstanceRepository combatInstanceRepository,
    ISessionHeroesClient sessionHeroesClient,
    IEncounterContextClient encounterContextClient,
    IHeroFighterInitializer heroFighterInitializer,
    IMonsterDefinitionRepository monsterDefinitionRepository,
    IOptions<MonsterGenerationOptions> options,
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
            if (combatOfEncounter.SessionId != request.SessionId)
            {
                throw new IdempotencyKeyReuseException(request.CommandId);
            }

            logger.Information(
                "Encounter {EncounterId} already produced combat {CombatId}.",
                request.EncounterId,
                combatOfEncounter.CombatId
            );
            return CreateCombatResult.From(combatOfEncounter);
        }

        List<Guid> heroIds = await GetHeroIdsAsync(request, cancellationToken);
        EncounterContext encounter = await GetEncounterContextAsync(request, cancellationToken);

        List<Fighter> fighters = [];
        foreach (Guid heroId in heroIds)
        {
            fighters.Add(await InitializeHeroAsync(heroId, cancellationToken));
        }

        IReadOnlyList<MonsterDefinition> definitions =
            await monsterDefinitionRepository.GetByClassAsync(
                request.EncounterType.ToMonsterClass(),
                cancellationToken
            );
        if (definitions.Count == 0)
        {
            throw new InvalidCombatEncounterException(
                $"no monster definition is configured for the '{request.EncounterType}' encounter type."
            );
        }

        for (int index = 0; index < encounter.MonsterQuantity; index++)
        {
            MonsterDefinition definition = definitions[Random.Shared.Next(definitions.Count)];
            fighters.Add(
                MonsterFighterFactory.Create(
                    definition,
                    encounter.Level,
                    heroIds.Count,
                    options.Value
                )
            );
        }

        CombatInstance combat = CombatInstance.Create(
            Guid.NewGuid(),
            request.SessionId,
            request.EncounterId,
            request.CommandId,
            request.EncounterType,
            fighters,
            clock.UtcNow
        );

        await combatInstanceRepository.AddAsync(combat, cancellationToken);

        logger.Information(
            "Combat {CombatId} created for session {SessionId} from encounter {EncounterId} ({EncounterType}): {HeroCount} hero(s), {MonsterCount} monster(s).",
            combat.CombatId,
            combat.SessionId,
            combat.EncounterId,
            combat.EncounterType,
            heroIds.Count,
            encounter.MonsterQuantity
        );

        return CreateCombatResult.From(combat);
    }

    private async Task<List<Guid>> GetHeroIdsAsync(
        CreateCombatCommand request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Guid> heroIds = await sessionHeroesClient.GetHeroIdsAsync(
            request.SessionId,
            cancellationToken
        );

        List<Guid> distinctHeroIds = [.. heroIds.Distinct()];

        if (distinctHeroIds.Count == 0)
        {
            throw new NoCombatParticipantsException(request.SessionId);
        }

        return distinctHeroIds;
    }

    private async Task<EncounterContext> GetEncounterContextAsync(
        CreateCombatCommand request,
        CancellationToken cancellationToken
    )
    {
        EncounterContext encounter = await encounterContextClient.GetAsync(
            request.SessionId,
            request.EncounterType,
            cancellationToken
        );

        if (encounter.EncounterType != request.EncounterType)
        {
            throw new InvalidCombatEncounterException(
                "the encounter context does not match the requested encounter type."
            );
        }

        if (
            encounter.MonsterQuantity <= 0
            || encounter.MonsterQuantity > options.Value.MaxMonstersPerEncounter
        )
        {
            throw new InvalidCombatEncounterException(
                $"the encounter requires an invalid number of monsters ({encounter.MonsterQuantity})."
            );
        }

        if (encounter.Level <= 0 || encounter.Level > options.Value.MaxFloor)
        {
            throw new InvalidCombatEncounterException(
                $"the encounter requires an invalid level ({encounter.Level})."
            );
        }

        return encounter;
    }

    private async Task<Fighter> InitializeHeroAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await heroFighterInitializer.InitializeAsync(heroId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(
                exception,
                "Combat creation failed: hero {HeroId} could not be initialized.",
                heroId
            );
            throw;
        }
    }

    private static void EnsureSameRequest(CombatInstance combat, CreateCombatCommand request)
    {
        bool isSameRequest =
            combat.SessionId == request.SessionId
            && combat.EncounterId == request.EncounterId
            && combat.EncounterType == request.EncounterType;

        if (!isSameRequest)
        {
            throw new IdempotencyKeyReuseException(request.CommandId);
        }
    }
}
