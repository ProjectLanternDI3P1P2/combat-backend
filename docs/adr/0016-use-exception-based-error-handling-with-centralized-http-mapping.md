# Use exception-based error handling with centralized HTTP mapping

Domain, application and technical failures use the normal .NET exception model,
and HTTP error responses are mapped centrally by Presentation middleware.

## Considered Options

A `Result<T>` pattern was considered but rejected.

Using Result types would introduce a second error model alongside exceptions and
require every operation to explicitly choose and propagate result variants.
Nobody in the development group currently uses that pattern as a standard.

Exceptions are the familiar .NET mechanism for both business rejections and
technical failures, while middleware provides one place to translate them into
HTTP responses.

## Consequences

Handlers remain focused on their normal execution path.

Controllers do not duplicate `try/catch` blocks or HTTP error mapping.

The middleware must maintain a clear mapping between known exceptions and
ProblemDetails responses, while unexpected exceptions are logged and returned as
generic server errors.
