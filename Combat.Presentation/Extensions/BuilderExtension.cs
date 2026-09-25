using Combat.Presentation.Extensions.LogExtension;
using Combat.Presentation.Grpc.Interceptors;
using Combat.Presentation.Middleware;
using Serilog;
using System.Text.Json.Serialization;

namespace Combat.Presentation.Extensions;

public static class BuilderExtension
{
    public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();
        builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

        ConfigureLogger(builder);

        builder.Services.AddTransient<ExceptionHandlingMiddleware>();
        builder.Services.AddTransient<GrpcExceptionInterceptor>();
        builder.Services.AddHttpClient();

        return builder;
    }

    private static void ConfigureLogger(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.With<LowercaseLevelEnricher>()
                .Destructure.With<IgnoreLoggingDestructuringPolicy>();
        }, preserveStaticLogger: true);

    }
}
