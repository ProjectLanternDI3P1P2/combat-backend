# Organize application code by feature and use case

Application code is organized feature-first, with files grouped by business use
case rather than by technical type.

## Considered Options

A technical structure could group all commands, queries, handlers and validators
into separate global folders.

That structure spreads one use case across several locations and increases the
navigation cost when developers move between squads or return to a service they
have not worked on recently.

Grouping the command or query, handler and validator together makes the
application behavior visible from the folder structure.

## Consequences

Related application files stay close together.

Developers can understand the available use cases by browsing `Features` without
jumping between technical folders.

Cross-cutting application behavior remains outside individual feature folders
when it genuinely applies to multiple use cases.
