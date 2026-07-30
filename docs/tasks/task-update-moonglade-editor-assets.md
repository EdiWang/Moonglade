# Update Moonglade.Editor Assets

## Original Goal

Update the main Moonglade application to use the latest `Moonglade.Editor` build.

## Background

Moonglade consumes prebuilt `Moonglade.Editor` ESM assets from `src/Moonglade.Web/wwwroot/lib/moonglade-editor/`. The editor package lives in sibling repository `E:\GitHub\ediwang\Moonglade.Editor`. The main app must not gain an npm/frontend build pipeline.

Project guidance requires using `.codex/skills/update-moonglade-editor-assets/scripts/update-moonglade-editor-assets.ps1` for this sync.

## Scope

- Rebuild and verify `Moonglade.Editor`.
- Copy only `moonglade-editor.js`, `moonglade-editor.js.map`, `moonglade-editor.css`, `moonglade-editor.formatter.js`, and `moonglade-editor.formatter.js.map` into Moonglade's checked-in static asset folder.
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

Task record created on 2026-07-29 and reused on 2026-07-30 for a new asset sync. Repository guidance and sync skill were reviewed. The asset sync script completed successfully, recopied the five expected runtime assets, verified hashes, and built the Moonglade Web project. The actual content diff in Moonglade is limited to `moonglade-editor.js`, `moonglade-editor.js.map`, and this task record; the CSS and formatter files were recopied but matched the previous committed content.

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
| 2026-07-30 | Read Moonglade and Moonglade.Editor AGENTS guidance | Passed | Main repo requires using the project-level asset sync skill. |
| 2026-07-30 | Read `update-moonglade-editor-assets` skill and script | Passed | Script copies the ESM JS/map, CSS, formatter JS/map assets, verifies hashes, and runs Web build. |
| 2026-07-30 | `git status --short` in `Moonglade` before sync | Passed | Clean. |
| 2026-07-30 | `git status --short` in `Moonglade.Editor` before sync | Passed | Clean. |
| 2026-07-30 | `powershell -ExecutionPolicy Bypass -File .codex/skills/update-moonglade-editor-assets/scripts/update-moonglade-editor-assets.ps1` | Passed | Ran `npm test` in `Moonglade.Editor` (117 passed), `npm run build` in `Moonglade.Editor`, copied five runtime assets, verified SHA-256 hashes, and built `src/Moonglade.Web/Moonglade.Web.csproj` successfully with 0 warnings and 0 errors. |
| 2026-07-30 | Integration code review | Passed | Existing Moonglade integration still uses stable `createMoongladeEditor`, `syncToTextarea`, `getHTML`, `getValue`, and `setValue` APIs; no integration code update required. |
| 2026-07-30 | `git diff --check` | Passed | No whitespace errors; only normal CRLF warnings for copied/static files. |
| 2026-07-30 | `git diff --name-only -- src/Moonglade.Web/wwwroot/lib/moonglade-editor` | Passed | Actual content changes are `moonglade-editor.js` and `moonglade-editor.js.map`; CSS and formatter assets were recopied but content-identical. |
| 2026-07-30 | `git status --short` in `Moonglade.Editor` after sync | Passed | Clean after the package build. |

Copied asset hashes:

| File | SHA-256 |
| --- | --- |
| `moonglade-editor.js` | `633950AD5D2D0BA392E1489D84CBFF7479A561147B1B1438787F032817DF6EA3` |
| `moonglade-editor.js.map` | `1DABCECDD48FA8A6FD926F4E4FD6B64C6F6414E474D046C1749B3B5517EFEACD` |
| `moonglade-editor.css` | `92AF8949834D53682649529030BA408143DA7A3195472B046ED5FEDF4E73210B` |
| `moonglade-editor.formatter.js` | `1059CDA72F0A56088147D46C77A98F97E892A6DB1D3B930F533B8CD4406A05FD` |
| `moonglade-editor.formatter.js.map` | `3C6AB12B4C15C55DA4E1237AD1E002C318E66F867288F2877852135788C54D5A` |

## Issues and Resolutions

None.

## Follow-ups

Main app browser smoke testing was not run in this task. The asset sync script verified the editor package tests/build, static asset hashes, and Moonglade Web build.

## Notes

The related editor package task record is `E:\GitHub\ediwang\Moonglade.Editor\docs\tasks\task-html-source-code-editor.md`.
