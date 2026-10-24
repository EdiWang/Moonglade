# Single Configuration Default List

## Original Goal

Maintain one list of runtime configuration defaults instead of separate loading and startup-persistence lists.

## Background

`BlogConfig.LoadFromConfig` lists setting types and assigns model defaults. `ConfigInitializer.SettingProviders` repeats the same types to create default JSON for persistence. This creates a maintenance mismatch risk and generates runtime and persisted defaults separately, including the new-installation manifest version and installation timestamp.

## Scope

Keep the typed assignment list in `BlogConfig`. Return the exact missing default JSON snapshots from loading and pass the new-installation flag to select the manifest default once. Have `ConfigInitializer` persist these snapshots without a second provider list. Update callers, regression tests, and developer documentation.

## Out of Scope

No reflection registry, new abstraction, dependency, settings value change, database schema change, or migration-policy change. Retain initialization order, persisted-setting precedence, malformed-JSON fallback, cancellation, and error propagation.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect loaders, default models, callers, and tests | None | Reference search | Complete |
| 2 | Consolidate default loading and persistence | 1 | Configuration and Setup tests | Complete |
| 3 | Verify integration and update documentation | 2 | Web tests/build and diff review | Complete |

## Execution Order

Inspect behavior, consolidate the loader's existing typed list, add behavior regressions, then verify startup and Web integration.

## Current Progress

The loader now returns default JSON snapshots and assigns the same values to memory. Removed the Setup provider dictionary, missing-provider paths, and shared mutable missing-key list. The existing typed assignments are the only default initialization list. Regression tests cover all settings, new/existing installations, persisted-value precedence, malformed JSON, repeated loads, and startup persistence. Updated `AGENTS.md`; no README or deployment changes are required.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-24 | Inspect repository and references | Complete | Clean starting tree; Setup is the only production loader caller. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore --verbosity quiet` | Passed | 35 tests. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore --verbosity quiet` | Existing failures | 23 passed, 2 unchanged migration-script version assertions failed. All configuration initializer and startup tests passed. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore --verbosity quiet` | Passed | 251 tests. |
| 2026-10-24 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero warnings or errors. |
| 2026-10-24 | Reference search and `git diff --check` | Passed | No duplicate provider list remains; no whitespace errors. |

## Issues and Resolutions

Two migration-script version assertions were already failing before this task on 16.9.0-preview.1 with the scripts' 16.8.1 marker. Keep this independent issue outside the refactor.

New installations previously assigned the fallback manifest to runtime while separately persisting the new-installation manifest. The loader now selects the correct variant once, and the returned JSON includes the same installation timestamp assigned to runtime.

## Follow-ups

No remaining implementation work. Browser testing and live SQL Server/PostgreSQL migrations were not run; this refactor does not alter UI, schema, queries, or migration policy. The two existing migration-script version assertions remain a separate issue.
