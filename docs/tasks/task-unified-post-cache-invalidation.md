# Unified Post Cache Invalidation

## Original Goal

Implement the approved rule: successful post content, metadata, and lifecycle writes invalidate the entire post partition, all RSS/Atom caches, and the sitemap. Reuse LiteBus events for both HTTP and scheduled publishing paths.

## Background

The owner writes one or two posts per month and accepts coarse invalidation. Existing readers use route links while several writers remove GUID keys. Category Atom and scheduled publishing invalidation are missing.

## Scope

- Publish PostChangedEvent after successful feature writes, including category/tag updates and deletes.
- Handle the event in Web with one cache invalidation policy.
- Remove redundant article cache removal and filters; retain page-specific invalidation.
- Verify real cache behavior, failed/no-op writes, batches, and concurrent fills.
- Update repository guidance for the new rule.

## Out of Scope

Other architecture recommendations, distributed caches, database migrations, public URL changes, authentication changes, and external notifications.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add event and post-commit publication to feature handlers | None | Feature tests | Complete |
| 2 | Centralize Web invalidation and remove duplicated callers | 1 | Real-cache and mediator wiring tests | Complete |
| 3 | Verify affected modules and synchronize documentation | 1, 2 | Web build and affected test projects | Complete |

## Execution Order

Inspect handlers and cache implementation, add the event path, migrate callers, add behavioral regression coverage, then build, test, and review the diff.

## Current Progress

The approved event path is implemented in 14 feature handlers. HTTP workflows no longer remove article caches independently. One concrete Web invalidator owns article/feed and sitemap versions, and the existing page filter uses sitemap-only invalidation. No production package or interface was added. Web integration tests reuse the SQLite test packages already used by Features tests.

Implementation, documentation, and verification are complete. The final Web build has zero warnings and errors. All 373 tests in the four affected test projects passed. No database migration or deployment configuration change is required.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-22 | Inspect production readers, feature writes, controllers, and existing tests | Complete | Publication must follow commit and precede logging/notifications. |
| 2026-10-22 | Web project build, no restore | Passed | No warnings or errors. |
| 2026-10-22 | Features test project, no restore | Passed | 96 tests. |
| 2026-10-22 | Web test project, no restore | Passed | 240 tests, including 34 new cache regression cases. |
| 2026-10-22 | BackgroundServices test project, no restore | Passed | 12 tests. Four existing CS8602 warnings in unchanged CannonServiceTests. |
| 2026-10-22 | Syndication test project, no restore | Passed | 25 tests. |
| 2026-10-22 | Final Web build and git diff --check | Passed | No build warnings/errors or whitespace errors. |

## Issues and Resolutions

- Tag deletion uses an explicit transaction; publish only after CommitAsync.
- Empty recycle bin uses ExecuteDeleteAsync; notify once only when rows were deleted.
- The recycle bin controller also handles pages; preserve page invalidation when removing its controller-wide filter.
- The installed cache library allows old in-flight factories to repopulate entries after Remove. Process-local versioned keys isolate these entries; sitemap gets a one-hour expiration to bound old-version entries left by delayed queries.
- Sitemap has an independent version so page filters keep working without clearing article/feed caches.

## Follow-ups

Process-local caches do not coordinate across replicas. Requests already in progress may still return their previously read data, but cannot populate the current cache version.
