# Unit Test Cleanup

## Original Goal

Remove low-value unit tests and organize the remaining tests around the features they protect.

## Background

The solution contains 15 xUnit v3 test projects using Microsoft.Testing.Platform. Most Web, Features, and Email tests are in flat project directories. Theme and utility tests include repeated happy paths, weak format-only checks, and tests that pass an uncancelled token without testing cancellation. The working tree was clean before this task.

## Scope

- Review test intent against the nearby implementation before removing or merging cases.
- Retain business rules, validation, security, error handling, cancellation, and data consistency coverage.
- Group the larger test suites by their production feature ownership, including their test helpers and namespaces.
- Document the layout and selection criteria in `src/Tests/README.md`.

## Out of Scope

Production behavior, dependency versions, test frameworks, external services, and database migrations.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inventory tests and establish a baseline | None | Run all solution tests | Complete |
| 2 | Consolidate redundant tests and strengthen retained assertions | 1 | Run affected test projects | Complete |
| 3 | Organize larger suites and their helpers by feature | 2 | Build and run all solution tests | Complete |
| 4 | Document criteria, inspect final diff, and record results | 3 | Review Git status and diff | Complete |

## Execution Order

Establish the baseline, remove or merge only cases with a documented reason, organize files and namespaces, then verify discovery and execution across all 15 test projects.

## Current Progress

The initial cleanup completed without changing production code, packages, project references, or the solution. Reorganized 59 existing files in Web, Features, and Email with matching namespaces. Split content processing and tag utilities by behavior, separated color calculation from the theme catalog, moved the outbox options validator out of the processor suite, and merged the comment rate-limit attribute test into the controller suite.

The final suite has 1,130 cases versus 1,167 at baseline, a net reduction of 37 cases. Test C# is approximately 1,200 lines shorter. Existing validation, security, cancellation, persistence failure, UTC, caching, and protocol tests remain. The README links to `src/Tests/README.md` for the new layout and maintenance guidance.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-25 | `git status --short` | Clean | Before edits |
| 2026-10-25 | `dotnet --version` | 10.0.402 | .NET 10 SDK available |
| 2026-10-25 | Baseline: `dotnet test --solution src/Moonglade.slnx --no-progress --output Normal` | 1,165 passed, 2 failed | All 15 projects discovered; both failures are migration script manifest version checks |
| 2026-10-25 | Focused Theme, Utils, and Configuration project runs | Passed | Verified the initial consolidation before directory moves |
| 2026-10-25 | Final: `dotnet test --solution src/Moonglade.slnx --no-progress --output Normal` | 1,128 passed, 2 failed, 0 skipped | All 15 projects built and executed; only the same baseline failures remain |
| 2026-10-25 | Compare moved files against HEAD | Passed | File moves preserve bodies except the explicit controller-test merge and validator-test extraction |
| 2026-10-25 | Final whitespace, namespace, and Git status checks | Passed | UTF-8/CRLF; no production changes |

## Issues and Resolutions

- Baseline failures: `MigrationManagerTests.TryMigrationAsync_MigrationScriptManifest_DoesNotRequireMigration` fails for both PostgreSQL and SQL Server. Both embedded scripts record `16.8.1`, while `Directory.Build.props` declares `16.9.0-preview.1`; the migration manager rejects automatic migration on a prerelease build. Both tests and the migration behavior are unchanged. This is outside test organization scope.
- While moving constructor assertions into generated feed metadata checks, the first run exposed an incorrect new expectation for RSS language casing. The generated value is the normalized `en-US`; the assertion was corrected to that observable result and the final full run passes Syndication tests.
- The baseline build reported four nullable warnings in Cannon logging assertions. Removing the startup-log-only test removes one of them; the other three are unchanged. No new compiler warnings remain.

## Consolidation Decisions

| Area | Removed or merged | Retained protection |
| --- | --- | --- |
| Configuration | Separate key/value and timestamp insertion tests | Existing creation test checks both persisted values and the full timestamp range; replacement and error propagation remain |
| Theme lists | Repeated counts, property checks, repeat-call equality, list identity, and uncancelled-token tests | Empty listing plus a mixed listing check system IDs/order and both custom records with all properties |
| Theme commands | Duplicate serialization/deletion and uncancelled-token happy paths | Creation checks arbitrary CSS properties; deletion includes additional properties; real cancellation coverage remains |
| Theme factory | Repeated format checks and self-referential lightening calculation | Parsed catalog JSON and fixed expected primary/secondary colors for all ten themes; exact color calculation cases and invalid inputs remain |
| Theme CSS | Duplicate successful rendering cases and repeated invalid-input setup | All system IDs, database-independent selection, exact custom CSS/fallback, blank/empty/invalid rules, and script sanitization remain |
| Tags | Dictionary getter/identity tests and repeated broad examples | Theories check exact ordinary, .NET, Unicode, and blank slugs; all active name-validation data and null input checks remain |
| Content | Repeated keyword setup and a commented-out test | All active keyword inputs remain in theories; rendering and comment-link security move to dedicated classes |
| Syndication | Constructor assignment checks and separate document-metadata checks | Generated RSS/Atom documents assert metadata; OPML checks metadata and all outlines together, with a separate empty-category case |
| Background queue | Startup-log-only test | Work execution, order, scope, queue limits, errors, and shutdown tests remain |
| Image names | Identity getter-only test | Exact generated filenames already include the configured identity |
| Web and Email | Scattered single-method tests | Rate-limit attribute coverage is beside controller behavior; outbox options validation has its own suite |

## Follow-ups

- Resolve the existing migration script/current-version mismatch in a separately scoped migration/release change; do not suppress the failing checks.
- No coverage report or real PostgreSQL/SQL Server migration run was performed. This task changes tests and documentation only.
- Saved fully qualified test filters must follow the new namespaces; assembly names and project-level test commands are unchanged.

## Follow-up: Obsolete Migration Test Assets

### User Clarification

The user confirmed that migration steps before v16.7 no longer need support, and asked whether the two remaining failures were caused by deleting those steps.

### Findings and Scope

The failures are unrelated to the removed migration steps. Commit `0bb051cdb` bumped the application to `16.9.0-preview.1`, while both script completion markers still identify `16.8.1`. Commit `56d331c89` subsequently removed only pre-v16.7 SQL steps and left those markers unchanged. The failing theory reads the markers from the current embedded scripts; it does not reference the old upgrade fixtures.

Both unused `Fixtures/*/latest-stable.sql` resources still described a v16.3.0 upgrade baseline. Repository-wide searches found no consumers. The remaining Setup tests use EF Core InMemory and do not reference SSH, containers, or the database-provider projects.

### Changes

- Removed the two unused v16.3.0 database fixtures and their embedded-resource entries.
- Removed unused `SSH.NET`, `Testcontainers.MsSql`, and `Testcontainers.PostgreSql` package references from Setup tests.
- Removed unused PostgreSQL and SQL Server project references from Setup tests.
- Preserved all current tests, including both current-script manifest checks. Manifest values used as configuration-preservation test data are not database upgrade fixtures and remain unchanged.
- Recorded the intentional removal of pre-v16.7 migration steps in the engineering reference and updated the test layout guide.
- Production code, scripts, and version numbers remain unchanged.

### Verification

| Date | Command or check | Result |
| --- | --- | --- |
| 2026-10-25 | Repository-wide fixture, SSH, container, and provider usage searches | No remaining consumers of removed resources/dependencies |
| 2026-10-25 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-progress --output Normal` | Restored and built successfully; 31 tests discovered, 29 passed, the same 2 manifest checks failed |
| 2026-10-25 | `git diff --check` and final status review | Passed; previous cleanup edits preserved |

### Remaining Limits

No real database migration was executed. The current script/application version mismatch remains a separate release/migration concern; removing support for older SQL steps does not resolve it.
