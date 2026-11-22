# Widget Composer B3 Collection Save and Verification

Status: accepted on 2026-11-21 (Asia/Taipei). B0-B3 are accepted; B4 is accepted on 2026-11-21 (Asia/Taipei); B5 is accepted and B6 is ready for user acceptance; deployment is not authorized. The [delivery plan](widget-composer-plan.md) records the implementation SHA and acceptance gate. Maintain current state only.

## Delivered scope and ownership

- Widgets owns complete-draft validation, authoritative replacement, canonical ordering, stable identities, server-owned creation times, transaction orchestration, detached saved snapshots, and actual change snapshots.
- Data owns `IWidgetCollectionLock`. PostgreSQL and SQL Server implement/register their native transaction-owned locks. `WidgetCollectionGate` is scoped and is registered by existing Web composition; LiteBus discovers the new command in the existing Widgets assembly.
- Web exposes complete collection GET/PUT, retains administrator authorization and antiforgery conventions, removes the handling instance's widget cache after persistence, and dispatches existing widget activity keys from safe change snapshots.
- The composer uses complete collection GET/PUT plus B5 nonpersisting POST preview. Old list/detail/individual routes and their unused handlers are retired. No new UI framework, dependency, configuration group, table, migration, or automatic upgrade runner is added. Schema 12 remains the prerequisite for existing databases.

## API and transition

| Route | Current contract |
| --- | --- |
| `GET /api/widgets` | Administrator-only complete `{ "widgets": [...] }` response; `Cache-Control: no-store`; includes disabled widgets and canonical ordering. |
| `PUT /api/widgets` | Administrator-only, antiforgery-protected replacement; returns the canonical saved collection in the same envelope. |
| `GET /api/widgets/list`, `GET /api/widgets/{id}`, `POST /api/widgets`, `PUT /api/widgets/{id}`, `DELETE /api/widgets/{id}` | Retired by the B4 caller cutover; there is no compatibility bridge. |
| `POST /api/widgets/preview` | Implemented in B5: same complete editable envelope/validation, administrator and antiforgery protection, uncached HTML, no widget/settings/audit writes. Optional dark/culture queries affect presentation only. |

The complete editable shape is specified in the [B1 contract](widget-composer-b1.md#3-collection-api-contract). The envelope must explicitly contain a non-null `widgets` array. Every element must explicitly include `id`, `title`, `widgetType`, `region`, `sortOrder`, `isEnabled`, and `contentCode`. IDs must be unique nonempty GUIDs. Null or incomplete elements, undefined enums, negative positions, oversized title/content, malformed type-specific JSON, and invalid link/image/button content are rejected before mutation. Error fields identify `Widgets[index].Field`, including content item paths when applicable. No new widget/link count cap is introduced.

An explicit `{"widgets":[]}` intentionally deletes the collection. Omitted persisted IDs are deleted, submitted absent IDs are created/restored, and matching IDs are updated. Existing rows retain creation UTC; restored/new rows retain the submitted ID and receive current server UTC. Input creation-time/content-type properties cannot replace server metadata. Sort by region, submitted order, and submitted array position for ties; persist and return contiguous region-local positions. Titles/content use existing trim behavior and preserve supported content fields.

Response widgets include the seven editable fields plus `contentType` and `createdTimeUtc`. The save snapshot is materialized from actual stored values while the transaction lock is held, preserving provider timestamp precision. It is returned after commit without an uncoordinated second read. Retrying the same stable-ID snapshot does not duplicate rows or unchanged activity events.

Binding/domain validation returns HTTP 400 with field errors. Authentication/authorization and antiforgery retain their existing status conventions. Persistence failures retain the normal infrastructure-error response; canceled or failed precommit writes roll back. Once commit returns successfully, postcommit activity dispatch does not use the aborted request token. Activity failures are isolated by the existing dispatcher. Actual created/updated/deleted outcomes use the existing event keys and detached IDs, titles, and supported type metadata; no content payload is logged.

A cache-removal failure after successful persistence returns HTTP 500 Problem Details with `committed: true` and `savedCollection` containing the saved envelope. Activity dispatch is still attempted. The B4 composer preserves the draft, blocks editing/resubmission, and offers confirmed reload. Generic infrastructure/transport errors without this marker do not establish rollback: the B4 client retains the draft and requires explicit whole-page overwrite confirmation before retry. There is no durable audit outbox or exactly-once request ledger.

## Transaction and read consistency

The gate rejects enclosing transactions and pending tracked changes, preserving the caller's unrelated work. Each provider execution-strategy attempt starts a transaction, acquires the database-local lock before reading widgets, performs its work, and commits. Failed attempts detach only widget entries and dispose the transaction so rollback releases the lock. A retry after a lost commit acknowledgment compares the attempted saved snapshot under the lock, preserving confirmed saved UTC and original change snapshots when already committed.

- PostgreSQL uses advisory key `5570754044645364807`, with `pg_advisory_xact_lock` for writers and `pg_advisory_xact_lock_shared` for list readers. These locks follow transaction lifetime. [PostgreSQL documentation](https://www.postgresql.org/docs/18/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
- SQL Server uses `sys.sp_getapplock`, resource `Moonglade.Widgets`, transaction owner, Shared/Exclusive modes, and a 30000 ms lock timeout. Every negative return is rejected with error 51001 rather than continuing unlocked; command timeout/cancellation also aborts the operation. [Microsoft documentation](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql)
- The complete unit runs within the provider's existing retry strategy. Complete-draft IDs originate with the client and remain stable across retries. [EF Core execution-strategy guidance](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)

List readers share the same lock resource so a read cannot assemble old/new rows across a replacement on SQL Server's normal locking isolation. Readers can run together; writers serialize even when the table is empty. No database isolation option or process-local semaphore is substituted for native coordination.

## Verification

| Check | Result |
| --- | --- |
| `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed; zero warnings/errors. |
| `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | 875 passed; zero failed/skipped. |
| `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` | 79 passed; zero failed/skipped. |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | 62 passed; zero failed/skipped. |
| Git whitespace, UTF-8/CRLF/final newline, and document-link checks | Passed. |

Permanent tests exercise actual handlers and SQLite transactions, complete/null/invalid input, field/item paths, canonical ties, metadata, duplicate/invalid IDs, explicit empty replacement, all-or-nothing mixed writes, reused context after rollback, cancellation before/after commit, simulated lost acknowledgment, activity isolation, postcommit cache failure, and the real controller/LiteBus/antiforgery boundary. The HTTP fixture uses real production controllers and the existing exception handler for failed-save recovery. No C# layout or translated-wording assertions were added.

Native checks used task-owned PostgreSQL 18.7 (`widgets_b3_pg`) and SQL Server 16.0.4325.4 (`widgets_b3_sql`), loopback-bound containers, generated ephemeral credentials, actual provider registration, and independent service roots/connections. Both passed:

- Mixed create/update/delete/order changes; validation rejection without mutation; stable retry; deletion/restoration with the same ID; surviving timestamps; removal of another draft's additions.
- Failure or cancellation after SQL writes but before commit; complete rollback; actual committed-acknowledgment fault/retry; cancellation after commit preserving success.
- Locking an empty collection, canceling a waiting writer, command timeout, rollback release/reacquisition; SQL Server also rejected the native negative lock return with error 51001.
- A paused uncommitted replacement held both a second writer and a list reader. After release, reads matched a complete committed collection, and the later writer's complete set won.
- Actual transitional create/update/delete mediator handlers waited for the same native lock and completed after release.

Each provider also passed actual HTTP sign-in with the synthetic owner, real antiforgery issuance, mixed replacement, invalid-save preservation followed by success, and two independent authenticated cookie sessions. A stale complete draft removed another session's addition and restored its deletion with a new server UTC. Canonical 18-widget snapshots and the resulting real public rendering/cache state were captured. These are backend/two-session checks; page-level two-tab drafts are verified in the B4 record.

Browser: ChatGPT in-app browser, PostgreSQL at 1440x900 and SQL Server at 1920x1080 CSS pixels, actual Razor pages/services/pipeline. Current-page Edit/Save succeeded; PostgreSQL reload retained the collection. Both showed the whole-collection result, added link before Tags, About B3 between Tags and Categories, and correct disabled filtering. No application console errors. Only the default fixture theme and English UI were exercised; no translated UI text changed. Browser version was not exposed by the documented read-only API. Temporary viewport overrides were reset.

## Evidence and cleanup

Local evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b3/`.

- `web-build.log`, `web-tests.log`, `setup-tests.log`, `node-tests.log`, and `host-build.log`.
- `PostgreSql-verify.log`, `SqlServer-verify.log`, `PostgreSql-http.log`, `SqlServer-http.log`, and sanitized host logs.
- `PostgreSql-http-saved.json`, `SqlServer-http-saved.json`: canonical synthetic saved collections, containing no real blog data or credentials.
- `browser.json`, `postgresql-public-added-1440.jpg`, `postgresql-public-moved-1440.jpg`, `sqlserver-saved-editor-1920.jpg`, and `sqlserver-public-1920.jpg`.

Both temporary hosts are stopped. B3 containers and volumes, temporary tools/runtime directories, and launcher are removed; logs/screenshots/synthetic snapshots remain. The helper referenced compiled assemblies directly and produced no private development-configuration copies. No development/production database was accessed in B3, and the temporary helper did not read the private override. No deployment or external service change was made.

Remaining scope: B6 user acceptance and separately authorized deployment. B5 verifies preview themes/settings/cultures, dragging and Link List opening flags. Public widget cache eviction remains the existing per-instance in-memory behavior; another application instance's public cache can remain warm until its usual expiry. Database collection coordination does not add distributed cache invalidation or durable audit delivery. B3 is accepted; B4 verification and acceptance are tracked in the delivery plan.
