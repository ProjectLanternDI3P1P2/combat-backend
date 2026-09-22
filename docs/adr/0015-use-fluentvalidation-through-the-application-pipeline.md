# Use FluentValidation through the application pipeline

Input validation is implemented with FluentValidation and integrated through the
application pipeline.

## Considered Options

Validation could be implemented with DataAnnotations or directly inside handlers.

FluentValidation keeps request validation separate from transport DTOs and
application orchestration, while integrating cleanly with MediatR pipeline
behaviors.

This avoids repeating validation calls in individual handlers and provides one
consistent validation mechanism.

## Consequences

Validators are associated with specific use cases and are executed before the
handler when the pipeline applies.

Input-shape validation stays separate from business invariants.

Critical business rules must still be enforced by Domain or Application logic
and must not rely only on FluentValidation.
