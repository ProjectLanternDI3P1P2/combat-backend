# ADR-GLOB-003 — API Gateway as the Single External Entry Point

**Status:** Accepted

## Context
The platform contains multiple independently deployed microservices.
Directly exposing each service would reveal internal topology and duplicate external routing and security concerns.
A Backend For Frontend is not required for the current frontend architecture.

## Decision
The API Gateway SHALL be the only backend component accessible from outside the Kubernetes cluster.
External clients SHALL NOT directly access individual microservices.
Internal HTTP and gRPC interfaces SHALL only be reachable from the internal network.

The Gateway SHALL handle external HTTP/HTTPS and SignalR/WebSocket routing, authentication and cross-cutting boundary policies.
It SHALL proxy Hub connections to the owning microservice without consuming RabbitMQ messages or owning gameplay state.
The Gateway SHALL NOT contain business logic.
No Backend For Frontend SHALL be introduced.

The frontend MAY call several APIs when displaying independent information.
Business-specific aggregation SHALL remain in the microservice owning the corresponding business capability.

## Consequences
- The system exposes a single controlled entry point.
- Internal topology remains hidden from clients.
- External authentication and routing are centralized.
- The Gateway becomes critical infrastructure.
- Some frontend screens may require multiple HTTP calls.
- Real-time gameplay connections are routed through the Gateway to their service-owned SignalR Hubs.

## Alternatives Considered
- Direct microservice exposure: rejected because it weakens the external security boundary.
- Backend For Frontend: rejected because current requirements do not justify an additional layer.
