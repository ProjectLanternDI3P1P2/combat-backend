using Combat.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ILogger = Serilog.ILogger;

namespace Combat.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware(ILogger logger, IHostEnvironment environment) : IMiddleware
{
    private static readonly JsonSerializerOptions ProblemDetailsJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException exception)
        {
            logger.Warning(exception, "Resource not found");
            await HandleNotFoundExceptionAsync(context, exception);
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "Validation error occurred");
            await HandleValidationExceptionAsync(context, exception);
        }
        catch (MonsterTypeSourceUnavailableException exception)
        {
            logger.Warning(exception, "Monster type source is unavailable");
            await HandleMonsterTypeSourceUnavailableExceptionAsync(context, exception);
        }
        catch (InvalidMonsterTypeCatalogException exception)
        {
            logger.Error(exception, "Monster type source returned an invalid catalog");
            await HandleInvalidMonsterTypeCatalogExceptionAsync(context, exception);
        }
        catch (ExternalServiceUnavailableException exception)
        {
            logger.Error(exception, "External service unavailable");
            await HandleProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "Service unavailable", exception.Message);
        }
        catch (InvalidHeroCombatDataException exception)
        {
            logger.Error(exception, "Invalid hero combat data received");
            await HandleProblemAsync(context, StatusCodes.Status502BadGateway, "Invalid upstream data", exception.Message);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/404",
            Title = "Resource not found",
            Detail = exception.Message,
            Status = StatusCodes.Status404NotFound,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }

    private static async Task HandleProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => ToCamelCase(group.Key),
                group => group.Select(error => error.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "https://httpstatuses.com/422",
            Title = "Validation error",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status422UnprocessableEntity,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    private static async Task HandleMonsterTypeSourceUnavailableExceptionAsync(
        HttpContext context,
        MonsterTypeSourceUnavailableException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/503",
            Title = "Monster type source unavailable",
            Detail = exception.Message,
            Status = StatusCodes.Status503ServiceUnavailable,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, ProblemDetailsJsonOptions),
            context.RequestAborted);
    }

    private static async Task HandleInvalidMonsterTypeCatalogExceptionAsync(
        HttpContext context,
        InvalidMonsterTypeCatalogException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/500",
            Title = "Invalid monster type catalog",
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errors"] = exception.Errors;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, ProblemDetailsJsonOptions),
            context.RequestAborted);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/500",
            Title = "Internal server error",
            Detail = environment.IsDevelopment() ? exception.Message : null,
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
