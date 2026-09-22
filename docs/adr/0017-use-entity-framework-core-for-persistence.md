# Use Entity Framework Core for persistence

Entity Framework Core is the standard persistence technology for backend
microservices.

## Considered Options

No alternative persistence library was seriously considered.

For this type of application, using an ORM avoids unnecessary manual persistence
code. EF Core is the native, mature and well-supported ORM in the .NET ecosystem
and is already familiar to the development team.

It provides LINQ, change tracking, mapping, migrations and relational database
support without introducing another persistence model.

## Consequences

EF Core-specific code stays in Infrastructure.

Repositories expose domain-specific persistence operations and do not call
`SaveChangesAsync`; command persistence is committed by the MediatR transaction
behavior.

Each microservice owns its own DbContext and migrations.

Direct SQL remains possible for exceptional cases, but does not replace EF Core
as the default persistence approach.
