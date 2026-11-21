# Widget Composer B4 Workspace and Draft Editing

Status: accepted on 2026-11-21 (Asia/Taipei). B0-B4 are accepted. B5 is accepted; B6 is not authorized. Maintain this file and the [delivery tracker](widget-composer-plan.md) as current state, without discussion history.

## Scope and design reference

Implement the accepted sidebar composer inside the existing Razor/Bootstrap/Alpine admin shell: structure on the left, selected-widget editor in the middle, reserved real-preview area on the right. The approved local reference is `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/sidebar-composer.jpg`; its reproducible structure and workflow are recorded in the delivery plan. No new visual concept or dependency is required.

Use the existing blue accent, neutral gray page background, white/theme-aware panels, existing fonts and Bootstrap icons. Preserve 8 px panel corners, compact muted region labels, a blue-accent selected row, inline expandable item rows, and independent scrolling. The supported minimum is 1440 CSS pixels; verify 1440 and 1920. The reference image itself is narrower than the supported admin minimum.

Intentional checkpoint differences from the sketch: preserve the actual admin navigation; keep page Save/Discard in the existing top toolbar; reserve the real preview with an explicit label instead of drawing a substitute; expose all retained fields, validation, and draft recovery. Real preview and dragging remain B5.

## Draft and recovery contract

- Load a complete validated collection before enabling mutation. Failed, absent, malformed, partial, duplicate-ID, or unsupported content never becomes an empty baseline.
- Keep a complete in-memory baseline and draft. Stable widget/item keys survive selection, expansion and reordering. Selection and disclosure do not save or discard edits. No browser-persistent draft or automatic writes.
- Preserve original content JSON when type-specific content has not changed, including optional flags and unknown properties. Edit all supported fields of Link List, Image Link and Button Link; preserve the one-to-three button rule and 2000-character content limit.
- Create by type, move within regions, change region, enable/disable, stage deletion, undo widget deletion, and discard against the complete baseline. Built-in anchors reflect settings but remain read-only.
- Confirm type changes before clearing only draft content. Confirm discard/reload when replacing an edited draft. Protect navigation with unsaved changes.
- A save freezes mutation and submits exactly one complete PUT. HTTP 400 identifies widget/field/item errors while retaining the draft. Network/infrastructure uncertainty retains the draft and requires explicit whole-page overwrite confirmation before a retry. Discard can restore local data but keeps the uncertain status until successful reload/save. A response with `committed: true` states that saving succeeded, keeps the draft for inspection, blocks blind resubmission, and offers explicit reload.
- Remove the old list/detail/individual routes and handlers after their only page caller switches to complete collection GET/PUT. Remove obsolete offcanvas and link/button dialogs. Retain the separate old renderer/security baseline only through B5's actual-template security cutover; it is not used by the new page.

## Verification and handoff

- [x] Actual draft-module behavior tests, including complete-load guards, retained fields/keys, local operations, failed save/retry and discard: 87 Node tests passed.
- [x] Web build and affected Web tests, including the complete API/security/error contract: build passed with zero warnings/errors; 874 Web tests passed.
- [x] Actual browser workflow at 1440/1920, both provider-backed fixtures, keyboard/focus/scroll checks, failures/empty/loading states, and five resource cultures.
- [x] Reference-to-render comparison covering structure, palette, typography, selected state, disclosure rows, and intentional checkpoint deviations.
- [x] Safe evidence retained outside the repository; both hosts stopped and task database containers/volumes removed. Temporary tools/runtime/launcher disposition remains pending.
- [x] Final implementation SHA, pending user acceptance, risks, and next permitted action recorded in the delivery plan.

## Delivered files and cutover

- `Pages/Admin/Widgets.cshtml` and page-only `wwwroot/css/admin.widgets.css` provide the three-panel workspace in the existing admin shell. Shared CSS only loses obsolete gallery-preview rules.
- `admin.widgets.draft.mjs` owns the baseline/draft, serialization, stable keys, local operations and validation. `admin.widgets.api.mjs` retains structured field errors and commit metadata. `admin.widgets.mjs` binds these to Alpine, the existing shared confirmation modal, loading/save states, recovery and focus. Dynamic/user content remains text-bound; shared confirmation bodies are HTML-escaped.
- The old offcanvas, link/button editing dialogs, their two modules, individual controller routes and unused Create/Update/Delete/Get-by-ID handlers are retired. Collection validation, atomic saves, authorization/antiforgery and actual-change audit/cache behavior remain in their B3 owners. B5 retires the approximate renderer and moves security coverage to actual compiled-template HTTP tests.
- All three types retain their supported fields. Image title/alt/link/CSS, link icons/new-tab, button text/CSS/new-tab, enabled state and placement/order are editable. A type switch retains identity/common fields and clears only draft content after confirmation. Title semantics remain unchanged on the public blog.
- All 42 new keys have translations in `de-DE`, `fr-FR`, `ja-JP`, `zh-Hans` and Hong Kong `zh-Hant`; confirmation/cancel reuse existing translated keys. No dependency, schema, upgrade SQL, runtime configuration, public template or settings-ownership change.

## Automated verification

Run from the repository root:

| Command | Result |
| --- | --- |
| `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed, zero warnings/errors. |
| `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | 874 passed, zero failed/skipped. |
| `node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs` | 87 passed, zero failed/skipped. |
| Resource completeness/duplicate-key, UTF-8/CRLF/final-newline, document-link and Git whitespace checks | Passed. |

Node checks import the actual draft, transport and Alpine factory. They exercise strict complete-load guards, optional/unknown content preservation, stable item keys and ordering, all types/limits, local create/type switch/delete/undo/discard, exactly one complete application-level save, explicit empty replacement, validation mapping to original/edited item indices, pending-save locks, malformed/network responses, uncertain retry confirmation and discard/reload after a lost commit acknowledgment, committed-failure reload and leave-handler registration/behavior. External APIs/Alpine registration alone are stubbed; the draft logic is not duplicated in tests. Existing Web handler/HTTP/security/antiforgery/transaction tests remain. Individual-handler characterization is replaced by collection coverage; the Web total decreases by one. No C# layout/localized-wording assertions or migration harnesses are added.

## Actual browser verification

ChatGPT in-app browser; PostgreSQL 18.7 (`widgets_b4_pg`) at 1440x900 and 1920x1080 CSS pixels; SQL Server 16.0.4325 (`widgets_b4_sql`) at 1920x1080. Both used disposable loopback containers, generated credentials, compiled production Razor, actual provider/services/LiteBus/request pipeline, normal owner sign-in and antiforgery. Configuration sources were cleared and replaced by synthetic settings; no private override was read by the host or copied into helper outputs. Default light fixture theme, synthetic password-only owner and disabled SMTP; this does not claim TOTP, other themes, production host or B6 release acceptance. Browser version is not exposed by the documented API. Test tabs are closed and the temporary viewport override is reset.

| Scenario | Observed result |
| --- | --- |
| Social Links edit/selection/advanced/reorder/placement | Name, URL, icon and opening flag survive switching widgets and collapsing settings; item IDs remain stable during draft edits. Native database snapshots plus recorded request methods show zero writes before Save and one PUT for the first saved collection. Reload retains changes. |
| All types | Create/edit/save Image Link, Button Link and Link List on PostgreSQL; retained advanced fields and false opening flags are present in the safe saved snapshot. SQL Server edits all three types, saves once and reloads persisted values. No Button Link/Link List editor overlap. |
| Ordering and draft operations | Widget region selection and up/down movement; inline link/button movement; three-button cap; zero-button validation; staged widget delete/undo; type-change cancel/confirm; whole-draft discard cancel/confirm all pass. |
| Two tabs | Another tab deletes Friend Link and adds a widget. Saving the older complete draft later restores Friend Link and removes that addition, verified after reload. |
| Empty | Stage removal of all eight widgets, verify eight Undo actions and focus on Add widget, save the explicit empty collection, then create/save from empty. |
| Loading | Delayed load displays Loading and disables Add/Save. HTTP 503 load failure stays an error without a false empty baseline; explicit reload recovers. |
| Validation | Client errors retain the full draft, expand affected items and focus the invalid field. Synthetic server HTTP 400 maps to the stable widget title; correction and subsequent save succeed. |
| Uncertain save | Connection abort retains the draft and explains uncertainty. Canceling overwrite confirmation sends no additional request; restoring the connection and confirming the same draft saves successfully. |
| Known committed failure | Real persistence followed by an injected cache-removal exception returns `committed: true`. UI states Saved, retains the draft, blocks edits/retry and permits disclosure for inspection. Reload cancel keeps the state; confirmed reload adopts saved data. A database snapshot verifies actual commit. |
| Saving | Delayed save disables inputs/Add/Save, allows selecting a different widget, and preserves that selection on success. |
| Keyboard/focus/scroll/long values | Tab reaches enabled state from title; Space/Enter operate selection/disclosure; selection focuses title and deletion focuses the neighboring widget or Add. Dense 18-widget structure, 128-character title and long URL fit without horizontal overflow at 1440. Pane contents remain scroll-accessible while Save remains in the toolbar. |
| Built-ins | Profile, Tags, Categories and Subscription reflect hidden settings, expose no settings controls, and a disabled sidebar has an explanation. No settings writes originate from this page. |
| Localization | All five resource cultures render translated toolbar/panel/placement/type labels at 1440, including long French/German labels without horizontal overflow. |
| Console | Final PostgreSQL and SQL Server page checks have no application/Alpine errors. Injected connection/HTTP failures are deliberate test cases, not claimed as error-free requests. |

The actual beforeunload handler is covered for clean/dirty/saving/known-committed states in Node. B5 browser verification also observes and cancels the native leave warning, preserving the draft; confirmed page-level Discard then restores Saved. Page-level confirmations use the existing Bootstrap modal.

## Reference-to-render fidelity

Compared the approved `sidebar-composer.jpg` with the final 1440/1920 screenshots in one visual review:

| Reference point | Final result |
| --- | --- |
| Structure/editor/preview hierarchy | Three aligned panels; stable structure on the left and one selected editor in the center. Preview reservation is explicit at this checkpoint. |
| Palette and surfaces | Existing blue accent, gray background, white panels, subtle borders and 8 px panel corners retained. |
| Typography | Existing admin font, compact 12 px region/type labels, 14 px form text and restrained semibold panel headings. |
| Selected state | Blue edge plus light blue surface and a visible keyboard focus indicator; long titles truncate in structure with their full title available. |
| Inline rows/disclosure | One expandable row per link/button, retained field values, muted row surface, Advanced options and explicit movement/removal controls. |
| Scrolling and save | Separate structure/editor content areas; top-level Save/Discard stays visible. No large gallery-card gaps. |
| Intentional differences | Preserve the real admin navigation and top toolbar rather than the sketch's simplified shell/footer. Retain an explicit type selector and all fields/help/validation/recovery. Use the labeled preview reservation until B5. The sketch is narrower than the supported admin minimum; 1440 and 1920 are the acceptance sizes. |

## Evidence, cleanup and continuation

Local evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b4/`.

- Build/test logs: `web-build.log`, `web-tests.log`, `node-tests.log`, `host-build.log`.
- Final views: `composer-1440-final.jpg`, `composer-1920-final.jpg`, `composer-1920-sql.jpg`; `culture-{zh-Hans,zh-Hant,ja-JP,fr-FR,de-DE}.jpg` and `cultures.json`.
- Recovery/settings: `uncertain-save.jpg`, `committed-save.jpg`, `empty-saved.jpg`, `hidden-builtins.jpg`, `disabled-sidebar.jpg`.
- Canonical synthetic snapshots and safe request-method counts: `PostgreSql-{unsaved,saved,all-types,retry,committed,recreated,final}.json`, corresponding `-requests.json`, and `SqlServer-saved.json`/`SqlServer-saved-requests.json`. These contain no actual blog data or credentials. Request counters restart with each fixture host; do not treat them as a global chronological ledger.
- Both hosts are stopped. B4 containers `moonglade-widgets-b4-pg` and `moonglade-widgets-b4-sql` and their volumes are removed after label verification. Temporary tools/runtime/launcher disposition is pending; they are stopped and no private development-configuration copy exists in the B4 evidence tree. B1 retained materials and B2 private backup are untouched.

To reproduce without this local machine: read the delivery plan and B1 fixture specification, build the current Web assembly, create task-only fresh PostgreSQL/SQL Server databases with generated credentials, and use actual application services/Razor/pipeline with synthetic owner/settings and loopback ports. Reference compiled assemblies in temporary helpers so private development overrides are not automatically copied. Adapt the committed fixture JSON to explicit regions/order and exercise the browser matrix above. Never reinitialize the development database as a fixture.

Implementation SHA: `9f3a014a60040ea3cd35fada57edc8ebe83af805` on `codex/widget-composer-b4`. User acceptance: accepted on 2026-11-21 (Asia/Taipei), recorded in the delivery tracker. Development/production database changes and deployment are outside this batch. No new schema means script 12 is not rerun by B4. B4 is accepted. B5 real preview/dragging/Link List opening correction are accepted on 2026-11-21 (Asia/Taipei); B6 full release acceptance remains unauthorized and unimplemented.
