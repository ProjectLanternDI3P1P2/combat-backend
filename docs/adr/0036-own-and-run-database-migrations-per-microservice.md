# Own and run database migrations per microservice

Each microservice owns and versions its Entity Framework Core migrations. The
shared template contains no migration files because its generated service must
create an initial migration for its own domain model.

Development startup and integration-test fixtures apply migrations. Deployed
environments use a dedicated migration execution mechanism or service.

## Considered Options

Shipping an initial migration in a template makes a generated service inherit a
schema history that is unrelated to its actual domain. Skipping migrations after
the service is created would make schema evolution unsafe and non-repeatable.

Sharing migrations between services would contradict database ownership and
couple independently evolving schemas.

A dedicated migration execution step keeps schema changes explicit and separate
from serving application traffic.

## Consequences

Migration files live with the persistence code of the microservice that owns the
database. Production-like deployments must execute them before or as a controlled
part of rollout.
