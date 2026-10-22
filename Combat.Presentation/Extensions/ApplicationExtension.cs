using Combat.Presentation.Middleware;
using Scalar.AspNetCore;

namespace Combat.Presentation.Extensions;

public static class ApplicationExtension
{
    public static WebApplication ConfigureStart(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(BuilderExtension.LocalFrontendsCorsPolicy);
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.MapControllers();
        app.MapGrpcServices();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opt =>
            {
                opt.Title = "Combat API";
                opt.Theme = ScalarTheme.DeepSpace;
                opt.AddApiKeyAuthentication("UserId", scheme => scheme.WithName("X-User-Id"));
                opt.EnablePersistentAuthentication();
            });
        }

        return app;
    }
}
