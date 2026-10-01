using Combat.Application;
using Combat.Infrastructure;
using Combat.Infrastructure.Persistence.Seeding;
using Combat.Presentation.Extensions;

// `--migrate` applies the EF Core migrations and exits. The Kubernetes init container
// runs it before the API starts (ADR-0037).
const string MigrateFlag = "--migrate";
bool migrateOnly = args.Contains(MigrateFlag);

var builder = WebApplication.CreateBuilder(args.Where(arg => arg != MigrateFlag).ToArray());
bool enableMonsterTypeMocks = MonsterTypeMockActivation.IsEnabled(
    builder.Environment,
    builder.Configuration
);

builder.ConfigureApi();

builder
    .Services.AddInfrastructureServices(builder.Configuration, enableMonsterTypeMocks)
    .AddApplicationServices();

var app = builder.Build();

if (migrateOnly)
{
    await app.Services.MigrateDatabaseAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();
