# ADR-GLOB-005 — Distributed Data Consistency Strategy

**Status:** Accepted

## Context
Each microservice owns its own database.
Some operations require strong consistency while others only trigger independent side effects.
Distributed database transactions would increase coupling and operational complexity.

Current strongly consistent operations such as purchases and player trades are owned entirely by Reward.
They can therefore use a single local PostgreSQL transaction.

## Decision
Strong consistency SHALL use local database transactions whenever possible.
State that must change atomically SHOULD belong to the same microservice when consistent with domain boundaries.

Microservices SHALL NOT use distributed database transactions or two-phase commit.
Cross-service side effects that do not require immediate atomic consistency SHALL use RabbitMQ and eventual consistency.

A Saga SHALL NOT be introduced by default.
A Saga MAY be introduced when a business workflow:
- modifies state owned by several microservices;
- requires an overall business outcome;
- cannot be redesigned as a local transaction;
- requires compensation after partial failure.

Reservation and confirmation SHOULD be preferred over destructive actions followed by compensation when appropriate.

## Consequences
- Strongly consistent operations remain simple ACID transactions.
- Services remain independent.
- Cross-service state may temporarily be inconsistent.
- Future distributed workflows may require explicit compensation logic.

## Alternatives Considered
- Two-phase commit: rejected because of coupling and operational complexity.
- Saga everywhere: rejected because many cross-service effects only require eventual consistency.
