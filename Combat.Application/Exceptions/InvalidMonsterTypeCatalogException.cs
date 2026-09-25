namespace Combat.Application.Exceptions;

public sealed class InvalidMonsterTypeCatalogException(
    IReadOnlyDictionary<string, string[]> errors)
    : Exception("The monster type source returned an invalid catalog.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
