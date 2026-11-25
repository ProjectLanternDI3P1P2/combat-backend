using System.Text.Json.Serialization;
using Combat.Application;
using Combat.Infrastructure;
using Combat.Infrastructure.Persistence;
using Combat.Infrastructure.Persistence.Seeding;
using Combat.Presentation.Extensions;
using Microsoft.EntityFrameworkCore;

bool migrateOnly = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(
    args.Where(argument => argument != "--migrate").ToArray()
);
builder.ConfigureApi();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
);

builder.Services.AddInfrastructureServices(builder.Configuration).AddApplicationServices();

var app = builder.Build();

if (migrateOnly)
{
    await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
    {
        CombatDbContext database = scope.ServiceProvider.GetRequiredService<CombatDbContext>();
        if (!database.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No database migrations are packaged in this image."
            );
        }

        await database.Database.MigrateAsync();
    }
    app.Logger.LogInformation("Database migrations completed; exiting without starting the API.");
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();
