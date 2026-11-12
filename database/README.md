# Database Migration Scripts

All future database migration SQL scripts belong in this directory. Administrators run them manually before starting an upgraded application; the application does not execute migration scripts.

## Authoring Rules

- When a task changes the database schema or requires migrating existing persisted data, include the corresponding SQL scripts here alongside the application changes. This rule applies to human developers and AI agents.
- Support both PostgreSQL and SQL Server. Clearly identify each script's database provider, applicable source versions or prerequisites, target release, and execution order.
- Use the next execution number as a filename prefix (for example, `11.upgrade-name.sql`). Use the same number for the corresponding scripts in both provider directories.
- Every script must be idempotent. Running it repeatedly must succeed and leave the same intended schema and data without duplicate objects, duplicate rows, or repeated data transformations.
- Check the relevant schema or data state before making changes. Guard table, column, constraint, and index changes, and ensure data updates do not reapply conversions or overwrite subsequent administrator edits on a rerun.
- Preserve UTC semantics, dependent keys and indexes, and existing data. Use transactions where supported by the operations, and document any transaction restrictions.
- The activity event-key cutover is an explicitly approved exception for legacy activity history only: its guarded one-time transition discards those rows. Reruns against the complete target schema must preserve new history, identity state, and all unrelated data.
- Do not restore the discarded cumulative scripts or introduce an automatic migration runner.

## Verification and Execution

Administrators apply the required SQL scripts directly. AI agents verify migration changes with temporary scripts or fixtures in disposable databases and remove those artifacts when finished.

1. Back up the database. When verifying a migration change, use a representative restored pre-upgrade database for each target provider.
2. During migration verification, check the intended schema and data changes, then run the same script again. Both runs must succeed. The second run must preserve the resulting schema and data.
3. Record the provider, prerequisites, and verification result with the change. If a target database is unavailable, explicitly report that the script and its idempotency were not verified against that database.
4. Have an administrator apply the required scripts before starting the upgraded application. Use backup restoration if rollback is needed.
