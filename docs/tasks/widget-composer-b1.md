# Widget Composer B1 Contracts and Baseline

Status: accepted on 2026-11-20 (Asia/Taipei). Contracts are approved; implementation and verification progress is tracked in the delivery plan.

This file specifies the current implementation target and verified baseline. Maintain it in place with the [delivery plan](widget-composer-plan.md); do not append discussion transcripts or superseded proposals. [Synthetic fixtures](widget-composer-fixtures.json) contain no real blog content or credentials.

## 1. Scope and ownership

- Web remains responsible for HTTP contracts, authorization, antiforgery, view data, preview document composition, and safe activity dispatch.
- Widgets owns collection validation, replacement rules, canonical order, and transactional save orchestration.
- Data owns the region/order entity mapping and the narrow provider coordination contract. The PostgreSQL and SQL Server projects implement the real transaction-lock behavior and register it with existing storage registration. This boundary exists for the two database implementations, not for mocking.
- Existing settings continue to own built-ins, sidebar visibility/side, and theme selection. No configuration group or independent widget settings store is added.
- B1 adds documentation, synthetic fixture data, and focused characterization tests only. B2 implements storage/upgrade compatibility; B3 implements collection saving; B4 implements the composer; B5 implements real preview and dragging.

## 2. Region and order contract

| Region identifier | Meaning | Legacy DisplayOrder | Display sequence |
| --- | --- | --- | --- |
| `BeforeProfile` | Before the profile anchor | -30 through -21 | 0 |
| `BeforeTags` | After profile, before tags | -20 through -11 | 1 |
| `BeforeCategories` | After tags, before categories | -10 through -1 | 2 |
| `AfterCategories` | After categories, before subscription | 0 through 999 | 3 |

Persist an explicit region and a nonnegative region-local `SortOrder`. The former numeric bands no longer impose capacity limits. Region identifiers are stable, culture-neutral strings at the HTTP boundary; labels are localized.

During upgrade, order each region by original DisplayOrder, then CreatedTimeUtc, then the ordinal lowercase canonical `D`-format GUID string. Assign contiguous SortOrder values starting at zero. Use equivalent explicit ordering on both providers; native SQL Server uniqueidentifier ordering must not be assumed to match the canonical GUID string order. Existing widgets retain IDs, creation times, enabled state, type, content, and existing unequal-order precedence.

Repeated script execution must not remap already converted values or undo later edits. Reject/report invalid legacy bands or incomplete schema states before conversion; do not assign a guessed region. There is no content cleanup in this migration.

Built-in anchors retain their logical slots when hidden. Show their hidden state in the structure pane; omit their content from the preview/public page. A disabled sidebar gets an explanatory management state without changing settings.

## 3. Collection API contract

All routes require the existing administrator policy. Writes and preview POST use the existing antiforgery conventions. Responses use camelCase and string enums. Private local configuration and credentials are never part of these payloads.

| Method and route | Input | Success |
| --- | --- | --- |
| `GET /api/widgets` | None | 200 with the complete canonical collection envelope. No partial list or paging. |
| `PUT /api/widgets` | Complete collection envelope below | 200 with the canonical saved collection, including server-owned UTC metadata. |
| `POST /api/widgets/preview` | Same editable collection shape | 200 `text/html` sidebar-preview document, with no persistent changes. |

Editable envelope example:

```json
{
    "widgets": [
        {
            "id": "10000000-0000-0000-0000-000000000016",
            "title": "Social Links",
            "widgetType": "LinkList",
            "region": "AfterCategories",
            "sortOrder": 0,
            "isEnabled": true,
            "contentCode": "[{\"name\":\"Example\",\"url\":\"https://example.com\",\"order\":0,\"openInNewTab\":true}]"
        }
    ]
}
```

### Input and canonicalization

- `widgets` must be explicitly present as a non-null array. Every element must be complete and non-null. `{"widgets":[]}` is an intentional empty replacement; absent/null/failed or incomplete loading never becomes an empty replacement.
- Editable fields are ID, title, type, region, sortOrder, enabled, and contentCode. A stable nonempty GUID identifies each draft widget, including new widgets, and is reused on retry. Duplicate IDs are invalid. Do not generate a new row ID on each save attempt.
- The server validates IDs and owns persisted metadata. Existing IDs retain their CreatedTimeUtc. An ID absent from the current collection, including a restored deleted widget, receives the server's current UTC creation time. Submitted creation times and content-type metadata are not writable.
- Title is required, trimmed for persistence, and limited to 128 characters. Content remains the existing type-specific JSON string, limited to 2000 characters per widget; preserve all supported fields. JSON remains the only stored content type.
- Validate known type/region values and nonnegative sortOrder. Sort each submitted region by sortOrder, with submitted array position as the stable tie-breaker, and return contiguous canonical region-local values starting at zero. The client renders the returned values after saving. Do not introduce a widget/link count cap; existing host request-size limits still apply.
- Preserve the existing valid empty Link List, required link name/URL, image URL requirement, optional image link, and one-to-three button rule. Invalid fields identify the widget and field/item path in validation responses. Validate before mutating any rows.
- Public Link List uses explicit true/false opening flags. For an omitted legacy flag, retain the previous effective new-tab default. Newly created links keep the new-tab default. Images/buttons preserve their existing flags and optional-link semantics.

### Replacement and errors

- The complete submitted set is authoritative. Omitted current IDs are deleted; absent-current submitted IDs are created/restored; matching IDs are updated. No stale-draft rejection or automatic merging.
- A successful later transaction replaces the earlier collection, including additions/deletions. Reads during save must not expose a partly replaced collection. Preview does not acquire a write lock or save a draft.
- Validation/binding errors use HTTP 400 and ProblemDetails-compatible field details. Unauthorized/forbidden and antiforgery failures retain existing conventions. Infrastructure errors retain an accurate 5xx outcome; never report success for precommit failure.
- Once database commit has succeeded, cache/audit failures cannot be described as rollback. Cache-error responses must distinguish committed state so the UI can retain the draft for comparison and offer reload without blindly resubmitting. Isolated activity-dispatch failure preserves the business save result under the existing dispatcher contract.
- Return canonical data from the completed save, not a new uncoordinated read that might return another writer's replacement. Do not log contentCode, passwords, or full request bodies.

### Cross-instance write coordination

Use a fixed database-local collection lock inside the transaction, acquired before reading current widgets. Hold it until commit/rollback. All current widget writers must coordinate during the transition. A process-local semaphore or a table-row lock on a possibly empty widget table is insufficient.

- PostgreSQL: transaction-level advisory lock using `pg_advisory_xact_lock`.
- SQL Server: `sys.sp_getapplock`, exclusive mode, transaction owner; inspect failure/timeout/cancellation return values instead of proceeding without the lock.
- Run the complete lock/read/write transaction within the provider's existing execution-strategy contract. Respect request cancellation before commit and preserve actual commit outcome afterwards. In B3, test separate connections, an empty collection, rollback, timeout/cancellation, and overlapping writers on both providers.

The native lock contracts are documented by [PostgreSQL](https://www.postgresql.org/docs/18/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS) and [Microsoft](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql). B1 verifies environment availability; it does not claim the future collection writer or locking implementation has been tested.

## 4. Existing caller and cutover inventory

| Caller | Required transition |
| --- | --- |
| `admin.widgets.mjs` | Replace list/detail and individual writes with complete collection load/save when B4 lands; maintain one draft across selections. |
| `admin.widgets.links.mjs`, `admin.widgets.buttons.mjs` | Preserve all content fields while moving dialog-specific editing into the draft editor; remove modules only when no callers remain. |
| `admin.widgets.render.mjs` | Retain security coverage while replacing approximate previews with B5's actual template rendering. |
| `WidgetsController` / Widgets queries and commands | Add the collection contract in B3; preserve the temporary existing-page routes only until B4 switches the page, then retire unused individual routes/handlers. |
| `WidgetsViewComponent` / `_Aside.cshtml` | Select explicit regions and order in B2, preserving built-in ownership and enabled filtering. |
| Current Widgets offcanvas | In B2, update only the necessary placement bindings/labels/request fields to explicit region/order so the old page remains functional. B4 replaces the layout and nested editing flow. |
| Seed / Setup / mappings | B2 updates fresh data and paired provider upgrades, including title length. Preserve nonempty-install safeguards and UTC handling. |
| `DiscoveryEndpointTests` / `SearchPageTests` | Keep real public composition and query setup coherent if query types or models change. |
| Widget validation/lifecycle/security tests | Move assertions to the replacement contract in the batch that changes it; preserve behavioral coverage without asserting removed source is absent. |
| Activity catalog/HTTP tests and widget audit events | Preserve supported event keys and safe target details. Full-collection replacement emits only actual create/update/delete outcomes after business persistence. |

The old numeric HTTP model is replaced in B2 together with the minimum current-page bindings; a permanent numeric-to-region compatibility layer is unnecessary. The old individual-operation routes may remain briefly for the current page, using only the new authoritative region/order model. GET-list/detail, POST-create, PUT-by-ID, and DELETE-by-ID retire with the B4 caller cutover. Other public APIs, blog URLs, and protocols remain unchanged.

## 5. Preview and draft behavior

- Render real sidebar/widget templates and current theme/settings into an isolated iframe document. Reuse the application settings capture for one render; never replace draft content with the persisted widget cache.
- Use iframe sandbox isolation without script, form, popup, or top-navigation privileges. Allow same-origin asset access only as needed for existing styles/fonts; do not enable scripts. Render preview content inert so links and subscription controls cannot operate the admin page or navigate away.
- The response is not stored or shared through application/proxy caches. It does not emit widget audit events, change settings, write database rows, or fetch arbitrary external URLs on the server. Images load through the browser under the existing URL and security rules.
- Keep all component fields safely encoded, preserve URL sanitization/rel protections, and isolate theme styles from admin styles. Do not weaken global CSP/security settings to make preview work; revisit a concrete conflict with the user.
- Debounce preview requests and ignore older responses. Invalid/incomplete edits and preview failures retain the editor and last successful preview with an explicit stale/unavailable state. They do not save or reset the draft.
- A valid complete baseline is required before destructive/save operations. A failed GET must remain a load failure, not become an empty collection.
- Switching widgets/collapsing advanced sections preserves unsaved data. Type switching requires confirmation before resetting only the draft's type-specific content. Discard restores the baseline; leaving a dirty page requires a warning.

## 6. Reproducible fixture specification

`widget-composer-fixtures.json` is the source of synthetic legacy rows and expected converted region/order. Content uses reserved example destinations and a local existing image. CreatedTimeUtc values are explicit UTC; GUIDs are deterministic.

| Fixture | Expected baseline / future check |
| --- | --- |
| Fresh initialization | One default Friend Links widget and 11 configuration groups on each provider. |
| Legacy collection | 18 widgets; 11 before Profile, 2 before Tags, 2 before Categories, 3 after Categories. One is disabled. |
| All types | Button Link, Image Link, and Link List retain their type-specific fields. |
| Legacy ties | Two rows share DisplayOrder -21 and creation time, requiring canonical GUID tie-breaking. Two rows share order zero. |
| Capacity/boundaries | Eleven widgets in the upper region and all eight legacy band endpoints. After B2 they have distinct contiguous region-local order. |
| Opening behavior | Three links with true, false, and omitted flags; baseline public rendering opens all in new tabs. B5 applies R9. |
| Empty/invalid | Explicit empty collection; missing/null/partial collections, duplicate/empty IDs, unsupported enums, malformed content, title 129, contentCode 2001, and zero/four buttons. Invalid cases are specified separately and not inserted into the valid legacy fixture. |
| Settings variants | Left/right/disabled sidebar, hidden Profile/Tags/Categories, and subscription disabled. Their integrated preview acceptance is required in B5/B6, not claimed by this baseline. |

To recreate: provision task-owned disposable databases, initialize each through the real StartUpInitializer, verify fresh counts, then replace only their seeded widgets with the JSON rows for legacy scenarios. Initialize a synthetic local owner through the real OwnerAccountStore for browser checks. Never use the developer/production database or copy its credentials. Keep temporary fixture hosts/scripts outside the repository. The current stopped local tools/runtime directories are retained as authorized reproduction materials; live disposable databases and their volumes are removed after verification. Reuse the committed data/specification for B2's temporary migration checks; no migration harness is committed.

## 7. Verified B1 results

- Source base: `c7ebf2bb16e6040ef5f1fc22af0701371266bcf7`; branch `codex/widget-composer-b1`.
- Toolchain: .NET SDK 11.0.100, application target net10.0 and runtime 10.0.15; Node v26.13.1. No dependency or framework change.
- Web build: passed, zero warnings/errors.
- Web tests: 825 passed, zero failed/skipped. Added existing-behavior coverage for legacy range boundaries, serialized-content length/required fields, real SQLite create/update/query/delete with metadata retention, and missing-row outcomes.
- Node behavior tests: 58 passed, zero failed/skipped. Added actual renderer checks for text encoding and executable link/image/button URL rejection.
- PostgreSQL: 18.7, task-owned image `postgres:18-alpine`, database names `widgets_b1_fresh` and `widgets_b1_legacy`, host binding 127.0.0.1:15432. Fresh and legacy invariants passed.
- SQL Server: 16.0.4325.4, task-owned image `mcr.microsoft.com/mssql/server:2022-latest`, same task database names, host binding 127.0.0.1:15433. Fresh and legacy invariants passed.
- Each legacy database retained 18 rows, one disabled row, and 11 rows before Profile after the browser cancellation walkthrough. Materialized creation timestamps were verified UTC.
- Browser: ChatGPT in-app browser at 1440x900 and 1920x1080 CSS pixels; browser version is not exposed by the read-only page API. Real compiled pages, production service registration, request pipeline, authentication/authorization, and real provider databases were used through a temporary fixture host.
- The fixture host clears default configuration sources, uses generated ephemeral credentials, task-only image/original/icon paths, and loopback-only ports 18081/18082. SMTP is disabled and the synthetic account uses password-only authentication; this is not TOTP or production-host acceptance.
- Provider fixture key rings are independent while cookies are scoped to hostname rather than port. Moving between these loopback hosts requires fresh sign-in/antiforgery renewal; this baseline does not claim production authentication compatibility.
- Browser checks passed: page identity, 18-item nonblank collection, no error overlay, actual Edit/Cancel and New/Cancel interactions, public enabled filtering, and existing opening-flag discrepancy. No application console errors; the public page's Development-environment warning is expected.
- Known baseline UI issue: Button Link editing also displays an empty Link List section. Evidence is the actual compiled-page screenshot. B4 replaces this flow; B1 does not change production markup or styles.
- New schemas, migration SQL, complete-collection replacement, native lock coordination, new editor, and real draft preview are not implemented or accepted in B1. No production data or private development override was used.

### Evidence

Durable local evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b1/`.

- `web-build-baseline.log`: actual Web build.
- `web-tests-final.log`: current 825-test Web result.
- `node-tests-final.log`: current 58-test Node result.
- `PostgreSql-fresh.log`, `SqlServer-fresh.log`: real fresh initialization checks.
- `PostgreSql-legacy.log`, `SqlServer-legacy.log`: real seeded legacy fixtures and temporary host logs with credential redaction.
- `browser-baseline.json`: page identity, viewport, count, and console result.
- `postgresql-editor-1440.jpg`: current editor and baseline visibility issue.
- `postgresql-widgets-1920.jpg`, `sqlserver-widgets-1920.jpg`, `sqlserver-widgets-1440.jpg`: provider/viewport baselines.

These local evidence paths are supplementary. The committed contract, synthetic JSON, and characterization tests are sufficient to recreate the scenarios on another machine. The fixture hosts are stopped and the database containers/volumes removed. The stopped local tools/runtime directories are retained as authorized reproduction materials. Cleanup status and the B1 implementation SHA are recorded in the delivery plan. B1 is accepted; B2 implementation is authorized and tracked in the delivery plan.
