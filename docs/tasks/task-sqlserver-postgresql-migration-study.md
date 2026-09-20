# SQL Server to PostgreSQL Migration Study

## Original Goal

Assess whether the supplied September 2026 production SQL Server BACPAC can be migrated to PostgreSQL 18, using only local resources and without changing production.

## Background

Moonglade uses provider-specific EF Core contexts for SQL Server and PostgreSQL. A new PostgreSQL database is created from the current EF Core model; the embedded PostgreSQL SQL script upgrades an existing Moonglade database and does not transfer SQL Server data. The supplied BACPAC has a `16.4.0` manifest.

## Scope

Restore the BACPAC to an isolated local SQL Server container, create an isolated PostgreSQL 18 database, copy and validate the data, and run representative application checks. Record compatibility gaps and cutover prerequisites.

## Out of Scope

No production connections or changes. No production cutover, permanent migration utility, application fix, or decision to discard historical records.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect repository provider mappings and backup | None | Schema and manifest inspection | Complete |
| 2 | Restore locally and create PostgreSQL schema | 1 | BACPAC import and EF `EnsureCreated` | Complete |
| 3 | Transfer and compare data | 2 | Row counts, foreign keys, all-column row hashes | Complete with five replies quarantined |
| 4 | Exercise the application and provider behavior | 3 | HTTP smoke checks, search comparison, rollback-only write check | Complete |
| 5 | Document decision and clean local resources | 4 | This record and cleanup | Complete; temporary-file deletion blocked |

## Execution Order

The source database was restored first. The target schema was created using `PostgreSqlBlogDbContext.Database.EnsureCreatedAsync()`. Data was copied by matching column names, treating SQL Server `*Utc` timestamps as UTC and converting SQL Server `date` to PostgreSQL `date`. Tables were copied in foreign-key order in one transaction, followed by identity-sequence resets. Five invalid reply rows were copied to a separate local quarantine table before commit.

## Current Progress

**Decision: technically feasible, but do not switch production to the current application and data as-is.** The main data set transfers to PostgreSQL 18. Three blocking or user-visible issues need resolution: five orphan replies, a date-filter exception, and changed case-sensitive search behavior. This study does not establish full production readiness for every admin and background workflow.

Follow-up on 2026-09-20: the public search date exception and case-sensitive keyword behavior were fixed and verified on both providers. The orphan replies and broader workflow checks remain open.

The local SQL Server container used SQL Server 2025 CU9. The PostgreSQL container used 18.6. Both were bound to loopback only. The application ran with an isolated environment, update checks and email delivery disabled, and local image-storage directories. No production system was contacted.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-09-20 | `sqlpackage /Action:Import` into local SQL Server | Passed | 19 tables, 7,909 rows, manifest `16.4.0`. |
| 2026-09-20 | EF Core `EnsureCreatedAsync()` on PostgreSQL 18.6 | Passed | All 19 application table and column names matched. |
| 2026-09-20 | Transactional transfer | Passed after quarantine | 7,904 rows in application tables; five orphan `CommentReply` rows in a separate local table. |
| 2026-09-20 | All-column row comparison | Passed | Per-row SHA-256 hashes matched across all 19 tables when timestamps were normalized to PostgreSQL microsecond precision; the five quarantined replies were included. |
| 2026-09-20 | Timestamp precision audit | Observed | 1,661 source timestamp values had a nonzero seventh fractional-second digit. PostgreSQL retains microseconds, so these lose less than one microsecond. |
| 2026-09-20 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | Zero warnings and errors. |
| 2026-09-20 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | Passed | 19 tests; these do not replace a full migrated-data workflow test. |
| 2026-09-20 | Local HTTP smoke | Passed | `/health`, `/health/ready`, `/`, `/rss`, `/atom`, `/sitemap.xml`, `/archive`, and one article returned 200. |
| 2026-09-20 | Local PostgreSQL write check | Passed | Created a tag and updated a post inside a transaction, then rolled back. Identity sequence and UTC write worked. |
| 2026-09-20 | Search without date filters | Diverged | `Windows`/`windows` returned 125/125 results on SQL Server and 124/91 on PostgreSQL. |
| 2026-09-20 | `/search?term=Windows&startDate=2020-01-01` and `endDate=2026-01-01` | Failed on PostgreSQL | Both returned 500 because Npgsql rejects `DateTimeKind.Unspecified` for `timestamp with time zone`. |
| 2026-09-20 | Public search fix: Features and Web test projects | Passed | 95 and 194 tests respectively; Web build had zero warnings and errors. |
| 2026-09-20 | LocalDB and PostgreSQL 18 search query on the same copied posts and facets | Passed | `Windows`, `windows`, and `wInDoWs` each returned the same 125 post slugs on both providers; with the 2020-01-01 to 2026-01-01 date range, each returned the same 14 slugs. |
| 2026-09-20 | Local HTTP `/search` on both providers | Passed | No-date, start-only, end-only, and combined-date searches returned 200 on both providers. |

## Issues and Resolutions

### Orphan replies

The SQL Server backup contains five `CommentReply` rows whose non-null `CommentId` has no matching `Comment`. SQL Server has no `CommentReply` foreign key; the current PostgreSQL EF model creates one. All other tested foreign-key relationships had zero orphans. The trial preserved these five records in `MigrationOrphanCommentReply` outside the application table. A production migration must explicitly decide whether to repair, archive, or otherwise resolve them; silently dropping them or disabling PostgreSQL constraints is not recommended.

### Search behavior

The source database uses `Chinese_PRC_CI_AS` collation. The local PostgreSQL database uses `en_US.utf8`, where the current `Contains` queries are case-sensitive. `SearchPostQuery` produces different results on the same copied records. The admin post and comment filters also use string `Contains` and warrant the same review. Choose the intended case-insensitive behavior and test it on both providers before cutover.

Follow-up fix: public search now lowercases both the keyword and searched text in the provider-neutral EF query. The copied-data comparison above found identical result sets across case variants and providers. Admin filters were not part of this fix.

### Date filters

The public search page passes browser-bound `DateTime` values directly to UTC query fields. A date-only URL produces `DateTimeKind.Unspecified`; Npgsql rejects it against `timestamp with time zone`. Normalize at the HTTP boundary while preserving the search date semantics, then check the activity-log, mention, and comment admin date filters for the same issue.

Follow-up fix: the search page marks date-only UTC calendar boundaries as `DateTimeKind.Utc` before dispatching the query. Start-only, end-only, and combined filters now return 200 on both providers. The admin date filters remain to be reviewed separately.

### Schema differences

The target has eight column-nullability differences and two string-length differences from the restored SQL Server schema. Existing data passed target constraints. PostgreSQL also adds indexes for its EF-generated foreign keys. Review these differences when designing the permanent transfer, but they did not prevent the trial import except for orphan replies.

## Follow-ups

1. Review the admin activity-log, mention, and comment date filters and the admin post/comment keyword filters for the same provider differences.
2. Decide how to preserve or reconcile the five historical orphan replies.
3. Build a repeatable transfer from a fresh cutover BACPAC with row-level comparison, identity-sequence reset, foreign-key validation, and a failure rollback path. Keep the SQL Server backup unchanged.
4. Run authenticated admin, comment, scheduled-publish, email-outbox, image, and performance checks against migrated PostgreSQL data before production cutover. Preserve the existing filesystem image mounts; image files are not in the BACPAC.

## Notes

Only aggregate counts and technical findings are recorded here. The backup, credentials, post content, and other production data remain outside the repository. Both temporary database containers were stopped and automatically removed. Automatic command review blocked `Remove-Item` for temporary files; the local SQL password and web log files were cleared to zero bytes. The temporary C# study project remains under the user temp directory, and the temporary image directories are empty.

The follow-up used a separate LocalDB database named `moonglade_pg_search_study`, leaving the existing `moonglade` database untouched. The follow-up LocalDB database was dropped after verification, the PostgreSQL study container was removed, and the follow-up web logs were cleared.
