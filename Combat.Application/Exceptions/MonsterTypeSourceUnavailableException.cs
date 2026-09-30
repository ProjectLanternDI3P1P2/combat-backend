namespace Combat.Application.Exceptions;

public sealed class MonsterTypeSourceUnavailableException()
    : Exception("No monster type source is configured.");
