# ADR-GLOB-002 — Kafka as the Asynchronous Message Broker

**Status:** Accepted

## Context
The game is expected to support very high concurrency, including load tests around 100,000 simultaneous players.
Most gameplay requests remain synchronous, but several workflows naturally publish domain events.
The same event may be consumed independently by several microservices.

Examples include combat completion, chest opening, progression updates and reward generation.
The Operations team is able to operate Kafka.
Event replay is not currently a functional requirement.

## Decision
Apache Kafka SHALL be used as the asynchronous messaging platform.
Kafka SHALL primarily transport domain events.
Operations requiring an immediate result SHALL use gRPC instead of Kafka.
Kafka payload contracts SHALL use versioned Protocol Buffers packages as defined by ADR-GLOB-007.

Multiple microservices MAY consume the same event independently through consumer groups.
The architecture SHALL assume at-least-once processing semantics.
Consumers SHALL therefore be idempotent when duplicate processing could alter business state.

No global event ordering SHALL be required.
Ordering MAY be preserved for a business entity using an appropriate partition key when necessary.

Detailed conventions for topics, partitions, consumer groups, retries, DLT and retention are defined separately. The currently defined business topics and their consumers are documented in `06-communication-flows.md`.

## Consequences
- Producers remain decoupled from event consumers.
- Consumers can scale horizontally.
- New consumers can be added without modifying producers.
- Consumers must tolerate duplicate deliveries.
- Asynchronous workflows use eventual consistency.
- Kafka adds operational complexity.

## Alternatives Considered
- RabbitMQ: strong queueing model, but less aligned with the expected event distribution model.
- NATS: simpler, but Kafka better matches the expected scalable event-streaming usage.
