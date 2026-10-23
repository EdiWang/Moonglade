# Remove Thin Post Workflow Commands

## Original Goal

Remove the three thin outer workflow commands without changing functionality or security.

## Background

`RecyclePostCommand`, `PublishPostWorkflowCommand`, and `UnpublishPostWorkflowCommand` each have one controller caller and only forward a feature command before writing an activity log. Other controllers already dispatch feature commands and use `BlogControllerBase.LogActivityAsync`.

## Scope

Delete the three records and handlers. Dispatch their feature commands directly in `PostController`, retain activity log fields and ordering, and replace wrapper tests with controller behavior coverage.

## Out of Scope

Retain `SavePostCommand`, its scheduling and notification coordination, feature handlers, cache events, HTTP routes and responses, authorization, and antiforgery conventions. No new services or dependencies.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect wrappers, callers, logs, and tests | None | Confirm all references | Complete |
| 2 | Remove wrappers and move dispatch/logging to existing controller | 1 | Controller regression tests | Complete |
| 3 | Run checks and synchronize documentation | 2 | Web and Features tests, Web build | Complete |

## Execution Order

Inspect references, simplify production callers, migrate tests, then verify and update this record.

## Current Progress

Removed the three forwarding commands and their handlers. Controller actions reuse the inherited activity logger after successful feature dispatch. Added regression coverage for log metadata, request identity, command ordering, soft deletion, and failure propagation without a success log. Updated `AGENTS.md`. All planned checks passed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-23 | Inspect callers and repository state | Complete | Clean starting tree; only three production callers. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore --verbosity quiet` | Passed | 250 tests, including six controller cases replacing three wrapper cases and existing cache invalidation coverage. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore --verbosity quiet` | Passed | 109 tests. |
| 2026-10-23 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero warnings or errors. |
| 2026-10-23 | Search source for deleted commands and run `git diff --check` | Passed | No references remain in source; no whitespace errors. |

## Issues and Resolutions

None.

## Follow-ups

No remaining work. Browser and live SQL Server/PostgreSQL verification were not run; the change does not alter UI, queries, schema, or provider behavior. No README or deployment updates are needed for this internal simplification.
