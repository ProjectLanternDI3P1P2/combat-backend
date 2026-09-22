# ADR-GLOB-001 — Communication Between Services

**Status:** Accepted

## Context
The platform uses microservices and must support a high number of simultaneous players.
External clients need standard web APIs, while internal synchronous calls require efficient typed communication.
Some workflows can be processed asynchronously and should avoid unnecessary synchronous coupling.

## Decision
Non-gameplay external client communication SHALL use HTTP REST APIs with JSON payloads through the API Gateway.
Gameplay interactions, in solo and multiplayer modes, SHALL use SignalR over WebSocket through the API Gateway. The Gateway routes each Hub connection to the owning microservice; it does not own gameplay state.
Synchronous microservice-to-microservice communication SHALL use gRPC with Protocol Buffers.
gRPC endpoints SHALL only be reachable from the internal Kubernetes network.
Asynchronous interservice communication SHALL use RabbitMQ with versioned Protocol Buffers contracts.

Use gRPC when the caller requires the result immediately to complete the current operation.
Use asynchronous messaging when processing can be deferred and the caller only needs to publish a business event.

A microservice MAY aggregate data from another service when this belongs to its business responsibility.
Independent frontend data MAY be retrieved through multiple HTTP requests.
Synchronous call chains SHALL contain at most two consecutive interservice gRPC calls.
Independent gRPC calls MAY be executed in parallel.

## Consequences
- REST remains simple and conventional for non-gameplay client operations.
- SignalR provides one real-time gameplay interaction model for both solo and multiplayer modes.
- gRPC provides efficient and strongly typed internal communication.
- Messaging reduces coupling for deferred workflows.
- The platform must maintain REST, SignalR, gRPC and RabbitMQ communication stacks.
- Deep synchronous dependency chains are prevented.

## Alternatives Considered
- REST for all communication: rejected due to weaker internal contracts and higher serialization overhead.
- gRPC for external clients: rejected to keep conventional web APIs and browser-compatible real-time gameplay connections.
- Messaging for all interservice communication: rejected because many gameplay operations need immediate responses.
