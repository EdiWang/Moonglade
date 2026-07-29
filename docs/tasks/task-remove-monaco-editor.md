# Remove Monaco Editor Artifacts

## Original Goal

Remove the remaining Monaco Editor traces after the user verified the Moonglade.CodeEditor migrations in the admin UI.

## Background

Moonglade.CodeEditor now handles the four Moonglade code-like editing surfaces: Markdown post content, Raw HTML page content, Page CSS, and Site custom CSS. The final cleanup should remove only the runtime integration artifacts that were kept during the staged migration.

## Scope

- Remove the `Moonglade.MonacoEditor` NuGet package reference from `Moonglade.Web`.
- Delete the old Monaco loader partial.
- Delete the old Markdown inline-attachment scripts that only supported Monaco.
- Remove global `.monaco-target` CSS.
- Remove the legacy form-submit fallback that called Monaco helper globals.
- Update integration documentation and task records.
- Remove unrelated local `Monaco` font fallback names so application source searches are clean.

## Out of Scope

- Rewriting historical task records that mention Monaco as migration background.
- Changing Moonglade.CodeEditor public API or static assets.
- Changing editor UX beyond deleting the obsolete Monaco integration path.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Locate remaining active Monaco runtime artifacts | Completed CodeEditor migrations | Repository search | Done |
| 2 | Remove package, partial, scripts, styles, legacy sync fallback, and local font fallback names | Task 1 | Static search and build | Done |
| 3 | Update docs and task records | Task 2 | Markdown review | Done |
| 4 | Run verification | Task 2 | `dotnet restore`, `dotnet build`, Web tests | Done |

## Execution Order

Deleted runtime artifacts first so static search could reveal any broken references. Then updated documentation to reflect that Monaco is no longer part of the active admin editing stack. Finally ran restore/build/test verification.

## Current Progress

Cleanup implementation and verification are complete.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-29 | User browser verification before cleanup | Passed | User confirmed the migrated CodeEditor page works and requested Monaco cleanup. |
| 2026-07-29 | Repository search before cleanup | Passed | Active Monaco artifacts were limited to the package reference, loader partial, inline-attachment scripts, `.monaco-target` CSS, and legacy sync fallback. |
| 2026-07-29 | `node --check src\Moonglade.Web\wwwroot\js\app\admin.editor.module.mjs` | Passed | Post editor form-sync module parsed successfully after removing the legacy fallback. |
| 2026-07-29 | Source search after cleanup | Passed | No active Monaco runtime markers remained under `src/`. Historical task docs and generated third-party files were excluded from runtime cleanup. |
| 2026-07-29 | Application source `Monaco` search after cleanup | Passed | No `Monaco` or `monaco` hits remained under application `src/` after excluding third-party Bootstrap assets. |
| 2026-07-29 | `dotnet restore src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Restore remained up to date after removing the Monaco package reference. |
| 2026-07-29 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |
| 2026-07-29 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | Web tests passed: 129 tests. |

## Issues and Resolutions

Historical task documents intentionally keep Monaco references as migration background. Generated third-party Bootstrap source maps can also contain the word `Monaco`; these are not editor integration artifacts.

## Follow-ups

- Continue normal browser regression checks for CodeEditor-backed admin pages after future editor changes.

## Notes

The active replacement editor assets remain under `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/`.
