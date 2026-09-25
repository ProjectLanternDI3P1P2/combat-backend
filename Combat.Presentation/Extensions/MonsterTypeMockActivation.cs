namespace Combat.Presentation.Extensions;

public static class MonsterTypeMockActivation
{
    private const string ConfigurationKey = "MonsterTypeMock:Enabled";

    public static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        return environment.IsDevelopment()
            && bool.TryParse(configuration[ConfigurationKey], out bool isConfigured)
            && isConfigured;
    }
}
