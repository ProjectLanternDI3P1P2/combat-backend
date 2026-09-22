# ADR-GLOB-008 — Kubernetes Discovery, Configuration and Secrets

**Status:** Accepted

## Context
The platform will be deployed on Kubernetes.
Services need internal discovery, environment-specific configuration and secret distribution.
No external secret manager such as Vault is planned.

## Decision
Kubernetes SHALL provide service discovery through Kubernetes Services and internal DNS.
Applications SHALL use stable service DNS names and SHALL NOT depend on pod IP addresses.
No additional registry such as Consul or Eureka SHALL be introduced.

Non-sensitive environment-specific configuration SHALL use Kubernetes ConfigMaps.
Sensitive configuration SHALL use Kubernetes Secrets.
Secrets SHALL NOT be committed in plaintext to source control.

Kubernetes Secrets SHALL primarily be injected into containers as environment variables.
Non-sensitive configuration MAY also be injected as environment variables.
.NET services SHALL consume configuration through the standard configuration system and `IOptions<T>`.

`appsettings.json` MAY contain safe defaults.
Environment-specific deployed values SHOULD come from Kubernetes.
No external secret-management platform SHALL initially be introduced.

## Consequences
- Native Kubernetes capabilities cover discovery and configuration.
- No additional discovery or secret-management infrastructure is required.
- Services can scale without changing dependency addresses.
- Operational security of Kubernetes Secrets remains important.
- Advanced secret rotation and leasing are not provided.

## Alternatives Considered
- Consul or Eureka: rejected because Kubernetes already provides service discovery.
- Vault: rejected because current requirements do not justify the additional infrastructure.
- Committed environment secrets: rejected for security reasons.
