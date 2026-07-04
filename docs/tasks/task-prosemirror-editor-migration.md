# ProseMirror Editor Migration

## Original Goal

Replace TinyMCE as the HTML post editor with a lighter custom editor built directly on ProseMirror. The user wants a careful research and migration plan first, with this task record used as the memory file for phased execution and progress tracking.

## Background

Before this migration, Moonglade supported two post content types: `html` and `markdown`; Markdown used Monaco, while HTML used TinyMCE 8.6.0 loaded dynamically on the admin post editing surface. After the migration, HTML post editing is handled by Moonglade.Editor and Markdown remains on Monaco.

Initial local context inspected before migration:

- `src/Moonglade.Web/Moonglade.Web.csproj` referenced `TinyMCE` 8.6.0 and `Moonglade.MonacoEditor`.
- `src/Moonglade.Web/wwwroot/js/app/admin.editpost.editor.mjs` loaded `/lib/tinymce/tinymce.min.js`, initialized TinyMCE for `contentType === 'html'`, and managed editor switching.
- `src/Moonglade.Web/wwwroot/js/app/admin.editor.module.mjs` contained `loadTinyMCE`, the TinyMCE toolbar/plugin list, image upload URL, code sample languages, and submit-time `triggerSave` behavior.
- `src/Moonglade.Web/Pages/Admin/EditPost.cshtml` rendered the HTML editor as `.post-content-textarea` and used Alpine to switch between HTML and Markdown editors.
- `src/Moonglade.Web/Controllers/ImageController.cs` already provides the `/image` upload endpoint with legacy-compatible `{ location, filename }` output and a 5 MB upload limit.
- `src/Moonglade.Web/Pages/_PostContentRender.cshtml` renders HTML post content using `@Html.Raw(content)`, so editor output must be constrained and predictable.
- `src/Moonglade.Web/wwwroot/css/admin.css` contained TinyMCE-specific `.tox-tinymce` layout styles.
- No repository-level `package.json`, npm lock file, Vite, Rollup, webpack, or esbuild configuration was found outside static vendor assets.

Relevant external research:

- ProseMirror is a JavaScript toolkit for building rich-text editors. Its guide describes a model/state/transaction/plugin architecture, where `prosemirror-state` owns editor state and transactions.
- ProseMirror's reference documentation includes `DOMParser` and `DOMSerializer` for converting between DOM/HTML and ProseMirror documents.
- `prosemirror-tables` provides a table schema extension, rowspan/colspan support, cell selection, commands, and a plugin that enforces table invariants.
- ProseMirror packages are MIT licensed.
- Current npm package versions checked on 2026-06-30:
  - `prosemirror-model` 1.25.9, MIT
  - `prosemirror-state` 1.4.4, MIT
  - `prosemirror-view` 1.41.9, MIT
  - `prosemirror-schema-basic` 1.2.4, MIT
  - `prosemirror-schema-list` 1.5.1, MIT
  - `prosemirror-commands` 1.7.1, MIT
  - `prosemirror-history` 1.5.0, MIT
  - `prosemirror-keymap` 1.2.3, MIT
  - `prosemirror-tables` 1.8.5, MIT
  - `prosemirror-gapcursor` 1.4.1, MIT
  - `prosemirror-inputrules` 1.5.1, MIT

Reference links:

- ProseMirror guide: https://prosemirror.net/docs/guide/
- ProseMirror reference: https://prosemirror.net/docs/ref/
- ProseMirror tables: https://github.com/ProseMirror/prosemirror-tables
- ProseMirror license: https://github.com/ProseMirror/prosemirror/blob/master/LICENSE

## Scope

This task covered:

- Add a custom Moonglade HTML editor built directly on ProseMirror for post `html` content.
- Keep the existing `contentType` values and persistence model unchanged.
- Preserve the Markdown editor behavior.
- Preserve the existing `/image` upload endpoint and storage pipeline.
- Support the user-requested feature set:
  - H1-H6 headings and paragraphs.
  - Bold, italic, underline, strikethrough.
  - Foreground color and background color.
  - Insert and edit tables.
  - Upload and insert images.
  - Insert code snippets.
  - Edit hyperlinks.
  - Blockquote.
  - Bullet and numbered lists.
  - Text alignment.
  - View and edit HTML source.
- Remove the legacy editor only after the ProseMirror editor is feature-complete and verified.
- Update relevant documentation after implementation.

## Out of Scope

The migration should not:

- Replace Monaco for Markdown or page HTML/CSS editing.
- Change the post database schema, route links, feed output, sitemap behavior, publishing workflow, Webmention, or IndexNow workflow.
- Add a new SPA framework.
- Add Word/Office-style paste cleanup, emoji insertion, special symbol insertion, line-height controls, or paragraph-spacing controls.
- Implement collaborative editing.
- Implement a media library or asset browser unless requested later.
- Solve all historical post HTML normalization in the initial editor replacement.

## Recommended Architecture

Build a small, first-party editor module around ProseMirror in a separate repository, then expose an adapter that mirrors the tiny subset of editor lifecycle operations Moonglade needs. The main Moonglade repository should consume browser-ready artifacts and should not receive a Node/frontend build pipeline.

Chosen editor repository:

```text
E:/GitHub/ediwang/Moonglade.Editor/
  AGENTS.md
  README.md
  package.json
  package-lock.json
  src/
    index.ts
    editor.ts
    schema.ts
    commands.ts
    html.ts
    styles.css
  test/
  demo/
  dist/
    moonglade-editor.js
    moonglade-editor.global.js
    moonglade-editor.css
```

The editor project uses npm and esbuild internally. Moonglade currently consumes copied static files from `Moonglade.Editor/dist/`; other long-term consumption models considered were:

- copied static files from `Moonglade.Editor/dist/`;
- an npm package artifact;
- a NuGet package that carries static web assets;
- a git submodule/subtree plus checked-in `dist/`.

Runtime integration completed:

- Replaced the legacy HTML editor loader with `ensureMoongladeHtmlEditor`.
- Replaced the legacy textarea editor initialization with `createMoongladeEditor({ textarea, uploadUrl: '/image', ... })`.
- Stored the editor instance as `window.htmlContentEditor`.
- Consumed methods:
  - `getHTML()`
  - `setHTML(html)`
  - `focus()`
  - `destroy()`
  - `syncToTextarea()`
  - `isDirty()` if needed later
- Kept submit behavior compatible with existing `syncEditorContent`.
- Preserved `formData.editorContent` as the posted HTML string.

## Schema Design

Start with a narrow schema that only allows required blog content:

Nodes:

- `doc`
- `paragraph`
- `heading` with levels 1-6
- `text`
- `blockquote`
- `bullet_list`
- `ordered_list`
- `list_item`
- `hard_break`
- `image`
- `code_block`
- `table`
- `table_row`
- `table_cell`
- `table_header`

Marks:

- `strong`
- `em`
- `underline`
- `strike`
- `link`
- `text_color`
- `background_color`

Attributes:

- Alignment should be an attribute on block nodes where needed, serialized as `style="text-align: ..."` or as a stable class. Prefer whichever better matches existing public post CSS.
- Text color/background color should be constrained to a small palette at the UI layer, but the parser should safely handle old content with existing inline colors.
- Links should allow `http`, `https`, `mailto`, root-relative paths, and fragment-only links. Reject or strip unsafe protocols.
- Images should allow `src`, `alt`, `title`, `width`, `height`, and `loading`. Set `loading="lazy"` by default.
- Code blocks should serialize language as `class="language-{name}"` on `<code>` where possible, while preserving compatibility with the existing highlight.js renderer.

## Feature Matrix

| Feature | ProseMirror module or custom work | Notes |
| --- | --- | --- |
| Paragraph and H1-H6 | `prosemirror-schema-basic` plus custom heading levels | Existing TinyMCE config only exposes H2-H4; new editor should expose H1-H6 as requested. |
| Bold and italic | Basic marks plus commands | Standard commands. |
| Underline | Custom mark | Serialize as `<u>`. |
| Strikethrough | Custom mark | Serialize as `<s>` or `<del>`; pick one and keep round-trip stable. |
| Foreground color | Custom mark | Prefer limited palette UI with safe CSS color validation. |
| Background color | Custom mark | Prefer `<span style="background-color: ...">`. |
| Tables | `prosemirror-tables` | Include row/column add/delete, header toggle, merge/split if feasible. |
| Images | Custom node and upload helper | Reuse `/image` endpoint. Support button upload, drag/drop, and paste image. |
| Code snippets | Custom or basic `code_block` node | Add language selector and output highlight.js-compatible classes. |
| Links | Custom link mark | Include edit/remove dialog, target policy decision, and URL validation. |
| Blockquote | Basic node | Standard command. |
| Bullet/numbered lists | `prosemirror-schema-list` | Include keyboard commands and toolbar state. |
| Text alignment | Custom block attributes and commands | Apply to paragraphs/headings/list items/table cells if needed. |
| HTML source | Monaco if already loaded, otherwise textarea | Round-trip via `DOMParser`/`DOMSerializer`, then sanitize. |
| Undo/redo | `prosemirror-history` | Add toolbar buttons and keyboard shortcuts. |
| Keyboard shortcuts | `prosemirror-keymap` and commands | Use common shortcuts for bold, italic, underline, undo, redo, lists. |

## Security and Content Safety

The HTML editor is an admin-only surface, but post content is rendered raw on public pages. Treat editor output as trusted author content with guardrails, not arbitrary HTML.

Required safeguards:

- Define a strict ProseMirror schema and parse pasted/source HTML through that schema.
- Strip unsupported tags and attributes during import from source mode.
- Reject unsafe link protocols.
- Remove event-handler attributes such as `onclick`.
- Avoid preserving arbitrary inline styles except the explicitly supported `color`, `background-color`, and `text-align`.
- Keep image upload validation on the server unchanged.
- Consider adding a server-side sanitizer in a later phase if the project wants defense in depth for admin-authored HTML.

## Dependency and License Plan

Introduce npm package references for ProseMirror modules rather than copying random browser builds manually.

Initial package set in `Moonglade.Editor`:

```json
{
  "dependencies": {
    "prosemirror-commands": "^1.7.1",
    "prosemirror-gapcursor": "^1.4.1",
    "prosemirror-history": "^1.5.0",
    "prosemirror-inputrules": "^1.5.1",
    "prosemirror-keymap": "^1.2.3",
    "prosemirror-model": "^1.25.9",
    "prosemirror-schema-basic": "^1.2.4",
    "prosemirror-schema-list": "^1.5.1",
    "prosemirror-state": "^1.4.4",
    "prosemirror-tables": "^1.8.5",
    "prosemirror-view": "^1.41.9"
  },
  "devDependencies": {
    "esbuild": "0.28.1",
    "typescript": "^5.8.3",
    "vitest": "^3.2.4",
    "jsdom": "^26.1.0"
  }
}
```

Before committing dependency changes:

- Confirm exact versions and licenses.
- Commit the npm lock file.
- Add third-party license notices if the project convention requires it.
- Remove legacy editor dependencies only after replacement verification passes.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Create research and migration task record | User selected ProseMirror | Task record exists under `docs/tasks/` | Complete |
| 2 | Create standalone `Moonglade.Editor` repository scaffold | Task 1 and decision to avoid frontend build in Moonglade | `npm install`, `npm test`, `npm run build` in `Moonglade.Editor` | Complete |
| 3 | Build ProseMirror schema, serializer, and sanitizer | Task 2 | Unit or browser fixture tests for HTML round-trip | Complete |
| 4 | Build basic editor view and toolbar shell | Task 3 | Manual browser check: editor loads existing HTML and syncs to textarea | Complete |
| 5 | Implement text formatting, headings, blockquote, lists, alignment, undo/redo | Task 4 | Browser checks and serialized HTML snapshots | Complete |
| 6 | Implement link dialog and safe URL handling | Task 4 | Link add/edit/remove checks, unsafe URL stripping checks | Complete |
| 7 | Implement image upload, paste, drag/drop, and image editing | Task 4 | Upload to `/image`, inserted image renders, `loading="lazy"` applied | Complete |
| 8 | Implement code snippet insertion and language selection | Task 4 | Public highlight.js still highlights saved snippets | Complete |
| 9 | Implement table insertion and editing controls | Task 4 | Add/delete row/column, header toggle, merge/split where supported | Complete |
| 10 | Implement HTML source view/edit mode | Tasks 3-9 | Source edit round-trips through schema and sanitizer | Complete |
| 11 | Choose and implement Moonglade consumption path for `Moonglade.Editor/dist` | Tasks 4-10 | Moonglade loads the built editor without npm/build tooling | Complete |
| 12 | Remove TinyMCE package/assets/config after parity | Task 11 | `dotnet build`, affected web tests, browser smoke tests | Complete |
| 13 | Update README/AGENTS/docs after implementation | Task 12 | Docs mention ProseMirror and build steps accurately | Complete |

## Execution Order

1. Keep this document updated before and after each implementation batch.
2. Develop and build the editor in `Moonglade.Editor`.
3. Keep Moonglade free of frontend build tooling; use only prebuilt editor assets during integration.
4. Integrate the editor into the existing HTML editor branch while leaving Markdown untouched.
5. Verify create/edit/save/preview/publish and editor switching.
6. Remove legacy editor dependencies only after verification passes.
7. Update long-lived docs after behavior and tooling are finalized.

## Current Progress

Research and planning are complete. Moonglade.Editor first-version implementation is complete and verified locally. The first Moonglade integration batch is complete.

Current decision:

- Use ProseMirror directly, not Tiptap.
- Keep the editor first-party and focused on Moonglade's required feature set.
- Keep frontend build tooling out of the Moonglade repository.
- Build the editor in a separate repository at `E:\GitHub\ediwang\Moonglade.Editor`, then let Moonglade consume prebuilt `dist` assets or a package artifact.
- Moonglade consumes copied built ESM release artifacts from `src/Moonglade.Web/wwwroot/lib/moonglade-editor/` and wires the post HTML editor path to `createMoongladeEditor(...)` through dynamic `import()`.
- TinyMCE has been removed after Moonglade integration smoke testing passed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-06-30 | `git status --short` before task record | Clean | No pre-existing worktree changes detected. |
| 2026-06-30 | Inspected TinyMCE and post editor integration points | Complete | See Background section. |
| 2026-06-30 | Checked for existing Node/frontend build files | None found | No `package.json`, lock file, Vite, Rollup, webpack, or esbuild config found outside vendor assets. |
| 2026-06-30 | Checked ProseMirror package versions and licenses with `npm view` | Complete | All proposed ProseMirror packages reported MIT licenses. |
| 2026-06-30 | Created standalone `E:\GitHub\ediwang\Moonglade.Editor` scaffold | Complete | The editor project contains TypeScript, ProseMirror, esbuild, Vitest, AGENTS.md, README, demo, tests, and build output. |
| 2026-06-30 | `npm test` in `Moonglade.Editor` | Passed | 3 Vitest/jsdom tests for HTML parsing, serialization, marks, and unsafe link stripping. |
| 2026-06-30 | `npm run build` in `Moonglade.Editor` | Passed | Emitted ESM, browser global, CSS, sourcemaps, and TypeScript declarations under `dist/`. |
| 2026-07-04 | Reviewed `Moonglade.Editor` AGENTS.md, README, implementation task, public API, and key source files | Complete | The editor exposes `createMoongladeEditor`, `getHTML`, `setHTML`, `syncToTextarea`, `focus`, `destroy`, `setSpellcheck`, configurable image upload, allowed image extensions, and code sample language options. |
| 2026-07-04 | `npm test` in `Moonglade.Editor` | Passed | 4 Vitest/jsdom files and 86 tests passed, covering safety, HTML, commands, and editor behavior. |
| 2026-07-04 | `npm run build` in `Moonglade.Editor` | Passed | Rebuilt ESM, browser-global bundle, CSS, maps, and declarations; size checks passed with JS and CSS under configured budgets. |
| 2026-07-04 | Inspected then-current Moonglade TinyMCE integration points | Complete | Migration was concentrated in `admin.editpost.editor.mjs`, `admin.editor.module.mjs`, `EditPost.cshtml`, `admin.css`, `Moonglade.Web.csproj`, and copied static assets. |
| 2026-07-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | Main application baseline builds successfully on .NET SDK 10.0.301 with 0 warnings and 0 errors. |
| 2026-07-04 | Copied `Moonglade.Editor/dist` ESM assets into Moonglade | Complete | Added `moonglade-editor.js`, `moonglade-editor.js.map`, and `moonglade-editor.css` under `src/Moonglade.Web/wwwroot/lib/moonglade-editor/`. |
| 2026-07-04 | Wired `EditPost` HTML mode to Moonglade.Editor | Complete | HTML mode now renders an editor host plus hidden textarea, dynamically imports the ESM editor, syncs `formData.editorContent`, and keeps Markdown editor behavior separate. |
| 2026-07-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` after integration | Passed | Main application builds successfully with 0 warnings and 0 errors. |
| 2026-07-04 | Browser static asset harness through `https://localhost:10210` | Passed | Verified ESM/CSS asset loading, editor rendering, typing, bold toolbar interaction, HTML source dialog, textarea/onChange sync, desktop screenshot, mobile toolbar wrapping, and no console warnings/errors. |
| 2026-07-04 | Browser check for `/admin/post/edit` | Blocked | Both in-app Browser and Chrome were redirected to `/auth/signin` with CAPTCHA. Full admin create/edit/save/preview/publish smoke test still requires a signed-in browser session or user-assisted CAPTCHA completion. |
| 2026-07-04 | User-confirmed Moonglade admin smoke test | Passed | User reported the smoke test passed and asked to complete cleanup. |
| 2026-07-04 | Removed TinyMCE runtime remnants | Complete | Removed the TinyMCE package reference, old editor initialization/fallback code, TinyMCE CSS files, TinyMCE-specific CSS selectors, and the untracked `wwwroot/lib/tinymce` vendor directory. |
| 2026-07-04 | Residual reference scan after cleanup | Complete | Runtime/source references are gone; remaining TinyMCE mentions are historical notes in this task record only. |
| 2026-07-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` after cleanup | Passed | Main application builds successfully with 0 warnings and 0 errors. |

## Issues and Resolutions

No open implementation issues remain from this cleanup.

Known risks to track:

- HTML source mode can bypass toolbar constraints unless source import is sanitized through the schema.
- Table editing is the highest-complexity requested feature.
- Introducing npm/esbuild adds a new developer prerequisite in `Moonglade.Editor`, but not in the main Moonglade repository.
- Public rendering currently uses raw HTML for HTML posts, so unsafe output must be prevented at editor import/export boundaries.
- TinyMCE-authored historical post HTML may contain unsupported tags or styles; Moonglade.Editor imports through its schema and sanitizer, so unsupported content may be normalized when edited.

## Follow-ups

- Decide the final consumption model from Moonglade: copy committed `dist` files, consume an npm package, consume a NuGet static asset package, or use a submodule/subtree.
- Decide whether text color/background color should use a fixed palette, custom color input, or both.
- Decide link policy for `target="_blank"` and `rel`.
- Decide how much table functionality is required for the first release: basic insert/edit versus merge/split/resizing.
- Decide whether to add server-side HTML sanitization as a separate hardening task.
- Confirm whether a future release note should mention that HTML editor output may be normalized when legacy posts are edited.

## Notes

Resume guidance for future agents:

- Read `AGENTS.md` first.
- TinyMCE has been removed after the first integration batch passed smoke testing.
- Keep changes scoped to the post HTML editor path.
- Preserve Markdown editor behavior.
- Preserve post content type values and the save payload shape.
- Use existing Bootstrap Icons and admin styling conventions for toolbar controls.
- Use browser testing for the final editor experience; text serialization tests alone are not enough.
