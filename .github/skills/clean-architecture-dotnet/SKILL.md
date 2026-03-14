---
name: clean-architecture-dotnet
description: 'Clean Architecture patterns for .NET 10 projects using WolverineFx CQRS, EF Core, FluentValidation, Keycloak OIDC, and CSharpier. Use for all .NET backend work across CaddyAdmin.Api, VaultFacade, and ArcaneVaultBridge.'
argument-hint: Handler, entity, migration, or architecture question
---

# Clean Architecture .NET

## When to Use

- Building or modifying handlers, entities, or services in .NET projects.
- Setting up WolverineFx command/query handlers.
- Working with EF Core DbContext, repositories, or migrations.
- Adding FluentValidation validators.
- Structuring Clean Architecture layers correctly.
- Ensuring Roslyn analyzer compliance.

## Layer Structure

```
Project.Domain/
├── Entities/           # Domain entities with behavior
├── Enums/              # All categorical values (MANDATORY enums)
├── ValueObjects/       # Immutable value types (DomainId, SecretPath)
├── Errors/             # Domain-specific error types
└── Events/             # Domain events

Project.Application/
├── Commands/
│   ├── CreateThing/
│   │   ├── CreateThingCommand.cs       (< 30 lines)
│   │   ├── CreateThingHandler.cs       (< 150 lines)
│   │   └── CreateThingValidator.cs     (< 80 lines)
│   └── UpdateThing/
│       └── ...
├── Queries/
│   ├── GetThing/
│   │   ├── GetThingQuery.cs
│   │   └── GetThingHandler.cs
│   └── ListThings/
│       └── ...
├── DTOs/               # Data transfer objects
├── Interfaces/         # Repository and service abstractions
└── Mappers/            # Entity ↔ DTO mapping

Project.Infrastructure/
├── Persistence/
│   ├── DbContext.cs
│   ├── Configurations/  # IEntityTypeConfiguration<T>
│   ├── Migrations/      # Auto-generated only
│   └── Repositories/    # Implement Application interfaces
├── External/            # Third-party API clients
└── DependencyInjection.cs

Project.Api/
├── Controllers/         # THIN: extract → dispatch → return
├── Middleware/
└── Program.cs
```

## WolverineFx Handler Pattern

### Commands (State Changes)

```csharp
// ✅ Correct: Focused command + handler
public record CreateDomainCommand(string Name, string Target, AccessLevel Access);

public class CreateDomainHandler
{
    public async Task<DomainDto> Handle(
        CreateDomainCommand command,
        IDomainRepository repository,
        CancellationToken ct)
    {
        var domain = Domain.Create(command.Name, command.Target, command.Access);
        await repository.AddAsync(domain, ct);
        return DomainMapper.ToDto(domain);
    }
}
```

### Queries (Read-Only)

```csharp
// ✅ Correct: Query returns data without side effects
public record GetDomainByIdQuery(DomainId Id);

public class GetDomainByIdHandler
{
    public async Task<DomainDto?> Handle(
        GetDomainByIdQuery query,
        IDomainRepository repository,
        CancellationToken ct)
    {
        var domain = await repository.GetByIdAsync(query.Id, ct);
        return domain is null ? null : DomainMapper.ToDto(domain);
    }
}
```

## Thin Controller Rule

Controllers ONLY: extract params → dispatch → return response.

```csharp
// ✅ Correct: Thin controller
[HttpPost]
public async Task<ActionResult<DomainDto>> Create(
    [FromBody] CreateDomainRequest request,
    [FromServices] IRequestDispatcher dispatcher)
{
    var command = new CreateDomainCommand(request.Name, request.Target, request.Access);
    var result = await dispatcher.InvokeAsync<DomainDto>(command);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

```csharp
// ❌ Prohibited: Business logic in controller
[HttpPost]
public async Task<ActionResult> Create([FromBody] CreateDomainRequest request)
{
    if (await _repo.ExistsAsync(request.Name)) // ❌ logic in controller
        return Conflict();
    var domain = new Domain(request.Name);     // ❌ entity creation in controller
    await _repo.AddAsync(domain);
    return Ok();
}
```

## Enum Usage (MANDATORY)

All categorical values MUST be enums. Location: `Project.Domain/Enums/`.

```csharp
// ✅ Correct
public enum AccessLevel { Public, Authenticated, Admin }
public enum AuditAction { Created, Updated, Deleted, Reloaded }

// ❌ Prohibited
public string Status { get; set; } // magic strings
if (role == "admin") // string comparison
```

## EF Core Rules

1. **One DbContext per project** with descriptive name.
2. **Fluent API** in `OnModelCreating` via `IEntityTypeConfiguration<T>`.
3. **Auto-generated migrations only** — never write SQL manually.
4. **Never modify** generated migration files after creation.
5. **Repository interfaces** defined in Application, implemented in Infrastructure.

## FluentValidation Pattern

```csharp
public class CreateDomainValidator : AbstractValidator<CreateDomainCommand>
{
    public CreateDomainValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(253)
            .Matches(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9\-]*[a-z0-9])?)*$");

        RuleFor(x => x.Target).NotEmpty();
        RuleFor(x => x.Access).IsInEnum();
    }
}
```

## VS Code Tasks (MANDATORY)

Use predefined tasks instead of manual `dotnet` commands:

| Task | Purpose |
|------|---------|
| `build (Debug)` | Compile debug build |
| `build (Release)` | Compile release build |
| `run api (Debug)` | Start development server |
| `watch api` | Hot-reload development |
| `test (Release)` | Run all tests |
| `format (csharpier)` | Format all code |
| `verify (Release)` | Build + test + format check |

## File Size Limits

| File Type | Max Lines |
|-----------|-----------|
| Handlers, services | 150 |
| Entities with invariants | 200 |
| Validators, DTOs | 100 |
| Utilities, helpers | 100 |

## Guardrails

- ✅ Always: Consult Context7 for .NET, EF Core, Wolverine, FluentValidation docs.
- ✅ Always: Use Serena for code exploration before reading full files.
- ✅ Always: Run `verify (Release)` task before completing work.
- ✅ Always: CSharpier formatting (the only formatter).
- ⚠️ Ask First: Schema/migration changes, new NuGet dependencies.
- 🚫 Never: Disable Roslyn analyzers or suppress warnings.
- 🚫 Never: Write manual SQL in migration files.
- 🚫 Never: Put business logic in controllers.
