using Combat.Application.Features.CombatUseCase.CreateCombat;
using Combat.Contracts.V1;
using Combat.Domain.ValueObjects;
using Grpc.Core;
using MediatR;
using AppMonster = Combat.Application.Features.CombatUseCase.CreateCombat.EncounterMonster;
using AppParticipant = Combat.Application.Features.CombatUseCase.CreateCombat.EncounterParticipant;
using ContractAbility = Combat.Contracts.V1.CombatAbility;
using ContractEncounterType = Combat.Contracts.V1.EncounterType;
using DomainEncounterType = Combat.Domain.Enums.EncounterType;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Grpc.Services;

/// <summary>
/// Internal gRPC surface used by the Dungeon service to open a combat.
/// REST remains the public client-facing API.
/// </summary>
public sealed class CombatGrpcService(IMediator mediator, ILogger logger)
    : CombatService.CombatServiceBase
{
    public override async Task<CreateCombatResponse> CreateCombat(
        CreateCombatRequest request,
        ServerCallContext context
    )
    {
        Guid sessionId = ParseGuid(request.SessionId, "session_id");
        Guid encounterId = ParseGuid(request.EncounterId, "encounter_id");
        Guid commandId = ParseGuid(request.CommandId, "command_id");
        DomainEncounterType encounterType = ParseEncounterType(request.EncounterType);
        Guid runId = ParseGuid(request.RunId, "run_id");
        List<AppParticipant> participants =
        [
            .. request.Participants.Select(participant => new AppParticipant(
                ParseGuid(participant.HeroId, "participants.hero_id"),
                participant.HasCurrentHp ? participant.CurrentHp : null,
                participant.HasCurrentMana ? participant.CurrentMana : null
            )),
        ];
        List<AppMonster> monsters =
        [
            .. request.Monsters.Select(monster => new AppMonster(
                ParseGuid(monster.ElementId, "monsters.element_id"),
                monster.IsBoss
            )),
        ];

        logger.Information(
            "gRPC combat creation requested for run {RunId}, encounter {EncounterId}: {HeroCount} hero(s), {MonsterCount} monster(s).",
            runId,
            encounterId,
            participants.Count,
            monsters.Count
        );

        CreateCombatResult result = await mediator.Send(
            new CreateCombatCommand(
                sessionId,
                encounterId,
                commandId,
                encounterType,
                runId,
                request.Floor,
                request.RoomId,
                participants,
                monsters
            ),
            context.CancellationToken
        );

        logger.Information(
            "gRPC combat creation succeeded for combat {CombatId} in session {SessionId}.",
            result.CombatId,
            result.SessionId
        );

        var response = new CreateCombatResponse
        {
            CombatId = result.CombatId.ToString(),
            SessionId = result.SessionId.ToString(),
            EncounterId = result.EncounterId.ToString(),
            EncounterType = ToContractEncounterType(result.EncounterType),
            Status =
                result.Status == Combat.Domain.Enums.CombatStatus.Completed ? "finished" : "active",
            RunId = result.RunId.ToString(),
        };

        foreach (CombatParticipantResult participant in result.Participants)
        {
            var contractParticipant = new CombatParticipant
            {
                FighterId = participant.FighterId.ToString(),
                Type = participant.Type.ToString(),
                ExternalId = participant.ExternalId.ToString(),
                Name = participant.Name,
                Level = participant.Level,
                CurrentHp = participant.CurrentHp,
                MaxHp = participant.MaxHp,
                CurrentMana = participant.CurrentMana,
                MaxMana = participant.MaxMana,
                Attack = participant.Attack,
                Defense = participant.Defense,
                Speed = participant.Speed,
                State = participant.State.ToString(),
                ClassCode = participant.Visual.ClassCode ?? string.Empty,
                SpriteKey = participant.Visual.SpriteKey ?? string.Empty,
                IsBoss = participant.Visual.IsBoss,
            };

            foreach (FighterAbility ability in participant.Abilities)
            {
                contractParticipant.Abilities.Add(
                    new ContractAbility
                    {
                        AbilityId = ability.Code,
                        Name = ability.Name,
                        ManaCost = ability.ManaCost,
                        TargetType = ability.TargetType,
                        Power = ability.Power,
                    }
                );
            }

            foreach (FighterEffect effect in participant.Effects)
            {
                var contractEffect = new CombatEffect
                {
                    EffectId = effect.EffectId.ToString(),
                    Name = effect.Name,
                    Kind = effect.Kind.ToString(),
                };

                if (effect.RemainingTurns is not null)
                {
                    contractEffect.RemainingTurns = effect.RemainingTurns.Value;
                }

                contractParticipant.Effects.Add(contractEffect);
            }

            response.Participants.Add(contractParticipant);
        }

        return response;
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        if (!Guid.TryParse(value, out Guid parsed))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, $"{fieldName} must be a valid UUID.")
            );
        }

        return parsed;
    }

    private static DomainEncounterType ParseEncounterType(ContractEncounterType encounterType)
    {
        return encounterType switch
        {
            ContractEncounterType.Enemy => DomainEncounterType.Enemy,
            ContractEncounterType.SubBoss => DomainEncounterType.SubBoss,
            ContractEncounterType.Boss => DomainEncounterType.Boss,
            _ => throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    "encounter_type is not a known encounter type."
                )
            ),
        };
    }

    private static ContractEncounterType ToContractEncounterType(DomainEncounterType encounterType)
    {
        return encounterType switch
        {
            DomainEncounterType.Enemy => ContractEncounterType.Enemy,
            DomainEncounterType.SubBoss => ContractEncounterType.SubBoss,
            DomainEncounterType.Boss => ContractEncounterType.Boss,
            _ => ContractEncounterType.Unspecified,
        };
    }
}
