FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Combat.Presentation.slnx ./
COPY Combat.Contracts/Combat.Contracts.csproj Combat.Contracts/
COPY Combat.Domain/Combat.Domain.csproj Combat.Domain/
COPY Combat.Application/Combat.Application.csproj Combat.Application/
COPY Combat.Infrastructure/Combat.Infrastructure.csproj Combat.Infrastructure/
COPY Combat.Presentation/Combat.Presentation.csproj Combat.Presentation/
COPY Combat.Test/Combat.Test.csproj Combat.Test/

RUN dotnet restore Combat.Presentation.slnx

COPY . .
RUN dotnet publish Combat.Presentation/Combat.Presentation.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080 8081

COPY --from=build /app/publish .

# Unprivileged "app" user shipped by the aspnet image.
USER $APP_UID

ENTRYPOINT ["dotnet", "Combat.Presentation.dll"]
