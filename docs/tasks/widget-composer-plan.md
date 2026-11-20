# Widget Composer Delivery Plan and Progress Tracker

Last updated: 2026-11-20 (Asia/Taipei).

## 1. Current checkpoint and authority

- Phase: B2 ready for user acceptance; B0 and B1 accepted on 2026-11-20 (Asia/Taipei).
- Application feature implementation: B2 region/order storage, paired upgrades, title limit, public placement, and necessary current-page bindings are implemented and verified. B3-B6 have not started.
- Plan status: B0/B1 accepted; B2 awaiting user acceptance. B3-B6 require separate execution authorization.
- Baseline branch: `master`.
- Baseline HEAD: `3169a59557b7acd519c612b9ea1bc543908c68b2` (`Show activity log details in a reading pane`).
- Planning baseline commit: `1e91dea838e593bf73e6799310d7b72c0855260e`.
- Execution authorization: B2 granted; B3-B6 pending.
- Development database: PostgreSQL script 12 applied and repeat-verified on the user-confirmed development target on 2026-11-20; full backup and successful restoration rehearsal are recorded in the B2 notes. Do not reinitialize or restore this database as a disposable fixture.
- Next action: obtain B2 user acceptance. B3 requires separate execution authorization.

This file contains the current requirements, implementation context, delivery gates, progress, and verification results. Update it in place after every batch or material change. Keep only the latest state: no questions and answers, discussion history, superseded proposals, or chronological work logs. Retain each batch's current outcome, implementation SHA, acceptance status, and unresolved limitations so work can resume without prior conversation context.

### Execution rules

- Work in batches that can each be implemented, tested, committed, and accepted.
- Current authorization covers B2 and its verification/documentation/commits. Do not implement B3-B6 features.
- Ask the user whenever an unresolved issue materially affects requirements, business behavior, architecture, ownership, or public contracts. Explain alternatives and recommend one; do not choose on the user's behalf.
- Continue independent research or documentation while awaiting an answer. Do not implement dependent work until the answer arrives.
- Implementation requires explicit authorization separate from plan review.
- Follow repository `AGENTS.md`, including English repository content, UTF-8/CRLF/final newline, existing module ownership, both database providers, and browser acceptance at 1440 CSS pixels or wider.
- Use the existing .NET and Node.js toolchain. The user's prohibition on the Java platform and its associated components remains in force.
- Production data changes, deployment, destructive operations outside disposable task fixtures, and external service changes require separate explicit authorization.

## 2. Target design and scope

The current `/admin/widgets` page displays three large preview cards per row. A large image stretches its row and leaves the neighboring cards mostly empty. Editing is hidden behind a three-dot menu. The editor is a 500-pixel offcanvas, with additional dialogs for individual links and buttons. The interim B2 editor uses a localized Placement selector and nonnegative Sort Order. The replacement composer remains scheduled for B4.

Implement a **sidebar composer**: a sidebar structure on the left, an inline content editor in the middle, and a live blog-sidebar preview on the right.

### Target screen

```text
Existing admin navigation and page toolbar
Widgets                       Add widget | Discard changes | Save changes
                         Draft / Saving / Saved / Save failed

Sidebar structure          Selected widget editor       Blog sidebar preview
-----------------------    -------------------------    --------------------
Before profile             Title                        Current theme
  About Button             Show on blog                 Draft content
Profile [built-in]          Placement                    Actual sidebar order
Before tags                Move up / Move down          Enabled widgets only
  MVP Badge                Type-specific content        Built-ins as configured
Tags [built-in]             Inline item expansion        No persistent writes
Before categories          Advanced options
Categories [built-in]
After categories
  Social Links [selected]
  Friend Link
  Online Tools
Subscription [built-in]
```

The sketch shows illustrative content and placement. Preserve the existing admin shell, navigation, toolbar conventions, and minimum supported width.

### Example complete workflow

1. Load a complete persisted widget collection as the draft baseline; select a widget without changing it.
2. Select Social Links. Expand its GitHub row in the middle pane and edit its name, URL, icon, or opening behavior inline.
3. Select `Before tags` or move the widget using its drag handle. Update the structure and preview without saving.
4. Reorder links within the widget using available movement controls; expose ordinary fields before advanced styling fields.
5. Edit another widget. Switching selection retains all draft changes.
6. Review the preview and press the page-level Save once. Validate and persist the complete collection atomically; return the canonical saved collection.
7. On success, clear the dirty state. On validation, network, or precommit database failure, retain the draft and identify the failure. Discard restores the last known saved baseline after confirmation when needed.
8. Warn before leaving with unsaved changes. Do not introduce browser-persistent drafts without a separate decision.

### Features retained, replaced, and excluded

| Treatment | Scope |
| --- | --- |
| Retain | Link List, Image Link, Button Link; create/delete; enabled state; widget placement/order; link order; button order; icons; CSS classes; image URL/title/alternative text/optional link; opening behavior; existing one-to-three button limit. |
| Replace | Three-column preview-card gallery, edit entry hidden in the action menu, numeric placement editing, widget offcanvas, and nested link/button editing dialogs. Inline expansion replaces the latter dialogs. |
| Clarify | Title is publicly rendered by Link List; image title and button labels remain their separate existing fields. Do not start showing widget titles for image/button widgets on the public blog. |
| Keep separate | Profile, Tags, Categories, subscription/feed settings, sidebar side/visibility, and theme configuration remain owned by their existing settings. Show built-ins as read-only structure references. |
| Exclude | A full page builder, arbitrary grid/column editing, freely movable built-ins, a component marketplace, new widget types, new UI framework, and automatic production release. |

## 3. Functional requirements

These requirements define the current scope. Ask before changing them or resolving a material ambiguity.

| ID | Requirement | Implementation contract |
| --- | --- | --- |
| R1 | Save the whole page as one atomic operation. | Create, edit, enable/disable, reorder, and delete are draft operations until Save. Do not simulate atomicity with multiple independently committed CRUD calls. |
| R2 | Replace numeric placement bands with explicit region plus order within that region. | Include shared/provider mapping, existing-data conversion, fresh seeding, both manual idempotent SQL upgrades, and public rendering changes. Preserve content, identifiers, UTC metadata, and existing placement. |
| R3 | Include dragging and clickable movement buttons in the first release. | Both operate on the same draft model. Provide placement selection and keyboard-operable controls so dragging is never required. |
| R4 | The later whole-page save overwrites the earlier collection. | Last successful save wins. Do not add stale-draft rejection, optimistic conflict dialogs, or automatic merging instead. |
| R5 | Preview uses real frontend component templates and the current theme. | Reuse rendering semantics, isolate the preview from admin styles/behavior, and add a protected nonpersisting draft-preview path. A simplified mock preview is insufficient. |
| R6 | Existing widgets may change type after explicit confirmation. | Confirm before discarding type-specific content in the draft. Cancel leaves the draft unchanged. Persisted content remains unchanged until Save. |
| R7 | Widgets API may change with this redesign. | Coordinate the new collection contract with all repository callers; other APIs remain unchanged. Do not maintain an unrequested permanent legacy ordering API. Document the cutover. |
| R8 | Standardize the widget title limit at 128 characters. | Align request validation, editor, entity mapping, and both database upgrades; do not truncate existing values. |
| R9 | Make public Link List rendering honor its opening-behavior switch. | Explicit true opens a new tab with appropriate rel protection; explicit false opens in the same tab. Preserve the existing new-link default and legacy missing-flag effective default. Apply the same behavior in preview. |
| R10 | Convert legacy order ties deterministically. | Sort by original DisplayOrder, CreatedTimeUtc, and ordinal lowercase canonical GUID string, then assign region-local order. Preserve unequal-order precedence. |
| R11 | Restored deleted widgets receive a new creation time. | Retain the submitted stable ID, assign current server UTC when the ID is absent, and preserve original creation times for surviving rows. |
| R12 | Retain the existing per-widget content capacity. | Keep ContentCode at 2000 characters; identify the affected widget on validation failure. Do not add widget/link count caps. |
| R13 | Coordinate collection writers through native transaction-level application locks. | PostgreSQL/SQL Server implementations stay in their provider projects. Do not add a collection lock table or rely on an in-process lock. |

### Whole-page overwrite semantics

The saved widget collection, not just modified rows, is authoritative. Capture these scenarios in tests and the acceptance walkthrough:

- Two drafts start with A and B. Another tab adds C and saves. Saving the older draft containing only A and B afterwards removes C.
- Another tab deletes B and saves. An older draft containing B, saved afterwards, restores B's submitted content.
- An explicitly empty collection can represent deleting the last widget; an absent, null, malformed, failed-to-load, or partially loaded collection must never be interpreted as an intentional empty replacement.
- Keep database replacement atomic and serialize overlapping writers appropriately on both providers. A process-local lock alone does not protect multiple application instances.
- Preserve IDs and creation timestamps for surviving rows. Reintroduced rows retain their submitted ID and receive current server UTC creation time. Never accept unchecked client-owned creation timestamps.

Whole-page overwrite, including its cross-tab effects, must be covered by acceptance. Conflict rejection and automatic merging are outside the current requirements.

### Conditions requiring clarification

- Apply R10 to legacy duplicate orders. Report any additional invalid-data or conversion ambiguity before changing its handling; do not claim an undefined legacy order can be preserved exactly.
- Out-of-band numeric positions, unsupported/malformed content, oversized payloads, or incomplete partially upgraded schemas must be reported with safe identifiers, not silently repaired, truncated, moved, or deleted.
- Do not invent maximum widget/link counts, change the three-button cap, remove a saved field, add persistence for drafts, or expand built-in settings ownership without asking.
- If a faithful isolated theme preview needs new dependencies, changes to global security policy, an admin-shell redesign, or server-side fetching of external images, stop that part and present alternatives.
- If batch size or implementation findings require splitting/reordering a batch in a way that changes its accepted behavior, update the proposal and ask before proceeding.

## 4. Current repository implementation context

This map reflects the verified B2 checkpoint. The approved future API and composer contracts are specified in the B1 notes and batch scopes; do not mistake those targets for already delivered features.

| Concern | Owning files / current behavior |
| --- | --- |
| Admin page | `src/Moonglade.Web/Pages/Admin/Widgets.cshtml`: card gallery, offcanvas, nested dialogs, translated data attributes, antiforgery token. |
| Admin state | `src/Moonglade.Web/wwwroot/js/app/admin.widgets.mjs`, `admin.widgets.links.mjs`, `admin.widgets.buttons.mjs`: independent CRUD, explicit region/order, JSON content, and type-specific draft clearing. |
| Admin preview | `src/Moonglade.Web/wwwroot/js/app/admin.widgets.render.mjs`: separate client-side renderer; it currently does not establish frontend-template/theme parity. |
| HTTP | `src/Moonglade.Web/Controllers/WidgetsController.cs`: GET list/detail, POST create, PUT update, DELETE; cache eviction and existing widget activity events follow mutations. `BlogControllerBase` supplies administrator authorization. Global MVC setup adds automatic antiforgery validation. |
| Domain | `src/Moonglade.Widgets/`: request/content validation, queries, commands, and `Types/`. Existing commands save independently. `Program.cs` already includes this assembly in handler loading. |
| Storage | `src/Moonglade.Data/Entities/WidgetEntity.cs`, `Configurations/WidgetConfiguration.cs`, `BlogDbContext.cs`: GUID ID, title, type, content type/code, WidgetRegion plus integer SortOrder, enabled, CreatedTimeUtc. JSON content maximum is 2000 characters; title validation and mapping maximum are both 128. Region is persisted as a string of at most 32 characters. |
| Fresh data | `src/Moonglade.Data/Seed.cs`: a Friend Links widget in AfterCategories at order zero. `src/Moonglade.Setup/StartUpInitializer.cs`: widgets participate in nonempty-database protection. |
| Public rendering | `src/Moonglade.Web/ViewComponents/WidgetsViewComponent.cs`, `Pages/Components/Widgets/Default.cshtml`, and `Pages/Shared/_Widget_LinkList.cshtml`, `_Widget_ImageLink.cshtml`, `_Widget_ButtonLink.cshtml`. Only enabled widgets render. ListWidgetsQuery orders by region, SortOrder, CreatedTimeUtc, and ordinal canonical GUID string; management and public rendering reuse this order. |
| Position anchors | `src/Moonglade.Web/Pages/Shared/_Aside.cshtml`: BeforeProfile, BeforeTags, BeforeCategories, AfterCategories, then subscription. Numeric bands exist only as the legacy migration source. Built-ins may be hidden by settings. |
| Opening behavior | Public Link List currently always uses a new tab despite storing an opening flag; image/button templates already use their own flag. R9 authorizes the scoped correction. |
| Shared UI | `src/Moonglade.Web/wwwroot/css/admin.css`, shared admin layout, and existing Alpine/Bootstrap modules. Toolbar remains outside `.admin-main-body`; do not regress other admin pages with broad shared-style edits. |
| Tests | Existing widget validation is in `src/Tests/Moonglade.Web.Tests/Models/WidgetRequestValidationTests.cs`. Web tests already reference the real Web application and use TestHost/SQLite. Frontend behavior tests use Node's built-in runner under `src/Tests/Moonglade.Web.Tests/JavaScript/`. No dedicated Widgets test project was found. |
| Other callers | `Hosting/DiscoveryEndpointTests.cs` and `Pages/SearchPageTests.cs` in Web tests set up `ListWidgetsQuery`. Activity catalog/HTTP tests cover widget events. Revisit these when contracts change. |
| Localization | `src/Moonglade.Web/Resources/Program.de-DE.resx`, `Program.fr-FR.resx`, `Program.ja-JP.resx`, `Program.zh-Hans.resx`, and `Program.zh-Hant.resx`. New English keys require all five translations. |
| Providers/upgrades | Shared EF mapping plus PostgreSQL snake_case and SQL Server PascalCase mappings. `database/README.md` requires manually run, numbered, idempotent scripts for both providers. B2 adds paired script 12.widget-regions.sql after 11; fresh schema creation uses the target model. Recheck numbering before any later schema work. |
| Export | The inspected application exporter supports Posts and Pages, not Widgets. Do not add a widget-export feature. Database-level backup remains necessary for upgrade acceptance. |

Read `AGENTS.md`, the relevant sections of `docs/agent-reference.md`, `README.md`, `src/Tests/README.md`, and `database/README.md` again when resuming if they changed. Treat local development overrides as private; do not copy connection strings, cookies, credentials, or payloads into this file.

## 5. Batch gates and progress

### Status definitions

Use: `Not started`, `In progress`, `Verification failed`, `Ready for user acceptance`, `Accepted`, or `Blocked on <specific decision/environment>`.

Implementation, verification, commit, and user acceptance are separate facts. A passing build, generated screenshot, commit, or assistant self-review does not constitute user acceptance. Only record `Accepted` with the user's explicit acceptance and date. Do not start the next implementation batch before that gate unless the user explicitly authorizes a different cadence.

### Standard procedure for every batch

1. Read this checkpoint and applicable source/tests; inspect `git status --short`, branch, and HEAD. Preserve unrelated user changes. Use the `codex/` branch prefix if creating an implementation branch.
2. Resolve batch-blocking questions with the user. Update the affected requirement to its latest confirmed state without recording the question, answer, or superseded alternatives. No answer is not approval.
3. Implement only the batch's scope and its necessary callers, validation, fixtures, translations, and documentation. Keep the application usable at each committed checkpoint.
4. Run affected tests, build Web for cross-module/wiring changes, and perform the batch's isolated acceptance checks. Record provider/environment, exact commands, results, and skipped checks.
5. Review security, public behavior, both providers, postcommit effects, and the focused diff. Fix relevant failures; do not weaken checks to achieve a pass.
6. Update this file with implementation/verification state and create a focused implementation commit after required checks pass. Never hide a failed or unavailable required check under a completed status.
7. Record the actual implementation SHA immediately after committing. Present a reviewable result and ask for user acceptance. Deployment is not implied.
8. On user acceptance, record the decision and acceptance date, then commit the tracking update before the next batch. A commit cannot contain its own hash: the small documentation checkpoint records the preceding implementation SHA; do not amend accepted code merely to embed its own SHA.

Local implementation commits within an explicitly authorized batch are part of this workflow. Do not push, deploy, modify an external service, or change a real production database based on this plan alone.

### Live progress table

| Batch | Scope | Implementation | Verification | Implementation SHA | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0 | Plan and requirements | Accepted | Source/path/requirements/format/diff checks passed | `c7ebf2bb16e6040ef5f1fc22af0701371266bcf7` | Accepted 2026-11-20 |
| B1 | Baseline, concrete contracts, acceptance fixtures | Accepted | Web build; 825 Web tests; 58 Node tests; both provider/browser baselines passed | `5c0dc4eb08d583872187f9058233e2a05c3d4cad` | Accepted 2026-11-20 |
| B2 | Region/order model and both database upgrades | Ready for user acceptance | Web build; 836 Web, 79 Setup, 62 Node tests; both native provider fresh/upgrade/rerun and browser checks passed | `823a46328fa80f83f211bd89cea5b26ce462b1a1` | Pending |
| B3 | Atomic whole-collection save | Not started | Not run | None | Pending |
| B4 | Composer and inline draft editing | Not started | Not run | None | Pending |
| B5 | Real preview and complete arrangement controls | Not started | Not run | None | Pending |
| B6 | Integrated provider/browser acceptance and handoff | Not started | Not run | None | Pending |

### B0 - Plan and requirements review

**Deliverable:** this English, repository-owned plan, including the current requirements, scope, verification gates, batch records, and fresh-conversation instructions.

**Check:** verify paths and source claims; validate document formatting and focused diff. No application startup, build, test run, database access, or migration execution is necessary for a document-only change.

**Acceptance:** the user explicitly accepts the requirements and batch boundaries. B0 documentation verification does not authorize B1. Record acceptance before starting a separately authorized implementation batch.

### B1 - Baseline, contracts, and fixtures

**Scope:** establish a safe acceptance environment and concrete contract notes before changing storage or behavior.

**Deliverables:** [B1 contracts and verified baseline](widget-composer-b1.md), [synthetic fixture data](widget-composer-fixtures.json), and existing-behavior C#/Node tests. The API/cutover specification is ready for B1 user review; no B2-B6 implementation is included.

- Run the existing relevant baseline checks and record independent failures with reproducible evidence.
- Use disposable PostgreSQL and SQL Server databases and an isolated local host/profile. Record safe fixture names, host port, browser, and cleanup ownership; do not repoint the user's active blog or edit private development overrides.
- Define region names, order semantics, collection request/response shape, explicit replacement/delete semantics, server-owned metadata, whole-page overwrite examples, validation/error mapping, and preview contract. Review material ambiguities with the user before B2/B3.
- Inventory all repository callers and test fixtures affected by Widgets API changes. Define when old routes/fields are retired and how intermediate batches remain usable. No permanent dual-authoritative ordering representation.
- Create synthetic fixture specifications for all three types, all four regions, disabled widgets, hidden built-ins, empty collection, and representative existing content. Specify numeric-order ties, boundaries, malformed content handling, payload limits, and the R8/R9 cases.
- Add only useful baseline characterization tests in the existing Web test project and frontend test folder; do not create a new test project or generic testing layer without a concrete need.

**Verification:** Web tests, existing Node behavior tests, Web build, and baseline browser walkthrough on the isolated host. Establish fresh and pre-upgrade fixtures for each provider. Record what is actually running, not just configuration values.

**User acceptance:** review the contract notes, exact migration preservation rules, last-save-wins examples, baseline issues, and fixtures. Agree to any newly exposed material choices before B2.

### B2 - Region/order model, preservation, and upgrades

**Deliverables:** [B2 storage and upgrade verification](widget-composer-b2.md), paired script 12, new model/callers, interim placement controls, and focused tests. Status: ready for user acceptance.

**Scope:** introduce the explicit region/order model and R8's 128-character title limit, keeping the current page and public sidebar functional until their replacement is ready.

- Update entity/shared/provider mappings, validation, owning query/command callers, fresh seed, and setup implications. Avoid changing existing content JSON or unrelated settings.
- Map legacy numeric bands to the four explicit regions. Preserve enabled state, IDs, title/content, UTC creation values, and order for unequal numeric positions. Apply only a reviewed tie policy; report invalid legacy states.
- Include paired, numbered manual idempotent SQL scripts under `database/PostgreSql/` and `database/SqlServer/`. Guard conversion so reruns preserve subsequent administrator edits. Never add an automatic migration runner.
- Explain any temporary field/route bridge required for intermediate compatibility and its scheduled removal. Keep one authoritative model and keep all intermediate writers coherent.
- Update public region selection without changing anchor positions, built-in configuration ownership, sidebar visibility, or public URLs.

**Verification:** affected Web/Setup tests and Web build; real fresh creation and representative upgrade on both providers; run each SQL script twice and prove the second run preserves results. Edit placement after the first run, rerun, and prove it is not reset. Include an eleventh widget in an upper region, order boundaries/ties, disabled rows, and title lengths 100/101/128/129. Check unrelated data remains unchanged. Remove temporary migration fixtures after capturing safe evidence; do not commit migration test harnesses.

**User acceptance:** compare before/after content and sidebar position on both providers; review upgrade/recovery instructions and the still-working current management page.

### B3 - Atomic whole-collection save

**Scope:** implement R1/R4's backend collection operation and coordinated Widgets API changes.

- Keep collection replacement rules/validation in `Moonglade.Widgets`; Web owns binding, authorization, antiforgery, response mapping, and existing audit integration.
- Validate the complete request before mutation; distinguish missing/null/incomplete input from an explicitly empty collection. Persist all create/update/delete/order/visibility changes in one transaction, with no partial success on validation or persistence failure.
- Honor whole-page last-successful-save wins, including cross-tab additions/deletions. Verify overlapping writers with real provider transactions, not only an in-memory test or in-process gate.
- Return canonical saved data and identifiers. Preserve server ownership of identity/UTC metadata. Retrying after a committed-but-lost response must not create duplicate widget rows; define a stable complete-snapshot identity contract.
- Invalidate the widget cache after successful persistence. Preserve existing widget activity event semantics and safe snapshots, including deleted names/IDs. Do not log private content payloads.
- Distinguish precommit failure from postcommit cache/audit failure. An audit failure must not make a committed business save appear rolled back; cache failures retain the repository's normal error behavior and need accurate committed-state handling.
- Keep the old management page working through a short, documented transition if it still uses old routes; finish the coordinated removal with B4 rather than leaving dead callers or unrequested legacy APIs.

**Verification:** owning handler/HTTP tests in Web tests; real transaction cases on both providers; administrator-only access, missing/invalid antiforgery rejection, mixed valid/invalid collection rejection, failed/canceled save rollback, explicit empty replacement, duplicate/invalid IDs, last-save-wins examples, overlapping writes, unchanged saved identity/UTC values, audit isolation, and cache invalidation. Exercise the real handler/HTTP composition. Web build is required.

**User acceptance:** demonstrate a mixed create/update/delete/reorder batch, a failed save with no partial persistence, a subsequent successful save, and two-tab replacement. Record the actual saved collection and resulting public sidebar using synthetic data.

### B4 - Composer and inline draft editing

**Scope:** replace the gallery/offcanvas/dialog flow with the structure/editor workspace and page-level draft actions, connected to B3.

- Keep the existing admin shell and toolbar conventions. Selection identifies a stable widget/item key and does not lose in-progress edits or focus.
- Provide create-by-type, visible enabled/disabled state, staged deletion, four-region placement, and clickable movement. Show hidden built-ins as structure references without enabling them or moving settings into this page.
- Support every existing field of the three types. Edit links/buttons inline; preserve stable keys when reordering/removing, and preserve data when collapsing advanced options.
- Implement confirmed type switching, one-to-three button validation, inline field feedback, dirty state, Save/Discard, leave protection, loading/error/empty states, and save-in-progress behavior. Disable destructive/save actions until a valid full baseline has loaded.
- Preview is integrated in B5; do not ship a simplified imitation as the approved real preview. A clearly labeled reserved preview area is acceptable only as this intermediate checkpoint.
- Update all five translations with introduced or changed keys in this batch. Remove superseded dialogs and unused modules only once callers have been migrated.
- Draft state remains in memory. Do not add automatic writes on field changes, selection, collapse, or preview. Explicit deletion remains reviewable until Save.

**Verification:** Web tests and Web build; meaningful Node tests importing the real draft module for create/edit/switch/delete/reorder/save/discard/failed-save flows. Browser acceptance at 1440 and 1920 CSS pixels; keyboard operation, scroll access, visible Save, focus after selection/deletion, empty/loading/failure states, long titles/URLs, and translated labels. Do not substitute C# CSS/text/source assertions for browser checks.

**User acceptance:** perform the end-to-end Social Links workflow, switch widgets without losing edits, create each type, cancel and confirm type changes, delete and discard, and recover from failed validation/network saves. Confirm the old nested editing flow is gone and retained fields still work.

### B5 - Real preview and arrangement completion

**Scope:** deliver R3/R5/R9, completing the selected screen.

- Add the protected, validated, nonpersisting draft-preview path and reuse actual widget/sidebar templates with the current theme/settings capture. Do not read the persisted widget cache in place of submitted draft content, mutate settings, emit widget activity entries, or persist preview data.
- Isolate public theme/CSS/content from the admin editor and prevent preview interactions from navigating or operating the admin page. Avoid server-side fetching/proxying of arbitrary media URLs. Preserve existing encoding, URL sanitization, security headers, and external-link protections.
- Show built-ins and sidebar visibility according to settings; clearly explain a disabled sidebar without enabling it. Hidden widgets remain manageable in structure and are omitted from blog output.
- Update previews on relevant draft changes; debounce requests, cancel/ignore obsolete responses, and retain a usable editor during preview failure. Do not let a slow earlier response replace the latest draft preview.
- Add widget drag handles and cross-region drops, using the same ordering operations as movement buttons/placement selection. Keep internal link/button order controllable; do not require dragging for any operation. Decide whether an installed helper is sufficient before considering a new dependency; ask before introducing a dependency that changes architecture or licensing.
- Apply R9 consistently in public and preview Link List rendering. Preserve explicit true/false, new-link defaults, and legacy missing-flag effective defaults without rewriting unrelated saved content.
- Complete translations and remove obsolete preview/transition code after callers and meaningful coverage have moved.

**Verification:** Web tests and relevant Node tests plus Web build; preview is authorized, antiforgery-protected where applicable, nonpersisting, safely encoded, and independent of the live widget cache. Test stale preview responses, invalid draft/errors, hostile URL/text inputs, invalid/missing image behavior, and type changes. In browsers at supported widths, compare preview with the actual public sidebar after saving for all types, theme appearances, hidden built-ins, and disabled/left/right sidebar settings. Verify dragging and non-drag alternatives, link opening/rel behavior, keyboard access, and no admin style leakage.

**User acceptance:** show draft/live separation, reorder across regions with both input methods, verify true/false/missing opening flags, confirm current-theme fidelity and preview failure recovery, and approve the finished composer.

### B6 - Integrated acceptance, documentation, and handoff

**Scope:** close remaining cross-batch regressions and deliver a reviewable release candidate. This batch does not authorize deployment.

- Run the final provider and browser matrix below on fresh and upgraded disposable databases. Recheck protected routes, existing public content URLs, built-in settings ownership, cache behavior, and activity history after real saves.
- Verify all supported translations and current theme appearances at 1440 and 1920 CSS pixels, including a short viewport, long content, disabled/empty/all-regions cases, and keyboard-only usage.
- Update `README.md`, `docs/agent-reference.md`, `src/Tests/README.md`, and `database/README.md` only where actual workflows, architecture/contracts, test commands, or upgrade instructions changed. Document Widgets API cutover and last-save-wins behavior. Do not rewrite unrelated maintained documentation.
- Run the full solution tests and the existing Node suite once for the integrated candidate. Broaden/repeat only when fixes or unresolved concerns justify it.
- Remove task-owned temporary hosts/databases/migration fixtures after evidence capture. Verify cleanup targets; preserve user environments, durable acceptance evidence, and the explicitly retained B1 local reproduction materials.
- Record the accepted commit range, provider/script results, browser evidence, remaining limitations, and operator upgrade/backup/recovery steps. A code revert alone is not a database rollback.

**User acceptance:** the user reviews the final checklist and remaining risks and explicitly accepts the release candidate. Deployment, production upgrade, and external changes remain separate future actions requiring explicit authorization.

## 6. Commands and acceptance matrix

Run from the repository root. These commands are planned checks, not claims that they ran in B0.

```powershell
git status --short
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
dotnet test --solution src/Moonglade.slnx
git diff --check
git status --short
```

Use the affected-project subset per batch; Setup tests are required for mapping/seeding/initialization changes. Add Configuration/Theme or other owning tests only if those modules actually change. Keep business and HTTP/security checks in the existing C# test project, frontend state/request behavior in Node tests, presentation/localized wording in browser acceptance, and migration verification in temporary disposable-database fixtures. Async xUnit tests use `TestContext.Current.CancellationToken`. Do not add interfaces or virtual methods solely for mocking.

| Acceptance area | Required evidence |
| --- | --- |
| PostgreSQL fresh | Actual schema creation/seed, all widget types/regions, atomic save, and public/preview render. |
| PostgreSQL upgrade | Representative legacy fixture, first/second script runs, data preservation, edit-between-runs preservation, title limit, and real transactional failure/overlap checks. |
| SQL Server fresh | Same functional outcomes with provider-native mapping. |
| SQL Server upgrade | Same preservation/idempotency and transaction checks with PascalCase schema. |
| Security | Actual administrator authorization/antiforgery outcomes; no mutation from preview; safe text/URL/image/class rendering; no credential/content-payload logging. |
| Business consistency | All-or-nothing save, authoritative complete collection, last-save-wins cross-tab scenarios, server-owned metadata, empty versus missing payload, failed-save draft retention, and postcommit truthfulness. |
| Public behavior | Existing URLs and settings ownership; correct region order, enabled filtering, all type-specific fields, R9 links, and cache/audit effects. |
| Browser | Named browser/version, 1440/1920 CSS pixel dimensions, themes/cultures checked, keyboard and movement controls, screenshots, preview/live parity, errors, and focus/scroll behavior. |
| Cleanup and recovery | Safe disposable target names, removed temporary fixtures/hosts, durable evidence paths, backup/restore instructions, and any unverified provider/environment. |

Unit tests, EF InMemory/SQLite tests, configuration inspection, and mockups cannot substitute for actual PostgreSQL/SQL Server upgrade acceptance or real application browser acceptance. If a required environment is unavailable, record the exact missing check and leave the relevant acceptance gate open.

## 7. Current batch verification records

Maintain one current record per batch and update it in place. Record the current verification result, implementation SHA, acceptance status/date, evidence paths, and unresolved failures or limitations. Replace obsolete results after revalidation; do not accumulate attempt histories, discussion transcripts, or superseded requirements. Large screenshots/logs may live in a durable task evidence directory. Never store secrets or real content payloads.

```text
Batch:
Status:
Execution authorization status:
Code revision / branch:
Implemented scope and affected files:
Contract/schema/configuration/localization changes:
Verification commands and exit/results:
Provider / fresh-or-upgrade / safe fixture name / script runs:
Browser / CSS viewport / culture / theme / evidence paths:
Failure-path and last-save-wins observations:
Unverified checks and precise reasons:
Temporary fixtures/host cleanup:
Implementation commit SHA:
User acceptance status / date:
Tracking checkpoint commit SHA:
Remaining risks / blockers / questions:
Next permitted action:
```

### B0 - Current verification status

- Status: accepted on 2026-11-20 (Asia/Taipei).
- Verification commit: `c7ebf2bb16e6040ef5f1fc22af0701371266bcf7`.
- Scope: this plan file only; no application, configuration, database, deployment, or external service changes.
- Planning baseline commit: `1e91dea838e593bf73e6799310d7b72c0855260e` on `master`.
- Source verification: source changes from `3169a59557b7acd519c612b9ea1bc543908c68b2` to the planning baseline are limited to this plan file. The documentation-only B0 commit preserved baseline widget behavior and application wiring.
- Requirement verification: functional requirements, B0-B6 scope, batch acceptance gates, verification matrix, current-state-only maintenance, and continuation instructions are present and consistent.
- Documentation checks: referenced repository paths and planned build/test command targets exist; UTF-8 without BOM, CRLF, final newline, balanced code fences, and Git whitespace validation passed.
- Check commands: `git diff --name-status 3169a59557b7acd519c612b9ea1bc543908c68b2..1e91dea838e593bf73e6799310d7b72c0855260e` (plan file only), inline Python document/path checks (passed), and `git diff --check` (exit 0).
- Application tests, application startup, provider verification, and browser acceptance: not run; these checks are outside the documentation-only B0 scope and remain required in the applicable later batches.
- Temporary fixtures and cleanup: none.
- User acceptance: accepted on 2026-11-20.
- Open B0 blockers: none.
- Next action: obtain B2 user acceptance; B3 execution authorization is pending.

### B1 - Current verification status

- Status: accepted on 2026-11-20; baseline, contracts, fixtures, characterization tests, and verification complete.
- Execution authorization: B2 granted; B3-B6 pending. Branch: `codex/widget-composer-b1`.
- Deliverables: `docs/tasks/widget-composer-b1.md`, `docs/tasks/widget-composer-fixtures.json`, real widget lifecycle/validation characterization, and actual renderer URL/text security tests.
- Commands/results: `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` passed with zero warnings/errors; `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` passed 825/825; `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` passed 58/58.
- Providers: PostgreSQL 18.7 and SQL Server 16.0.4325.4; task-only fresh and legacy databases on each provider. Fresh seeds: one widget and 11 configuration groups. Legacy: 18 widgets, one disabled, 11 before Profile; UTC metadata verified.
- Browser: ChatGPT in-app browser, 1440x900 and 1920x1080 CSS pixels, real compiled pages and service/pipeline composition, both providers. Edit/Cancel and New/Cancel passed; public enabled filtering and opening-flag baseline confirmed. Only the expected Development console warning; no application console errors.
- Known baseline issue: Button Link editor also shows the Link List region; assigned to B4's editor replacement. No production UI changes in B1.
- Scope exclusions: no schema/upgrade SQL, new save/lock/preview implementation, production data access, private development override changes, or deployment. TOTP, full culture/theme/settings matrix, migrations, and new-feature acceptance remain later-batch checks.
- Evidence: current results and durable local paths are listed in `docs/tasks/widget-composer-b1.md`. The committed contracts/JSON/tests support reproduction without those local files.
- Local artifacts: fixture hosts stopped; task-owned PostgreSQL/SQL Server containers and their volumes removed; ports 15432/15433/18081/18082 have no listeners. Durable logs/screenshots and stopped external `b1/tools`/`b1/runtime` directories are retained as authorized local reproduction materials. No temporary fixture host or migration harness is committed.
- Implementation commit SHA: `5c0dc4eb08d583872187f9058233e2a05c3d4cad`.
- User acceptance: accepted on 2026-11-20.
- Next action: obtain B2 user acceptance; B3 execution authorization is pending.

### B2 - Current verification status

- Status: ready for user acceptance, 2026-11-20 (Asia/Taipei). Branch: `codex/widget-composer-b2`. B3-B6 have not started and require separate execution authorization.
- Scope: explicit region/order mapping and seeding, canonical query/public ordering, paired manual script 12, title 128, interim current-editor placement fields, all five translations, focused command/model/frontend tests, and operator documentation.
- Current HTTP cutover: individual CRUD remains; create/update require `region` and `sortOrder`, and responses no longer expose `displayOrder`. Whole-collection envelope/save/locks and real preview are not implemented yet. No temporary legacy bridge exists.
- Commands/results: `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` passed with zero warnings/errors; Web tests 836/836; Setup tests 79/79; Node tests 62/62; format/resource/document checks and `git diff --check` passed. Exact commands and coverage are in `widget-composer-b2.md`.
- Native providers: PostgreSQL 18.7 and SQL Server 16.0.4325.4. Both passed 20-row synthetic upgrade preservation, canonical GUID ties, upper-region capacity, invalid-order/partial-schema rollback, repeated execution including subsequent edits, title boundaries, empty legacy conversion, new-model fresh initialization, and target-schema guards. No production database was accessed.
- Browser: ChatGPT in-app browser, 1440x900/1920x1080; both providers across upgraded/fresh schemas. Save/reload, order 1000, title 128, public anchor placement, disabled filtering, and all five localized placement selectors passed. No application console errors. Browser version was not captured.
- Evidence and limits: `docs/tasks/widget-composer-b2.md`; durable local logs/screenshots under `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b2/`. Full themes/settings, TOTP, collection concurrency, and integrated composer acceptance remain later batches. The existing Button Link editor overlap remains assigned to B4.
- Development migration: user-confirmed PostgreSQL `192.168.0.101:5432/moonglade`, completed 2026-11-20 at 19:30 (Asia/Taipei). Complete private backup restored into an isolated database; script 12 ran twice in the rehearsal and twice on development. All five widgets and all 22 unrelated public tables passed preservation checks. Target columns and local backup/manifest paths are in `widget-composer-b2.md`. No application host was started.
- Cleanup: all B2 hosts stopped; task containers and volumes removed; B2 migration tools, hosts, runtime directories, and launcher scripts removed, including development rehearsal materials. Logs/screenshots and the private development backup/manifest are retained. B1 reproduction tools remain as authorized. Generated private development-configuration copies in B1/B2 build outputs were removed; the repository source override was used only in process for the authorized development migration and was not modified or copied into artifacts.
- Implementation commit SHA: `823a46328fa80f83f211bd89cea5b26ce462b1a1`.
- User acceptance: pending.
- Remaining risks: schema 12 and matching application must be upgraded together with stopped hosts and a backup; reverting the binary alone cannot restore the old schema. Development restoration rehearsal passed; other real environments require their own backup rehearsal. Treat the retained full development backup as private.
- Next permitted action: obtain B2 user acceptance. Do not begin B3 without authorization.

## 8. Resume from a fresh conversation

1. Read this whole file, especially Sections 1, 3, 5, and the current batch verification records.
2. Read `AGENTS.md` and task-relevant engineering/test/database instructions. Inspect current branch, HEAD, `git status --short`, and the recorded implementation commits; do not overwrite unrelated changes or trust old status values.
3. Establish whether the user authorized implementation and which batches were explicitly accepted. If the latest state is still B0 planning, do not start coding from this plan alone.
4. Reverify drift-prone facts, pending environments, file paths, script numbering, and the source/tests affected by the next batch. Do not rerun accepted checks without a new reason.
5. Tell the user the current checkpoint, outstanding decisions, and the next permitted action. Ask before choosing a material unresolved alternative; continue independent work while waiting.
6. Complete the permitted batch, update its record/table/checkpoint, commit with verification evidence, and obtain explicit acceptance before advancing.
7. Follow the current functional requirements, including whole-page last-save-wins, the 128-character title limit, and Link List opening behavior. Keep this file current without adding discussion history.
