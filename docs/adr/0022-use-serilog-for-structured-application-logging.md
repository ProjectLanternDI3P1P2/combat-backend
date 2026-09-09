# Use Serilog for structured application logging

Backend services use Serilog for structured application logging.

## Considered Options

The built-in .NET logging abstractions remain available, but a concrete logging
provider is still required for configuration and output.

Serilog provides straightforward structured logging configuration and a mature
sink ecosystem. Logs can be routed to the observability stack selected by
operations, including systems such as Loki or OpenTelemetry-compatible
collectors.

This keeps application logging consistent without binding business code to a
specific log storage backend.

## Consequences

Log messages use structured properties rather than string interpolation.

Sensitive values such as passwords, secrets and tokens must not be logged.

Operational destinations can evolve without changing the logging model used by
application code.
