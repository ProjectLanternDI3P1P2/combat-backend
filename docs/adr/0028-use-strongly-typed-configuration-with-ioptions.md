# Use strongly typed configuration with IOptions

Application configuration is accessed through strongly typed `IOptions<T>`
models.

## Considered Options

Reading values directly from `IConfiguration` is simple but spreads string keys
throughout the codebase.

Experience from previous projects shows that duplicated configuration keys become
difficult to refactor, validate and review, especially when they are written as
unrelated string literals.

Typed options centralize the contract and integrate with dependency injection and
startup validation.

## Consequences

Configuration concerns are represented by dedicated option classes.

Required configuration can be validated when the application starts.

Business and application code do not depend on scattered configuration key
strings.
