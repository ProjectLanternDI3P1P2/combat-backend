# Use REST for client APIs and gRPC for synchronous inter-service calls

Client-facing backend APIs use REST over HTTP.

Synchronous communication between backend microservices uses gRPC.

## Considered Options

REST is the established interaction model for frontend clients and fits the
resource-oriented APIs exposed by the project. No competing client-facing API
style provided a clear benefit.

For service-to-service communication, gRPC is designed for application-to-
application calls, provides strongly defined contracts and uses an efficient
binary protocol compared with general client-facing JSON APIs.

Using REST everywhere would be simpler operationally, but would discard those
benefits for internal synchronous communication.

## Consequences

Presentation exposes versioned REST endpoints for frontend and external clients.

Internal synchronous contracts are defined separately as gRPC contracts.

Asynchronous communication remains independent from this choice and will use the
message broker selected later.
