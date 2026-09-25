using Combat.Presentation.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Combat.Test.Presentation;

public class HeroMockActivationTests
{
    [Theory]
    [InlineData("Development", "true", true)]
    [InlineData("Development", "false", false)]
    [InlineData("Production", "true", false)]
    [InlineData("Staging", "true", false)]
    public void IsEnabled_EnvironmentAndOptIn_ReturnsExpectedResult(
        string environmentName,
        string configuredValue,
        bool expected)
    {
        // Arrange
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HeroMock:Enabled"] = configuredValue
            })
            .Build();

        // Act
        bool result = HeroMockActivation.IsEnabled(environment.Object, configuration);

        // Assert
        result.Should().Be(expected);
    }
}
