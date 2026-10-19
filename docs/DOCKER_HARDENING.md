# Combat Dockerfile Hardening Procedure

## Overview

This document describes the Docker build and hardening procedure used for the `Combat` microservice.

The objective is to provide a Docker image that is:

- Secure
- Optimized
- Production-ready
- Easy to maintain
- Compatible with CI/CD pipelines
- Efficient regarding Docker layer caching
- Executed with a non-root user
- Free from unnecessary SDK and source files at runtime

The Dockerfile uses a multi-stage build strategy with dedicated stages for:

1. Dependency restoration
2. Application build
3. Automated tests
4. Application publishing
5. Production runtime

The structure follows the common Docker hardening baseline established for the project's .NET microservices.

---

# Project Structure

The `Combat` solution contains the following projects:

```text
Combat.Presentation.slnx

Combat.Contracts/
├── Combat.Contracts.csproj

Combat.Domain/
├── Combat.Domain.csproj

Combat.Application/
├── Combat.Application.csproj

Combat.Infrastructure/
├── Combat.Infrastructure.csproj

Combat.Presentation/
├── Combat.Presentation.csproj

Combat.Test/
├── Combat.Test.csproj
```

`Combat.Presentation` is the ASP.NET Core entry point of the microservice.

`Combat.Contracts` contains the owned Protobuf contracts and generated gRPC types.

---

# Dockerfile Architecture

The Dockerfile follows this pipeline:

```text
RESTORE
   |
   v
 BUILD
   |
   +--------> TEST
   |
   v
 PUBLISH
   |
   v
 RUNTIME
```

Each stage has a dedicated responsibility.

The final runtime image contains only the files required to execute the application.

The .NET SDK, source code, test projects and build artifacts are not included in the production image.

---

# 1. Restore Stage

The restore stage uses the .NET SDK image:

```dockerfile
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
```

The SDK is required because NuGet dependencies must be restored before the application can be compiled.

The working directory is:

```dockerfile
WORKDIR /src
```

Only the solution and project files are copied before the restore operation.

Example:

```dockerfile
COPY Combat.Domain/Combat.Domain.csproj \
     Combat.Domain/
```

This is important for Docker layer caching.

If application source code changes while the project files remain unchanged, Docker can reuse the existing NuGet restore layer.

This avoids downloading all dependencies again.

---

# NuGet Configuration

Combat uses a custom `NuGet.config`.

It is copied into the restore stage:

```dockerfile
COPY NuGet.config ./
```

The restore operation uses a BuildKit secret:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=secret,id=github_packages_credentials,env=NuGetPackageSourceCredentials_github \
    dotnet restore Combat.Presentation.slnx
```

The GitHub Packages credentials are therefore provided to the build process without being written into the Dockerfile.

Secrets must not be embedded into the Docker image.

---

# NuGet BuildKit Cache

The NuGet package directory is mounted as a BuildKit cache:

```text
/root/.nuget/packages
```

Example:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Combat.Presentation.slnx
```

This improves build performance by reusing downloaded NuGet packages between builds.

The cache is especially useful in CI/CD environments where the same dependencies are restored repeatedly.

---

# 2. Build Stage

The build stage starts from the restore stage:

```dockerfile
FROM restore AS build
```

The complete source tree is then copied:

```dockerfile
COPY . .
```

The application is compiled using Release configuration:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build Combat.Presentation.slnx \
    --configuration Release \
    --no-restore
```

The `--no-restore` option is used because dependency restoration has already been performed in the previous stage.

This avoids running NuGet restore twice.

---

# 3. Test Stage

The test stage is based on the build stage:

```dockerfile
FROM build AS test
```

The Combat test project is executed with:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet test Combat.Test/Combat.Test.csproj \
    --configuration Release \
    --no-build \
    --verbosity normal \
    --logger "console;verbosity=normal"
```

The `--no-build` option prevents the project from being compiled again.

If a test fails, Docker returns a non-zero exit code.

This allows CI/CD to stop before producing or deploying a production image.

The test stage can be executed independently:

```bash
docker build \
    --target test \
    -t combat-test .
```

---

# 4. Publish Stage

The publish stage generates the production application files:

```dockerfile
FROM build AS publish
```

The application is published using:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Combat.Presentation/Combat.Presentation.csproj \
    --configuration Release \
    --output /app/publish \
    --no-build \
    --no-restore \
    /p:UseAppHost=false
```

The published files are stored in:

```text
/app/publish
```

## UseAppHost

The following option is used:

```text
/p:UseAppHost=false
```

This prevents .NET from generating a platform-specific executable.

The application is started directly with:

```bash
dotnet Combat.Presentation.dll
```

This keeps the runtime image focused on the managed application and required runtime components.

---

# 5. Runtime Stage

The final image uses the ASP.NET Core runtime image:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
```

The .NET SDK is not included in the final image.

This reduces:

- Image size
- Attack surface
- Number of installed components
- Potential vulnerabilities

Only the ASP.NET Core runtime required to execute Combat is present.

---

# Runtime Environment

The runtime configures the application explicitly for production:

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
```

## ASPNETCORE_ENVIRONMENT

```text
ASPNETCORE_ENVIRONMENT=Production
```

This ensures that the production environment is used when the container runs.

Development-specific behavior must not be enabled by the production Docker image.

---

## ASPNETCORE_HTTP_PORTS

```text
ASPNETCORE_HTTP_PORTS=8080
```

The public HTTP API listens on port:

```text
8080
```

---

## DOTNET_RUNNING_IN_CONTAINER

```text
DOTNET_RUNNING_IN_CONTAINER=true
```

This explicitly identifies the execution environment as a container.

---

## DOTNET_EnableDiagnostics

```text
DOTNET_EnableDiagnostics=0
```

.NET runtime diagnostics are disabled in the production container.

This reduces unnecessary runtime capabilities.

Diagnostics should only be enabled when explicitly required for troubleshooting or observability.

---

# Network Ports

Combat uses two ports:

```text
8080
8081
```

The Dockerfile therefore contains:

```dockerfile
EXPOSE 8080 8081
```

## Port 8080

Port `8080` is the client-facing HTTP API.

## Port 8081

Port `8081` is the internal gRPC endpoint.

The gRPC endpoint must not be exposed through the public Kubernetes ingress.

Kubernetes networking must ensure that port `8081` is only reachable by authorized internal services.

The distinction between the REST API and internal gRPC interface is intentional.

---

# Non-Root Execution

The final container does not run as root.

The Dockerfile uses the unprivileged application user provided by the ASP.NET Core image:

```dockerfile
USER $APP_UID
```

Application files are copied with the correct ownership:

```dockerfile
COPY --from=publish \
     --chown=$APP_UID:$APP_UID \
     /app/publish ./
```

This ensures that the runtime user owns the application files.

Running the application without root privileges limits the potential impact of a container compromise.

---

# Application Entry Point

The application starts with:

```dockerfile
ENTRYPOINT ["dotnet", "Combat.Presentation.dll"]
```

The final process is therefore:

```bash
dotnet Combat.Presentation.dll
```

No shell is required to launch the application.

---

# Docker Ignore File

A `.dockerignore` file should be located next to the Dockerfile.

Recommended configuration:

```gitignore
# Git
.git
.gitignore

# IDE
.vs
.vscode
.idea

# .NET build files
**/bin/
**/obj/
**/TestResults/

# Visual Studio
*.user
*.suo

# Docker
Dockerfile*
docker-compose*
.dockerignore

# Documentation
README*
docs/

# Operating system
.DS_Store
Thumbs.db

# Secrets
.env
.env.*
*.pfx
*.p12
*.key
*.pem
secrets/
```

The `.dockerignore` prevents unnecessary or sensitive files from being sent as part of the Docker build context.

This improves:

- Security
- Build speed
- Cache efficiency
- Image consistency

---

# Building the Test Image

The Docker test stage can be executed with:

```bash
docker build \
    --target test \
    -t combat-test .
```

The command fails if one or more automated tests fail.

This stage should be used by CI before a production image is pushed to the container registry.

---

# Building the Production Image

The production image can be built using:

```bash
docker build \
    -t combat-api:latest .
```

Docker executes the required build stages and generates the final runtime image.

For production deployments, `latest` should not be used as the deployment identifier.

Prefer immutable version tags.

Examples:

```text
combat-api:1.4.2
```

or:

```text
combat-api:git-a84f91c
```

---

# GitHub Packages Secret

The Dockerfile uses the following BuildKit secret:

```text
github_packages_credentials
```

It is consumed during NuGet restore:

```dockerfile
--mount=type=secret,
id=github_packages_credentials,
env=NuGetPackageSourceCredentials_github
```

The secret must only be available during the restore operation.

It must not be written using:

```dockerfile
ENV
```

or:

```dockerfile
ARG
```

It must not be copied into the image filesystem.

The objective is to ensure that GitHub Packages credentials do not become part of an image layer.

---

# Production Security Principles

## Multi-Stage Build

The build dependencies and runtime dependencies are separated.

The final image does not contain:

- .NET SDK
- Compiler
- Source code
- Test projects
- Intermediate build files
- Development tooling

---

## Non-Root User

The application runs using:

```dockerfile
USER $APP_UID
```

The application therefore does not require root privileges.

---

## Reduced Runtime Surface

The production image uses the ASP.NET Core runtime rather than the full .NET SDK.

This reduces the number of components available inside the running container.

---

## Disabled Diagnostics

Production diagnostics are disabled:

```text
DOTNET_EnableDiagnostics=0
```

This removes runtime capabilities that are not normally required by the application.

---

## Secrets Are Not Embedded

Secrets must never be written directly into the Dockerfile.

Never use:

```dockerfile
ENV DATABASE_PASSWORD=my-password
```

or:

```dockerfile
ENV JWT_SECRET=my-secret
```

Secrets must instead be injected at runtime.

For Kubernetes deployments, the preferred mechanism is a Kubernetes Secret or an external secret-management solution.

---

# Optional Chiseled Runtime

For environments requiring additional hardening, the standard ASP.NET Core runtime can potentially be replaced with a chiseled image:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
```

Chiseled images contain fewer operating system components and therefore provide a smaller attack surface.

However, this should only be enabled after validating that Combat does not require additional Linux libraries or runtime utilities.

The standard ASP.NET Core runtime remains the safer compatibility baseline.

---

# CI/CD Integration

The intended Docker lifecycle is:

```text
Source Code
    |
    v
Docker Test Stage
    |
    v
Automated Tests
    |
    v
Production Image Build
    |
    v
Security Scan
    |
    v
Container Registry
    |
    v
Kubernetes Deployment
```

A CI/CD pipeline should therefore:

1. Build the test stage.
2. Execute the tests.
3. Build the production image.
4. Scan the resulting image.
5. Tag the image with an immutable version.
6. Push the image to the container registry.
7. Deploy the image to Kubernetes.

---

# Image Tagging

Production deployments should use immutable image identifiers.

Avoid:

```text
combat-api:latest
```

Prefer:

```text
combat-api:1.4.2
```

or:

```text
combat-api:git-a84f91c
```

An immutable tag makes it possible to identify exactly which application version is running in Kubernetes.

It also makes rollback more predictable.

---

# Image Scanning

The production image should be scanned before deployment.

Possible tools include:

```text
Trivy
Docker Scout
Grype
Snyk
Microsoft Defender for Containers
```

The project already uses Trivy as part of its repository security workflow, but container-image scanning should remain a distinct concern when the production image is built.

---

# Base Image Pinning

For stronger supply-chain security, production base images can be pinned by SHA-256 digest.

Example:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:<IMAGE_DIGEST>
```

This guarantees that the exact base image identified by the digest is used.

Digest updates should be intentional and reviewed.

---

# Kubernetes Considerations

The Docker image is intended to run in Kubernetes.

The Kubernetes deployment should preserve the security properties established by the image.

Recommended controls include:

- Run as non-root
- Read-only root filesystem where compatible
- Drop unnecessary Linux capabilities
- Disable privilege escalation
- Define CPU and memory requests
- Define CPU and memory limits
- Configure liveness probes
- Configure readiness probes
- Use immutable image tags
- Inject secrets at runtime
- Restrict access to the internal gRPC port

The Dockerfile alone cannot provide all of these protections. Kubernetes security configuration must complement the hardened image.

---

# Final Docker Security Architecture

```text
                       Combat source code
                              |
                              v
                    +-------------------+
                    |      RESTORE      |
                    |   NuGet packages  |
                    +---------+---------+
                              |
                              v
                    +-------------------+
                    |       BUILD       |
                    |   Release build   |
                    +---------+---------+
                              |
                    +---------+---------+
                    |                   |
                    v                   v
             +-------------+     +-------------+
             |    TEST     |     |   PUBLISH   |
             | Unit/tests  |     | Runtime     |
             +-------------+     | files       |
                                 +------+------+
                                        |
                                        v
                                +---------------+
                                |    RUNTIME    |
                                | ASP.NET Core  |
                                | Non-root      |
                                | Diagnostics   |
                                | disabled      |
                                +-------+-------+
                                        |
                         +--------------+--------------+
                         |                             |
                         v                             v
                    HTTP API                      Internal gRPC
                     TCP 8080                       TCP 8081
                         |                             |
                         v                             v
                     Public                       Internal
                     traffic                       traffic
```

---

# Summary

The hardened Combat Dockerfile provides:

- Multi-stage Docker builds
- Docker BuildKit NuGet caching
- Secure GitHub Packages credential handling
- Explicit automated test stage
- Release builds
- Optimized publishing
- `UseAppHost=false`
- Minimal production runtime
- Non-root execution
- Explicit production configuration
- Disabled .NET diagnostics
- Correct runtime file ownership
- Reduced attack surface
- No SDK in the production image
- No source code in the production image
- No test project in the production image
- CI/CD compatibility
- Immutable image tagging support
- Container security scanning support
- Separate HTTP and internal gRPC ports

This structure should be used as the Combat Docker security baseline and kept aligned with the common hardening standard used by the other .NET microservices.
