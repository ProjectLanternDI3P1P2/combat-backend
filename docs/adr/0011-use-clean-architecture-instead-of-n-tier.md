# Use Clean Architecture instead of N-tier architecture

Backend microservices use Clean Architecture with Domain, Application,
Infrastructure and Presentation projects.

## Considered Options

A traditional N-tier architecture was considered because it is simpler to start
and familiar to many developers.

Clean Architecture was selected because dependencies point toward the business
core instead of letting domain behavior depend on persistence or presentation
details. It provides stronger separation of concerns, better testability and
clearer boundaries for technical implementations.

The explicit structure is also important because developers rotate between
squads. A predictable architecture reduces the time required to understand a
service after returning to it.

## Consequences

Domain code remains independent from ASP.NET Core, EF Core and other technical
implementations.

Use cases are isolated in Application, technical implementations live in
Infrastructure, and HTTP concerns stay in Presentation.

The architecture introduces more projects and interfaces than a simple N-tier
application, but provides a common navigation model for all squads.
