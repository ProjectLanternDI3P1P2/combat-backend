using Combat.Application;
using Combat.Infrastructure;
using Combat.Infrastructure.Persistence.Seeding;
using Combat.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
bool enableMonsterTypeMocks = MonsterTypeMockActivation.IsEnabled(
    builder.Environment,
    builder.Configuration
);

builder.ConfigureApi();

builder
    .Services.AddInfrastructureServices(builder.Configuration, enableMonsterTypeMocks)
    .AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();
