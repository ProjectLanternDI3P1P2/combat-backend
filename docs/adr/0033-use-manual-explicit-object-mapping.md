# Use manual and explicit object mapping

Object mapping between API contracts, application models and domain objects is
implemented manually.

## Considered Options

AutoMapper, Mapster and source-generated mappers such as Mapperly could reduce
repetitive mapping code.

The project uses AI-assisted development, which already reduces the cost of
writing straightforward mapping code. Manual mapping keeps conversions visible
during review, easy to debug and free from hidden convention-based behavior.

A mapping library would add another abstraction and project-wide dependency
without solving a significant problem for this codebase.

## Consequences

Mapping code is explicit and discoverable.

No shared mapping library is introduced.

Some repetitive assignments are accepted in exchange for transparency and easier
review of generated code.
