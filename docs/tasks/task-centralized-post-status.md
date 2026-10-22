# Centralize Post Status Assignments

## Original Goal

Centralize article status field assignments without changing publication URLs, time semantics, validation, or side effects. Reuse existing production code rather than introducing abstractions for unit testing.

## Background

Create, edit, publish, unpublish, cancel schedule, and scheduled publication repeat assignments for status, publication time, scheduled time, and route. Editing a scheduled post into the published state currently leaves the scheduled time behind.

## Scope

Reuse `PostEntityHelper` for the four related fields across the six write handlers. Extend command behavior tests and document the shared rule.

## Out of Scope

No state machine, service interface, dependency, schema migration, public API change, or publication-date policy change. Postponement only changes a scheduled time and remains separate. Soft deletion and restoration retain their existing behavior.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect state writers and add regression coverage | None | Reproduce stale scheduled time | Complete |
| 2 | Consolidate assignments and retain caller-specific time policies | 1 | Features tests | Complete |
| 3 | Check integration and update documentation | 2 | Web build and affected tests | Complete |

## Execution Order

Complete steps 1 through 3 in order. No external services or production data are involved.

## Current Progress

All six state writers use `PostEntityHelper.SetStatus`. The helper assigns status, publication time, scheduled time, and route together and rejects query-only or unknown status values. Callers retain publication and last-modified timestamp policies. Creation resolves slug conflicts before setting the route; edits use the normalized new slug. Existing persistence, event publication, cache invalidation, and scheduling eligibility checks remain in their commands. Updated `AGENTS.md`; no deployment or README changes are required.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-23 | Inspect state writers and callers | Complete | Editing retains published dates; manual publish resets them; scheduled publish retains last-modified time. |
| 2026-10-23 | Features tests before production changes | Expected failure | Scheduled-to-published edit retained its scheduled time; 99 passed, 1 failed. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore --verbosity quiet` | Passed | 109 tests, including transitions, publication dates, slug conflicts, and invalid write statuses. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore --verbosity quiet` | Passed | 247 tests, including save workflows and cache invalidation. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.BackgroundServices.Tests/Moonglade.BackgroundServices.Tests.csproj --no-restore --verbosity quiet` | Passed | 12 tests; four existing CS8602 warnings in unchanged `CannonServiceTests.cs`. |
| 2026-10-23 | `dotnet test --project src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj --no-restore --verbosity quiet` | Passed | 25 tests. |
| 2026-10-23 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero warnings or errors. |
| 2026-10-23 | `git diff --check` | Passed | No whitespace errors. |

## Issues and Resolutions

Scheduled-to-published edits omitted clearing `ScheduledPublishTimeUtc`; the regression reproduced the issue before the refactor and now passes. Published edits also clear any previously retained scheduled time without resetting the publication date.

## Follow-ups

No remaining implementation work. Browser and live SQL Server/PostgreSQL checks were not run: no UI, schema, or provider-specific query changes were made. Existing stale scheduled values are normalized on subsequent state writes; no bulk production data cleanup is performed.
