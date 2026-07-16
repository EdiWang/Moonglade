# Content Moderator Keywords

## Original Goal

Move ContentModerator keywords out of `appsettings.json` and into database-backed comment settings so administrators can configure the keyword list on `/admin/settings/comment`.

## Background

Moonglade stores runtime blog settings in the `BlogConfiguration` table through `IBlogSettings<T>` models. `CommentSettings` already backs the comment settings form and is saved through `SettingsController.Comment`. Content moderation now supports only local filtering, so the local keyword list should live with comment settings instead of host configuration.

## Scope

List what this task will change.

- Add a word-filter keyword setting to `CommentSettings`.
- Bind the keyword setting on the admin comment settings page.
- Make local moderation read keywords from the current database-backed blog configuration.
- Remove remaining `ContentModerator` host configuration from `appsettings.json`.
- Update tests and developer-facing docs.

## Out of Scope

- Adding a separate keyword management page.
- Changing comment moderation modes or approval workflow.
- Reintroducing remote moderation providers.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect settings persistence and moderation service wiring | None | Code inspection | Complete |
| 2 | Move keywords into `CommentSettings` and update moderation DI | Task 1 | Moderation tests, Web build | Complete |
| 3 | Add admin UI and localization resources | Task 2 | Web build | Complete |
| 4 | Update docs and remove appsettings configuration | Task 2 | Search and build | Complete |
| 5 | Run focused tests and final diff review | Tasks 2-4 | `dotnet test`, `dotnet build` | Complete |

## Execution Order

First update the durable settings model and service boundary, then update the admin form and localized labels, then remove stale appsettings/docs references, and finally run focused tests/builds.

## Current Progress

The keyword list now lives in `CommentSettings.WordFilterKeywords`, is editable from `/admin/settings/comment`, and is read at runtime through an `IModerationKeywordProvider` backed by `IBlogConfig`. The `ContentModerator` section has been removed from `appsettings.json`.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-04 | Code inspection | Complete | `CommentSettings` is persisted as a whole via `SettingsController.Comment`. |
| 2026-07-04 | `dotnet test src/Tests/Moonglade.Moderation.Tests/Moonglade.Moderation.Tests.csproj` | Passed | 49 tests passed. |
| 2026-07-04 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 93 tests passed. |
| 2026-07-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | 0 warnings, 0 errors. |

## Issues and Resolutions

- Running Web tests and Web build in parallel on Windows briefly hit a `VBCSCompiler` file lock on `Moonglade.Web.dll`. Re-running the build after tests completed succeeded.

## Follow-ups

No known follow-ups.

## Notes

`CommentSettings.DefaultValue` must include a safe default for the new property so existing installations get the field when the setting is initialized or reserialized.
