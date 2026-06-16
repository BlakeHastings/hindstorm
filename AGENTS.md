# AGENTS.md

Guidance for AI coding agents working in Hindstorm. This is the canonical, tool-agnostic source of
conventions, read by Codex, Cursor, Copilot, Gemini CLI, and others. Claude Code reads it through a
one-line `CLAUDE.md` that imports this file. Human-facing onboarding lives in `README.md`.

## Project

Hindstorm is reverse event storming for .NET: it recovers an event-storming model from compiled
assemblies by reflecting over attributes, then exports it as JSON, Mermaid, or Graphviz DOT.

- `src/Hindstorm.Annotations/` — the node and edge attributes. Zero dependencies, `netstandard2.0`.
  Referenced from a consumer's domain layer, so it must stay dependency-free and trim-clean.
- `src/Hindstorm/` — the scanner and the JSON/Mermaid/DOT exporters (`netstandard2.0` + `net8.0`).
- `src/Hindstorm.AspNetCore/` — an optional dev endpoint serving the model (`net8.0`).
- `tests/Hindstorm.Tests/` — xUnit tests.
- `samples/Hindstorm.Sample/` — a runnable demo that scans its own domain and prints the diagram.

## Build and test

```bash
dotnet build Hindstorm.sln -c Release        # build everything
dotnet test Hindstorm.sln -c Release         # run the unit + integration tests
dotnet run --project samples/Hindstorm.Sample # see the sample model printed as Mermaid
```

## Conventions

`TreatWarningsAsErrors` is on and `Nullable` is enabled solution-wide, so a warning fails the build.
Library projects generate XML documentation; missing doc comments on public members are errors. The
conventions below are the ones the compiler does not enforce.

### Keep the attribute contract stable and unopinionated

The attributes in `Hindstorm.Annotations` are a public contract that consumer code is annotated with and
the scanner reads by reflection. Do not rename or repurpose an attribute, its `Kind`/`Direction`, or its
constructor shape without a deliberate, versioned change. Hindstorm ships no `AggregateRoot` or
`IDomainEvent` base type: the library stays unopinionated about a consumer's building blocks and learns
their shape only through `ScannerOptions` (for example `HandlerInterface`). Keep it that way.

### Constructors

Primary constructors are reserved for records (positional) and small lightweight types (simple wrappers,
adapters, DTOs). Logic-bearing classes use an explicit `private readonly` field plus a constructor:

```csharp
private readonly ISkillService _skills;
public SkillsCommand(ISkillService skills) => _skills = skills;
```

Use the explicit form whenever you want `readonly` enforcement on dependencies, a place for guard clauses,
or clarity about where fields come from. Primary constructors offer no performance benefit (equivalent IL).

### XML doc comments

`<summary>` is one short sentence describing *what* a thing is; rationale and design intent go in
`<remarks>`. Document the non-obvious (units, nullability meaning, ordering, side effects), not the
signature. The interface or base type owns the canonical docs; an override carries a bare `<inheritdoc />`.

### Tests

Tests are contract-first: they assert observable behavior of the public surface, derived from the
documented contract rather than the implementation. Cover both what should work and what should be
rejected. Use xUnit `Assert` only; do not add FluentAssertions (v8+ requires a paid license). When a test
needs a domain to scan, define small fixture types in a dedicated namespace rather than reusing another
test's fixtures.

## Commits and releases

This repo uses Conventional Commits and release-please. See `CONTRIBUTING.md` for the commit format and how
a release is cut and published. Do not bump version numbers by hand: MinVer derives the version from the
git tag release-please creates.
