# Update Moonglade.Editor Assets

## Original Goal

Update the main Moonglade application to use the latest `Moonglade.Editor` build that adds the CodeMirror-backed HTML source view.

## Background

Moonglade consumes prebuilt `Moonglade.Editor` ESM assets from `src/Moonglade.Web/wwwroot/lib/moonglade-editor/`. The editor package lives in sibling repository `E:\GitHub\ediwang\Moonglade.Editor` and has been updated so HTML source mode supports syntax highlighting, folding, and find/replace. The main app must not gain an npm/frontend build pipeline.

Project guidance requires using `.codex/skills/update-moonglade-editor-assets/scripts/update-moonglade-editor-assets.ps1` for this sync.

## Scope

- Rebuild and verify `Moonglade.Editor`.
- Copy only `moonglade-editor.js`, `moonglade-editor.js.map`, and `moonglade-editor.css` into Moonglade's checked-in static asset folder.
- Verify copied asset hashes.
- Build `src/Moonglade.Web/Moonglade.Web.csproj`.
- Record verification outcome.

## Out of Scope

- Changing the `Moonglade.Editor` public API.
- Adding npm, Vite, webpack, Rollup, esbuild, or package locks to the main Moonglade repository.
- Copying global bundles, declarations, or unused editor build output.
- Changing admin editor integration code unless the copied build requires it.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect repository guidance and current status | None | AGENTS/skill review, `git status` | Done |
| 2 | Run editor asset sync script | Task 1 | Script completes hash verification | Done |
| 3 | Review changed Moonglade files | Task 2 | `git status --short`, diff/stat review | Done |
| 4 | Record verification and remaining risks | Task 3 | Task record update | Done |

## Execution Order

Use the bundled sync script first because it performs the intended package-side test/build, asset copy, hash verification, and Moonglade Web build in one repeatable flow. Review the resulting diff afterward to confirm no unexpected files changed.

## Current Progress

Task record created on 2026-07-29. Repository guidance and sync skill were reviewed. The asset sync script completed successfully and updated the three checked-in Moonglade.Editor assets. No Moonglade integration JavaScript changes were needed because the editor public API remained compatible.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-29 | Read Moonglade and Moonglade.Editor AGENTS guidance | Passed | Main repo requires using the project-level asset sync skill. |
| 2026-07-29 | Read `update-moonglade-editor-assets` skill and script | Passed | Script copies only ESM JS, map, and CSS assets and runs Web build. |
| 2026-07-29 | `powershell -ExecutionPolicy Bypass -File .codex/skills/update-moonglade-editor-assets/scripts/update-moonglade-editor-assets.ps1` | Passed | Ran `npm test` in `Moonglade.Editor` (94 passed), `npm run build` in `Moonglade.Editor`, copied the ESM JS/map/CSS assets, verified SHA-256 hashes, and built `src/Moonglade.Web/Moonglade.Web.csproj` successfully with 0 warnings and 0 errors. |
| 2026-07-29 | Integration code review | Passed | `admin.editpost.editor.mjs` still uses stable `createMoongladeEditor`, `syncToTextarea`, and `getHTML` APIs; no integration code update required. |
| 2026-07-29 | `git diff --check` | Passed | No whitespace errors; only normal CRLF warnings for copied/static files. |
| 2026-07-29 | `git status --short` in `Moonglade` | Passed | Changed files are the three editor assets plus this task record. |
| 2026-07-29 | `git status --short` in `Moonglade.Editor` | Passed | Clean after the package build. |

Copied asset hashes:

| File | SHA-256 |
| --- | --- |
| `moonglade-editor.js` | `9689E88A8B015340B59A086FD6ABEA12F483CA3B0BB45CA69722A8E3F0D0AA22` |
| `moonglade-editor.js.map` | `2022869D5C7620D3854F07FF1CE4B3A383219038E07E67F0D202D5EE7D595C0C` |
| `moonglade-editor.css` | `7B162B05325AE6695517577627CDC74C2B0F7E787B9CB5CE4851ACB670649E3B` |

## Issues and Resolutions

None.

## Follow-ups

Main app browser smoke testing was not run in this task. The asset sync script verified the editor package tests/build, static asset hashes, and Moonglade Web build. The editor package itself was browser-checked before this sync.

## Notes

The related editor package task record is `E:\GitHub\ediwang\Moonglade.Editor\docs\tasks\task-html-source-code-editor.md`.
