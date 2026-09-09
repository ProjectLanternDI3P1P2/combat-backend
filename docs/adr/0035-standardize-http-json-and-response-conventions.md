# Standardize HTTP JSON and response conventions

HTTP APIs use System.Text.Json with camelCase property names, string enum values
and ISO 8601 date/time values.

Successful responses return their DTO directly without a generic response
envelope.

Paginated collection endpoints use `page` and `pageSize`.

## Considered Options

Each squad could configure JSON and response shapes independently, but that would
force clients to handle unnecessary differences between services.

A generic `{ "data": ... }` success envelope was rejected because it adds a
wrapper without providing useful information for normal REST responses.

Offset/limit pagination was also considered, but page/pageSize is more familiar
to the project team and sufficient for expected client-facing collections.

## Consequences

Frontend clients see the same serialization rules across backend services.

Enums remain human-readable in HTTP contracts.

Collection APIs that need pagination expose consistent parameter names.

Errors remain separate and use the ProblemDetails convention.
