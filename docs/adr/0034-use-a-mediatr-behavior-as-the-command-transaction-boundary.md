# Use a MediatR behavior as the command transaction boundary

Each application command is one persistence transaction boundary.

A MediatR pipeline behavior commits the scoped EF Core DbContext after a command
handler completes successfully.

Repositories and command handlers do not call `SaveChangesAsync` themselves.

## Considered Options

Repositories could save after every write, but a command touching several
repositories could then partially commit before a later operation fails.

Handlers could explicitly use an `IUnitOfWork`, but that would repeat transaction
plumbing in every command and expose persistence coordination as a handler
responsibility.

A MediatR behavior matches the existing CQRS architecture and applies the rule
consistently without additional handler code.

## Consequences

Changes made by one successful command are persisted together.

If the handler fails before completion, the behavior does not commit its tracked
changes.

Queries do not use this command transaction behavior.

Cases requiring transaction semantics beyond one local service database require
a separate architectural decision.
