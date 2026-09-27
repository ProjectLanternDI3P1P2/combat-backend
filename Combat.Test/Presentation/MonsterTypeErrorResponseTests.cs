using Combat.Application.Exceptions;
using Combat.Presentation.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Text.Json;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Presentation;

public class MonsterTypeErrorResponseTests
{
    [Fact]
    public async Task InvokeAsync_SourceUnavailable_ReturnsServiceUnavailableProblemDetails()
    {
        // Arrange
        var middleware = CreateMiddleware();
        DefaultHttpContext context = CreateContext();

        // Act
        await middleware.InvokeAsync(
            context,
            _ => throw new MonsterTypeSourceUnavailableException());

        // Assert
        JsonDocument response = await ReadResponseAsync(context);
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().Be("application/problem+json");
        response.RootElement.GetProperty("title").GetString()
            .Should().Be("Monster type source unavailable");
    }

    [Fact]
    public async Task InvokeAsync_InvalidCatalog_ReturnsExplicitCatalogErrors()
    {
        // Arrange
        var middleware = CreateMiddleware();
        DefaultHttpContext context = CreateContext();
        var exception = new InvalidMonsterTypeCatalogException(new Dictionary<string, string[]>
        {
            ["monsterTypes[0].Id"] = ["Id is required."]
        });

        // Act
        await middleware.InvokeAsync(context, _ => throw exception);

        // Assert
        JsonDocument response = await ReadResponseAsync(context);
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        response.RootElement.GetProperty("title").GetString()
            .Should().Be("Invalid monster type catalog");
        response.RootElement.GetProperty("errors")
            .GetProperty("monsterTypes[0].Id")[0]
            .GetString().Should().Be("Id is required.");
    }

    private static ExceptionHandlingMiddleware CreateMiddleware()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        return new ExceptionHandlingMiddleware(new Mock<ILogger>().Object, environment.Object);
    }

    private static DefaultHttpContext CreateContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
    }
}
