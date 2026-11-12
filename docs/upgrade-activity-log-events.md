# Upgrade Activity History to Stable Event Keys

The release introducing stable activity event keys replaces legacy numeric events
and duplicate persisted wording with a canonical activity record. This is a
manual schema prerequisite for existing installations, not an automatic startup
migration. Do not run the new application against the legacy schema or the old
application against the new schema.

**The release starts with an empty activity history.** The legacy schema upgrade
permanently deletes all existing activity rows. An installation that already ran
a preview with the new schema needs the separate one-time reset below. Preserve
posts, categories, settings, comments, and all other application data.

## Prerequisites and Order

1. Stop every application instance, background worker, and other writer using the
   database. Keep them stopped through verification and until the matching new
   application is ready.
2. Back up the complete database and verify restoration. Rehearse all required
   upgrades on a representative restored copy, not a configured development or
   production database.
3. Complete the [category integer ID upgrade](upgrade-category-int-identity.md)
   if category IDs are still GUIDs. Its PostgreSQL script requires the original
   PascalCase names.
4. On PostgreSQL, complete the [snake_case naming upgrade](upgrade-postgresql-snake-case.md)
   if needed, after the category upgrade and before this activity upgrade.
   SQL Server keeps PascalCase names and needs no naming conversion.
5. Run the complete script for your provider as a standalone maintenance command,
   outside another transaction. Do not extract or run its `DELETE` separately.

| Provider | Script | Required table |
| --- | --- | --- |
| PostgreSQL | [`database/PostgreSql/3.activity-log-events.sql`](../database/PostgreSql/3.activity-log-events.sql) | `public.activity_log` after the naming upgrade |
| SQL Server | [`database/SqlServer/3.activity-log-events.sql`](../database/SqlServer/3.activity-log-events.sql) | `dbo.ActivityLog` |

Use a PostgreSQL table-owner maintenance connection (or a superuser), with
visibility into the table and its owned sequence. SQL Server maintenance requires
`SELECT`, `DELETE`, and `ALTER` on the table and complete metadata visibility
(`VIEW DEFINITION` on the database); use a suitably authorized DBA connection.
Do not broaden the application's normal runtime grants for this operation.

## Execution Examples

These commands are suggestions for an administrator **after backup, restoration
rehearsal, and authorization**. They are not executed by the application. From the
repository root, supply connection settings for the selected maintenance database
through your normal secure client configuration. Do not put passwords or real
connection strings in scripts, documentation, shell history, or test evidence.

PostgreSQL uses the libpq `PGHOST`, `PGPORT`, `PGDATABASE`, and `PGUSER` settings
and a secure password file or the client's password prompt:

```powershell
psql --set=ON_ERROR_STOP=1 --file=database\PostgreSql\3.activity-log-events.sql
```

Do not add `--single-transaction`; the script owns its transaction. On an error,
`ON_ERROR_STOP` stops processing and the standalone connection rolls back.

SQL Server with an administrator-approved Windows integrated-authentication
connection:

```powershell
sqlcmd -S "<maintenance-server>" -d "<maintenance-database>" -E -b -i database\SqlServer\3.activity-log-events.sql
```

Keep encrypted connections and certificate validation enabled according to your
client configuration; do not disable validation to work around a connection
failure. For another authentication method, use the client's supported secure
credential mechanism rather than adding a password to the command.

Both scripts acquire exclusive table locks within their transaction and use a
10-second lock timeout. PostgreSQL also uses a five-minute statement timeout.
SQL Server's delete may need time and transaction-log space proportional to the
legacy history size. An error, lock timeout, or unsuccessful DDL rolls back the
transaction. Keep the host stopped, inspect the cause, and retry the complete
script only after resolving it. Do not remove guards or switch to `TRUNCATE` or
`CASCADE` to force an upgrade through.

## Reset an Already-Upgraded Preview

If this installation already ran a pre-release build with the canonical activity
schema, complete the same stop, backup, and verification steps above. The guarded
schema script leaves an already-upgraded table untouched. After confirming that
the script succeeded and that you are connected to the intended database, clear
that installation's pre-release history once, before starting the release.

On PostgreSQL, run `DELETE FROM public.activity_log;`. On SQL Server, run
`DELETE FROM dbo.ActivityLog;`. Verify the activity table is empty. This reset is
only for the release cutover of an already-upgraded preview. On subsequent
schema-script reruns, preserve the history written by the release.

## Exact Supported Shapes

The scripts accept only all nine legacy columns or all ten final columns, with
the exact names, built-in types, lengths, and nullability below. Additional
columns, missing columns, partial upgrades, wrong lengths, wrong timestamp
types/precision, or incorrect nullability fail **before any deletion**.
The presence of `EventKey` alone is not an upgraded-schema check.

The retained `Id` must be a non-null `bigint` with its existing database identity
generator and single-column primary key. PostgreSQL accepts `GENERATED ALWAYS`
or `GENERATED BY DEFAULT AS IDENTITY` backed by a `bigint` sequence; legacy
`bigserial`/ordinary defaults are not this supported identity shape. SQL Server
accepts the existing `bigint IDENTITY` seed and increment. Neither script
recreates the table, primary key, or generator, nor resets or consumes identity
values.

### Legacy Source: Nine Columns

| Property / SQL Server name | PostgreSQL name | PostgreSQL type | SQL Server type | Nullable |
| --- | --- | --- | --- | --- |
| `Id` | `id` | `bigint` identity | `bigint IDENTITY` | No |
| `EventId` | `event_id` | `integer` | `int` | No |
| `EventTimeUtc` | `event_time_utc` | `timestamp with time zone` | `datetime2(7)` | Yes |
| `ActorId` | `actor_id` | `character varying(100)` | `nvarchar(100)` | Yes |
| `Operation` | `operation` | `character varying(100)` | `nvarchar(100)` | Yes |
| `TargetName` | `target_name` | `character varying(200)` | `nvarchar(200)` | Yes |
| `MetaData` | `meta_data` | `text` | `nvarchar(max)` | Yes |
| `IpAddress` | `ip_address` | `character varying(50)` | `nvarchar(50)` | Yes |
| `UserAgent` | `user_agent` | `character varying(512)` | `nvarchar(512)` | Yes |

### Final Target: Ten Columns

| Property / SQL Server name | PostgreSQL name | PostgreSQL type | SQL Server type | Nullable |
| --- | --- | --- | --- | --- |
| `Id` | `id` | `bigint` identity | `bigint IDENTITY` | No |
| `EventKey` | `event_key` | `character varying(100)` | `nvarchar(100)` | No |
| `EventTimeUtc` | `event_time_utc` | `timestamp with time zone` | `datetime2(7)` | No |
| `ActorKind` | `actor_kind` | `integer` | `int` | No |
| `ActorDisplayName` | `actor_display_name` | `character varying(100)` | `nvarchar(100)` | Yes |
| `TargetId` | `target_id` | `character varying(100)` | `nvarchar(100)` | Yes |
| `TargetDisplayName` | `target_display_name` | `character varying(200)` | `nvarchar(200)` | Yes |
| `IpAddress` | `ip_address` | `character varying(50)` | `nvarchar(50)` | Yes |
| `UserAgent` | `user_agent` | `character varying(512)` | `nvarchar(512)` | Yes |
| `Details` | `details` | `text` | `nvarchar(max)` | Yes |

`ActorKind` values are Administrator = `1` and System = `2`; the application owns
this enum contract. `Details` is optional JSON serialized into text, not a
provider-specific JSON column. No enum or JSON check constraints are added.
PostgreSQL timestamps use the standard unqualified precision. Stored and
cross-boundary times remain UTC; SQL Server `datetime2(7)` does not itself encode
a time zone, so UTC writes remain an application obligation. PostgreSQL session
display time zones do not change the stored instant. The cutover performs no
timestamp conversion or backfill because legacy rows are removed.

After the complete legacy shape and dependency guards pass, the script deletes
activity rows, drops only `EventId`, `ActorId`, `Operation`, `TargetName`, and
`MetaData`, makes `EventTimeUtc` required, and adds the new fields. Existing
`IpAddress` and `UserAgent` definitions are unchanged. Physical column order is
not part of the contract.

### Dependency Guards

Both providers reject foreign keys pointing to or from the activity table,
additional indexes, check/default constraints, nonstandard generated columns,
and triggers. The sole approved index is the existing primary key on `Id`.
PostgreSQL permits the ordinary identity, composite row type, TOAST storage, and
PostgreSQL 18 NOT NULL catalog dependencies, but rejects other recorded
dependencies, rules, row security, inheritance, and partitioning. SQL Server
rejects catalog-visible expression dependencies, local synonyms, row-security
predicates, change tracking, CDC, replication, temporal/memory-optimized tables,
full-text indexes, and manually created statistics. Ordinary SQL Server
optimizer-created statistics and a heap underlying a nonclustered primary key
are permitted. Enabled PostgreSQL database event triggers and SQL Server database
DDL triggers are rejected to avoid unreviewed DDL side effects.

These checks also apply on reruns. A supported complete target schema returns
without data or schema changes; it does not delete entries written since the
cutover, reseed the identity, or modify unrelated rows. Unexpected/custom schemas
require a DBA-reviewed adaptation instead of silent schema loss. Catalog checks
cannot discover every dynamic-SQL, cross-database, or external consumer; audit
custom reporting and integrations before upgrading.

## Migration Verification

When changing the migration, AI agents verify the following on a disposable restored copy for each provider.

1. Seed representative legacy activity, including nullable legacy timestamps,
   and unrelated posts, settings, categories, and comments. Record unrelated
   values/counts, the activity table identity, primary key, and generator state.
2. Apply the script and verify the exact ten-column target. Legacy history must
   be empty, all unrelated data unchanged, and the original table, key, and
   generator retained.
3. Insert a synthetic canonical activity with a UTC timestamp, target/details,
   and an automatically generated ID. Run the same script again. Verify that
   the entire row and all unrelated data remain unchanged, and that the next
   insert continues the existing generator rather than restarting it.
4. On separate disposable copies, verify missing-table, unknown/extra-column,
   partial-shape, wrong-type/length/nullability, invalid identity/primary-key,
   and extra-dependency failures retain the original history and schema.
5. Verify fresh schema creation, writes, listing, filtering, readable details,
   and Clear all using the matching application. Fresh installations use normal
   `EnsureCreated` and seeding; **no manual activity upgrade script is required**.

Remove the temporary verification artifacts when finished. Record provider execution and rerun results with the release evidence. Report any provider that could not be verified.

The application neither bootstraps these scripts automatically nor checks schema
compatibility at startup. Only reopen the installation after the required scripts
have completed and the matching application is deployed by an
authorized administrator.

## Rollback and Remaining Risks

A failed script rolls back its transaction; inspect the error before retrying.
After a committed cutover, restoring legacy activity history requires the
pre-upgrade database backup and the matching pre-cutover application. Merely
rolling back application binaries is not compatible with the new schema.

Keep the maintenance window closed until verification finishes. Once the site
has reopened, restoring the **whole database** from the pre-upgrade backup also
loses unrelated new posts, edits, settings, comments, and other writes made
since that backup. A DBA must plan recovery or reconcile those later writes
before an authorized restore. Never reverse this upgrade by recreating empty
legacy columns and claim the discarded history was recovered.
