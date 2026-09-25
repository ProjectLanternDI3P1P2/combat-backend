using Combat.Presentation.Extensions;
using Combat.Application;
using Combat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
bool enableHeroMocks = HeroMockActivation.IsEnabled(builder.Environment, builder.Configuration);
bool enableMonsterTypeMocks = MonsterTypeMockActivation.IsEnabled(builder.Environment, builder.Configuration);

builder.ConfigureApi(enableHeroMocks);

builder.Services
    .AddInfrastructureServices(
        builder.Configuration,
        enableMonsterTypeMocks: enableMonsterTypeMocks,
        enableHeroMocks: enableHeroMocks)
    .AddApplicationServices();

var app = builder.Build();

app.ConfigureStart(enableHeroMocks);

await app.RunAsync();
