# Shared build versioning

This repository uses a single version source for all projects.

## Files

- `build/version.base.json` — manually maintained base version (`major.minor.patch`).
- `build/version.state.json` — persisted state for automatic increments.
- `build/generated/BuildVersion.g.cs` — generated file with assembly attributes and a shared `BuildVersion` class.

## Rules

1. `major.minor` are set manually in `version.base.json`.
2. `patch` is initialized from `version.base.json` and then increments automatically.
3. `buildCounter` increments automatically.
4. `patch` and `buildCounter` increment only when source fingerprint changes.
5. Build date is stored as UTC and exposed via `BuildVersion.BuildDateUtc`.

## Fingerprint inputs

Hash includes repository files with extensions:

- `.cs`, `.csproj`, `.props`, `.targets`, `.resx`, `.json`, `.sln`, `.slnx`

Excluded paths/files:

- `.git/`, `bin/`, `obj/`, `build/generated/`
- `build/version.state.json`, `build/version.state.lock`

## Build integration

`Directory.Build.targets` runs `build/VersioningTool` before compilation (once via `DataStorage` project build path).

`Directory.Build.props` includes generated `BuildVersion.g.cs` into every project and disables SDK auto assembly info generation.
