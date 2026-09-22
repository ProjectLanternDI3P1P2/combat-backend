# Use REST for non-gameplay client APIs, SignalR for gameplay, and gRPC for synchronous inter-service calls

Non-gameplay client-facing backend APIs use REST over HTTP.

Gameplay commands and state updates use SignalR over WebSocket, for both solo and multiplayer play.

Synchronous communication between backend microservices uses gRPC.

## Considered Options

REST is the established interaction model for resource-oriented frontend
operations such as profile, inventory and progression. Gameplay needs server
push and a single interaction model shared by solo and multiplayer modes;
SignalR provides that model over WebSocket.

For service-to-service communication, gRPC is designed for application-to-
application calls, provides strongly defined contracts and uses an efficient
binary protocol compared with general client-facing JSON APIs.

Using REST everywhere would be simpler operationally, but would discard those
benefits for internal synchronous communication.

## Consequences

Presentation exposes versioned REST endpoints for non-gameplay frontend
operations and service-owned SignalR Hubs for gameplay. Both are exposed only
through the API Gateway.

Internal synchronous contracts are defined separately as gRPC contracts.

Asynchronous communication uses Kafka with versioned Protocol Buffers contracts.
