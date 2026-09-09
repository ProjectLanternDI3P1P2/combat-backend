# Use a shared backend repository template as an initialization baseline

All backend microservices start from a shared GitHub repository template that
provides the expected project structure, tooling and baseline conventions.

The template is only an initialization baseline. Once a microservice is created,
it evolves independently and does not track later template changes.

## Considered Options

A `dotnet new` template or shared package would provide stronger automation, but
would also add maintenance and versioning overhead that is unnecessary for this
project.

With AI-assisted development, a concrete example repository is enough to clone,
rename and adapt quickly. It also gives developers who are less familiar with
Clean Architecture or the selected technologies a complete structure they can
inspect directly.

Using documentation only would not provide the same executable starting point.

## Consequences

All squads start with the same architecture, CI setup and code organization,
which reduces bootstrap work and differences between repositories.

The template is not a shared runtime dependency. Microservices may diverge when
their own requirements justify it, and improvements made later to the template
are not automatically propagated to existing services.
