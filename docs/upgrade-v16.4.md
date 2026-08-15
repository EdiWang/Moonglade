# Upgrade to Moonglade v16.4

## Supported Upgrade Path

This database upgrade supports Moonglade v16.3.0 as its only source version. Upgrade older installations to v16.3.0 first and verify them before scheduling this maintenance window.

The v16.4 migration changes the persisted UTC contract:

- SQL Server UTC timestamp columns become `datetime2(7)`.
- PostgreSQL UTC timestamp columns become `timestamp with time zone` (`timestamptz`).
- `PostViewDaily.ViewDateUtc` becomes `date` on both providers.
- The obsolete `LoginHistory` table is removed.
- PostgreSQL no longer enables `Npgsql.EnableLegacyTimestampBehavior`.
- Stored timestamp values continue to represent UTC. Published routes and archive dates retain their UTC calendar semantics.

The migration is transactional, but it is not treated as automatically reversible. A verified database backup is the rollback mechanism.

## Maintenance Prerequisites

Before the window:

1. Confirm that the running application is exactly v16.3.0 and identify whether `ConnectionStrings:DatabaseProvider` is `SqlServer` or `PostgreSql`.
2. Confirm that the v16.4 application package has the correct connection string and provider setting without copying credentials into logs or deployment records.
3. Test the provider's backup and restore procedure in a non-production environment.
4. Confirm sufficient database storage for both the backup and migration transaction.
5. Reserve a maintenance window of up to one hour and ensure every application instance and background worker can be stopped together.
6. Record the v16.3.0 deployment artifact and configuration needed for rollback.

## Backup and Verification

Use the database platform's normal managed-backup mechanism when one is available. The following commands are examples; replace names and paths with operator-controlled values and supply credentials through a secure mechanism.

SQL Server:

```sql
BACKUP DATABASE [Moonglade]
TO DISK = N'<backup-file-path>'
WITH COPY_ONLY, INIT, CHECKSUM;

RESTORE VERIFYONLY
FROM DISK = N'<backup-file-path>'
WITH CHECKSUM;
```

PostgreSQL:

```bash
pg_dump --format=custom --file=moonglade-before-v16.4.dump <database-name>
pg_restore --list moonglade-before-v16.4.dump
```

A backup is not considered verified merely because its command succeeded. Restore it to a separate database and check representative row counts before starting production maintenance.

## Upgrade Procedure

1. Put the site into maintenance mode and stop every v16.3.0 application instance. Do not leave a scheduler or email worker connected to the database.
2. Take and verify the final pre-upgrade backup.
3. Deploy the v16.4 application package while keeping traffic closed.
4. Apply the database migration using one of these modes:
   - With `AutoDatabaseMigration=true`, start one v16.4 instance. Startup reads the installed manifest, executes the provider script before configuration initialization writes, and then completes normal initialization.
   - With `AutoDatabaseMigration=false`, keep the application stopped and apply the matching cumulative script manually before startup. The scripts are `src/Moonglade.Setup/MigrationScripts/SqlServer/migration.sql` and `src/Moonglade.Setup/MigrationScripts/PostgreSql/migration.sql`. Use tooling that stops on errors; the SQL Server script requires a client that understands `GO` batch separators.
5. Review startup logs. Do not open traffic if migration or initialization reports a failure.
6. Run the database and application checks below.
7. Reopen traffic only after every check passes.

Do not start multiple v16.4 instances concurrently for the first automatic migration attempt.

Manual execution must preserve the same single-transaction boundary used by application startup. For PostgreSQL, use `psql --set ON_ERROR_STOP=1 --single-transaction --file=<migration-script> <database-name>`. For SQL Server, run `sqlcmd -b` or SQL Server Management Studio with an operator-owned wrapper on one connection:

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO
:r <absolute-path-to-migration.sql>
GO
COMMIT TRANSACTION;
GO
```

If any batch fails, stop and confirm that the transaction rolled back before investigating. Do not continue with later batches manually.

## Database Validation

SQL Server should report 22 `datetime2(7)` UTC columns, one `date` daily-view key, and no `LoginHistory` table:

```sql
SELECT COUNT(*) AS UtcTimestampColumns
FROM sys.columns AS c
INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id
INNER JOIN sys.tables AS t ON c.object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
WHERE s.name = N'dbo'
  AND t.is_ms_shipped = 0
  AND c.name LIKE N'%Utc'
  AND ty.name = N'datetime2'
  AND c.scale = 7;

SELECT ty.name AS ViewDateType
FROM sys.columns AS c
INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[PostViewDaily]')
  AND c.name = N'ViewDateUtc';

SELECT COUNT(*) AS LoginHistoryTableCount
FROM sys.tables
WHERE object_id = OBJECT_ID(N'[dbo].[LoginHistory]');
```

PostgreSQL should report 22 `timestamp with time zone` UTC columns, one `date` daily-view key, and no `LoginHistory` table:

```sql
SELECT COUNT(*) AS "UtcTimestampColumns"
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name LIKE '%Utc'
  AND data_type = 'timestamp with time zone';

SELECT data_type AS "ViewDateType"
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'PostViewDaily'
  AND column_name = 'ViewDateUtc';

SELECT COUNT(*) AS "LoginHistoryTableCount"
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name = 'LoginHistory';
```

For both providers, also confirm that `PK_PostViewDaily`, `IX_PostViewDaily_ViewDateUtc`, `IX_EmailOutboxMessage_Dequeue`, and `IX_SiteVerificationFile_NormalizedFileName` exist.

## Application Smoke Checks

Run these checks while traffic remains closed:

1. Confirm `/health` and `/health/ready` return healthy results.
2. Sign in to `/admin` and load the dashboard, post list, comments, and settings.
3. Open a published post and confirm its existing route still resolves.
4. Check `/rss`, `/atom`, `/sitemap.xml`, and an archive page for plausible UTC dates.
5. Create and cancel a future scheduled post. If testing publication, use disposable content and verify that the successful processing time becomes its publication time.
6. Confirm recent comments, daily view totals, and queued email rows are still present.
7. Review logs for timestamp-kind, Npgsql, conversion, or database initialization errors.

Daylight-saving gaps and overlaps are intentionally rejected when an author schedules a local time; the author must select a different time.

## Rollback

If migration, initialization, or smoke validation fails:

1. Keep traffic closed and stop every v16.4 instance.
2. Preserve the failed-upgrade logs for diagnosis without recording database credentials.
3. Restore the verified pre-upgrade backup to a clean database or replace the failed database according to the platform's restore procedure.
4. Restore the v16.3.0 application artifact and its configuration.
5. Start one v16.3.0 instance and validate database connectivity, admin sign-in, published routes, feeds, comments, and scheduled posts.
6. Reopen traffic only after the restored deployment passes validation.

Do not attempt to reverse the temporal column changes with ad hoc SQL.

## Rehearsal Results

The repository integration harness uses SQL Server 2025 and PostgreSQL 18. A representative fixture contains 340 posts and 789 comments, matching the observed development database scale. On the local Docker Desktop rehearsal performed on 2026-08-15:

| Operation | SQL Server 2025 | PostgreSQL 18 |
| --- | ---: | ---: |
| Transactional cumulative migration | 309 ms | 83 ms |
| Backup plus verification | 320 ms | 265 ms |
| Restore and row-count validation | 557 ms | 226 ms |

Both restored databases retained 340 posts and 789 comments. These measurements demonstrate ample margin within the one-hour maintenance target for this data scale, but they are reference values rather than a production duration guarantee. Networked storage, managed-service backup behavior, database load, and deployment orchestration can dominate the actual window.
