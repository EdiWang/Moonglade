# Widget Composer B5 Real Preview and Arrangement

Status: accepted on 2026-11-21 (Asia/Taipei). B0-B5 are accepted. B6 is not authorized. Maintain current state only with the [delivery tracker](widget-composer-plan.md).

## Scope and implementation contract

- Protected, antiforgery-validated `POST /api/widgets/preview` accepts the same complete editable collection as Save and returns a noncached HTML document. A presentation-only `dark` query value follows the current browser appearance; it changes no settings.
- Widgets owns shared validation/canonicalization/trim through `WidgetCollectionRequest.Materialize`; Save preserves server-owned IDs/creation metadata while preview projects untracked draft entities only. Preview does not acquire a widget write lock, access the persisted widget cache, save data or emit widget audit events.
- Web composes the document using the actual `_Aside`, widget partials, built-ins, captured current settings, theme stylesheet and freshly captured optional custom CSS, embedded with raw-text boundary escaping. The public default path continues using its normal cache. The iframe uses the desktop viewport width so public media queries remain intact. Its 278px sidebar is displayed in a scrolling slice; parent-side ResizeObserver measurement follows content/image height without enabling iframe scripts.
- Preview uses a sandboxed iframe without script, form, popup or top-navigation privileges. Rendered content is inert, field values remain encoded, existing link/image sanitization is preserved and a preview-only CSP adds restrictions. No global security policy relaxation or server-side media proxy/fetch.
- Debounce draft changes, abort/ignore obsolete requests and preserve the last successful preview with an explicit stale/error/invalid state. Selection/disclosure do not generate preview writes or replace draft state. Theme appearance changes refresh preview.
- Native drag handles move widgets within/between the four regions through the same draft ordering operation as movement buttons and placement selection. Read-only built-ins stay anchored; no drag dependency or new mandatory gesture. Inline link/button movement remains available.
- Link List true/false opening behavior is shared by public and preview templates. Missing legacy flags default to new-tab, matching the former effective public behavior; no content rewrite or migration.
- Replace the labeled preview reservation and retire the old approximate renderer only after its text/URL security coverage moves to actual compiled-template tests. Update all five translations for new keys.

## Progress and verification

- [x] Record B4 acceptance and B5 authority; branch `codex/widget-composer-b5`.
- [x] Implement the shared draft projection and protected real-template preview route.
- [x] Implement and browser-verify debounced preview lifecycle and draft drag operations.
- [x] Ten new keys translated in all five resources; obsolete reservation keys/renderer removed. Actual compiled-template security tests cover script/attribute encoding, executable URLs, new/same/legacy link flags, authorization/antiforgery, invalid/empty drafts, no persistence/audit/settings writes and unchanged widget cache.
- [x] Web build: zero warnings/errors. Full Web tests: 879 passed, zero failed/skipped. Node tests: 91 passed, zero failed/skipped; includes preview timing/cancellation/stale/error/height lifecycle and drag/model/transport behavior.
- [x] Isolated PostgreSQL/SQL Server browser verification: actual public/preview parity, light/dark/current themes, left/right/disabled sidebar, hidden built-ins, all types/flags, invalid/missing images, stale/error recovery, drag and keyboard/button alternatives, 1440/1920 layouts and all resource cultures.
- [x] Final reference-to-render comparison and evidence/fixture disposition.
- [x] Implementation commit and tracking SHA: `31d4401361754d9df1967ede0516218b666056e4`.

## Environment and continuation

Use task-owned loopback-only fixture databases and synthetic owner/settings. Never access or reinitialize development/production databases for these checks. Keep private development overrides out of temporary helper outputs. The accepted local design reference remains `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/sidebar-composer.jpg`.

Safe evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b5/`. Browser plugin is not available in this session; browser QA uses the installed Playwright CLI fallback, without adding a project dependency. B4 temporary tools/runtime/launcher disposition remains pending and its files are untouched.

Native browser verification is complete for this batch. Browser: isolated Microsoft Edge 158.0.4370.33 via Playwright CLI 0.1.25. No schema, deployment or external service change; existing public configuration/cache contracts are preserved. Await separate B6 execution authority.

Custom CSS contract: preview immediately uses the currently saved captured CSS, independently of the existing one-hour public browser asset cache. Public cache behavior remains unchanged; use a fresh public asset cache for parity acceptance. Old public tabs may temporarily differ.
## Verification results

Commands (repository root):

```powershell
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
```

Full Web tests: 879 passed, zero failed/skipped. Node: 91 passed, zero failed/skipped. Web build: zero warnings/errors. Actual compiled-template HTTP checks cover authorization, antiforgery, complete invalid/empty drafts, text/attribute encoding, executable URLs, opening flags, no writes/audit/cache replacement, and custom-CSS raw-text boundary protection including mixed-case/composed terminators. Node covers shared movement, native drag orchestration, transport, debounce, abort/obsolete responses even when abort is ignored, invalid/error recovery and observer cleanup/height changes.

The fixture hosts use actual production Razor/services/pipeline/authentication/antiforgery and independent task-owned fresh databases: PostgreSQL 18.7 `widgets_b5_pg` at loopback port 15532; SQL Server 16.00.4325 `widgets_b5_sql` at loopback port 15533. Host ports were 18581/18582. Synthetic owner data only; no private development override was loaded/copied. No schema changes or migration scripts are needed in B5.

| Browser acceptance | Observed result |
| --- | --- |
| Both providers | All three types edited, one full PUT, reload retained, actual public update and exact sidebar HTML parity after saving (excluding the inactive subscription modal). Preview POSTs did not persist. SQL at 1920x1080; PostgreSQL at 1440x900 and 1920x1080. No horizontal page overflow. |
| Real template geometry | Public and preview sidebar width 278px and profile height 320px match. Iframe viewport follows outer desktop width so public media queries remain intact; the parent displays a scrolling slice. |
| Drag and alternatives | Native same/cross-region drag, selected widget/content retention, placement selection and Move up/down passed. Actual Enter selection focuses title; Space activates movement controls; keyboard PageDown scrolls preview while the toolbar remains visible. No automatic PUT. |
| Opening and interaction | Explicit true supplies new-tab/rel protection; false omits target/rel; missing legacy flag defaults true. Preview links, including same-tab links, remain inert with no parent navigation or new tab. |
| Failure and changing content | A preview 503 retains last document/editor and Refresh recovers. Superseded delayed request is aborted; latest content wins. Confirmed type switch produces an invalid draft without losing last preview; repaired image input recovers. Missing image uses actual browser fallback; measured height shrinks and long content remains scrollable. |
| Theme and CSS | Light/dark and built-in green/current theme match live sidebar styles. Saved custom CSS appears immediately in preview, remains isolated from admin, and matches public after clearing only the isolated browser asset cache. Existing public cached radius 18px remained while preview immediately used 24px; fresh public cache used 24px. |
| Sidebar settings | Right and Left match actual public output/location; all hidden built-ins retain four structure anchors and editable widgets; Disabled shows an explanation and no public/preview aside. Original normal right/all-built-ins fixture settings restored. |
| Cultures | zh-Hans, zh-Hant, ja-JP, fr-FR and de-DE at 1440: translated toolbar/panels and actual preview built-ins, matching iframe language, no overflow. |
| Leave guard and console | Native beforeunload observed/canceled retains page and draft; confirmed local Discard restores Saved. Final PostgreSQL/SQL Server pages have zero application/Alpine console errors. Injected 503, intentionally missing image and canceled request are classified test faults. |

## Visual fidelity review

The approved `sidebar-composer.jpg` and final 1440/1920 rendered screenshots were viewed together after implementation.

| Reference point | Final implementation |
| --- | --- |
| Three-panel hierarchy | Left structure, flexible inline editor, actual preview on the right, aligned panel headings. |
| Structure and anchors | Four explicit region groups, muted read-only built-ins, selected accent edge and retained selection; compact drag handles added. |
| Editing flow | One selected widget, ordinary fields first, inline expandable links/buttons, advanced options collapsed and existing movement controls. |
| Palette and typography | Existing admin accent/theme, white bordered panels and readable labels retained; green custom theme affects its intended public preview only. |
| Toolbar and scrolling | Existing admin shell/top Save/Discard retained; structure/editor/preview scroll independently, with no page overflow at 1440/1920. |
| Real preview fidelity | Actual public markup, theme, 278px content and desktop profile geometry replace the reservation. Large synthetic image/profile and content differ from illustrative sketch because the real templates/data are reused. |
| Intentional differences | Existing admin navigation/top toolbar, type selector, validation and recovery remain. The sketch's footer Save and simplified shell are not substituted for repository conventions. |

## Evidence, cleanup and continuation

Local evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b5/`.

- Build/test logs: `web-build.log`, `web-tests.log`, `node-tests.log`, `preview-http-tests.log`, `host-build.log`.
- Final screenshots: `final-postgresql-1440.jpg`, `final-sqlserver-1920.jpg`; supporting `dark-composer-1920.jpg`, `green-custom-theme-1920.jpg`, `current-custom-css-1920.jpg`, five `culture-*.jpg`, `sidebar-left.jpg`, `sidebar-hidden.jpg`, `sidebar-disabled.jpg`, `preview-failure.jpg`, `missing-image.jpg`.
- Safe observations: `final-browser.log`, `sql-browser.log`, `save-browser.log`, `failure-browser.log`, `stale-browser.log`, `image-browser.log`, `dark-browser.log`, `theme-browser.log`, `css-freshness-browser.log`, `cultures-browser.log`, `left-browser.log`, `hidden-browser.log`, `disabled-browser.log`, `keyboard-browser.log`, `leave-browser.log`.
- Synthetic snapshots: `PostgreSql-saved.json`, `SqlServer-saved.json` and corresponding `-requests.json`; each observed saved workflow contains one whole-collection PUT. Counters restart per host, not a global chronology.
- Task browser session closed, both hosts stopped; ownership-checked B5 containers/volumes removed and ports 15532/15533/18581/18582 have no listeners. Temporary `tools`, `runtime`, `run-fixture.ps1` and `browser.ps1` are retained as user-authorized local reproduction materials. No private development-configuration copies were generated. B1 retained materials, B4 pending files and B2 private backup are untouched.
- To reproduce independently, read the delivery plan/B1 synthetic fixture specification, build current Web, create disposable provider databases with generated temporary credentials, reference compiled assemblies in an external helper and clear default configuration sources. Run actual service/pipeline/Razor/auth and compare the browser matrix above. Never use the development database as a fixture. Browser plugin was unavailable; install it for future sessions if preferred, or retain the regular Playwright CLI fallback without adding a project dependency.

Implementation commit SHA: `31d4401361754d9df1967ede0516218b666056e4` on `codex/widget-composer-b5`. User acceptance: accepted on 2026-11-21 (Asia/Taipei). Next permitted action: await B6 execution authorization; B6 remains unauthorized. Full solution/Setup/migration and TOTP release acceptance were not repeated: no schema/seeding/startup/account changes; integrated release gates remain B6. Existing per-instance public cache invalidation is unchanged, and old public tabs may temporarily retain cached custom CSS.
