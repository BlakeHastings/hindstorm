# Contributing to Hindstorm

Thanks for helping out. This project automates versioning and publishing from your commit messages, so
the one thing worth reading before you open a PR is the commit format.

## Build and test

```bash
dotnet build Hindstorm.sln -c Release
dotnet test Hindstorm.sln -c Release
```

`TreatWarningsAsErrors` and `Nullable` are on, so the build is strict. Tests use xUnit; please keep new
tests contract-first (assert observable behavior of the public surface, cover both success and rejection)
and use xUnit `Assert` rather than FluentAssertions.

## Conventional Commits

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org). The prefix decides how
the version moves:

| Prefix | Meaning | Version effect (pre-1.0) |
| --- | --- | --- |
| `feat:` | a new capability | minor bump (0.1.0 → 0.2.0) |
| `fix:` | a bug fix | patch bump (0.1.0 → 0.1.1) |
| `docs:`, `test:`, `chore:`, `ci:`, `refactor:` | no shipped behavior change | no release |
| `feat!:` or a `BREAKING CHANGE:` footer | breaking change | minor while pre-1.0, major once at 1.0+ |

Examples:

```
feat: add an ExternalSystem concept attribute
fix: escape pipe characters in Mermaid edge labels
docs: expand the AspNetCore endpoint example
```

Scope changes that do not affect the published packages (samples, docs, CI) to a non-`feat`/`fix` type so
they do not trigger a release on their own.

## How a release happens

You do not tag or bump versions by hand.

1. Your commits land on `main`.
2. [release-please](https://github.com/googleapis/release-please) maintains a "Release PR" that bumps the
   version from the commit history and updates `CHANGELOG.md`.
3. Merging that Release PR creates the `vX.Y.Z` tag and a GitHub Release.
4. MinVer reads the tag and stamps it into the assemblies; the release workflow packs and pushes
   `Hindstorm.Annotations`, `Hindstorm`, and `Hindstorm.AspNetCore` to nuget.org via OIDC trusted
   publishing (no stored API key).

So: write good Conventional Commits, and the version, changelog, tag, and publish take care of themselves.
