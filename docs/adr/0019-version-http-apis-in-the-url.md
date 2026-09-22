# Version HTTP APIs in the URL

HTTP API major versions are part of the route, starting with `/api/v1`.

## Considered Options

Header-based versioning and content negotiation were considered possible
alternatives.

URL versioning makes the selected contract immediately visible in requests,
logs, documentation and manual tests. It also keeps routing and API discovery
simple for developers and clients.

Headers would hide the version from the most visible part of the request without
providing a useful benefit for this project.

## Consequences

Backward-compatible additions remain in the current major API version.

Breaking contract changes require a new major route such as `/api/v2`.

Multiple versions may temporarily coexist when compatibility requires it.
