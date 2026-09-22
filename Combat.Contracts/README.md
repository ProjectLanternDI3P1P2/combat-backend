# Combat.Contracts

Versioned Protocol Buffers contract and generated C# gRPC client/server types for
the Combat service.

## Installation

Configure the organisation's GitHub Packages NuGet source, then pin a released
version of the package:

```xml
<PackageReference Include="Combat.Contracts" Version="0.1.1" />
```

For a C# gRPC consumer, also reference `Grpc.Net.Client`, create a channel for the
Combat service's internal endpoint, then construct
`Combat.Contracts.V1.CombatPlayerService.CombatPlayerServiceClient`.

The original `.proto` source is included in this package under `proto/`.
