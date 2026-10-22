using Combat.Presentation.Extensions.LogExtension;
using Combat.Presentation.Grpc.Interceptors;
using Combat.Presentation.Middleware;
using Serilog;

namespace Combat.Presentation.Extensions;

public static class BuilderExtension
{
    /// <summary>
    /// Lets a frontend dev server on another port call the service directly. Browsers
    /// normally reach services through the API Gateway, which owns CORS (ADR-GLOB-003):
    /// the policy is only applied in Development, for the origins in Cors:AllowedOrigins.
    /// </summary>
    public const string LocalFrontendsCorsPolicy = "LocalFrontends";

    public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
    {
        string[] allowedOrigins =
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
            options.AddPolicy(
                LocalFrontendsCorsPolicy,
                policy =>
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("X-Correlation-Id")
            )
        );

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<CorrelationIdInterceptor>();
            options.Interceptors.Add<GrpcExceptionInterceptor>();
        });

        ConfigureLogger(builder);

        builder.Services.AddTransient<ExceptionHandlingMiddleware>();
        builder.Services.AddTransient<CorrelationIdInterceptor>();
        builder.Services.AddTransient<GrpcExceptionInterceptor>();
        builder.Services.AddHttpClient();

        return builder;
    }

    private static void ConfigureLogger(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.With<LowercaseLevelEnricher>()
                    .Destructure.With<IgnoreLoggingDestructuringPolicy>();
            },
            preserveStaticLogger: true
        );
    }
}
