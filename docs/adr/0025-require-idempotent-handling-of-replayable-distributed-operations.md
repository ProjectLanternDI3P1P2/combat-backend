# Require idempotent handling of replayable distributed operations

Distributed operations that may be repeated must protect business invariants
against duplicate execution.

## Considered Options

RabbitMQ is the selected broker and the platform assumes at-least-once delivery
semantics (ADR-GLOB-002).

Duplicates can still occur in distributed systems through retries, timeouts,
redelivery or recovery behavior. Ignoring this possibility would make business
correctness depend on transport behavior.

Examples include granting the same reward twice or applying the same combat
result more than once.

## Consequences

Handlers evaluate whether duplicate execution can create an invalid business
effect.

`MessageId` or a business idempotency key is used where appropriate.

The exact deduplication storage or broker mechanism remains an implementation
decision and is not fixed by this ADR.
