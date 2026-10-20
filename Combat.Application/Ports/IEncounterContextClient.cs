using Combat.Application.Models;
using Combat.Domain.Enums;

namespace Combat.Application.Ports;

/// <summary>Application port for the Dungeon service operation <c>GetEncounterContext</c>.</summary>
public interface IEncounterContextClient
{
    /// <summary>Returns the quantity of monsters and the level required by the encounter.</summary>
    /// <exception cref="Exceptions.ExternalServiceUnavailableException">The Dungeon service cannot be reached.</exception>
    Task<EncounterContext> GetAsync(
        Guid sessionId,
        EncounterType encounterType,
        CancellationToken cancellationToken
    );
}
