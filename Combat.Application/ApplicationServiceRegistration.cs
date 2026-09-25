using Combat.Application.Features.MonsterTypes;
using Combat.Application.PipelineBehavior;
using Combat.Application.Ports;
using Combat.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Combat.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IMonsterTypeCatalog, MonsterTypeCatalog>()
            .ConfigureMediatR()
            .ConfigureFluentValidation()
            .AddScoped<IHeroCombatDataProvider, HeroCombatDataProvider>();
    }

    private static IServiceCollection ConfigureMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cf =>
        {
            cf.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            cf.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cf.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services;
    }

    private static IServiceCollection ConfigureFluentValidation(this IServiceCollection services)
    {
        return services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
