# CodeEditor Edit Page HTML/CSS Migration

## Original Goal

Replace Monaco with Moonglade.CodeEditor for the admin Edit Page Raw HTML and Page CSS editors after the Site CSS pilot was verified successfully.

## Background

The Site CSS pilot already added Moonglade.CodeEditor static assets under `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/` and proved the basic integration path. This task extends that same approach to `Pages/Admin/EditPage.cshtml` and `wwwroot/js/app/admin.editpage.mjs`, which currently load `_MonacoLoaderScript` and use Monaco helper globals for the Raw HTML and CSS page editors.

## Scope

- Load CodeEditor CSS/global JS on `Pages/Admin/EditPage.cshtml`.
- Replace Raw HTML and CSS Monaco editor initialization in `admin.editpage.mjs`.
- Preserve existing Alpine form data, preview behavior, save flow, and keyboard shortcut behavior.
- Keep Markdown post editing and the remaining Monaco package/assets untouched.

## Out of Scope

- Markdown post editor migration.
- Removing `_MonacoLoaderScript`, `Moonglade.MonacoEditor`, or Monaco static assets.
- Changing page API contracts or persistence behavior.
- Adding a frontend build pipeline to Moonglade.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect current EditPage Monaco wiring | Site CSS pilot complete | File review | Done |
| 2 | Update EditPage Razor resource wiring and editor target class | Task 1 | Web build | Done |
| 3 | Replace Monaco JS calls with CodeEditor public API | Task 2 | Web build and static reference check | Done |
| 4 | Verify and document risks | Task 3 | `dotnet build`, Web tests, optional browser check | Done |

## Execution Order

Update Razor resource wiring first so the CodeEditor global is available to the page module. Then replace JavaScript initialization and synchronization while preserving existing `formData` ownership. Finally run build/test verification and record any browser-validation limitations.

## Current Progress

Implementation is complete. `EditPage.cshtml` now loads the CodeEditor CSS/global script instead of `_MonacoLoaderScript`. The Raw HTML and CSS editor containers now use `page-code-editor`, and `admin.editpage.mjs` initializes CodeEditor instances through the public API. The HTML editor is initialized after Alpine `nextTick` so the `x-show` editor container is visible first; the CSS editor remains lazily initialized when the CSS tab is shown.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-29 | File review | Passed | EditPage currently loads `_MonacoLoaderScript`, initializes HTML immediately, lazily initializes CSS on tab show, and syncs editor values into Alpine `formData`. |
| 2026-07-29 | Static reference check | Passed | EditPage files no longer reference Monaco loader/helpers, AMD `require`, or Monaco `layout()`; only CodeEditor assets/API remain. |
| 2026-07-29 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after rerunning serially. |
| 2026-07-29 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | Web tests passed: 129 tests. |
| 2026-07-29 | `npm test` in `Moonglade.CodeEditor` | Passed | CodeEditor Vitest suite passed: 23 tests. |
| 2026-07-29 | `npm run build` in `Moonglade.CodeEditor` | Passed | CodeEditor declarations, bundles, CSS, and size budgets passed. |
| 2026-07-29 | Long-code scroll issue review | Fixed | Chrome inspection showed the real page was not giving CodeMirror a fixed-height parent because `form#page-edit-form` did not receive the Razor CSS isolation scope attribute. Changed the rule to `.page-editor-container ::deep #page-edit-form` so it compiles to a selector that matches the rendered form. A new Chrome tab verified `.cm-scroller` is 481px tall with 19636px content and internal scrolling works. |
| 2026-07-29 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Blocked by running app lock | Build reached CSS generation, but copying `Moonglade.Web.exe` / `Moonglade.Web.dll` failed because the local site was running and Visual Studio held the output. The generated scoped CSS was inspected directly and contained `.page-editor-container[b-00z0zu9pz5] #page-edit-form { min-height: 0; }`. |

## Issues and Resolutions

Running `dotnet build` and `dotnet test` against Moonglade in parallel caused CS2012 file-lock errors in shared `obj\Debug\net10.0` outputs. Rerunning the commands serially passed. Keep Moonglade build/test commands serial unless isolated output paths are configured.

After real-page testing, long Raw HTML content could not scroll inside the CodeEditor instance. Chrome inspection showed CodeMirror was not the event problem: `.cm-scroller` had `overflow-y: auto`, but its height had expanded to the full document because the flex parent chain was broken. The immediate break was `#page-edit-form`: Razor rendered the `<form>` without the page CSS isolation scope attribute, so `#page-edit-form[b-00z0zu9pz5] { min-height: 0; }` did not match and the form kept the flex-item default `min-height: auto`. The fix uses `.page-editor-container ::deep #page-edit-form`, which compiles to `.page-editor-container[b-00z0zu9pz5] #page-edit-form` and matches the rendered form. A fresh Chrome tab then showed the form constrained to the viewport and `.cm-scroller` as the only tall scroll container.

## Follow-ups

- Browser-check `/admin/page/edit` after approving local app startup against the configured database.
- Migrate Markdown post editing separately if this second stage is accepted.

## Notes

Use the existing `src/Moonglade.Web/wwwroot/lib/moonglade-code-editor/` assets added by the Site CSS pilot.
