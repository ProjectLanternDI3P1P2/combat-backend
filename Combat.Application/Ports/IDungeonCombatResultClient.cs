using Combat.Domain.Enums;

namespace Combat.Application.Ports;

/// <summary>Application port for the Dungeon service operation <c>ReportCombatResult</c>.</summary>
public interface IDungeonCombatResultClient
{
    /// <returns>The run status Dungeon reports back ("active", "won" or "lost").</returns>
    /// <exception cref="Exceptions.ExternalServiceUnavailableException">Dungeon cannot be reached.</exception>
    Task<string> ReportAsync(CombatResultReport report, CancellationToken cancellationToken);
}

public sealed record CombatResultReport(
    Guid CommandId,
    Guid CombatId,
    Guid RunId,
    Guid EncounterId,
    CombatResult Result,
    IReadOnlyList<HeroFinalState> Heroes,
    IReadOnlyList<Guid> DefeatedElementIds
);

public sealed record HeroFinalState(Guid HeroId, int CurrentHp, int CurrentMana, bool KnockedOut);
