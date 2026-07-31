# Moonglade Editor NuGet Migration

## Original Goal

Migrate Moonglade from checked-in Moonglade.Editor static assets and the project-local asset-sync Codex skill to the published `Moonglade.Editor.StaticAssets` NuGet package.

## Background

Moonglade targets .NET 10 and already uses `MapStaticAssets()` in `src/Moonglade.Web/Extensions/WebApplicationExtensions.cs`, so ASP.NET Core static web assets from Razor Class Library/NuGet packages are available through `/_content/{PACKAGE ID}/...`. `Moonglade.Editor.StaticAssets` 0.5.0 has been published to NuGet.org and exposes editor assets under `/_content/Moonglade.Editor.StaticAssets/moonglade-editor/`.

## Scope

- Add `Moonglade.Editor.StaticAssets` as a package reference to the Web project.
- Update editor CSS and ESM import paths to use the NuGet static web asset URL.
- Remove checked-in editor assets from `src/Moonglade.Web/wwwroot/lib/moonglade-editor/`.
- Remove the obsolete `.codex/skills/update-moonglade-editor-assets` skill.
- Update developer documentation that referenced the old copy workflow.

## Out of Scope

- Changing editor behavior or Moonglade.Editor source code.
- Publishing a new editor package version.
- Adding frontend build tooling to Moonglade.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add NuGet package reference | None | `dotnet restore` / build | Done |
| 2 | Update CSS and JavaScript asset paths | Task 1 | Search for old paths and build | Done |
| 3 | Remove old checked-in assets and skill | Tasks 1-2 | Search for old skill/path references | Done |
| 4 | Update docs | Tasks 1-3 | Markdown review | Done |
| 5 | Verify Web project | Tasks 1-4 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Done |

## Execution Order

First add the package and point runtime references at its `_content` paths. Then remove the no-longer-used local assets and sync skill. Finally update documentation and build the Web project.

## Current Progress

Implementation and verification are complete.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-31 | `rg -n "lib/moonglade-editor|update-moonglade-editor-assets" src .codex AGENTS.md README.md -g "!**/bin/**" -g "!**/obj/**"` | Passed | No remaining runtime or skill references; only the new AGENTS.md warning mentions the old path. |
| 2026-07-31 | `dotnet restore src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Restored the Web project with the new package reference. |
| 2026-07-31 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |
| 2026-07-31 | Isolated `NUGET_PACKAGES` restore/build for `src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Confirmed the published package contributes `lib/net10.0/Moonglade.Editor.StaticAssets.dll` in `project.assets.json`. |
| 2026-07-31 | Static web asset manifest inspection | Passed | Confirmed `_content/Moonglade.Editor.StaticAssets/moonglade-editor/` entries for CSS, main editor JS, and formatter JS. |

## Issues and Resolutions

The first local restore could read a stale global NuGet cache entry created during earlier package experiments. Verification was repeated with an isolated `NUGET_PACKAGES` directory, which confirmed the published NuGet.org package resolves as `net10.0`.

## Follow-ups

None yet.

## Notes

Expected editor asset base path: `/_content/Moonglade.Editor.StaticAssets/moonglade-editor/`.
