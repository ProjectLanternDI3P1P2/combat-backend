# .NET Backend Service Template

Starting point for a backend microservice: a .NET 10 Clean Architecture solution,
the CI pipeline that guards it, and the branching flow that releases it.

Vocabulary is defined in [CONTEXT.md](./CONTEXT.md). Shared technical choices are
recorded in [BACKEND_TECHNICAL_DECISIONS.md](./BACKEND_TECHNICAL_DECISIONS.md),
and the decisions behind this repository's own shape in [docs/adr](./docs/adr).

## Structure

```text
Combat.Domain/          entities, enums, domain services, repository interfaces
Combat.Application/     commands, queries, handlers, validators, pipeline behaviours
Combat.Infrastructure/  EF Core, repository implementations, external services
Combat.Presentation/    HTTP API: controllers, DTOs, middleware
Combat.Contracts/       owned Protobuf contracts and generated gRPC client/server types
Combat.Test/            xUnit tests for all of the above
```

`Presentation` is the Clean Architecture layer name for the HTTP API. There is no
user interface.

## Commands

```powershell
dotnet tool restore
dotnet restore Combat.Presentation.slnx
dotnet build Combat.Presentation.slnx
dotnet test --solution Combat.Presentation.slnx
dotnet run --project Combat.Presentation/Combat.Presentation.csproj
```

## Internal gRPC contract

`Combat.Contracts` owns the versioned `combat_player_v1.proto` contract and the
generated C# gRPC types. It is referenced locally by the server projects; it never
pulls this service's Domain or Application types into the wire contract.

The template exposes `CombatPlayerService/GetPlayer` on its internal gRPC endpoint.
The REST API remains the client-facing interface. Locally, gRPC listens on
`http://localhost:8081`; Docker binds it only to loopback. In Kubernetes, expose
that port through an internal-only Service, never through the ingress.

Concrete gRPC service implementations in `Presentation/Grpc/Services` are mapped
automatically at startup. A new service only needs to inherit from its generated
contract base class; no additional `MapGrpcService<T>()` call is needed.

`Infrastructure/Grpc/Clients/PlayerGrpcClient` shows the consumer-side pattern.
Handlers depend on the `Application/Ports/IPlayerClient` port and its application
model, never on Protobuf or gRPC types. The adapter uses the generated typed client,
maps its response, and applies the configurable `Grpc:Player:TimeoutSeconds` deadline.

`Combat.Contracts` has an independent release line. A change outside
`Combat.Contracts/` never releases the package. When a contract release is made,
release-please creates a `contracts-vN.0.0` tag and `publish-contracts.yaml`
publishes the matching NuGet package to GitHub Packages. The contract number used
by consumers is therefore V1, V2, V3, and so on; minor and patch contract package
versions are deliberately never generated. A consuming repository configures its
NuGet source as `https://nuget.pkg.github.com/<organisation>/index.json` and pins a
released `Combat.Contracts` version.

The package page appears after the first release. To let a consuming repository's
GitHub Actions workflow restore the package without a personal token, grant that
repository `Read` access under **Package settings > Manage Actions access**. Its
workflow then needs `permissions: { packages: read }` and can authenticate its NuGet
source with the automatically-provided `GITHUB_TOKEN`. Developers authenticate once
on their own workstation with a personal access token (classic) scoped to
`read:packages`; neither kind of token belongs in a repository.

## Running the stack

```bash
docker compose up -d --build
```

The API listens on <http://localhost:8080>, Postgres on host port 5433, and the
RabbitMQ management UI on <http://localhost:15672> (`combat` / `combat`). Because
`ASPNETCORE_ENVIRONMENT` is `Development`, the OpenAPI document is served at
`/openapi/v1.json` and the Scalar UI at `/scalar`.

```bash
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
docker compose down -v   # -v also drops the database volume
```

**The schema does not exist yet.** There are no EF Core migrations, and nothing
calls `EnsureCreated`, so `/api/v1/players` fails against an empty database while
the health endpoints still answer — they check nothing. Migration execution is
listed as an open decision in
[BACKEND_TECHNICAL_DECISIONS.md](./BACKEND_TECHNICAL_DECISIONS.md); until it is
settled, create the schema by hand or add migrations to your own service.

## Toolchain

The SDK version is pinned in `global.json`; `dotnet tool restore` installs the
coverage collector and the git-hook runner declared in `dotnet-tools.json`.
Run `dotnet husky install` once per clone to enable the pre-commit hook — git
hook paths are local configuration and cannot be committed.

## Configuration

PostgreSQL is configured through the `ConnectionStrings` section.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=combat;Username=combat",
    "PasswordFile": "/run/secrets/postgres_password"
  }
}
```

`PasswordFile` is optional. It injects the password from a Docker or Kubernetes
secret instead of storing it in the configuration file.

## Asynchronous messaging

`IMessagePublisher` is the application seam for integration messages; its
`MessageEnvelope` contains no RabbitMQ type. `RabbitMqMessagePublisher` is the
RabbitMQ adapter registered when `RabbitMq:Enabled` is true. It serializes the
broker-independent Protobuf envelope from `combat_events_v1.proto`, declares the
durable `combat.events` topic exchange, and publishes each event with the routing
key `<type>.v<version>`.

Creating a player publishes `combat.player.created.v1`, whose payload is the
versioned `PlayerCreated` Protobuf message. In Compose, the adapter connects to
the `rabbitmq` service. For a local run without the broker, leave `Enabled` false;
the no-op adapter keeps the application runnable while preserving the same
application interface.

## Branching flow

```text
feature/xxx --squash--> dev --merge commit--> main --> tag + CHANGELOG
                         ^                      |
                         +----- back-merge -----+
```

- `dev` is the default branch. Open every feature pull request against it, and
  **squash** on merge: one feature becomes one conventional commit.
- Promote by opening a pull request from `dev` to `main` and merging it with a
  **merge commit**. Never squash this one — release-please reads the individual
  commits ([ADR-0002](./docs/adr/0002-merge-strategy-depends-on-the-target-branch.md)).
- release-please then maintains independent release pull requests on `main` for
  the application and the Protocol Buffer contracts. Merging one writes its
  changelog, bumps only its version and tags it (`vX.Y.Z` for the application,
  `contracts-vN.0.0` for contracts).
- A back-merge from `main` to `dev` follows automatically
  ([ADR-0003](./docs/adr/0003-automatic-back-merge-from-main-to-dev.md)).

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org):
`feat:` and `fix:` appear in the changelog and move the version, everything else
(`chore:`, `ci:`, `refactor:`, `test:`, `docs:`, `build:`, `style:`) is hidden and
moves nothing.

## Continuous integration

| Workflow | Runs on | Does |
| --- | --- | --- |
| `ci.yaml` | PR to `dev` / `main`, push to `main` | Calls the reusable lint, test and build workflows |
| `sonar.yaml` | PR and push to `dev`, except Dependabot | Builds and tests under the SonarScanner for .NET, uploads coverage |
| `security.yml` | PR to `dev` / `main`, push to `main` | Trivy filesystem scan, zizmor workflow audit |
| `release-please.yaml` | push to `main` | Maintains the release pull request |
| `back-merge.yaml` | after a release | Opens and merges `main` → `dev` |

Formatting is enforced by `dotnet format --verify-no-changes --severity warn`,
which reads `.editorconfig`. The same command runs locally as a pre-commit hook
through Husky.Net, installed by `dotnet tool restore`.

## Adding integration tests

There are none yet, and `Combat.Test` holds unit tests only —
`PlayerRepositoryTests` uses the EF Core in-memory provider, which is not a real
database. Real integration tests would need a `WebApplicationFactory` for the
HTTP surface and a containerised PostgreSQL for persistence.

Both are cross-cutting choices affecting all five services, so pick them as a
shared decision and record an ADR before adding them here. Once they exist, give
them their own job in `ci.yaml` so a slow suite does not gate the fast feedback
from lint and unit tests.

## Setting up a new repository from this template

1. Create the repository **public** (SonarQube Cloud's free tier requires it).
2. Import the organisation into SonarQube Cloud and create the project, then:
   - **Branches**: delete the `dev` entry SonarQube Cloud created on its own,
     then rename the main branch from `main` to `dev` — the rename is refused
     while a branch of that name already exists. The free plan covers one
     long-lived branch, and `dev` is the one that matters (ADR-0005).
   - **Administration → Analysis method**: switch **Automatic Analysis off**.
     Left on, it competes with the scanner and every CI analysis fails.
   - **Administration → New code**: number of days, 30.
3. Add `SONAR_TOKEN` and `BOT_TOKEN` as secrets. `BOT_TOKEN`, not the default
   `GITHUB_TOKEN`: a pull request opened by the latter triggers no workflow, so
   the release pull request would never get a CI run.
4. Set `dev` as the default branch and protect both `dev` and `main`. Required
   checks: `Lint / dotnet format`, `Test / dotnet test`, `Build / dotnet build`,
   `Trivy Security Scan`, `GitHub Actions audit`. **Not** `SonarQube Cloud scan`:
   it is skipped on Dependabot pull requests, and a required check that never
   runs blocks them forever. Keep "require linear history" **off**, or the merge
   commits this flow depends on become impossible.
5. Add two rulesets so the merge strategy is enforced rather than merely written
   down (ADR-0007): one on `dev` allowing **squash** only, one on `main` allowing
   **merge commits** only. Give organisation admins a bypass on the `dev` one —
   the back-merge workflow merges `main` into `dev` with a merge commit and would
   otherwise be refused.
6. Enable auto-merge on the repository; the back-merge workflow uses it.
7. Rename the `Combat.*` projects to your service name, and update `/k:` and
   `/o:` in `.github/workflows/sonar.yaml`.
