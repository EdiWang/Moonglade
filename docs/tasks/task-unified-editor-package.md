# Unified Editor Package

## Original Goal

Merge Moonglade.Editor and Moonglade.CodeEditor into one editor package so Moonglade admin pages initialize different editor modes through parameters instead of loading separate editor libraries.

## Background

Moonglade currently consumes two sibling TypeScript editor packages as prebuilt static assets:

- `Moonglade.Editor` provides ProseMirror-based rich HTML post editing.
- `Moonglade.CodeEditor` provides CodeMirror-based Markdown, raw HTML, and CSS editing.

The main Moonglade application stores post content as `EditorContent` plus `ContentType` (`html` or `markdown`), so the backend content model does not require two frontend packages. The preferred architecture is one package with a mode-based public API and internal dual engines: ProseMirror for rich HTML and CodeMirror for code-like modes.

## Scope

- Add CodeMirror code-editing modes to the `Moonglade.Editor` package.
- Add a mode-based unified creation API while preserving compatibility exports during migration.
- Update package build output to include the formatter runtime needed by code modes.
- Replace Moonglade admin page references to `Moonglade.CodeEditor` with the unified `Moonglade.Editor` package.
- Remove checked-in `moonglade-code-editor` static assets after migration.
- Update relevant developer documentation.

## Out of Scope

- Rewriting rich HTML editing to use CodeMirror.
- Rewriting Markdown/CSS/raw HTML editing to use ProseMirror.
- Changing Moonglade post storage or renderer behavior.
- Adding new editor features beyond the existing two packages' capabilities.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Capture repo status and create task record | None | Git status clean | Done |
| 2 | Merge CodeEditor dependencies and source into Moonglade.Editor | Task 1 | TypeScript build | Done |
| 3 | Add unified mode-based API and compatibility exports | Task 2 | Vitest package tests | Done |
| 4 | Build unified editor assets and copy them into Moonglade | Task 3 | Build output and file checks | Done |
| 5 | Switch Moonglade admin integrations to unified package | Task 4 | Web build and static reference check | Done |
| 6 | Update docs and run final verification | Task 5 | Targeted builds/tests | Done |

## Execution Order

Start in `Moonglade.Editor` because the main app should consume only prebuilt static assets. After the unified package builds and tests pass, copy the release assets into the main Moonglade repository, then update Razor/JavaScript integrations and documentation.

## Current Progress

Completed. Unified editor package source, build scripts, tests, and documentation have been updated in `Moonglade.Editor`. Built assets have been copied into the main Moonglade application. Admin post, page, and appearance editor integrations now initialize `createMoongladeEditor({ mode })` from `/lib/moonglade-editor/moonglade-editor.js`.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-30 | `git status --short` in Moonglade, Moonglade.Editor, Moonglade.CodeEditor | Passed | All three repositories were clean before changes. |
| 2026-07-30 | `npm run types` in Moonglade.Editor | Passed | Unified package type declarations compile. |
| 2026-07-30 | `npm test` in Moonglade.Editor | Passed | 5 test files, 97 tests. |
| 2026-07-30 | `npm run build` in Moonglade.Editor | Passed | Generated JS, CSS, formatter runtime, and type declarations within updated size budgets. |
| 2026-07-30 | Static search for `MoongladeCodeEditor`, `moonglade-code-editor`, and `createMoongladeCodeEditor` in active Moonglade app files | Passed | No active main-app references remain outside historical task docs. |
| 2026-07-30 | `node --check` for edited Moonglade admin `.mjs` files | Passed | Post editor, page editor, and appearance settings modules parse successfully. |
| 2026-07-30 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |

## Issues and Resolutions

- Local policy rejected a broad recursive delete attempt for the old static asset directory. The tracked `moonglade-code-editor` asset files were removed with explicit patch operations instead.
- `npm install` reported one high-severity audit issue in the editor package dependency graph. It was not auto-fixed in this task because `npm audit fix` may introduce broader dependency changes outside the merge scope.

## Follow-ups

- Consider removing compatibility exports after one release cycle if no consumers use the old `createMoongladeCodeEditor` global.
- Revisit bundle splitting if a single unified entry causes unacceptable page load size.

## Notes

The unified package should keep Moonglade free of npm/frontend build requirements. The main application should continue consuming checked-in static assets.
