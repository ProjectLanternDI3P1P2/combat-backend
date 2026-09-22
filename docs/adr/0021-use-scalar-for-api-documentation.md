# Use Scalar for API documentation

Backend HTTP APIs use Scalar as the interactive API documentation interface.

## Considered Options

Swagger UI was the previous common choice in ASP.NET Core projects.

The .NET 10 API tooling used by the project no longer depends on Swagger UI as
the default interactive documentation experience, and Scalar provides a modern
interface over the generated OpenAPI contract.

Introducing another documentation UI would add configuration without a project
need.

## Consequences

Each service exposes its OpenAPI contracts through Scalar.

Developers use the same documentation interface across all backend repositories.

This ADR concerns the documentation UI only; OpenAPI remains the underlying API
description format.
