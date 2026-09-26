using Combat.Application;
using Combat.Infrastructure;
using Combat.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApi();

builder.Services.AddInfrastructureServices(builder.Configuration).AddApplicationServices();

var app = builder.Build();

app.ConfigureStart();

await app.RunAsync();
