# VersioningTool — Detailed Guide

## 1. Purpose

`build/VersioningTool` is an internal utility that computes and generates a shared build version for all projects in this repository.

Output artifact:

- `build/generated/BuildVersion.g.cs`
  - assembly/file/informational versions,
  - build number,
  - UTC build timestamp,
  - source fingerprint hash.

---

## 2. Inputs

- `build/version.base.json`
  - manually maintained base version (`major.minor.patch`)
- `build/version.state.json`
  - persisted state for automatic increments

---

## 3. Version Calculation Flow

1. Read base version.
2. Compute source fingerprint hash.
3. If hash changed:
   - increment `CurrentPatch`,
   - increment `BuildCounter`,
   - update UTC build date.
4. Generate `BuildVersion.g.cs`.
5. Persist updated state.

---

## 4. Fingerprint scope

Included file extensions:

- `.cs`, `.csproj`, `.props`, `.targets`, `.resx`, `.json`, `.sln`, `.slnx`

Excluded paths/files:

- `.git/`
- `bin/`, `obj/`
- `build/generated/`
- `build/version.state.json`
- `build/version.state.lock`

---

## 5. Locking and concurrent safety

A lock file (`build/version.state.lock`) protects state updates.

- waits up to 120 retries,
- ~100ms between retries,
- lock is removed on completion.

This prevents race conditions in concurrent builds.

---

## 6. Manual run

```bash
dotnet run --project build/VersioningTool -- /workspace/SafetyMonitor
```

If repo path is omitted, current working directory is used.

---

## 7. Build integration

The tool is executed from `Directory.Build.targets`, and generated output is included by `Directory.Build.props`, so all projects compile with the same version metadata.

---

## 8. Related files

- `../README.versioning.md`
- `../../Directory.Build.targets`
- `../../Directory.Build.props`
