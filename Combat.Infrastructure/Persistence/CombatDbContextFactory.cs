using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Combat.Infrastructure.Persistence;

/// <summary>Creates the context for EF Core commands without starting the HTTP application.</summary>
public sealed class CombatDbContextFactory : IDesignTimeDbContextFactory<CombatDbContext>
{
    public CombatDbContext CreateDbContext(string[] args)
    {
        string configurationDirectory = FindPresentationConfigurationDirectory();
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to create EF Core migrations.");

        DbContextOptions<CombatDbContext> options = new DbContextOptionsBuilder<CombatDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CombatDbContext(options);
    }

    private static string FindPresentationConfigurationDirectory()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            string presentationDirectory = Path.Combine(directory.FullName, "Combat.Presentation");
            if (File.Exists(Path.Combine(presentationDirectory, "appsettings.json")))
            {
                return presentationDirectory;
            }
        }

        throw new InvalidOperationException("Could not find Combat.Presentation/appsettings.json from the current directory.");
    }
}
