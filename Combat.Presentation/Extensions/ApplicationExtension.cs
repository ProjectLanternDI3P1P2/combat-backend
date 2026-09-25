using Combat.Presentation.Hubs;
using Combat.Presentation.Middleware;
using Scalar.AspNetCore;

namespace Combat.Presentation.Extensions;

public static class ApplicationExtension
{
    public static WebApplication ConfigureStart(this WebApplication app, bool enableHeroMocks = false)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.MapControllers();
        app.MapGrpcServices();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        if (enableHeroMocks)
        {
            app.MapHub<CombatHub>("/hubs/combats");
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opt =>
            {
                opt.Title = "Combat API";
                opt.Theme = ScalarTheme.DeepSpace;
            });
        }

        return app;
    }
}
