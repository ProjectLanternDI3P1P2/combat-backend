# ADR-GLOB-007 — Ownership and Distribution of Service Contracts

**Status:** Accepted

## Context
gRPC and RabbitMQ require contracts shared between producers and consumers.
Manually duplicating `.proto` files risks divergence between repositories.
A single global contracts repository would weaken service ownership.

## Decision
Protocol Buffers SHALL be used for both gRPC and RabbitMQ contracts.
Each microservice SHALL own the contracts it exposes or publishes.

Each service SHALL build a versioned NuGet contract package, for example `Reward.Contracts`.
The package MAY contain generated C# types from the owned `.proto` definitions.
Consumers SHALL explicitly reference a released version of the producer contract package.

There SHALL NOT be a single centralized repository containing all service contracts.
There SHALL initially be no schema registry for asynchronous message contracts.
Contracts SHALL remain independent from internal Domain, Application and Persistence models.
A microservice SHALL NOT reference the Domain or Application project of another microservice.

Contracts SHALL evolve backward-compatibly whenever possible.
Protocol Buffer field numbers SHALL NOT be reused.
Removed field numbers SHOULD be reserved.
Every released contract change SHALL create the next explicit contract version:
V1, V2, V3, and so on. The corresponding NuGet version and Git tag SHALL be
`N.0.0` and `contracts-vN.0.0`. Contract versions are independent from the
application version and are incremented only for changes under the contract
package directory. Contract packages SHOULD be published through CI/CD.

## Consequences
- Each producer remains the source of truth for its contracts.
- Consumers can upgrade independently.
- Manual `.proto` synchronization is avoided.
- Internal NuGet package publication must be maintained.
- Without Schema Registry, compatibility relies on development and CI practices.

## Alternatives Considered
- Manual `.proto` duplication: rejected because copies can diverge.
- Central contracts repository: rejected because it creates shared ownership.
- Schema Registry: deferred because it is not currently required.
