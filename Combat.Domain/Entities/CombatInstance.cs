using Combat.Domain.Enums;

namespace Combat.Domain.Entities;

/// <summary>
/// A combat instance. It owns the temporary state of every participant for the duration
/// of the confrontation and never writes that state back to the owning services.
/// </summary>
public sealed class CombatInstance
{
    private readonly List<Fighter> _fighters;
    private readonly IReadOnlyList<Fighter> _readOnlyFighters;

    private CombatInstance(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        CombatStatus status,
        DateTimeOffset startedAt,
        IEnumerable<Fighter> fighters
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(fighters));
        ArgumentNullException.ThrowIfNull(fighters);

        _fighters = [.. fighters];
        _readOnlyFighters = _fighters.AsReadOnly();

        CombatId = combatId;
        SessionId = sessionId;
        EncounterId = encounterId;
        CommandId = commandId;
        EncounterType = encounterType;
        Status = status;
        StartedAt = startedAt;
    }

    public Guid CombatId { get; }

    /// <summary>Game session the combat belongs to. One combat belongs to exactly one session.</summary>
    public Guid SessionId { get; }

    /// <summary>Encounter that caused the combat. The same encounter never creates two combats.</summary>
    public Guid EncounterId { get; }

    /// <summary>Identifier of the command that created the combat. It guarantees the replay safety.</summary>
    public Guid CommandId { get; }

    public EncounterType EncounterType { get; }
    public CombatStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public IReadOnlyList<Fighter> Fighters => _readOnlyFighters;

    public static CombatInstance Create(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        IEnumerable<Fighter> fighters,
        DateTimeOffset startedAt
    )
    {
        EnsureIdentifiers(combatId, sessionId, encounterId, commandId);

        List<Fighter> participants = [.. fighters];

        if (participants.Count == 0)
        {
            throw new ArgumentException(
                "A combat requires at least one participant.",
                nameof(fighters)
            );
        }

        if (participants.All(fighter => fighter.Type != FighterType.Hero))
        {
            throw new ArgumentException("A combat requires at least one hero.", nameof(fighters));
        }

        if (participants.All(fighter => fighter.Type != FighterType.Monster))
        {
            throw new ArgumentException(
                "A combat requires at least one monster.",
                nameof(fighters)
            );
        }

        if (
            participants.Select(fighter => fighter.FighterId).Distinct().Count()
            != participants.Count
        )
        {
            throw new ArgumentException(
                "A combat cannot contain the same fighter twice.",
                nameof(fighters)
            );
        }

        return new CombatInstance(
            combatId,
            sessionId,
            encounterId,
            commandId,
            encounterType,
            CombatStatus.Active,
            startedAt,
            participants
        );
    }

    /// <summary>Rebuilds a combat from its persisted state.</summary>
    public static CombatInstance Rehydrate(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId,
        EncounterType encounterType,
        CombatStatus status,
        DateTimeOffset startedAt,
        IEnumerable<Fighter> fighters
    )
    {
        EnsureIdentifiers(combatId, sessionId, encounterId, commandId);

        return new CombatInstance(
            combatId,
            sessionId,
            encounterId,
            commandId,
            encounterType,
            status,
            startedAt,
            fighters
        );
    }

    private static void EnsureIdentifiers(
        Guid combatId,
        Guid sessionId,
        Guid encounterId,
        Guid commandId
    )
    {
        if (combatId == Guid.Empty)
        {
            throw new ArgumentException("The combat identifier is required.", nameof(combatId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("The session identifier is required.", nameof(sessionId));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException(
                "The encounter identifier is required.",
                nameof(encounterId)
            );
        }

        if (commandId == Guid.Empty)
        {
            throw new ArgumentException("The command identifier is required.", nameof(commandId));
        }
    }
}
