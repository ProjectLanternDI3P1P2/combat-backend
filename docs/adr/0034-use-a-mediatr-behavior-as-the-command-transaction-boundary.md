# Use a MediatR behavior as the command transaction boundary

Each application command is one persistence transaction boundary.

A MediatR pipeline behavior commits the scoped EF Core DbContext after a command
handler completes successfully.

Both `ICommand` and `ICommand<TResponse>` implement the `ICommandRequest` marker
so commands returning a result use the same transaction boundary. Queries do not.

An internal command invoked inside an existing database transaction reuses that
transaction. Only its owner saves and commits, so monster generation can participate
in combat initialization without committing partial state.

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

Monster generation takes a PostgreSQL transaction-scoped advisory lock derived
from its idempotency key before reading or staging the monster. Concurrent commands
with the same key wait for the first transaction to finish, then return its monster
or reject incompatible parameters. The unique database index remains a backstop.
The repository also checks tracked monsters so a repeated internal call can reuse
a monster staged earlier in the same transaction. Repositories do not save changes.

Cases requiring transaction semantics beyond one local service database require
a separate architectural decision.
