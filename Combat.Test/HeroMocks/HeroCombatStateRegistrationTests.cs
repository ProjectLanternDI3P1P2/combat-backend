using Combat.Application;
using Combat.Application.Ports;
using Combat.Infrastructure;
using Combat.Infrastructure.HeroMocks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Combat.Test.HeroMocks;

public class HeroCombatStateRegistrationTests
{
    [Fact]
    public void AddServices_HeroMockDisabled_BuildsValidatedProviderWithUnavailableSource()
    {
        // Arrange
        var services = CreateServices(enableHeroMocks: false);

        // Act
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        provider.GetRequiredService<IHeroCombatStateSource>()
            .Should().BeOfType<UnavailableHeroCombatStateSource>();
    }

    [Fact]
    public void AddServices_HeroMockEnabled_ResolvesMockSource()
    {
        // Arrange
        var services = CreateServices(enableHeroMocks: true);

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IHeroCombatStateSource>()
            .Should().BeOfType<MockHeroCombatStateSource>();
    }

    private static ServiceCollection CreateServices(bool enableHeroMocks)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<Serilog.ILogger>(_ => new LoggerConfiguration().CreateLogger());
        services.AddInfrastructureServices(configuration, enableHeroMocks: enableHeroMocks);
        services.AddApplicationServices();
        return services;
    }
}
