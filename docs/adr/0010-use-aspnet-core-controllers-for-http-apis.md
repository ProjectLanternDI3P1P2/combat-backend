# Use ASP.NET Core Controllers for HTTP APIs

HTTP APIs are implemented with ASP.NET Core Controllers.

## Considered Options

Minimal APIs were considered as an alternative implementation style.

Controllers are already used by the developers across their existing C# projects,
so they require no new conventions or mental model. They also provide a clear and
predictable structure for APIs that will grow over time.

Minimal APIs would not provide enough benefit to justify introducing another API
style across multiple squads.

## Consequences

HTTP endpoints are organized consistently in controller classes across all
microservices.

Controllers remain thin and delegate application behavior through the Application
layer.

The project accepts the additional ceremony of Controllers in exchange for a
more familiar and uniform structure.
