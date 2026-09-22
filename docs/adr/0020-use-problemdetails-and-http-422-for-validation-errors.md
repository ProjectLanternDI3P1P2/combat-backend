# Use ProblemDetails and HTTP 422 for validation errors

HTTP errors use ASP.NET Core ProblemDetails.

Request validation failures return HTTP 422 Unprocessable Entity with structured
field errors.

## Considered Options

A custom error response would require the project to maintain a contract already
covered by a standard ASP.NET and HTTP representation.

HTTP 400 could represent invalid input, but the application benefits from
distinguishing malformed requests from syntactically valid requests that fail
validation rules.

HTTP 422 expresses that distinction directly.

## Consequences

Clients receive one consistent error model across microservices.

Validation responses expose an `errors` dictionary mapping field names to one or
more validation messages.

Validation failures can be distinguished from generic bad requests by status
code, and exception-to-HTTP mapping remains centralized.
