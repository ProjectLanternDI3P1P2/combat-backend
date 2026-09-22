# Use distributed-safe identifiers and timestamps

Business and technical identifiers use `Guid` by default.

Dates and timestamps use `DateTimeOffset`.

## Considered Options

Database-generated integer identifiers are simple inside one database, but the
project consists of independently owned microservices that exchange identifiers.

GUIDs can be generated independently without relying on a central sequence and
avoid collisions between services or persistence stores.

`DateTime` can represent timestamps but may omit the originating offset and
introduce ambiguity in distributed exchanges. `DateTimeOffset` makes the offset
explicit.

## Consequences

Services can create identifiers without coordinating with another service or a
shared database.

Persisted and exchanged timestamps retain explicit offset information and should
normally represent UTC.

Specific exceptions require an explicit domain reason.
