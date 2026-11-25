using Combat.Presentation.Extensions.LogExtension;
using Combat.Presentation.Grpc.Interceptors;
using Combat.Presentation.Middleware;
using Serilog;
using Serilog.Templates;

namespace Combat.Presentation.Extensions;

public static class BuilderExtension
{
    private const string ConsoleJsonTemplate =
        """
        { {
            timestamp: @t,
            level: level,
            service: 'combat-backend',
            origin: 'backend',
            message: @m,
            properties: Rest(),
            exception: if @x is not null then {
                type: TypeOf(@x),
                message: Inspect(@x).Message,
                stack: Inspect(@x).StackTrace
            } else undefined()
        } }
        """ + "\n";

    public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
    {
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
                    .Enrich.With<SensitiveDataEnricher>()
                    .Destructure.With<IgnoreLoggingDestructuringPolicy>()
                    .WriteTo.Console(new ExpressionTemplate(ConsoleJsonTemplate));
            },
            preserveStaticLogger: true
        );
    }
}
