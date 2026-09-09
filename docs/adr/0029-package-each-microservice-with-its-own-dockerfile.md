# Package each microservice with its own Dockerfile

Every backend microservice owns its own Dockerfile.

## Considered Options

A shared backend image or common container definition could reduce duplication
between services.

Microservices must remain independently buildable and deployable, and may develop
different runtime or packaging requirements over time even when they start from a
similar baseline.

Keeping the Dockerfile with the service preserves that autonomy.

## Consequences

Each service can evolve its container build independently.

Container configuration may be duplicated between repositories.

The shared template provides the initial Dockerfile, but existing services do not
depend on later template updates.
