# ADR-GLOB-004 — Authentication and Internal Identity Propagation

**Status:** Accepted

## Context
Clients authenticate with Keycloak and send an access token to the backend.
Validating the same external JWT in every microservice would duplicate authentication concerns.
Only the API Gateway is externally reachable.

## Decision
The API Gateway SHALL validate Keycloak access tokens.
Individual microservices SHALL NOT independently validate the external JWT for Gateway-originated requests.

After authentication, the Gateway SHALL inject trusted internal identity headers.
The internal identity contract MAY contain:
- `X-User-Id`
- `X-Username`
- `X-Roles`
- `X-Correlation-Id`

`X-User-Id` SHALL be the stable backend identity.
`X-Username` SHALL NOT be used as a stable business identifier.
`X-Roles` SHALL contain only global roles; business authorization SHALL remain inside microservices.

The Gateway SHALL remove or overwrite trusted internal headers received from external clients.
Relevant identity data MAY be propagated through gRPC metadata.
Internal service-to-service calls SHALL initially trust the isolated Kubernetes network.
No mTLS or OAuth2 Client Credentials SHALL initially be required.

## Consequences
- Keycloak integration is centralized.
- Microservices are not coupled to Keycloak token structure.
- Business authorization remains close to domain data.
- Internal network compromise has a larger trust impact.
- Stronger service identity can be introduced later if required.

## Alternatives Considered
- JWT validation in every service: rejected because it duplicates external authentication concerns.
- mTLS or service OAuth: deferred because current security requirements do not justify the added complexity.
