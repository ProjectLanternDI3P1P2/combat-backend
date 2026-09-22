# ADR-GLOB-006 — Resilience of Synchronous gRPC Communication

**Status:** Accepted

## Context
Synchronous dependencies can propagate latency and failures across microservices.
Retries at several layers can also amplify traffic during an incident.

## Decision
Every interservice gRPC call SHALL define an explicit deadline or timeout.
Timeout values SHALL remain configurable and MAY differ by operation.

Retries SHALL only target transient failures.
Automatic retries SHALL only be used when replaying the operation is safe.
Non-idempotent operations SHALL NOT be retried automatically without an explicit idempotency mechanism.
Business failures SHALL NOT be retried.

The API Gateway SHALL NOT retry failed business operations.
The service making the gRPC call SHALL own the resilience policy for that dependency.
Circuit breakers SHALL be used for significant synchronous dependencies.

A synchronous request SHALL traverse at most two consecutive interservice gRPC calls.
A deeper chain SHALL trigger an architectural review of service responsibilities and coupling.
Independent calls MAY be executed concurrently.
Degraded responses MAY be implemented when allowed by the business requirement.

## Consequences
- Failures are contained more effectively.
- Retry storms are reduced.
- Deep synchronous dependency chains are prevented.
- Resilience policies must be configured and maintained.
- Developers must understand idempotency before enabling retries.

## Alternatives Considered
- Unlimited retries: rejected because they amplify incidents.
- Retries at every layer: rejected because nested retries multiply traffic.
- No circuit breaker: rejected because known failing dependencies should fail fast.
