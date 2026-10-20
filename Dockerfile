# syntax=docker/dockerfile:1.10

ARG DOTNET_VERSION=10.0

# ============================================================
# 1. RESTORE
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore

WORKDIR /src
COPY NuGet.config ./

# Copier uniquement les fichiers projet pour profiter
# au maximum du cache Docker.
COPY Combat.Presentation.slnx ./

COPY Combat.Contracts/Combat.Contracts.csproj \
     Combat.Contracts/

COPY Combat.Domain/Combat.Domain.csproj \
     Combat.Domain/

COPY Combat.Application/Combat.Application.csproj \
     Combat.Application/

COPY Combat.Infrastructure/Combat.Infrastructure.csproj \
     Combat.Infrastructure/

COPY Combat.Presentation/Combat.Presentation.csproj \
     Combat.Presentation/

COPY Combat.Test/Combat.Test.csproj \
     Combat.Test/

# Cache NuGet BuildKit
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=secret,id=github_packages_credentials,env=NuGetPackageSourceCredentials_github \
    dotnet restore Combat.Presentation.slnx


# ============================================================
# 2. BUILD
# ============================================================
FROM restore AS build

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build Combat.Presentation/Combat.Presentation.csproj \
    --configuration Release \
    --no-restore


# ============================================================
# 3. TEST
# ============================================================
FROM build AS test

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet test Combat.Test/Combat.Test.csproj \
    --configuration Release \
    --no-restore \
    --verbosity normal \
    --logger "console;verbosity=normal"


# ============================================================
# 4. PUBLISH
# ============================================================
FROM build AS publish

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Combat.Presentation/Combat.Presentation.csproj \
    --configuration Release \
    --output /app/publish \
    --no-build \
    --no-restore \
    /p:UseAppHost=false


# ============================================================
# 5. RUNTIME
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

WORKDIR /app

# ============================================================
# ASP.NET CORE
# ============================================================
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080 8081

# Copier uniquement les fichiers nécessaires au runtime
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish ./

# Exécution non-root
USER $APP_UID

ENTRYPOINT ["dotnet", "Combat.Presentation.dll"]