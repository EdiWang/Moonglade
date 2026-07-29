# CodeEditor Site CSS Pilot

## Original Goal

Replace Monaco with Moonglade.CodeEditor only on the admin Appearance / Site Custom CSS settings surface as a pilot integration.

## Background

Moonglade currently uses Monaco for multiple admin code-editing surfaces. This task intentionally changes only `Pages/Admin/Settings/Appearance.cshtml` and its `wwwroot/js/app/admin.settings.appearance.mjs` module. Other Monaco consumers and the `Moonglade.MonacoEditor` package reference remain in place.

Moonglade.CodeEditor provides prebuilt static assets from the sibling `Moonglade.CodeEditor` repository, so Moonglade should not add npm, Vite, webpack, Rollup, or esbuild.

## Scope

- Add Moonglade.CodeEditor static assets under `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/`.
- Load the CodeEditor CSS/global script from the Appearance settings page.
- Initialize the CSS editor with the CodeEditor public API and sync it to the hidden textarea before settings submit.

## Out of Scope

- Post Markdown editor migration.
- Page raw HTML or page CSS migration.
- Removing Monaco assets, loader partial, or NuGet package reference.
- Changing Appearance settings persistence or validation.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect current Appearance Monaco wiring | None | File review | Done |
| 2 | Copy CodeEditor static assets | Sibling package build | File presence, Web build | Done |
| 3 | Update Appearance Razor and JavaScript wiring | Static assets present | Web build, browser check | Done |
| 4 | Verify and document risks | Implementation complete | `dotnet build`, optional local browser check | Done |

## Execution Order

Copy the prebuilt static assets first, then replace page-level script/style wiring, then update JavaScript initialization and form synchronization. Verify with a Web project build and browser check if local startup succeeds.

## Current Progress

Implementation is complete. CodeEditor static assets were copied into `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/`. `Appearance.cshtml` now loads CodeEditor CSS/global JS instead of `_MonacoLoaderScript`; `admin.settings.appearance.mjs` now creates a CSS CodeEditor instance and syncs it before settings submit. Full local browser validation was not run because app startup may create, migrate, or initialize the configured local database.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-29 | File review | Passed | `Appearance.cshtml` loads `_MonacoLoaderScript`; `admin.settings.appearance.mjs` uses Monaco helper globals. |
| 2026-07-29 | Static asset copy | Passed | Copied CSS, global JS, formatter JS, and JS source maps from sibling CodeEditor build output. |
| 2026-07-29 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |
| 2026-07-29 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | Web tests passed: 129 tests. |
| 2026-07-29 | Static reference check | Passed | Appearance page no longer references Monaco loader/helpers and now uses CodeEditor assets/API. |

## Issues and Resolutions

Full browser validation against `/admin/settings/appearance` was deferred because starting the app can touch the configured database through startup initialization and automatic migration.

## Follow-ups

- Browser-check `/admin/settings/appearance` after approving local app startup against the configured database.
- If the pilot is accepted, migrate the remaining Monaco surfaces in separate, independently verifiable tasks.

## Notes

The CodeEditor task record for the package-side view is `E:\GitHub\ediwang\Moonglade.CodeEditor\docs\tasks\task-moonglade-site-css-pilot.md`.
