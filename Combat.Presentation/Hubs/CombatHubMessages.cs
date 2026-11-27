using Combat.Application.Features.CombatUseCase;
using Combat.Domain.Enums;
using Combat.Domain.ValueObjects;

namespace Combat.Presentation.Hubs;

public sealed record JoinCombatRequest(Guid CombatId);

/// <param name="Type">"attack", "ability", "defend", "flee" or "pass".</param>
/// <param name="AbilityCode">Code of the ability, for "ability".</param>
public sealed record SubmitActionRequest(
    Guid CommandId,
    Guid CombatId,
    Guid ActorId,
    string Type,
    Guid? TargetId,
    string? AbilityCode
);

public sealed record PassTurnRequest(Guid CommandId, Guid CombatId, Guid ActorId);

public static class CombatCommandErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "notFound";
    public const string Refused = "refused";
    public const string Conflict = "conflict";
    public const string Internal = "internal";
}

public sealed record CombatCommandError(string Code, string Message);

/// <summary>Explicit acknowledgement of every Hub call.</summary>
/// <param name="Events">Events produced by this command only, for the client to animate.</param>
public sealed record CombatCommandResult(
    Guid? CommandId,
    bool Accepted,
    CombatCommandError? Error,
    CombatSnapshotDto? Snapshot,
    IReadOnlyList<CombatEventDto> Events
)
{
    public static CombatCommandResult Accept(
        Guid? commandId,
        CombatSnapshotDto snapshot,
        IReadOnlyList<CombatEventDto>? events = null
    ) => new(commandId, true, null, snapshot, events ?? []);

    public static CombatCommandResult Refuse(Guid? commandId, string code, string message) =>
        new(commandId, false, new CombatCommandError(code, message), null, []);
}

/// <summary>Combat snapshot sent to clients; field names are the wire contract.</summary>
public sealed record CombatSnapshotDto(
    Guid Id,
    Guid RunId,
    Guid EncounterId,
    string Trigger,
    string Status,
    string? Result,
    bool CanFlee,
    int Floor,
    CombatTurnDto? Turn,
    IReadOnlyList<Guid> UpcomingTurns,
    IReadOnlyList<CombatFighterDto> Fighters,
    IReadOnlyList<CombatEventDto> Log
)
{
    public static CombatSnapshotDto From(CombatView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new CombatSnapshotDto(
            view.CombatId,
            view.RunId,
            view.EncounterId,
            view.EncounterType == EncounterType.Boss ? "boss" : "collision",
            view.Status == CombatStatus.Completed ? "finished" : "active",
            view.Result switch
            {
                CombatResult.Victory => "victory",
                CombatResult.Defeat => "defeat",
                CombatResult.Fled => "fled",
                _ => null,
            },
            view.CanFlee,
            view.Floor,
            view.ActiveFighterId is { } actorId
                ? new CombatTurnDto(
                    view.Turn,
                    view.Round,
                    actorId,
                    view.TurnDeadline,
                    view.TurnDurationSeconds
                )
                : null,
            view.UpcomingTurns,
            [.. view.Fighters.Select(CombatFighterDto.From)],
            [.. view.Events.Select(CombatEventDto.From)]
        );
    }
}

/// <param name="DeadlineUtc">Server deadline of the hero's turn: the server clock decides.</param>
public sealed record CombatTurnDto(
    int Number,
    int Round,
    Guid ActorId,
    DateTimeOffset? DeadlineUtc,
    int TimeMaxSeconds
);

public sealed record CombatFighterDto(
    Guid Id,
    string Side,
    string Type,
    Guid ExternalId,
    string Name,
    int Level,
    int CurrentHp,
    int MaxHp,
    int CurrentMana,
    int MaxMana,
    int Attack,
    int Defense,
    int Speed,
    string State,
    bool IsDefending,
    IReadOnlyList<CombatAbilityDto> Abilities,
    CombatVisualDto Visual
)
{
    public static CombatFighterDto From(CombatFighterView fighter)
    {
        string side = fighter.Type == FighterType.Hero ? "hero" : "monster";
        return new CombatFighterDto(
            fighter.FighterId,
            side,
            side,
            fighter.ExternalId,
            fighter.Name,
            fighter.Level,
            fighter.CurrentHp,
            fighter.MaxHp,
            fighter.CurrentMana,
            fighter.MaxMana,
            fighter.Attack,
            fighter.Defense,
            fighter.Speed,
            fighter.State == FighterState.KnockedOut ? "outOfCombat" : "active",
            fighter.IsDefending,
            [
                .. fighter.Abilities.Select(ability => new CombatAbilityDto(
                    ability.Code,
                    ability.Name,
                    ability.ManaCost,
                    ability.Target.ToString(),
                    ability.Power
                )),
            ],
            CombatVisualDto.From(fighter.Visual)
        );
    }
}

public sealed record CombatAbilityDto(
    string Code,
    string Name,
    int ManaCost,
    string TargetType,
    int Power
);

/// <summary>Codes only, never an image: the client owns the catalogues.</summary>
public sealed record CombatVisualDto(
    string Kind,
    string? ClassCode,
    CombatAppearanceDto? Appearance,
    string? SpriteKey,
    bool IsBoss
)
{
    public static CombatVisualDto From(FighterVisual visual) =>
        new(
            visual.Kind,
            visual.ClassCode,
            visual.HasAppearance
                ? new CombatAppearanceDto(
                    visual.SkinTone!.Value,
                    visual.HairStyle!,
                    visual.HairColor
                )
                : null,
            visual.SpriteKey,
            visual.IsBoss
        );
}

public sealed record CombatAppearanceDto(int SkinTone, string HairStyle, int? HairColor);

/// <param name="Type">camelCase event type, e.g. "attacked", "damaged", "knockedOut".</param>
public sealed record CombatEventDto(
    int Sequence,
    int Turn,
    string Type,
    Guid? ActorId,
    Guid? TargetId,
    int Amount,
    string? Label,
    bool IsCritical
)
{
    public static CombatEventDto From(FightEvent fightEvent)
    {
        string name = fightEvent.Type.ToString();
        return new CombatEventDto(
            fightEvent.Sequence,
            fightEvent.Turn,
            char.ToLowerInvariant(name[0]) + name[1..],
            fightEvent.ActorId,
            fightEvent.TargetId,
            fightEvent.Amount,
            fightEvent.Label,
            fightEvent.IsCritical
        );
    }
}
