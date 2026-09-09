# Do not use shared backend libraries between microservices

Backend microservices do not share runtime code through common application,
domain or technical libraries.

Shared conventions are allowed; shared implementation is not.

## Considered Options

A shared package could reduce duplication for common abstractions and technical
helpers.

It would also couple service evolution, create synchronized package updates and
turn common code into a coordination point for roughly thirty developers.

The project explicitly favors independent microservices. A service must remain
free to evolve differently when its own requirements change.

## Consequences

Some technical or structural code may be duplicated between repositories.

That duplication is accepted in exchange for independent evolution and reduced
cross-service coupling.

No shared backend library should be introduced later without superseding this ADR
with a new project-wide decision.
