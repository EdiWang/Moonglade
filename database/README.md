# Database Migration Scripts

All future database migration SQL scripts belong in this directory. Administrators run them manually before starting an upgraded application; the application does not execute migration scripts.

## Authoring Rules

- When a task changes the database schema or requires migrating existing persisted data, include the corresponding SQL scripts here alongside the application changes. This rule applies to human developers and AI agents.
- Support both PostgreSQL and SQL Server. Clearly identify each script's database provider, applicable source versions or prerequisites, target release, and execution order.
- Every script must be idempotent. Running it repeatedly must succeed and leave the same intended schema and data without duplicate objects, duplicate rows, or repeated data transformations.
- Check the relevant schema or data state before making changes. Guard table, column, constraint, and index changes, and ensure data updates do not reapply conversions or overwrite subsequent administrator edits on a rerun.
- Preserve UTC semantics, dependent keys and indexes, and existing data. Use transactions where supported by the operations, and document any transaction restrictions.
- Do not restore the discarded cumulative scripts or introduce an automatic migration runner.

## Verification and Execution

1. Back up the database and test the script on a restored, representative pre-upgrade database for its target provider.
2. Verify the intended schema and data changes, then run the same script again. Both runs must succeed; the second must preserve the resulting schema and data.
3. Record the provider, prerequisites, and verification result with the change. If a target database is unavailable, explicitly report that the script and its idempotency were not verified against that database.
4. Have an administrator apply the required scripts before starting the upgraded application. Use backup restoration if rollback is needed.

The previous cumulative migration scripts have been discarded. This directory currently establishes the conventions for future scripts and supplies no upgrade SQL.
