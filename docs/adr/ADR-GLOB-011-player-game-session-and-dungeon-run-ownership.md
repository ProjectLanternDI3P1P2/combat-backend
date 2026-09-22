# ADR-GLOB-011 — Player Owns Game Sessions and Dungeon Owns Dungeon Runs

**Status:** Accepted

## Context

Solo and multiplayer play need a party lifecycle and a generated dungeon
lifecycle. Treating both concepts as one aggregate would make either Player or
Dungeon responsible for data outside its domain: selected heroes and party
membership on one side, generated rooms and run progression on the other.

The terms are deliberately distinct. A technical authentication session belongs
to neither service; it is handled by Keycloak and the API Gateway.

## Decision

Player SHALL own the `GameSession`: its creator, participants, selected heroes
and session state. A `GameSession` represents a solo party or a multiplayer
party and references one `DungeonRun` after it starts.

Dungeon SHALL own the `DungeonRun`: generated dungeon structure, rooms,
positions, room state and run lifecycle. A `DungeonRun` references its owning
Player `GameSession` by identifier but does not own party membership.

Player SHALL synchronously request `CreateDungeonRun` from Dungeon when the
creator starts a session. The creator alone may request abandonment; Player
shall synchronously request `AbandonDungeonRun` from Dungeon. Dungeon publishes
`DungeonRunEnded` after a run reaches `Won`, `Lost` or `Abandoned`; Player then
closes the related `GameSession`.

The participant roster is locked once the `DungeonRun` starts. No player may
join an active run.

## Consequences

- Party and hero-selection rules remain in Player.
- Generated dungeon state and run transitions remain in Dungeon.
- Services reference each other's identifiers rather than sharing aggregates or
  databases.
- Starting and abandoning a run require synchronous cross-service coordination.
- Run completion is propagated asynchronously and is eventually reflected in
  the `GameSession` state.

## Alternatives Considered

- Dungeon owning both party and run: rejected because it would duplicate or
  displace Player's ownership of heroes and party membership.
- Player owning the generated dungeon: rejected because generation, rooms and
  movement are Dungeon responsibilities.
- A shared Session service: rejected because the product is constrained to five
  business microservices and does not justify a sixth capability.
