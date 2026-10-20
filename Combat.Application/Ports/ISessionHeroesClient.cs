namespace Combat.Application.Ports;

/// <summary>Application port for the Player service operation <c>GetSessionHeroes</c>.</summary>
public interface ISessionHeroesClient
{
    /// <summary>Returns the identifiers of every hero belonging to the session.</summary>
    /// <exception cref="Exceptions.SessionNotFoundException">The session does not exist.</exception>
    /// <exception cref="Exceptions.ExternalServiceUnavailableException">The Player service cannot be reached.</exception>
    Task<IReadOnlyList<Guid>> GetHeroIdsAsync(Guid sessionId, CancellationToken cancellationToken);
}
