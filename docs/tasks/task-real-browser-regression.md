# Real Browser Regression for Six Refactors

## Original Goal

Run the local development environment and use installed Edge or Chrome to verify all behavior affected by the six preceding refactors. The user authorized SSH access and creating an empty database on the existing intranet database server for first-installation testing.

## Scope

Verify article cache invalidation, atomic settings persistence/runtime replacement, article status assignments, direct recycle/publish/unpublish operations with logging, concrete service wiring, and the single configuration-default list. Cover both the existing development database and a dedicated empty PostgreSQL database.

## Constraints

Keep credentials, connection strings, authentication state, screenshots, and temporary browser scripts out of repository records. Mark test content explicitly. Isolate new-installation image files and icon cache. Suppress email delivery and route IndexNow calls to local test targets. Retain existing application data and restore settings modified for testing.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect environment, connect SSH, start existing blog | None | SSH and startup logs, browser identity | Complete |
| 2 | Start empty-database installation | 1 | Database initialization and browser sign-in | Complete |
| 3 | Exercise settings, posts, cache, protocols, images, and comments | 1, 2 | Real UI actions plus HTTP/database evidence | Complete, findings below |
| 4 | Restore test changes and summarize evidence | 3 | Data restoration, findings, screenshots | Complete |

## Execution Order

Prepare both environments, verify first installation and existing data loading, execute behavior checks, investigate failures, then restore test changes and record results.

## Current Progress

Completed testing in installed, headed Microsoft Edge through Playwright CLI because the Browser plugin is unavailable. Tested desktop 1440 x 1000 and mobile 390 x 844. Existing blog runs at `https://localhost:10210`; the isolated installation runs at `https://localhost:10211` against `moonglade_qa_20261024`, owned by the existing development application role. PostgreSQL runs on the authorized intranet server and was accessed through SSH key authentication and container-local psql.

Final source/build revision is `fcc00eca6` (`16.9.0-preview.1`). Early browser flows used `ab444eadd`, which differs only in test formatting/using directives; both hosts were subsequently rebuilt/restarted on `fcc00eca6`, and final smoke checks passed. No application source or configuration files were edited during this task. This repository task record is the only added file.

## Verification Log

| Date | Check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-24 | SSH key access and PostgreSQL connectivity | Passed | Accepted the server fingerprint as authorized. |
| 2026-10-24 | Existing database startup | Passed | Configuration loaded without adding defaults; Development skips migration. |
| 2026-10-24 | Edge homepage render | Passed | Correct page identity and meaningful content. |
| 2026-10-24 | New installation | Passed | Empty database creates schema, seed content, theme, local account, and 11 configuration rows. Admin sign-in succeeds without TOTP in normal Development mode. |
| 2026-10-24 | Unified article cache invalidation | Passed except tag deletion | Warmed post, homepage, RSS, Atom, category feeds, category/tag listings, and sitemap before writes. Publish, edit, unpublish, recycle, restore, category rename/delete, and tag rename immediately update affected readers. Tag deletion is blocked by an existing transaction error. |
| 2026-10-24 | Published article edits | Passed | Explicitly preserving the slug preserves the route and publication timestamp. Updated HTML/Markdown content appears immediately. Existing article editor type remains unchanged when the default editor changes. |
| 2026-10-24 | Article status assignments | Passed | Draft/published/scheduled transitions clear incompatible publication/scheduled timestamps and route values. Postpone, cancel, and Publish Now work. Browser local scheduling converts to UTC. |
| 2026-10-24 | Automatic scheduled publication | Passed | Due at 07:37:06.407 UTC, published at 07:37:06.425059 UTC; scheduled time cleared, LastModifiedUtc preserved, feed updated. |
| 2026-10-24 | Direct lifecycle commands and activity logs | Passed | Real UI publish/unpublish/recycle/restore and permanent deletion work; activity metadata identifies the article and actor. Empty recycle bin also succeeds. |
| 2026-10-24 | Settings persistence and runtime replacement | Passed | Seven settings forms and custom menu save successfully. Default editor changes affect new editors immediately and survive restart. |
| 2026-10-24 | Missing settings row during save | Passed | Temporarily removing ContentSettings causes save to fail visibly; runtime retains its original snapshot. The row was restored. |
| 2026-10-24 | Database-rejected settings write | Passed | A temporary CHECK constraint rejects a content update; persisted and runtime settings retain the previous values. Constraint removed afterward. |
| 2026-10-24 | Related image/general settings | Passed | Avatar/CDN changes update both settings together. An injected database failure leaves both persisted JSON values and both runtime values unchanged. |
| 2026-10-24 | Concurrent and unchanged settings writes | Passed | Eight general and eight content writes return 204; runtime matches the final database values. Unchanged general/content/image writes preserve persisted modification timestamps. |
| 2026-10-24 | Account settings | Passed | Database-rejected TOTP setup retains the empty runtime/persisted secret. Removing the constraint allows enrollment, verification, and reset. Changing credentials permits sign-in with the new values; default test credentials were restored and verified. |
| 2026-10-24 | Concrete service registration | Passed | Existing/new startup, icon generation, image naming/validation, comment submission guard, and syndication execute through their actual runtime registrations. |
| 2026-10-24 | Image upload and sanitization | Passed | Real editor upload inserts an image and saves content. Valid PNG/SVG uploads succeed; invalid PNG bytes return 400. Script/event attributes are removed from both primary and original SVG files. |
| 2026-10-24 | Image delivery and isolation | Passed | Browser-direct local HTTPS CDN image loads; legacy path redirects. Direct delivery returns GET/HEAD 200, If-None-Match 304, range 206 with eight requested bytes, and private original 404. CDN setting restored. |
| 2026-10-24 | Comments and request boundaries | Passed | Real anonymous comment enters review; approval makes it visible. Honeypot, too-fast, and missing timestamp requests return 400. Anonymous admin API access returns 401; authenticated missing-CSRF write returns 400. Invalid custom-menu JSON returns 400 without altering settings. |
| 2026-10-24 | Single default configuration list | Passed for valid/missing configuration | Fresh install persists all 11 defaults. Complete existing stores add no defaults. Removing AdvancedSettings then restarting restores it without changing installation time. Original configuration values/timestamps were restored after the test. |
| 2026-10-24 | Malformed stored configuration | Failed, existing behavior | Invalid FeedSettings JSON leads to duplicate-key insertion during initialization and site-wide 500. Restored the exact original row and restarted successfully. |
| 2026-10-24 | Separate sitemap invalidation | Passed | Publishing/unpublishing a page adds/removes its sitemap URL and changes public visibility; article RSS remains byte-identical. Temporary pages were removed. |
| 2026-10-24 | Render/console/responsive checks | Passed with existing category warning | Correct titles, meaningful page content, no framework error overlay, working images/comment rendering, and no mobile horizontal overflow. Development warning and expected errors from negative requests were distinguished from product failures. |
| 2026-10-24 | Build | Passed | Web project builds with zero warnings and errors. |
| 2026-10-24 | Full automated suite | 1144 passed, 2 failed, 0 skipped | `dotnet test src/Moonglade.slnx --no-restore --verbosity quiet`; the two existing migration-manifest tests fail for PostgreSQL and SQL Server. |
| 2026-10-24 | Restoration | Passed | Existing database retains 339 non-deleted published posts; marked test article is permanently removed and default editor restored to HTML. Isolated database has three posts, 11 configuration rows, default admin credentials, TOTP disabled, no injected QA constraints, and no QA backup tables. |

## Issues and Resolutions

### PostgreSQL tag deletion returns 500

- Reproduce: sign into the isolated installation, open Tags, and delete `qa-renamed-tag`; `DELETE /api/tags/3` returns 500 and the tag remains.
- Evidence: server reports that `NpgsqlRetryingExecutionStrategy` does not support the user-initiated transaction. `DeleteTagCommand.cs:21` begins that transaction outside the configured execution strategy.
- History: the transaction predates the six refactors (commit `e6e17797d8`, March 2026). The recent cache event is never reached on this failure.
- Status: fixed and verified in the follow-up [retry-safe tag deletion task](task-tag-delete-retry.md), including actual PostgreSQL and SQL Server LocalDB. The formerly failing retained tag was successfully deleted through Edge. The initial findings above remain the historical results of this regression run.

### Malformed persisted settings prevent startup

- Reproduce: back up FeedSettings, replace its JSON with invalid text, and restart the isolated host. The homepage returns 500 with the initialization failure message.
- Evidence: `BlogConfig.LoadFromConfig` falls back to defaults and sends the existing key to `AddDefaultConfigurationCommand`; its unconditional INSERT violates `PK_BlogConfiguration`.
- History: the same fallback-to-insert sequence exists before the default-list refactor (`ab444eadd^`). This is not a newly introduced regression.
- Status: recorded, not fixed. Restored both backed-up configuration values and timestamps, removed the backup table, and confirmed successful restart with no defaults added.

### Category modal browser pattern is invalid

- Reproduce: use the category editor in Edge and validate/save a slug.
- Evidence: the browser reports an invalid regular expression under HTML pattern's `v` rules for `(?!-)([a-z0-9-]+)` in `Pages/Admin/Category.cshtml:81`.
- Impact: browser pattern validation is ignored; valid category saves still work and server validation remains in place.
- History: the markup predates the six refactors (commit `c7d6fce899`, February 2026).
- Status: fixed in the follow-up category validation task by escaping the literal hyphen in the HTML pattern. The existing .NET validation and accepted slug values are unchanged. Reference: [HTML pattern character escaping](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/pattern#overview).
- Verification: reproduced the original Edge error and incorrect acceptance of `Invalid_slug`. After the fix, 13 browser validity cases pass; invalid create/edit submissions send no API request, valid creation returns 201, valid editing returns 204, and direct invalid API input still returns 400. No regular-expression console errors occur. Eight CategoryControllerTests pass and the Web build has zero warnings/errors. The temporary category was removed from the isolated LocalDB database; temporary host/browser sessions were stopped. No README or AGENTS.md update was needed.
- Evidence: `category-validation-results.json`, `category-edit-results.json`, `category-invalid-blocked.png`, and `category-edit-saved.png` under `%LOCALAPPDATA%/Temp/moonglade-tag-delete-20261024`. Browser validation used installed headed Edge at 1440 x 1000 through Playwright CLI; Firefox/Safari were not exercised. No new dependency or production test abstraction was introduced.

### Existing automated migration assertions

`MigrationManagerTests.TryMigrationAsync_MigrationScriptManifest_DoesNotRequireMigration` fails for both providers: expected NotRequired, actual UnsupportedVersion. The cumulative scripts identify version 16.8.1 while this application is 16.9.0 preview. These failures were already known before this testing task. Development initialization skips production migration, so successful first installation does not establish production upgrade compatibility.

### Test environment corrections

The new database initially belonged to the database administrator rather than the application's development role. Its owner was corrected before application testing; no existing database permissions changed. Early browser harness locator/navigation mistakes were corrected and the affected checks rerun. Expected 400/500 responses from deliberate fault injection were not treated as product defects.

## Evidence and Retained Environment

Temporary scripts, logs, screenshots, fixtures, and browser state are outside the repository under `%LOCALAPPDATA%/Temp/moonglade-qa-20261024-six-refactors`. Useful evidence includes `public-desktop.png`, `public-mobile.png`, `new-published-editor.png`, `settings-write-failure.png`, `final-activity-logs.png`, `new-server-malformed-default.log`, and `dotnet-test.log`. Authentication state and TOTP enrollment evidence must remain private.

Both development hosts and local integration test targets remain running for review. The isolated database and image directories are retained. Its representative article is `/post/2026/10/24/qa-six-refactors-new`. Normal Development TOTP behavior and default test credentials were restored. Existing development test content was removed; normal activity logs, setting modification timestamps, and read counters generated by testing remain.

Email delivery was disabled through process environment overrides. All IndexNow targets were redirected to a local HTTP test receiver. CDN delivery used a local HTTPS fixture server reading only the isolated primary image root. No runtime overrides were written into application configuration files.

## Follow-ups

Address the three reproduced existing defects separately. Real SQL Server execution, Production upgrade scripts, external SMTP delivery, and public search-engine/CDN integration were not exercised. Automated tests cover both provider migration decisions but currently include the two failures described above. No permanent README or architecture-document changes were needed because the task changed no product behavior, architecture, or deployment instructions.
