# CodeEditor Markdown Post Editor Migration

## Original Goal

Replace Monaco with Moonglade.CodeEditor for Markdown post editing in the admin Edit Post page.

## Background

Site CSS, Raw HTML page, and Page CSS editing have already moved to Moonglade.CodeEditor. Markdown post editing is the last confirmed Moonglade code-like editing surface still using Monaco. The current Markdown post editor dynamically loads Monaco plus `inline-attachment` and binds image upload to a separate drop/paste hint area.

## Scope

- Load Moonglade.CodeEditor assets on `Pages/Admin/EditPost.cshtml`.
- Replace Markdown Monaco initialization in `admin.editpost.editor.mjs`.
- Preserve HTML post editing through `Moonglade.Editor`.
- Preserve Alpine form state, autosave, preview, publish, dirty tracking, and content-type switching.
- Preserve Markdown image paste/drop upload through the existing `/image` endpoint.
- Update post editor layout CSS for the CodeEditor surface.

## Out of Scope

- Removing Monaco package references or shared Monaco loader partials.
- Changing post API contracts, persistence, content type semantics, or image upload endpoint behavior.
- Changing the HTML rich-text editor.
- Adding frontend build tooling to Moonglade.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect current EditPost Monaco and upload wiring | None | File review | Done |
| 2 | Create migration task records | Task 1 | Markdown review | Done |
| 3 | Update Razor asset loading and Markdown editor container | Task 1 | Static reference check | Done |
| 4 | Replace Markdown editor JS with CodeEditor public API | Task 3 | Build and browser-oriented checks | Done |
| 5 | Update CSS and docs | Task 4 | Build and docs review | Done |
| 6 | Run verification | Task 5 | `dotnet build`, Web tests, optional browser check | Done |

## Execution Order

Update Razor asset loading first so the CodeEditor global is available. Then replace the Markdown branch in `admin.editpost.editor.mjs`, keeping HTML editor behavior untouched. Update CSS after the rendered surface is known, then run build/test verification and record any limitations.

## Current Progress

Implementation, command-line verification, and user browser verification are complete. `EditPost.cshtml` now loads CodeEditor CSS/global JS and renders the Markdown editor target as `post-code-editor`. `admin.editpost.editor.mjs` no longer loads Monaco, AMD `require`, `inline-attachment`, or the Monaco attachment adapter for Markdown posts. Markdown image paste/drop upload now uses CodeEditor's `markdownImageUpload.upload(file)` hook to post multipart form-data to `/image` and map `{ location, filename }` into `{ url }`. HTML post editing still uses `Moonglade.Editor`.

After browser verification, the final Monaco cleanup was handled in `docs/tasks/task-remove-monaco-editor.md`.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-29 | File review | Passed | Markdown post editor uses Monaco and `inlineAttachment.editors.monaco.attach(...)`; HTML post editor uses `Moonglade.Editor` and remains out of scope. |
| 2026-07-29 | Static reference check | Passed | EditPost Markdown migration files no longer reference Monaco `require`, `inlineAttachment`, or the old upload hint area. Global `.monaco-target` styles remain for later full Monaco cleanup. |
| 2026-07-29 | `node --check src\Moonglade.Web\wwwroot\js\app\admin.editpost.editor.mjs` | Passed | Markdown editor module parsed successfully. |
| 2026-07-29 | `node --check src\Moonglade.Web\wwwroot\js\app\admin.editor.module.mjs` | Passed | Legacy sync helper module parsed successfully after CodeEditor-compatible sync fallback. |
| 2026-07-29 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |
| 2026-07-29 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | Web tests passed: 129 tests. |
| 2026-07-29 | `Test-NetConnection localhost:10210` | Not running | Browser verification was not attempted because the local Moonglade site was not listening and starting it can trigger configured database initialization. |
| 2026-07-29 | User browser verification | Passed | User verified the migrated Markdown post editor and requested final Monaco cleanup. |

## Issues and Resolutions

The migration removed all active EditPost Markdown Monaco usage. After user browser verification, the `Moonglade.MonacoEditor` package reference, `_MonacoLoaderScript`, Monaco inline-attachment scripts, legacy sync fallback, and global `.monaco-target` styles were removed in the final cleanup task.

## Follow-ups

- Continue browser regression checks when changing CodeEditor integration behavior.

## Notes

Keep the CodeEditor integration through static assets under `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/`.
