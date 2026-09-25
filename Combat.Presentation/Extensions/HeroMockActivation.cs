namespace Combat.Presentation.Extensions;

public static class HeroMockActivation
{
    private const string ConfigurationKey = "HeroMock:Enabled";

    public static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        return environment.IsDevelopment()
            && bool.TryParse(configuration[ConfigurationKey], out bool isConfigured)
            && isConfigured;
    }
}
