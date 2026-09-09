# Use one repository per microservice

Each backend microservice is maintained in its own Git repository.

## Considered Options

A monorepo would make all services immediately available to developers and AI
agents in one working tree, which can simplify cross-service changes.

The project is organized around separate squads with regular developer rotation.
Most work should affect one microservice at a time, and cross-service changes are
intentionally kept to a minimum.

Separate repositories provide clearer CI/CD pipelines, ownership and change
history, while reducing the temptation for developers or AI agents to modify
unrelated services.

## Consequences

Each squad can work primarily in the repository of the microservice it owns for
the current feature.

CI/CD pipelines and service history remain independent.

Changes that genuinely span multiple services require coordinated pull requests,
which is accepted as preferable to making cross-service modifications routine.
