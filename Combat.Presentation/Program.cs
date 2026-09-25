using Combat.Presentation.Extensions;
using Combat.Application;
using Combat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
bool enableMonsterTypeMocks = MonsterTypeMockActivation.IsEnabled(builder.Environment, builder.Configuration);

builder.ConfigureApi();

builder.Services
    .AddInfrastructureServices(builder.Configuration, enableMonsterTypeMocks)
    .AddApplicationServices();

var app = builder.Build();

app.ConfigureStart();

await app.RunAsync();
