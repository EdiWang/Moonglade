# UTC Time and Database Type Migration

## Original Goal

Analyze and correct Moonglade's time and time-zone handling, then migrate SQL Server temporal columns to `datetime2(7)` and PostgreSQL temporal columns to `timestamp with time zone` without changing published URL semantics.

The work must be delivered as independently executable, testable, and committable batches. Only the latest stable release, `v16.3.0`, is supported as the upgrade source, and the migration will ship with `v16.4.0` rather than a patch release.

## Background

Moonglade persists cross-boundary timestamps as UTC, but its current database schemas do not consistently preserve that contract:

- The SQL Server cumulative migration script creates temporal columns as `datetime`, while some EF Core defaults can create `datetime2`, resulting in schema drift.
- The PostgreSQL provider maps timestamps to `timestamp without time zone` and enables `Npgsql.EnableLegacyTimestampBehavior`.
- Values read from time-zone-less columns can have `DateTimeKind.Unspecified`. Some output paths call `ToUniversalTime()` or serialize the value without an explicit UTC designator, so results can depend on the application host time zone.
- Scheduled publishing accepts a browser local time and IANA time-zone identifier but does not explicitly reject daylight-saving invalid or ambiguous local times.
- The scheduled-publish transition needs to record the actual successful publication time, not the originally requested schedule time.

A read-only inventory of the database configured by `src/Moonglade.Web/appsettings.Development.json` confirmed 24 temporal columns: 22 active UTC timestamp columns, `PostViewDaily.ViewDateUtc`, and `LoginHistory.LoginTimeUtc`. The active database contained about 340 posts and 789 comments. `LoginHistory` was empty and is no longer represented by the application model. `PostViewDaily` contained 13 rows and all date keys were at midnight. The temporal data ranges were compatible with the target SQL Server types.

## Confirmed Decisions

- SQL Server UTC timestamps use `datetime2(7)`.
- PostgreSQL UTC timestamps use `timestamp with time zone` (`timestamptz`).
- `Npgsql.EnableLegacyTimestampBehavior` is removed.
- `PostViewDaily.ViewDateUtc` becomes SQL Server `date` / PostgreSQL `date` and should use `DateOnly` in the application model.
- The entire legacy `LoginHistory` table is dropped.
- Persisted and cross-boundary timestamps remain UTC.
- Published routes, archives, and date-based lookup semantics remain UTC.
- Daylight-saving invalid and ambiguous scheduled local times are rejected with a clear request to choose another time.
- A scheduled post's publication timestamp is the actual successful publication time.
- Existing handwritten, cumulative provider migration SQL remains the migration mechanism.
- The migration runs only when upgrading the latest stable release to the next major or minor release.
- A maintenance window of up to one hour is acceptable.
- Docker Desktop may be used for disposable SQL Server and PostgreSQL verification.
- Disposable integration tests use PostgreSQL 18 and SQL Server 2025 container images.

## Scope

- Add repeatable migration tests against real disposable SQL Server and PostgreSQL containers.
- Correct startup ordering so schema migration precedes writes that require the new timestamp contract.
- Define provider-neutral UTC materialization/persistence behavior and provider-specific database types.
- Make API, feed, structured-data, and browser boundaries explicit about UTC.
- Validate scheduled local times against daylight-saving gaps and overlaps.
- Extend both cumulative migration scripts to convert all active temporal columns, convert the daily-view key to a date, preserve dependent keys/indexes, and remove `LoginHistory`.
- Remove PostgreSQL legacy timestamp behavior.
- Add focused unit, integration, upgrade, and time-zone regression coverage.
- Update deployment and upgrade documentation when the implementation is complete.

## Out of Scope

- Supporting upgrades from releases older than the latest stable release.
- Changing published URL or archive date semantics from UTC to a blog-specific time zone.
- Changing the current handwritten cumulative migration mechanism to EF Core migrations.
- Running migration SQL against the configured development database during implementation or automated tests.
- General date/time refactoring unrelated to persisted or cross-boundary behavior.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add pinned SQL Server/PostgreSQL container fixtures, latest-stable schema fixtures, and tests for embedded script loading, transactional rollback, data preservation, and idempotent cumulative migration execution. | Confirmed migration decisions and Docker Desktop | `Moonglade.Setup.Tests` unit and container integration tests; Web build | Completed |
| 2 | Reorder startup initialization so database migration completes before configuration initialization or any other temporal writes. | Batch 1 | Startup/setup unit tests and both provider container smoke tests | Completed |
| 3 | Add a provider-neutral UTC timestamp contract and deterministic provider mappings; map daily views to `DateOnly`. | Batches 1-2 | Mapping/unit tests and both provider round-trip tests | Completed |
| 4 | Correct UTC output boundaries in feeds, JSON/JSON-LD, Razor models, and browser parsing/formatting without changing route semantics. | Batch 3 | Unit tests under multiple host time zones plus focused Web tests | Completed |
| 5 | Reject daylight-saving invalid and ambiguous schedule times with localized validation feedback. | Batches 3-4 | Scheduling unit tests for normal, gap, and overlap times; Web request tests | Completed |
| 6 | Implement the cumulative SQL Server and PostgreSQL schema cutover, remove `LoginHistory`, remove PostgreSQL legacy timestamp behavior, and record actual successful publish time. | Batches 1-5 | Upgrade from latest-stable fixtures twice; schema/data/index assertions; scheduled-publish integration tests | Completed |
| 7 | Run full regression and upgrade rehearsals, measure migration duration, and update README/AGENTS/upgrade documentation. | Batches 1-6 | Full solution tests/build, both provider rehearsals, documented rollback/backup checklist | Not started |

## Execution Order

Each batch is implemented, verified, reviewed, and committed independently. Later batches depend on the test infrastructure in batch 1. Database type changes are deliberately deferred until application reads, writes, validation, startup ordering, and output boundaries have deterministic UTC behavior. The final cutover is rehearsed from frozen latest-stable fixtures before it is considered ready for a release.

For the production upgrade, the intended order is: stop the application, take and verify a database backup, deploy the next major/minor release, run the cumulative migration before application initialization writes, execute smoke checks, and reopen traffic. If validation fails, stop the new application and restore the backup with the previous release; the handwritten migration is not treated as automatically reversible.

## Current Progress

Batches 1 through 3 are complete. Startup configuration initialization is split into a read-only load and a later missing-default write phase. Existing databases load `SystemManifestSettings`, perform the migration check and content-type backfill, and only then write missing configuration defaults. SQL Server and PostgreSQL container tests prove this order against the v16.3.0 fixture after applying the current cumulative script. New databases still seed the current schema before configuration initialization and do not run upgrade migration.

The application model now identifies all 22 active `*Utc` timestamp properties as one persistence contract. SQL Server maps them to `datetime2(7)`, PostgreSQL maps them to `timestamp with time zone`, materialization restores `DateTimeKind.Utc`, and tracked writes reject local or unspecified `DateTime` values. `PostViewDaily.ViewDateUtc` is a `DateOnly` mapped to provider `date`, including dashboard range queries.

The disposable database baseline now uses `postgres:18-alpine` and `mcr.microsoft.com/mssql/server:2025-latest`. The pulled images reported PostgreSQL 18.6 and SQL Server 17.0.4075.5 respectively.

Batches 1 through 5 are complete. Server-rendered timestamp attributes, post-management API strings, JSON-LD, and feeds now emit explicit UTC values. Legacy unspecified feed values preserve their stored UTC clock time instead of being converted through the host time zone. Browser code uses a shared UTC parser, converts local filter boundaries explicitly to UTC, performs one UTC-to-local conversion for scheduled-time inputs, and constructs published post routes from UTC calendar components.

Scheduled local wall-clock values are now checked against the browser's IANA time zone before conversion. Normal values convert to UTC; daylight-saving gaps and overlaps, missing times, and missing or invalid time-zone identifiers return a localized HTTP 400 validation response and do not execute the post save command or wake the scheduled publisher.

Batches 1 through 6 are complete. The cumulative scripts now convert the 22 active SQL Server timestamp columns to `datetime2(7)` and the PostgreSQL columns to `timestamp with time zone`, convert the daily-view key to `date`, rebuild dependent keys and indexes, and drop `LoginHistory`. PostgreSQL attaches UTC explicitly while converting legacy wall-clock values, so the result is independent of the database session time zone. The Npgsql legacy timestamp switch has been removed. Scheduled publication now uses a clock value captured after due posts are selected, and both providers verify that the persisted publication timestamp and route date use that actual processing time rather than the requested schedule. Batch 7 is the next executable unit.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-14 | Read-only inventory of configured SQL Server database | Passed | Confirmed row counts, 24 temporal columns, dependent indexes, empty `LoginHistory`, and compatible data ranges without recording secrets. |
| 2026-08-14 | `docker version` | Passed | Docker Desktop server 29.7.2 was available for disposable integration databases. |
| 2026-08-14 | `dotnet test src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | Passed | 20/20 tests passed. SQL Server 2022 and PostgreSQL 17.6 containers verified the v16.3.0 baseline, current cumulative migration twice, data/index preservation, and transactional rollback. |
| 2026-08-14 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | Build completed with 0 warnings and 0 errors. |
| 2026-08-14 | `dotnet list src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj package --vulnerable --include-transitive` | Passed | No vulnerable direct or transitive packages were reported by the configured sources. |
| 2026-08-14 | `dotnet test src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` after batch 2 | Passed | 22/22 tests passed. Both providers verified read-only configuration loading before migration and missing-default writes after migration. |
| 2026-08-14 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` after batch 2 | Passed | Build completed with 0 warnings and 0 errors. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore` after batch 3 | Passed | 95/95 tests passed, including `DateOnly` daily-view writes and dashboard aggregation. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` after batch 3 | Passed | 25/25 tests passed. Fresh SQL Server and PostgreSQL databases verified all 22 mappings, UTC-kind round trips, UTC write rejection, provider date storage, and translated `DateOnly` range queries. Existing v16.3.0 upgrade harness tests also remained green. |
| 2026-08-15 | `dotnet test src/Moonglade.slnx --no-restore` after batch 3 | Blocked by unrelated baseline issue | All executed projects passed, including Features, Setup, Configuration, Email, BackgroundServices, Auth, Syndication, Web, and other test suites. `Moonglade.Webmention.Tests` did not compile because unchanged test code references the inaccessible internal `WebmentionUrlSafetyValidator.IsPublicAddress` member. |
| 2026-08-15 | Upgrade disposable database images and rerun `Moonglade.Setup.Tests` | Passed | 25/25 tests passed with PostgreSQL 18.6 (`postgres:18-alpine`) and SQL Server 17.0.4075.5 / SQL Server 2025 (`mcr.microsoft.com/mssql/server:2025-latest`). |
| 2026-08-15 | Run JavaScript UTC boundary tests with `TZ=UTC` and `TZ=America/Los_Angeles` | Passed | 5/5 tests passed in each time zone. UTC parsing, route dates, scheduled-input conversion, and local filter boundaries remained deterministic. |
| 2026-08-15 | Run focused formatter and feed tests with `TZ=UTC` and `TZ=America/Los_Angeles` | Passed | The same explicit UTC output and legacy unspecified-value behavior passed in both host time zones. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Utils.Tests/Moonglade.Utils.Tests.csproj --no-restore` after batch 4 | Passed | 288/288 tests passed, including explicit ISO UTC formatting and rejection of local values at UTC output boundaries. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj --no-restore` after batch 4 | Passed | 23/23 tests passed. RSS and Atom preserve legacy unspecified UTC clock values without host-zone conversion. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` after batch 4 | Passed | 165/165 tests passed, including explicit `Z` designators for post-management API timestamps. |
| 2026-08-15 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` after batch 4 | Passed | Build completed with 0 warnings and 0 errors. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` after batch 5 | Passed | 173/173 tests passed. Normal, DST gap, DST overlap, missing time-zone, missing time, no-save/no-wake behavior, and localized ProblemDetails responses are covered. |
| 2026-08-15 | Run focused scheduling tests with `TZ=UTC` and `TZ=America/Los_Angeles` | Passed | 7/7 focused tests passed in each host time zone while resolving `America/New_York` normal, gap, and overlap wall-clock values. |
| 2026-08-15 | Validate scheduling resource keys in all non-English resource files | Passed | All four validation keys are present in `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`. |
| 2026-08-15 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` after batch 5 | Passed | Build completed with 0 warnings and 0 errors. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` after batch 6 | Passed | 25/25 tests passed with PostgreSQL 18.6 and SQL Server 2025 containers. Each v16.3.0 fixture was upgraded twice; all 22 target timestamp types, the `date` key, UTC values, dependent key/index definitions, `LoginHistory` removal, and transactional rollback were verified. PostgreSQL migration ran under an `America/Los_Angeles` session time zone. |
| 2026-08-15 | Real-provider scheduled publication verification after batch 6 | Passed | Fresh SQL Server and PostgreSQL databases persisted the injected successful processing time in UTC and generated the route from that date across a UTC day boundary. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore` after batch 6 | Passed | 95/95 tests passed, including deterministic due-selection and successful-publication timestamps. |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` after batch 6 | Passed | 173/173 tests passed. |
| 2026-08-15 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` after batch 6 | Passed | Build completed with 0 warnings and 0 errors. |

## Issues and Resolutions

- SQL Server and PostgreSQL currently materialize UTC data without a reliable UTC kind/offset contract. The migration therefore cannot be only a column-type alteration; batches 3-5 establish deterministic application behavior before batch 6 changes storage types.
- Current startup initialization can write configuration before migration. Batch 2 moves migration ahead of temporal writes so the PostgreSQL legacy-behavior removal and `timestamptz` cutover are safe.
- `EmailOutboxMessage` and `PostViewDaily` have indexes involving converted columns. Batch 6 must explicitly preserve or rebuild these indexes and validate their definitions.
- The first SQL Server fixture assertion included system objects because it queried `INFORMATION_SCHEMA` by schema alone. The assertion now joins `sys.tables` and excludes `is_ms_shipped` objects; both providers report the expected 22 baseline columns and 24 columns after the existing v16.4 schema addition.
- Testcontainers 4.13.0 initially resolved vulnerable `SSH.NET` 2025.1.0. The test project pins patched `SSH.NET` 2026.0.0, and NuGet's vulnerability audit is clean.
- Migration needs the persisted `SystemManifestSettings` version, so configuration initialization could not simply move wholesale after migration. It is now explicitly two-phase: read-only load before migration, missing-default writes after migration.
- `AutoDatabaseMigration=false` retains its existing manual-migration semantics. The migration check still occurs before configuration writes, but operators who disable automatic migration must apply the cumulative script manually before starting the new release.
- SQL Server does not preserve `DateTime.Kind` in a timestamp column. Batch 3 applies a model-wide materialization converter so all `*Utc` values return as `Utc`, while change-tracked saves reject local and unspecified values before either provider is called.
- Several provider configuration classes previously listed only some timestamp properties. Batch 3 retains their provider-specific mappings but also applies a final model-wide convention, preventing unmapped current or future `*Utc` properties from silently using provider defaults.
- The full solution test command currently has an unrelated compile-time baseline failure in `Moonglade.Webmention.Tests`: unchanged test code cannot access the internal `WebmentionUrlSafetyValidator.IsPublicAddress` member. This batch does not alter that module; all batch-specific and all other executed suites pass.
- Several Razor timestamp attributes used the sortable `u` format while browser code guessed whether a missing `T` or offset meant UTC. Batch 4 replaces those boundaries with invariant round-trip ISO strings carrying an explicit `Z` and centralizes browser parsing.
- The admin post list rebuilt published URLs with local `getFullYear`, `getMonth`, and `getDate` values. Posts near a UTC day boundary could therefore receive a different route date in non-UTC browsers. It now uses UTC calendar components, preserving the existing server route semantics.
- Scheduled-time display code previously adjusted the browser offset manually and then read local date components, applying the offset twice for explicit UTC inputs. Batch 4 performs one conversion. Daylight-saving gap and overlap rejection remains intentionally deferred to batch 5.
- `TimeZoneInfo.ConvertTimeToUtc` alone does not express the required product choice for repeated local times. Batch 5 checks `IsInvalidTime` and `IsAmbiguousTime` first and rejects both cases instead of selecting an offset or allowing a conversion exception to become a generic save failure.
- A browser `datetime-local` value represents wall-clock components and carries no offset. The resolver explicitly changes its `DateTimeKind` to `Unspecified` before validating it against the supplied IANA time zone, preventing the application host time zone from influencing the result.
- Schedule validation previously returned a plain-text conflict response, while the shared browser request code only extracts messages from ProblemDetails. Batch 5 returns an explicit HTTP 400 validation ProblemDetails response localized at the controller boundary, so the existing editor toast displays the reason and asks the author to choose again.
- PostgreSQL implicitly interprets a `timestamp without time zone` through the current database session time zone when converting it to `timestamptz`. Batch 6 uses `AT TIME ZONE 'UTC'` for every legacy UTC column and verifies the result under a non-UTC session, preserving the stored clock values as UTC instants.
- SQL Server cannot alter indexed temporal columns in place while the dependent index or key exists. Batch 6 drops and recreates the email dequeue index and the daily-view primary key/index within the migration transaction, then checks their column order after two executions.
- The scheduler previously sampled `DateTime.UtcNow` before querying due posts. Batch 6 uses `TimeProvider`, retaining a separate due cutoff while sampling the persisted publication time after selection. Provider integration tests cross a UTC date boundary to prove both `PubDateUtc` and `RouteLink` use the successful processing time.

## Follow-ups

- Capture representative production-like backup/restore timings during batch 7 and confirm they fit the one-hour maintenance window.
- Keep all unresolved business or compatibility choices in this record and ask the user before changing an already confirmed decision.

## Notes

- Actual connection strings and credentials from `appsettings.Development.json` must never be copied into this task record, source files, logs, or commits.
- Route links and date-based public behavior remain UTC even when local display formatting is corrected.
- Container fixtures represent the supported latest-stable upgrade boundary; they are not a promise to support arbitrary historical schemas.
