namespace Combat.Application.Exceptions;

public sealed class IdempotencyKeyReuseException(Guid idempotencyKey)
    : Exception(
        $"The idempotency key '{idempotencyKey}' was already used with different combat parameters."
    );
