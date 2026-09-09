# Use C# and .NET 10 for backend services

All backend microservices are implemented in C# on .NET 10.

.NET 10 is selected because it is the latest Long Term Support release available
for the project.

## Considered Options

No other language was seriously considered. C# is known by the whole cohort and
is already used extensively in training and professional projects by most team
members.

The .NET ecosystem is mature, well supported and well suited to building
microservices, HTTP APIs, persistence layers and distributed applications.

Choosing another language would increase onboarding and review costs without
providing a clear project benefit.

## Consequences

All squads work with a common language and runtime they already understand.

Libraries, code reviews, debugging practices and operational tooling remain
consistent across backend services.

Future runtime upgrades should favor supported LTS releases unless a project-wide
decision states otherwise.
