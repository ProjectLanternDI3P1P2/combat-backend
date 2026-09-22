# Structure application use cases with CQRS and MediatR

Application behavior is organized as explicit commands and queries handled
through MediatR.

The primary goal is to structure use cases, not to introduce separate read and
write data stores.

## Considered Options

Large shared application services were rejected because they accumulate unrelated
behavior and increase navigation cost when developers move between squads.

CQRS gives each use case an explicit entry point. MediatR integrates naturally
with that organization, resolves handler dependencies through dependency injection
and provides pipeline behaviors for cross-cutting concerns.

The project uses behaviors for concerns that apply consistently to handlers,
including validation, logging and command transactions.

## Consequences

Commands modify state and queries read state.

One handler represents one clear application use case.

Successful commands are committed by a transaction behavior, so repositories and
handlers do not independently decide when to call `SaveChangesAsync`.

The application contains more small types than a service-class approach, but use
cases and cross-cutting behavior remain explicit and consistent.
