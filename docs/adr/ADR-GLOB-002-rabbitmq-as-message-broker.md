# ADR-GLOB-002 - RabbitMQ as the Asynchronous Message Broker

**Status:** Accepted

## Context
The platform needs asynchronous communication for a small number of domain
events, including combat completion, consumable usage and dungeon run completion.
The expected event volume and number of routing keys are limited, and event
replay is not currently a functional requirement.

The same event may be consumed independently by several microservices. The
selected broker must therefore support reliable delivery, consumer isolation
and horizontal scaling while remaining straightforward to implement, deploy and
operate as a cluster.

## Decision
RabbitMQ SHALL be used as the asynchronous messaging platform.
RabbitMQ SHALL primarily transport domain events. Operations requiring an
immediate result SHALL use gRPC instead of RabbitMQ.

Message payloads SHALL use versioned Protocol Buffers packages as defined by
ADR-GLOB-007. Application contracts SHALL remain independent from RabbitMQ
client types and transport-specific details.

Events SHALL be published to durable exchanges using versioned routing keys.
Each consuming microservice SHALL own a durable queue bound to the routing keys
it consumes. This allows several services to receive the same event while
multiple instances of one service compete on their service-owned queue.

Messages SHALL be acknowledged only after successful processing. Failed or
unacknowledged messages MAY be redelivered, so the architecture SHALL assume
at-least-once processing semantics. Consumers SHALL be idempotent whenever
duplicate processing could alter business state.

No global event ordering SHALL be required. When ordering is required for a
business entity, publishers SHALL use a stable business key and consumers SHALL
preserve that constraint explicitly rather than assuming global broker ordering.

Retry, dead-letter exchange, queue retention and delivery limit conventions
SHALL be defined with the broker configuration. The currently defined routing
keys and consumers are documented in the microservice communication architecture.

## Consequences
- Producers remain decoupled from event consumers.
- New consumers can be added by binding a new service-owned queue.
- Consumer instances can scale horizontally through competing consumption.
- Consumers must tolerate duplicate delivery and delayed processing.
- Asynchronous workflows use eventual consistency.
- The small number of event flows does not require an event-streaming platform.
- RabbitMQ is simpler for the team to implement and operate for the MVP,
  including as a cluster.
- Exchanges, queues, bindings, acknowledgements, retries and dead-lettering must
  be configured and monitored.

## Alternatives Considered
- Apache Kafka: strong event-streaming, partitioning, retention and replay
  capabilities, but these capabilities are not required by the current volume,
  number of event flows or replay needs. Its implementation and operational
  model would add unnecessary complexity for the MVP.
- NATS: lightweight and simple, but RabbitMQ provides a more familiar durable
  queueing and routing model for the required workflows.
