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
3. Have an administrator apply the required scripts before starting the upgraded application. Use backup restoration if rollback is needed.

## Widget Region/Order Upgrade (12)

The widget-composer schema requires [PostgreSQL script 12](PostgreSql/12.widget-regions.sql) or [SQL Server script 12](SqlServer/12.widget-regions.sql), after the applicable upgrades through `11.owner-oidc-binding-only.sql`. The source schema has `DisplayOrder` (`display_order`); the target has `Region`/`SortOrder` (`region`/`sort_order`) and a 128-character title column. Fresh installations create the target schema directly.

1. Stop every application host and back up the database. Verify the upgrade on a restored copy before touching the real database.
2. Run script 12 for the configured provider. It uses one transaction and holds the widget table stable throughout conversion. Invalid legacy positions, oversized titles, and incomplete schemas fail instead of being silently repaired.
3. Verify widget identifiers, content, enabled state, creation timestamps, placement, and ordering, then run the script again. A complete target schema is validated without reapplying conversion or overwriting later administrator edits.
4. Start the matching upgraded application. Check `/admin/widgets` and the public sidebar. Deploy the application and schema together; the previous binary cannot use the new columns.

Conversion maps `[-30,-21]` to `BeforeProfile`, `[-20,-11]` to `BeforeTags`, `[-10,-1]` to `BeforeCategories`, and `[0,999]` to `AfterCategories`. Within each region it assigns zero-based positions ordered by original DisplayOrder, CreatedTimeUtc, then ordinal lowercase canonical GUID string. IDs, content, titles, enabled flags, and UTC metadata are preserved. Empty legacy collections are supported. Subsequent positions are nonnegative integers, without the former numeric-band capacity.

If validation fails, inspect the reported schema or safe widget identifier before retrying; do not manually delete columns or fabricate replacement positions. If PostgreSQL reports an error inside the script transaction, issue `ROLLBACK` or close that session before inspecting or retrying; do not leave an aborted transaction open. For rollback, keep all hosts stopped, restore the pre-upgrade backup, and restore its matching application version. Reverting the application binary alone is insufficient. There is no automatic migration runner or reverse conversion.

## Unified Account Upgrade (13)

Run [PostgreSQL script 13](PostgreSql/13.unified-accounts.sql) or [SQL Server script 13](SqlServer/13.unified-accounts.sql) after the applicable upgrades through script 12. Fresh installations create the unified schema directly.

1. Stop all application hosts, take a database backup and test the migration on a restored copy.
2. Run script 13 for the configured provider. It renames the account table and adds the collaborator account state and nullable activity actor ID. Existing owner rows and the OIDC foreign key retain the same identity. Account, avatar, OIDC, activity and Data Protection records are preserved.
3. Repeat script 13 against the unified schema and confirm that the schema and data remain stable. To rehearse an earlier script again, restore a backup from its applicable starting version.
4. Start the matching upgraded application. Verify the owner sign-in or pending setup, public profile and applicable OIDC binding. If rollback is required, stop the hosts and restore the pre-upgrade database backup together with the previous application version.
