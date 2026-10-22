# Default Editor in Admin Settings

## Original Goal

Move `DefaultEditor` from application configuration into the admin settings UI. Ask the user about unresolved behavior before choosing an implementation.

## Background and Scope

`PostController.GetMeta` reads the application configuration to select the editor for new posts. `StartUpInitializer` also reads it when filling empty historical post content types. `ContentSettings` already supports database persistence, runtime updates, and the Content settings form.

Affected areas: configuration model, admin Content UI and localization resources, post metadata, legacy startup handling, matching tests, application configuration, and developer documentation. Existing posts with explicit content types retain their format.

## Task Breakdown and Execution Order

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect readers, settings persistence, and tests; confirm UI and historical behavior | None | Repository searches and nearby implementation | Complete |
| 2 | Implement the confirmed model, UI, metadata, and startup changes | 1 | Focused tests and Web build | Complete |
| 3 | Update configuration documentation and review the final diff | 2 | Searches, diff checks, and test results | Complete |

## Current Progress

The working tree was clean before work. The user confirmed Settings > Content, HTML / Markdown, HTML default, immediate effect for new posts. They explicitly chose to delete both the old application configuration and historical content-type backfill, without importing old values.

Implemented the persisted setting with enum validation, the existing settings save flow, post metadata updates, four localized labels/help texts, and removal of startup backfill and unused configuration dependencies. Added focused tests for defaults, older settings JSON, serialization, validation, persistence, runtime changes, existing post format, and startup preserving empty historical content types.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-22 | Inspect configuration readers, settings, callers, and tests | Complete | No implementation changes yet; .NET SDK 10.0.402 available |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj` | Passed | 22 tests |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 204 tests |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` | Passed | 22 tests |
| 2026-10-22 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero errors; six existing NU1902/NU1903 warnings for System.Security.Cryptography.Pkcs |
| 2026-10-22 | Review diff and search remaining configuration readers | Passed | No application-level DefaultEditor readers or historical backfill remain |

## Issues and Resolutions

The application configuration previously controlled both authoring defaults and historical content interpretation. The user explicitly chose to retire the historical backfill rather than replace it. No schema change or historical data rewrite is needed.

## Follow-ups

No implementation work remains. Browser interaction was not exercised. Existing System.Security.Cryptography.Pkcs dependency vulnerability warnings remain outside this task. No new dependencies or unrelated refactoring were added.
