using Combat.Application.Ports;
using Combat.Infrastructure.ExternalServices.Player;
using Combat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Combat.Test.Integration;

public sealed class CombatWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    /// <summary>Records the results reported to the (fake) Dungeon service.</summary>
    public RecordingDungeonCombatResultClient DungeonResults { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["RabbitMq:Enabled"] = "false",
                }
            )
        );
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CombatDbContext>>();
            services.RemoveAll<CombatDbContext>();
            services.RemoveAll<IOptions<DatabaseOptions>>();
            services.AddSingleton<IOptions<DatabaseOptions>>(
                Options.Create(new DatabaseOptions { DefaultConnection = connectionString })
            );
            services.AddDbContext<CombatDbContext>(options => options.UseNpgsql(connectionString));
        });
        // Player and Dungeon do not run in the test host: test doubles stand in for them.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHeroSnapshotClient>();
            services.AddScoped<IHeroSnapshotClient, MockHeroSnapshotClient>();
            services.RemoveAll<IDungeonCombatResultClient>();
            services.AddSingleton<IDungeonCombatResultClient>(DungeonResults);
        });
    }
}

public sealed class RecordingDungeonCombatResultClient : IDungeonCombatResultClient
{
    private readonly List<CombatResultReport> _reports = [];

    public IReadOnlyList<CombatResultReport> Reports
    {
        get
        {
            lock (_reports)
            {
                return [.. _reports];
            }
        }
    }

    public Task<string> ReportAsync(CombatResultReport report, CancellationToken cancellationToken)
    {
        lock (_reports)
        {
            _reports.Add(report);
        }

        return Task.FromResult(
            report.Result == Combat.Domain.Enums.CombatResult.Defeat ? "lost" : "active"
        );
    }
}
