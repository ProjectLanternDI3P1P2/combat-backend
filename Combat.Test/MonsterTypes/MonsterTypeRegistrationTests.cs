using Combat.Application;
using Combat.Application.Ports;
using Combat.Infrastructure;
using Combat.Infrastructure.MonsterMocks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Combat.Test.MonsterTypes;

public class MonsterTypeRegistrationTests
{
    [Fact]
    public void AddServices_MockEnabled_ResolvesMockSourceAndValidatedCatalog()
    {
        // Arrange
        ServiceProvider provider = CreateServices(enableMonsterTypeMocks: true);

        // Act
        IMonsterTypeSource source = provider.GetRequiredService<IMonsterTypeSource>();
        IMonsterTypeCatalog catalog = provider.GetRequiredService<IMonsterTypeCatalog>();

        // Assert
        source.Should().BeOfType<MockMonsterTypeSource>();
        catalog.Should().NotBeNull();
    }

    [Fact]
    public void AddServices_MockDisabled_ResolvesUnavailableSourceWithoutFallback()
    {
        // Arrange
        ServiceProvider provider = CreateServices(enableMonsterTypeMocks: false);

        // Act
        IMonsterTypeSource source = provider.GetRequiredService<IMonsterTypeSource>();

        // Assert
        source.Should().BeOfType<UnavailableMonsterTypeSource>();
    }

    private static ServiceProvider CreateServices(bool enableMonsterTypeMocks)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructureServices(
            configuration,
            enableMonsterTypeMocks: enableMonsterTypeMocks);
        services.AddApplicationServices();
        return services.BuildServiceProvider();
    }
}
