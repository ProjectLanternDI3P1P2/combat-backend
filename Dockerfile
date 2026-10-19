# syntax=docker/dockerfile:1.10

ARG DOTNET_VERSION=10.0

# ============================================================
# 1. RESTORE
# ============================================================
FROM dhi/dotnet:10.0-sdk AS build

WORKDIR /src
COPY NuGet.config ./

# Copier uniquement les fichiers projet pour profiter
# au maximum du cache Docker.
COPY Dungeon.Presentation.slnx ./

COPY Dungeon.Contracts/Dungeon.Contracts.csproj \
     Dungeon.Contracts/

COPY Dungeon.Domain/Dungeon.Domain.csproj \
     Dungeon.Domain/

COPY Dungeon.Application/Dungeon.Application.csproj \
     Dungeon.Application/

COPY Dungeon.Infrastructure/Dungeon.Infrastructure.csproj \
     Dungeon.Infrastructure/

COPY Dungeon.Presentation/Dungeon.Presentation.csproj \
     Dungeon.Presentation/

COPY Dungeon.Test/Dungeon.Test.csproj \
     Dungeon.Test/

# Cache NuGet BuildKit
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=secret,id=github_packages_credentials,env=NuGetPackageSourceCredentials_github \
    dotnet restore Dungeon.Presentation.slnx


# ============================================================
# 2. BUILD
# ============================================================
FROM restore AS build

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build Dungeon.Presentation/Dungeon.Presentation.csproj \
    --configuration Release \
    --no-restore


# ============================================================
# 3. TEST
# ============================================================
FROM build AS test

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet test Dungeon.Test/Dungeon.Test.csproj \
    --configuration Release \
    --no-restore \
    --verbosity normal \
    --logger "console;verbosity=normal"


# ============================================================
# 4. PUBLISH
# ============================================================
FROM build AS publish

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Dungeon.Presentation/Dungeon.Presentation.csproj \
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

ENTRYPOINT ["dotnet", "Dungeon.Presentation.dll"]