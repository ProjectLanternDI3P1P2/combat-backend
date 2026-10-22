# Configuration des communications — Combat

Référence : [architecture des communications](architecture/ARCHITECTURE_DES_COMMUNICATIONS_MICROSERVICES.fr.md), ADR-GLOB-002, 006, 008 et 009.

Cette configuration décrit la cible, même lorsque les adaptateurs sont absents.
Les futures implémentations devront lire ces sections via la configuration .NET.
Ajouter une clé ne crée pas le client, le consommateur ou l'instrumentation.
Le Compose de `frontend-game` fournit les valeurs de déploiement.

## gRPC sortant

| Variable | Adresse Compose |
| --- | --- |
| `Grpc__Player__Address` | `http://player:8081` |
| `Grpc__Rewards__Address` | `http://reward:8081` |
| `Grpc__Dungeon__Address` | `http://dungeon:8081` |

Chaque dépendance possède `Grpc__<Service>__TimeoutSeconds` (5 secondes par défaut).
Les futurs clients doivent aussi lire :
- `Grpc__Resilience__Retry__MaxAttempts=1` : pas de retry automatique par défaut.
- `Grpc__Resilience__CircuitBreaker__FailureThreshold=5`.
- `Grpc__Resilience__CircuitBreaker__BreakDurationSeconds=30`.

Ces clés/seuils sont une convention locale, pas des valeurs imposées par les ADR.
Tout retry exige une opération rejouable et la conservation du `CommandId`.

## RabbitMQ

Paramètres communs : `RabbitMq__Enabled`, `RabbitMq__HostName`,
`RabbitMq__Port`, `RabbitMq__VirtualHost`, `RabbitMq__UserName`,
`RabbitMq__Password`, `RabbitMq__ExchangeName`.
Exchange de ce service : `combat.events`.

## Autres paramètres fournis par Compose

- `ConnectionStrings__DefaultConnection` : uniquement la base de ce service.
- `Kestrel__Endpoints__Http__Url=http://+:8080`, `Protocols=Http1`.
- `Kestrel__Endpoints__Grpc__Url=http://+:8081`, `Protocols=Http2`.
- `Cors__AllowedOrigins__0` et `__1` : origines des deux fronts locaux.
- `Serilog__MinimumLevel__Default` : niveau de logs.
- `OTEL_SERVICE_NAME=combat`.
- Variables standard réservées à l'instrumentation OpenTelemetry :
  `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_SDK_DISABLED`, `OTEL_EXPORTER_OTLP_ENDPOINT`,
  `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_TRACES_EXPORTER`, `OTEL_METRICS_EXPORTER`,
  `OTEL_LOGS_EXPORTER`, `OTEL_TRACES_SAMPLER`.
  SDK désactivé par défaut ; aucun collecteur installé. Pour l'activer, fournir
  un endpoint joignable depuis le réseau interne et intégrer le SDK dans le service.

Keycloak est validé à la gateway (ADR-GLOB-004), pas dans ce service :
aucune variable de validation JWT ou de client secret n'est injectée ici.

## Image et contrats privés

Le Dockerfile expose 8080 et 8081 en interne ; Compose ne les publie pas sur l'hôte.
`NuGet.config` réserve les packages de contrats à GitHub Packages.
Les identifiants passent par le secret BuildKit `github_packages_credentials`,
injecté dans `NuGetPackageSourceCredentials_github`, au format
`Username=<login>;Password=<token>;ValidAuthenticationTypes=Basic`.
Ne pas mettre de jeton dans un ARG, une image ou un fichier versionné.

```sh
docker build --secret id=github_packages_credentials,env=GITHUB_PACKAGES_CREDENTIALS .
```
