# Expose separate liveness and readiness health checks

Every backend service exposes separate liveness and readiness health endpoints:

`/health/live` and `/health/ready`.

## Considered Options

A single `/health` endpoint would be simpler but cannot distinguish a running
process from an application that is not ready to receive traffic.

The platform will run on Kubernetes, where liveness and readiness probes have
different operational purposes.

ASP.NET Core health checks support this model directly.

## Consequences

Kubernetes can restart an unhealthy process based on liveness while independently
removing an unready service from traffic.

Readiness checks may include dependencies required to serve requests, while
liveness checks must remain lightweight and focused on process health.

Health checks must not perform unnecessarily expensive operations.
