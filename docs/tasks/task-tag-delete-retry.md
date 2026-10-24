# Retry-safe Tag Deletion

## Original Goal

Fix tag deletion returning HTTP 500 and verify actual PostgreSQL and SQL Server behavior. The user authorized the existing isolated PostgreSQL environment and local SQL Server LocalDB or Docker Desktop.

## Background and Scope

Both database registrations enable retries. DeleteTagCommand starts an explicit transaction outside the execution strategy, which rejects its DELETE statements. Preserve atomic deletion of PostTag links and the tag, existing HTTP outcomes, and one post-commit cache invalidation event. Inspect the controller, handler callers, relationship mappings, and feature/cache tests before changing code.

## Task Breakdown and Execution Order

| No. | Task | Dependency | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add retry/rollback regression coverage | None | Fail against the old handler | Complete |
| 2 | Use the provider execution strategy for the transaction | 1 | Focused feature/cache tests | Complete |
| 3 | Exercise both actual database providers | 2 | API, database, cache, failure rollback | Complete |
| 4 | Final review and record results | 3 | Build, diff, cleanup | Complete |

## Current Progress

Implemented the fix in DeleteTagCommandHandler using `CreateExecutionStrategy().ExecuteInTransactionAsync`. The two DELETE statements remain atomic and the provider can retry them together. The verification delegate checks whether the tag was deleted if commit acknowledgement fails, preserving the affected-row result and one cache event after a successful commit. The existing not-found result and non-cancelable post-commit notification remain unchanged.

No schema, provider registration, application abstraction, dependency, or API contract changes were needed. No README or AGENTS.md update was required; this task record captures verification. Test connections, browser state, scripts, logs, and screenshots remain outside the repository in `%LOCALAPPDATA%/Temp/moonglade-tag-delete-20261024`.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-24 | Tag regression tests before the fix | Reproduced | Four cases fail with the same user-initiated transaction rejection when retries are enabled. |
| 2026-10-24 | Focused TagCommandTests after the fix | Passed, 9 cases | Includes missing tags, rollback, statement retry, and recovery after commit acknowledgement loss. |
| 2026-10-24 | Full Moonglade.Features.Tests project | Passed, 112 cases | Retry simulation is confined to tests; no production interfaces or virtual methods were added. |
| 2026-10-24 | PostCacheInvalidationTests | Passed, 34 cases | Successful tag deletion invalidates article/feed/sitemap caches; unrelated caches retain their rules. |
| 2026-10-24 | Web build | Passed | Zero warnings and errors. |
| 2026-10-24 | SQL Server 2025 LocalDB | Passed | New isolated `moonglade_tag_qa_20261024` database initialized through the actual host and retry-enabled SQL Server registration. |
| 2026-10-24 | Existing isolated PostgreSQL installation | Passed | Used `moonglade_qa_20261024` through the actual host and retry-enabled PostgreSQL registration. The original development database was not modified. |
| 2026-10-24 | Edge admin deletion, both providers | Passed | Deleted a tag shared by two published posts through the confirmation modal: HTTP 204, tag removed, both posts and another tag's two links preserved. |
| 2026-10-24 | Empty and missing tags, both providers | Passed | Empty tag deletion returns 204; missing/repeated deletion returns 404. |
| 2026-10-24 | Real database rollback, both providers | Passed | Temporary triggers reject tag deletion after link deletion. HTTP 500 is expected for this injected fault; direct database checks confirm the tag and its link remain. After removing the triggers, deletion returns 204. |
| 2026-10-24 | Original PostgreSQL reproduction | Passed | Edge successfully deletes the formerly failing `qa-renamed-tag` (ID 3); its article remains publicly readable. |
| 2026-10-24 | Public rendering and cache, both providers | Passed with existing sidebar TTL | Warmed article/tag/index pages refresh their own tag data. Both article URLs remain 200 and retain the unrelated tag. The independently cached sidebar is described below. |
| 2026-10-24 | Activity logging, both providers | Passed | Successful deletes have actor `admin`, operation `Delete Tag`, and the correct tag ID. Rejected/missing deletes do not produce success activity logs. |
| 2026-10-24 | Cleanup and final health | Passed | No injected trigger/function remains in either database; both hosts report ready. Marked fixture posts and the unrelated tag remain only in isolated databases for review. |

## Issues and Resolutions

The entire transaction must be retried as a unit. Cache notification must run once after successful commit, including recovery from a lost commit acknowledgement. EF Core's built-in helper handles the transaction and success verification without an application abstraction. Reference: [EF Core connection resiliency and transactions](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions).

The regression tests use a retrying SQLite execution strategy and interceptors to reproduce both a transient DELETE failure and a lost commit acknowledgement. A rejected DELETE test proves that earlier link deletions roll back and no cache event is published. Actual SQL Server and PostgreSQL tests additionally exercise provider SQL, retry-enabled registration, foreign keys, database-trigger failures, and the authenticated HTTP/UI path.

The existing tag sidebar uses a separate Razor `<cache>` fragment in `Pages/Shared/_Aside.cshtml`, controlled by WidgetCacheMinutes (default 30 minutes). It can retain a deleted tag until expiry even though article metadata, the main tag index, and the admin list update immediately. This is outside the transaction fix; its behavior was recorded rather than changing the earlier cache policy.

Browser plugin was unavailable, so installed Playwright CLI drove headed Edge at 1440 x 1000. Final screenshots are `sql-tag-deleted.png` and `postgres-tag-deleted.png`. The SQL Server tag screen has no relevant console errors. The reused PostgreSQL fixture points at the previous task's stopped local CDN, so its avatar requests fail; this does not affect tag deletion or database assertions. No external email or IndexNow delivery was performed.

## Follow-ups

The separately reported malformed-settings and category HTML pattern issues, plus the independent sidebar fragment TTL, remain outside this task. No live network disconnection was forced against either database; deterministic tests cover retry and ambiguous commit recovery. Actual database failures were tested with temporary triggers instead. Test databases are retained; temporary hosts and the IndexNow receiver are stopped after verification.
