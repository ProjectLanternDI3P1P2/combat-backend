namespace Combat.Infrastructure.Grpc.Configuration;

public sealed class DungeonGrpcClientOptions
{
    public const string SectionName = "Grpc:Dungeon";

    public string Address { get; init; } = "http://localhost:8081";
    public int TimeoutSeconds { get; init; } = 5;
}
