# Keep database migrations out of the shared template

The shared template contains no Entity Framework Core migrations. It uses
`EnsureCreated` only for its Development seed and test databases.

Each service chooses its own production schema-evolution strategy when its
domain model and deployment constraints are known.

## Considered Options

Shipping an initial migration in a template makes a generated service inherit a
schema history that is unrelated to its actual domain.

Sharing migrations between services would contradict database ownership and
couple independently evolving schemas.

A dedicated migration execution step keeps schema changes explicit and separate
from serving application traffic.

## Consequences

Services that adopt migrations must own, version and execute them independently.
`EnsureCreated` is not a production migration strategy and must be replaced
before a persistent production database is introduced.
