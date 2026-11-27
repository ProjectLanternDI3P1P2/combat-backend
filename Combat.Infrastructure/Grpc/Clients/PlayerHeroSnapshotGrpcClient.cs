extern alias PlayerContracts;

using Combat.Application.Exceptions;
using Combat.Application.Models.HeroCombat;
using Combat.Application.Ports;
using Combat.Infrastructure.Grpc.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Options;
using PlayerContracts::Player.Contracts.V1;

namespace Combat.Infrastructure.Grpc.Clients;

/// <summary>Reads the combatant snapshot of a hero from the Player service (internal gRPC).</summary>
public sealed class PlayerHeroSnapshotGrpcClient(
    PlayerHeroService.PlayerHeroServiceClient client,
    IOptions<PlayerGrpcClientOptions> options
) : IHeroSnapshotClient
{
    private const string ServiceName = "Player";

    public async Task<HeroCombatSnapshot> GetCombatantSnapshotAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        CombatantSnapshotReply reply;
        try
        {
            reply = await client.GetCombatantSnapshotAsync(
                new GetCombatantSnapshotRequest { HeroId = heroId.ToString() },
                new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds),
                    cancellationToken: cancellationToken
                )
            );
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            throw new HeroNotFoundException(heroId, exception);
        }
        catch (RpcException exception)
            when (exception.StatusCode
                    is StatusCode.Unavailable
                        or StatusCode.DeadlineExceeded
                        or StatusCode.Internal
                        or StatusCode.Unknown
            )
        {
            throw new ExternalServiceUnavailableException(ServiceName, exception);
        }

        if (!Guid.TryParse(reply.HeroId, out Guid receivedHeroId) || receivedHeroId != heroId)
        {
            throw new InvalidHeroCombatDataException(
                heroId,
                ["Player did not return the requested hero identifier."]
            );
        }

        if (reply.Level < 1)
        {
            throw new InvalidHeroCombatDataException(heroId, ["Player returned an invalid level."]);
        }

        return new HeroCombatSnapshot
        {
            HeroId = receivedHeroId,
            Name = reply.Name,
            Level = reply.Level,
            CurrentHp = reply.CurrentHp,
            MaxHp = reply.MaxHp,
            CurrentMana = reply.CurrentMana,
            MaxMana = reply.MaxMana,
            Stats = new HeroCombatStats
            {
                Attack = reply.Attack,
                Defense = reply.Defense,
                Speed = reply.Speed,
            },
            Abilities =
            [
                .. reply.Abilities.Select(ability => new HeroAbility
                {
                    Code = ability.Code,
                    Name = ability.Name,
                    ManaCost = ability.ManaCost,
                    TargetType = ability.TargetType,
                    Power = ability.Power,
                }),
            ],
            ClassCode = reply.ClassCode,
            Appearance = reply.Appearance is null
                ? null
                : new HeroAppearanceSnapshot(
                    reply.Appearance.SkinTone,
                    reply.Appearance.HairStyle,
                    reply.Appearance.HasHairColor ? reply.Appearance.HairColor : null
                ),
        };
    }
}
