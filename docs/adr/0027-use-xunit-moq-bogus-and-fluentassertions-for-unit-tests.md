# Use xUnit, Moq, Bogus and FluentAssertions for unit tests

Backend unit tests use xUnit, Moq, Bogus and FluentAssertions.

## Considered Options

The team already uses xUnit broadly, making it the most familiar test framework
for the squads.

Moq is a mature and widely used .NET mocking library and is used for dependency
behavior.

Bogus avoids manually constructing repetitive test data and is also useful when
creating realistic datasets for tests.

FluentAssertions produces assertions that are easier to read during human review,
which is particularly useful for code generated or modified with AI assistance.

## Consequences

All repositories use the same test vocabulary and libraries.

Tests follow Arrange, Act and Assert and focus on observable behavior.

Simple domain objects should not be mocked unnecessarily, and Bogus should improve
readability rather than make tests random or nondeterministic.
