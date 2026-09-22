# Version asynchronous message contracts

Every asynchronous message contract is explicitly versioned, starting at `v1`.

## Considered Options

Unversioned events would be simpler initially but would make contract evolution
implicit and risky once producers and consumers are deployed independently.

Independent evolution of microservices is a project requirement. A producer must
be able to evolve without forcing every consumer to be released at the same
time.

Explicit message versions make compatibility boundaries visible.

## Consequences

Backward-compatible additions remain in the existing message version.

Removing, renaming or changing the meaning or type of required data creates a new
message version.

Consumers must tolerate compatible additions and must not treat a new optional
Protocol Buffers field as a breaking change.
