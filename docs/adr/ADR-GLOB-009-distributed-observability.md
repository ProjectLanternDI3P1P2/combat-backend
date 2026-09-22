# ADR-GLOB-009 — Distributed Observability

**Status:** Accepted

## Context
A user operation may cross the API Gateway, HTTP endpoints, SignalR/WebSocket connections, gRPC calls, RabbitMQ publishers and RabbitMQ consumers.
Without shared observability context, diagnosing distributed latency and failures becomes difficult.

## Decision
OpenTelemetry SHALL be used for distributed tracing and telemetry instrumentation.
Serilog SHALL be used for structured application logging.

Trace context SHALL be propagated across:
- incoming HTTP requests;
- SignalR/WebSocket connection and command handling;
- API Gateway forwarding;
- gRPC calls;
- RabbitMQ message processing where supported.

Logs SHOULD include trace and correlation information.
A correlation identifier SHALL be propagated across synchronous request boundaries.
Relevant trace or correlation metadata SHOULD be propagated with asynchronous messages when appropriate.

Application logs SHALL be structured.
The telemetry storage, visualization and alerting backend are outside the scope of this ADR.

## Consequences
- Operations can be followed across microservice boundaries.
- Distributed failures and latency are easier to diagnose.
- Instrumentation remains vendor-neutral.
- Telemetry adds runtime and storage overhead.
- Context propagation must remain consistent across communication mechanisms.

## Alternatives Considered
- Application logs only: rejected because they do not provide sufficient distributed request visibility.
- Proprietary instrumentation: rejected in favor of OpenTelemetry portability.
