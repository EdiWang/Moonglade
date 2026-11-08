# Tests

The 15 test projects follow the production project boundaries and use xUnit v3 with Microsoft.Testing.Platform. They contain both focused unit tests and integration tests for database queries, service registration, and HTTP behavior.

## Layout

| Project | Organization |
| --- | --- |
| `Moonglade.Features.Tests` | Production feature folders: `Post`, `Page`, `Comment`, `Category`, `Tag`, `Dashboard`, and `SiteVerification`. |
| `Moonglade.Web.Tests` | `Controllers`, `Pages`, `Commands`, `Handlers`, `Services`, `Models`, `Caching`, and `Hosting`. `Structure` contains settings compatibility experiments and SQLite account-writer boundary tests; its Razor fixtures are compiled by the Razor SDK in the test project. Shared test helpers live in `TestSupport`. Page folders mirror the production pages. |
| `Moonglade.Email.Tests` | `Configuration`, `Delivery`, `Notifications`, and `Outbox`. |
| `Moonglade.Utils.Tests` | Content processing tests are split by behavior under `Content`; tag validation and normalization live under `Tags`. Other utilities have their own test files. |
| `Moonglade.Setup.Tests` | Startup, configuration initialization, preservation of existing content and installation metadata, and UTC validation. |
| Other test projects | Keep the existing small, focused suites close to their production ownership. |

Namespaces match folders. Import shared Web helpers from `Moonglade.Web.Tests.TestSupport`; keep helpers used by only one suite local.

Settings tests exercise production types, HTTP endpoints, and compiled application pages rather than parallel candidate models or test-only Razor fixtures. They cover immutable JSON binding and request defaults, parsed menu limits, subscription-page TagHelper metadata and saved values, and SQLite commit/publication behavior. `LocalAccountCommandTests` exercises the production Auth writer with SQLite, including failure, no-op, cancellation, shared serialization, event ordering, and detached credentials. Auth tests own protected account parsing and state isolation; Setup tests own manifest parsing and preflight validation. Real LiteBus registration covers both writers and the shared gate. The outer-transaction test deliberately characterizes an unsafe composition of the current command and must be revised when that boundary is redesigned, not interpreted as an approved production pattern.

## Test Value

Protect business outcomes, public input validation, security boundaries, error handling, cancellation, time handling, and data consistency. Service registration and HTTP pipeline tests remain useful even when their assertions are small: they protect application wiring that isolated handler tests cannot exercise.

Redundant setup is reduced with theories when the same operation and assertions apply to several inputs. Separate tests remain appropriate for different side effects or failure paths. Assertions target observable results, such as persisted values, generated URLs, serialized documents, HTTP responses, and queued notifications.

Tests that only repeat a happy path, pass an uncancelled token without testing cancellation, inspect a trivial getter, or check a startup log add little protection when stronger behavioral coverage already exists. Failing tests are investigated and reported; failure alone is never a reason to delete or weaken a test.

See [AGENTS.md](../../AGENTS.md#development-and-verification) for the repository's verification requirements.

## Database Upgrade Coverage

Startup tests use EF Core InMemory to verify settings initialization and preservation of stored content, manifest JSON, and timestamps for existing databases with missing or older manifests. They do not prove relational schema compatibility.

Startup safety tests exercise each mapped application set as the only existing data and verify that a missing account prevents any repair or reseeding. The opt-in `StartupInstallationDatabaseTests` use the same disposable PostgreSQL and SQL Server fixture as the migration tests, enabled with `MOONGLADE_TEST_CATEGORY_DATABASES=1`. They cover empty/new schemas, repeated startup, transaction rollback at seed/default writes, cancellation, provider retries, lost commit acknowledgment, state-query failures, and preservation of posts/comments when settings or accounts are missing. These fault scenarios must never run against a real blog database. Normal runs explicitly skip this provider group.

`StartupApplicationTests` covers the outer application boundary: empty settings, missing accounts, and invalid protected accounts must throw before hosted services start, preserving scheduled posts and every settings row/timestamp. Successful initialization returns to the caller, which can then start the host normally. This complements initializer tests; it prevents a diagnostic error host from silently running business workers after initialization fails.

Operation-consistency tests pause real SQLite RSS/Atom queries (all/category) during settings publication and verify old metadata, item count, full content, CDN formatting, and author email throughout, followed by fresh state on a reused handler and explicit supplied-context preservation. Real front-page rendering pauses the data query while publishing new pagination/menu/theme settings; handlers, layout, components and actual cached theme CSS retain one capture, and the next render refreshes. View/Partial result tests preserve prepared handler captures and show a separate operation can capture new state without an HttpContext-wide freeze.

Comment tests publish settings during domain save, checking captured moderation/review/notification decisions and propagated command/event context. Email tests preserve supplied context while independent next operations refresh; restart-bound sender behavior remains covered. Image storage-time publication cannot change an in-progress upload's original-retention policy. Warm feed/sitemap tests publish a new root without advancing invalidation to protect the publication/event gap. Metadata-driven initialization/default fixtures inspect BlogSettingsSnapshot, since IBlogConfig exposes only Capture().

Snapshot tests verify the singleton reader alias, init-only values/nested immutable menus, and retained old roots. Barrier-based command tests pause after persistence but before publication, verify all-old state until release, assert the updated group is visible before events, and queue a Feed save prepared from an old capture to protect unrelated latest groups. Rejected writes retain the exact root; no-op saves preserve timestamps while refreshing runtime state. Real front/admin Razor rendering tests verify local avatar URLs at root and `/blog`, with no shared snapshot mutation; the broader concurrent render/query tests cover operation/render consistency.

Settings-default tests exercise the pure parser across all ordinary groups, absent versus explicit values, case-insensitive/duplicate saved fields, protected-state exclusion, and independent default menu collections. Setup tests verify that loading valid partial JSON twice preserves the original JSON and timestamps without writes or repair warnings; the opt-in provider group verifies the same persistence contract on both supported databases. Paused-repair tests verify that prepared settings and accounts remain unpublished until repairs finish, and a failed repair preserves the previous runtime state. Corrupt-group tests assert a single repair Warning without logging the stored payload.

Settings validation tests cover ordinary whole-group recovery, protected semantic failure before repairs, invalid direct writes without persistence or runtime changes, nonmutating validation, nested menus, undefined enums, UTF-8 CSS boundaries, and approved legacy/setup states. Web TestHost tests exercise model validation and parsed-menu command failures as HTTP validation problems, alongside blank sender names and inactive legacy avatar JSON compatibility. The opt-in provider group also verifies semantic recovery and protected failure preservation on both supported databases.

Settings cache tests in `PostCacheInvalidationTests.Settings.cs` warm real RSS/Atom/category feeds and sitemap before saving through LiteBus. They cover full-content/count changes, site metadata, sequential group saves, image CDN changes, theme generations, old in-flight factories, save failure, and cancellation after commit. Command tests assert change-event ordering and no event for rejected/no-op writes. Scheduled worker tests synchronize on actual waits/dispatches to verify disabling pending work, waking while disabled, and preserving already running publication. The SQLite/LiteBus regression uses the real next-time query and publish command to catch overdue work waiting for the polling interval; query tests also exclude deleted or undated schedules. Browser acceptance should use a disposable database and isolated host configuration; verify restart-dependent toggles in both directions without touching a real blog.

Settings and avatar audit tests inject dispatch exceptions and cancellation after real SQLite saves, asserting successful responses, actual persisted/runtime values, safe contextual Error logs, and authenticator-reset sign-outs. Separate faults cover validation, settings/asset database writes, cache events, and sign-out, ensuring they are not swallowed by audit isolation. HTTP tests cover all ordinary settings routes, including image settings without a storage-service dependency. Site-icon and post-controller regressions preserve their excluded audit contracts. Avatar tests preserve asset/audit failures while asserting that settings JSON/timestamps and the captured root remain unchanged, even with image CDN enabled. A real LiteBus discovery test verifies avatar asset persistence. Image endpoint/storage tests cover article-image delivery.

Activity Log tests use SQLite and real handlers to verify clear-only management, exact deleted counts and UTC administrator entries, delete failures, and post-clear dispatch/cancellation/persistence failures. A failed activity insert must not reappear on a later save in the same context or discard unrelated tracked changes. HTTP tests use the real compiled ActivityLogs page, local administrator authorization policy, and antiforgery tokens with synthetic authentication claims. Public comment tests preserve moderation, review, persistence, and actual email outbox enqueueing without visitor activity entries. Node tests exercise confirmed, cancelled, and failed clear flows.

Manual upgrade tests for category IDs and PostgreSQL naming use opt-in disposable database containers. See [Migration Verification](../../docs/agent-reference.md#migration-verification) for prerequisites and limits; ordinary runs explicitly skip those relational groups.

## Run Tests

Run commands from the repository root. For a focused change, select the owning project:

```powershell
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
```

To verify all test projects, including test discovery after moving files:

```powershell
dotnet test --solution src/Moonglade.slnx
```

The root `global.json` selects Microsoft.Testing.Platform. Moving tests within a project needs no compile or solution entries; update saved fully qualified test filters when namespaces change.

Web JavaScript behavior tests use Node.js's built-in test runner without additional packages. They cover frontend helpers and image selection, including avatar/site-icon drag and drop, preview resizing, and invalid or empty selections:

```powershell
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
```
