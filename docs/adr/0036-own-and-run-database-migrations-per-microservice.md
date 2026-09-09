# Own and run database migrations per microservice

Each microservice owns and versions its own Entity Framework Core migrations.

Developers create migrations with EF Core tooling when the service data model
changes.

Application startup does not automatically execute `Database.Migrate()` outside
development environments.

Deployed environments use a dedicated migration execution mechanism or service.

## Considered Options

Running migrations automatically when every application instance starts is simple,
but it couples normal startup to schema mutation and becomes unsafe when several
instances start concurrently.

Sharing migrations between services would contradict database ownership and
couple independently evolving schemas.

A dedicated migration execution step keeps schema changes explicit and separate
from serving application traffic.

## Consequences

Migration files live with the persistence code of the microservice that owns the
database.

Production-like deployments must execute migrations before or as a controlled
part of rollout.

Each service can evolve its schema independently without coordinating a shared
migration project.
