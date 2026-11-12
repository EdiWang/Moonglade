# Tests

The 15 test projects follow the production project boundaries and use xUnit v3 with Microsoft.Testing.Platform. They contain both focused unit tests and integration tests for database queries, service registration, and HTTP behavior.

## Layout

| Project | Organization |
| --- | --- |
| `Moonglade.Features.Tests` | Production feature folders: `Post`, `Page`, `Comment`, `Category`, `Tag`, `Dashboard`, and `SiteVerification`. |
| `Moonglade.Web.Tests` | `Controllers`, `Pages`, `Commands`, `Handlers`, `Services`, `Models`, `Caching`, and `Hosting`. `Structure` covers settings snapshot and SQLite account-writer boundaries. Shared test helpers live in `TestSupport`. Page folders mirror production ownership, not presentation structure. |
| `Moonglade.Email.Tests` | `Configuration`, `Delivery`, `Notifications`, and `Outbox`. |
| `Moonglade.Utils.Tests` | Content processing tests are split by behavior under `Content`; tag validation and normalization live under `Tags`. Other utilities have their own test files. |
| `Moonglade.Setup.Tests` | Startup, configuration initialization, preservation of existing content and installation metadata, and UTC validation. |
| Other test projects | Keep the existing small, focused suites close to their production ownership. |

Namespaces match folders. Import shared Web helpers from `Moonglade.Web.Tests.TestSupport`; keep helpers used by only one suite local.

Settings tests exercise production types and HTTP endpoints rather than parallel candidate models. They cover immutable JSON binding and request defaults, parsed menu limits, capability-based test-email gating, and SQLite commit/publication behavior. Account tests exercise the production Auth store through SQLite, including credential verification, revision conflicts, failed/canceled write cleanup and enclosing-transaction rejection. Auth owns independent account state and cookie validation; Setup owns manifest parsing and initialization safeguards. Real registration resolves the ordinary LiteBus writer and write gate separately from scoped Auth storage. Account operations never publish credentials through the ordinary settings snapshot.

## Test Value

Protect business outcomes, public input validation, security boundaries, error handling, cancellation, time handling, and data consistency. Service registration and HTTP pipeline tests remain useful even when their assertions are small: they protect application wiring that isolated handler tests cannot exercise.

C# tests do not provide fine-grained UI coverage. Do not assert compiled Razor/HTML structure, CSS classes/styles, layout, menus/buttons, localized UI wording, or UI asset/source text. Verify presentation in the browser instead, and do not add replacement tests merely to prove a removed UI feature or test is absent.

Preserve functional PageModel/query/handler behavior, HTTP/API contracts, authentication/authorization, antiforgery enforcement, security encoding/sanitization, and feed/protocol/SEO semantics. Rendering may be used to exercise a request or security boundary, but assertions must target that boundary rather than its visual markup. Text-only bindings for untrusted activity details and encoded administrator claims are security checks, not appearance checks. HTTP culture negotiation and resource format-argument integrity remain functional checks; exact translated UI wording does not. Localized labels returned by an API are checked as response data, not as browser presentation acceptance.

Redundant setup is reduced with theories when the same operation and assertions apply to several inputs. Separate tests remain appropriate for different side effects or failure paths. Assertions target observable results, such as persisted values, generated URLs, serialized documents, HTTP responses, and queued notifications.

Tests that only repeat a happy path, pass an uncancelled token without testing cancellation, inspect a trivial getter, or check a startup log add little protection when stronger behavioral coverage already exists. Failing tests are investigated and reported; failure alone is never a reason to delete or weaken a test.

See [AGENTS.md](../../AGENTS.md#development-and-verification) for the repository's verification requirements.

## Database and Startup Coverage

Startup tests use EF Core InMemory to verify settings initialization and preservation of stored content, manifest JSON, and timestamps for existing databases with missing or older manifests. They do not prove relational schema compatibility.

Startup safety tests exercise each mapped application set as the only existing data and verify that absent installation configuration prevents reseeding. Account absence alone is a valid setup state when installation configuration exists.

Local authentication uses independent account storage; obsolete credential JSON remains inert. Auth's SQLite tests cover profile-only rejection, email normalization, stable identity, proof-gated initialization, expiry and stale revisions, password/TOTP changes, failure/cancellation cleanup and enclosing-transaction rejection. Storage tests are distinct from Web HTTP security and actual browser acceptance.

Web account HTTP checks cover the pending page/API/feed gate, health and static endpoints, antiforgery, authentication rate limiting, protected setup tickets, account-revision cookies, mandatory setup for profile-only owners, and rejection of legacy OIDC cookies. Keep these functional/security checks separate from presentation: verify setup, password/reset dialogs, real authenticator entry and localization in an isolated browser. For an all-environment TOTP check, use a correctly published Production host; an unpublished Debug build does not automatically enable static-web-asset providers in Production.

Owner-email cutover regressions cover the Auth-owned public profile, password-protected Local changes, unchanged-address revision/session retention, stale cookies, preservation of profile-only owners during local setup, and ordinary General writes that cannot replace account email. Atom tests exercise missing profiles, warm/cold category caches, supplied profile identity and old in-flight queries; HTTP FOAF checks retain mailbox hashing and per-operation captures. Notification tests retain queued recipients/payloads, refresh independent operations and skip only owner-directed mail when no profile exists. These business/protocol tests complement browser verification of Account editing, Gravatar and local account navigation; they do not assert UI markup.

`StartupApplicationTests` covers the outer application boundary: absent installation configuration or an invalid manifest must throw before hosted services start, preserving scheduled posts and every settings row/timestamp. An absent independent account permits pending setup; malformed legacy credential JSON is ignored without a credential repair. Successful initialization returns to the caller, which can then start the host normally. This complements initializer tests; it prevents a diagnostic error host from silently running business workers after unrelated initialization failures.

Operation-consistency tests pause real SQLite RSS/Atom queries (all/category) during settings publication and verify old metadata, item count, full content, CDN formatting, and author email throughout, followed by fresh state on a reused handler and explicit supplied-context preservation. Search requests pause the data query during publication and assert the captured page size, followed by the new page size on the next request. View/Partial result tests preserve prepared handler captures and show a separate operation can capture new state without an HttpContext-wide freeze.

Comment tests publish settings during domain save, checking captured moderation/review/notification decisions and propagated command/event context. Email tests preserve supplied context while independent next operations refresh; restart-bound sender behavior remains covered. Image storage-time publication cannot change an in-progress upload's original-retention policy. Warm feed/sitemap tests publish a new root without advancing invalidation to protect the publication/event gap. Metadata-driven initialization/default fixtures inspect BlogSettingsSnapshot, since IBlogConfig exposes only Capture().

Snapshot tests verify the singleton reader alias, init-only values/nested immutable menus, and retained old roots. Barrier-based command tests pause after persistence but before publication, verify all-old state until release, assert the updated group is visible before events, and queue a Feed save prepared from an old capture to protect unrelated latest groups. Rejected writes retain the exact root; no-op saves preserve timestamps while refreshing runtime state. Public/admin requests at root and with `/blog` PathBase preserve the shared snapshot; these isolated request tests do not establish end-to-end subpath hosting support.

Settings-default tests exercise the pure parser across all ordinary groups, absent versus explicit values, case-insensitive/duplicate saved fields, protected-state exclusion, and independent default menu collections. Setup tests verify that loading valid partial JSON twice preserves the original JSON and timestamps without writes or repair warnings. Paused-repair tests verify that prepared settings remain unpublished until repairs finish, and a failed repair preserves the previous runtime state. Corrupt-group tests assert a single repair Warning without logging the stored payload.

Settings validation tests cover ordinary whole-group recovery, protected semantic failure before repairs, invalid direct writes without persistence or runtime changes, nonmutating validation, nested menus, undefined enums, UTF-8 CSS boundaries, and approved legacy/setup states. Web TestHost tests exercise model validation and parsed-menu command failures as HTTP validation problems, alongside blank sender names.

Settings cache tests in `PostCacheInvalidationTests.Settings.cs` warm real RSS/Atom/category feeds and sitemap before saving through LiteBus. They cover full-content/count changes, site metadata, sequential group saves, image CDN changes, theme generations, old in-flight factories, save failure, and cancellation after commit. Command tests assert change-event ordering and no event for rejected/no-op writes. Scheduled worker tests synchronize on actual waits/dispatches to verify disabling pending work, waking while disabled, and preserving already running publication. The SQLite/LiteBus regression uses the real next-time query and publish command to catch overdue work waiting for the polling interval; query tests also exclude deleted or undated schedules. Browser acceptance should use a disposable database and isolated host configuration; verify restart-dependent toggles in both directions without touching a real blog.

Settings and avatar audit tests inject dispatch exceptions and cancellation after real SQLite saves, asserting successful responses, actual persisted/runtime values, safe contextual Error logs, and authenticator-reset sign-outs. Separate faults cover validation, settings/asset database writes, cache events, and sign-out, ensuring they are not swallowed by audit isolation. HTTP tests cover all ordinary settings routes, including image settings without a storage-service dependency. Site-icon and post-controller regressions preserve their excluded audit contracts. Avatar tests preserve asset/audit failures while asserting that settings JSON/timestamps and the captured root remain unchanged, even with image CDN enabled. The account avatar tests cover Auth registration, independent persistence, administrator authorization and antiforgery. Image endpoint/storage tests cover article-image delivery.

Activity Log tests pin all 50 canonical event keys, groups, and labels. SQLite writer/read tests cover UTC, generated IDs, captured targets surviving deletion, typed scalar/flat-array/enum details, string limits, explicit System actors, rejected restricted details, deterministic time/ID ordering, pagination, and inclusive date/key filters. A failed activity insert must not reappear on a later save in the same context or discard unrelated tracked changes. Clear-only tests retain exact deleted counts and delete/dispatch/cancellation/persistence failure isolation. HTTP tests use the real compiled ActivityLogs page to obtain antiforgery tokens, with the local administrator authorization policy and synthetic authentication claims. They verify canonical grouped filters/list/detail API contracts across supported cultures, empty-detail 200 versus missing-row 404, obsolete numeric-filter rejection, safe actor/target fallbacks, and text-only handling of untrusted details. Public comment tests preserve moderation, review, persistence, and actual email outbox enqueueing without visitor activity entries. Node tests import the actual module to exercise string-key/date filters, supplied summaries, structured typed details and empty context, pagination, and confirmed/cancelled/failed clear flows.

B3 settings tests compare effective immutable values inside the actual configuration write gate independently of persisted JSON equality. The direct Configuration matrix checks every ordinary field against the exact 51-field B0 value allowlist for full and partial saves, restricted-only and mixed differences, all nine no-op groups, structural nested menus, approved scalar boundaries, and overlapping same/different-group saves. Legacy JSON case/order/omitted-field normalization can advance raw persistence timestamps and cache events without producing an effective-change audit. Results must wait for successful persistence, runtime publication and cache-event completion; precommit failures retain old state, while postcommit cache failures remain visible without a fabricated successful result.

Real SQLite Web composition checks the safe `Changes` envelope, durable old/new values, no-op suppression, restricted-only empty arrays, audit dispatch/cancellation/insert isolation, actual feed/sitemap invalidation, and no stale audit resurrection. Password and authenticator operations remain named events without protected details and retain their existing session/sign-out policies. Direct Activity writer/reader tests independently pin all nine event allowlists and reject unknown or wrong-event keys, repeated keys/members, unchanged values, malformed envelopes and nonscalar changes before persistence or read exposure, with payload-free diagnostics. Ordinary settings events require typed `Changes` (`[]` for an update with no permitted values). Other events expose their ordinary `Fields` with `changes: null`. Isolated HTTP tests use the actual administrator policy and compiled-page antiforgery tokens for ordinary POST/PATCH saves, verify empty 204 bodies, and check all 51 labels in five cultures plus plain-text Razor old/new bindings. Node tests verify the intentional empty state and every approved choice in the six localized settings enums. These tests use synthetic data and EF InMemory/SQLite only; they do not run a developer database, schema migration, full application startup or browser acceptance.

Automatic-publication tests in the Features, BackgroundServices and Web projects protect B4's committed-publication handoff. The direct feature matrix pins cutoff/execution-time separation across a UTC date rollover, exact before/at/after boundaries, null/deleted/draft/already-published exclusions, unchanged modification times, generated routes, EF result counts and empty/failed/cancelled saves. Detached ordered read-only snapshots survive subscriber mutations. Postcommit cache and handoff notifications remain noncancelable; cache errors still reach the activity subscriber, retain the original exception, and do not undo publication. Handoff failures retain successful counts or the original cache failure.

Web tests independently assert per-post System commands, approved `post.automatically-published`/PostId-only details, captured ID/title/occurrence time, sequential best-effort dispatch, safe diagnostics and continuation after failure/cancellation. Writer tests retain the default UTC time, persist supplied UTC exactly, reject local/unspecified values and detach failed inserts before a later unrelated save. SQLite/LiteBus scenarios separate publication/ineligible state and Post/RssCategory/AtomCategory/feed/sitemap cache results from activity entries, inject one failed insert, and verify no repeat entries or stale resurrection. Five-culture HTTP checks retain target snapshots after live rename/deletion, canonical filter/list/detail identity, required occurrence time and plain-text Razor bindings. Actual Web service composition discovers the new subscriber without test-only registration. Worker tests execute the real feature command for zero/positive results and cache/handoff failures, preserving int dispatch, wake/toggle/shutdown behavior without sleeping through the error backoff. These tests use only synthetic EF InMemory/SQLite data and TestServer; no real database, provider migration, outbound notification, full hosted-service startup or browser is used.

Five-culture writer-to-HTTP regressions cover every non-settings allowed detail label, including the exact `Widget Type`, `Accent Color`, `File Name` and `Content Type` resource keys, typed scalar/array/enum values, literal markup/space-containing names, and the compiled page's plain-text field bindings. These automated fixtures do not constitute browser verification or user acceptance.

Activity clear-all confirmation behavior remains in the JavaScript suite: shared-modal options, cancellation, awaited delete/reload/success, and failed-delete sequences. C# HTTP tests retain authorized clearing, antiforgery rejection, durable counts and audit behavior; they do not assert confirmation wording, action markup, drawer structure or CSS.

The repository suite covers current application behavior and EF Core mappings. See [Migration Verification](../../docs/agent-reference.md#migration-verification) for temporary SQL migration checks.

## Run Tests

Run commands from the repository root. For a focused change, select the owning project:

```powershell
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --project src/Tests/Moonglade.BackgroundServices.Tests/Moonglade.BackgroundServices.Tests.csproj
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

## Owner Account Release Acceptance

Run the solution and frontend test commands above and build the Web project separately. For runtime acceptance, use independent disposable databases on PostgreSQL and SQL Server for fresh installs, upgraded local accounts, and upgraded profile-only OIDC sites. Use synthetic data, isolated key/image directories, loopback HTTPS and explicitly isolated configuration; never inherit a developer connection string or run faults against a real blog.

Apply the published account scripts with every host stopped, compare unrelated rows and retained General values/timestamps, and repeat both scripts before restarting. Verify pending page/feed/health/static behavior, a rejected setup submission without persistence, and restart without reseeding or overwriting data. Complete real browser setup/sign-in with TOTP required and disabled, then verify password changes, immediate email editing, reset/re-enrollment, direct links and localized rendering. Recheck persisted account identity after restart and script reruns following later account edits.

Use legacy OIDC deployment settings and an old OIDC-shaped session ticket in cutover tests. Verify local setup is required for a profile-only site, the previous cookie cannot access admin, the retired identity route returns 404, and setup keeps the owner record and profile. Use the browser to verify the local setup and sign-in flows and Account Settings.

OIDC binding tests use the real ASP.NET Core OIDC handler with a synthetic provider. They exercise discovery, PKCE, signed tokens, explicit confirmation, and stale or canceled binding attempts. For runtime acceptance, restore a disposable database into a new Docker instance, then verify the stored binding and a fresh provider code exchange without supplying the client secret again.

OIDC sign-in tests use the same handler to check exact issuer and subject matching, a changed configuration during the round trip, local-only logout, and revocation after disabling, unbinding, or deleting. They also verify that local access survives provider revocation and that refreshing a cookie after a password change preserves its OIDC origin. Use a browser with the synthetic issuer for repeatable verification and record live-provider acceptance separately.
