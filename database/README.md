# Database Migration Scripts

All future database migration SQL scripts belong in this directory. Administrators run them manually before starting an upgraded application; the application does not execute migration scripts.

## Authoring Rules

- When a task changes the database schema or requires migrating existing persisted data, include the corresponding SQL scripts here alongside the application changes. This rule applies to human developers and AI agents.
- Support both PostgreSQL and SQL Server. Clearly identify each script's database provider, applicable source versions or prerequisites, target release, and execution order.
- Every script must be idempotent. Running it repeatedly must succeed and leave the same intended schema and data without duplicate objects, duplicate rows, or repeated data transformations.
- Check the relevant schema or data state before making changes. Guard table, column, constraint, and index changes, and ensure data updates do not reapply conversions or overwrite subsequent administrator edits on a rerun.
- Preserve UTC semantics, dependent keys and indexes, and existing data. Use transactions where supported by the operations, and document any transaction restrictions.
- The activity event-key cutover is an explicitly approved exception for legacy activity history only: its guarded one-time transition discards those rows. Reruns against the complete target schema must preserve new history, identity state, and all unrelated data.
- Do not restore the discarded cumulative scripts or introduce an automatic migration runner.

## Verification and Execution

1. Back up the database and test the script on a restored, representative pre-upgrade database for its target provider.
2. Verify the intended schema and data changes, then run the same script again. Both runs must succeed; the second must preserve the resulting schema and data.
3. Record the provider, prerequisites, and verification result with the change. If a target database is unavailable, explicitly report that the script and its idempotency were not verified against that database.
4. Have an administrator apply the required scripts before starting the upgraded application. Use backup restoration if rollback is needed.

## Available Scripts

| Upgrade | PostgreSQL | SQL Server | Prerequisites and execution order |
| --- | --- | --- | --- |
| Category integer identity IDs | [PostgreSql/category-int-identity.sql](PostgreSql/category-int-identity.sql) | [SqlServer/category-int-identity.sql](SqlServer/category-int-identity.sql) | Existing GUID-based `Category.Id` and `PostCategory.CategoryId` in the standard Moonglade schema. Apply the script for your provider once before starting the release introducing integer category IDs; repeat executions are safe. See the [upgrade guide](../docs/upgrade-category-int-identity.md). |
| PostgreSQL snake_case table and column names | [PostgreSql/snake-case-names.sql](PostgreSql/snake-case-names.sql) | [SqlServer/snake-case-names.sql](SqlServer/snake-case-names.sql) (no-op) | All 19 standard PostgreSQL tables in `public`; integer category IDs are required. Apply the category ID upgrade first if needed, then the naming script before starting the release introducing snake_case mappings. SQL Server keeps PascalCase names. See the [upgrade guide](../docs/upgrade-postgresql-snake-case.md). |
| Stable activity event keys | [PostgreSql/activity-log-events.sql](PostgreSql/activity-log-events.sql) | [SqlServer/activity-log-events.sql](SqlServer/activity-log-events.sql) | Complete standard nine-column legacy activity table or ten-column target, with its original `bigint` identity and primary key. Complete the category ID upgrade first if needed, then PostgreSQL snake_case naming if applicable, then this script before starting the release introducing stable activity event keys. **Legacy activity history is deleted only at cutover**; reruns preserve new history. Unknown/partial shapes and dependencies fail before deletion. Stop all hosts and back up; see the [upgrade and rollback guide](../docs/upgrade-activity-log-events.md). |
| SEO settings | [PostgreSql/seo-settings.sql](PostgreSql/seo-settings.sql) | [SqlServer/seo-settings.sql](SqlServer/seo-settings.sql) | Existing configuration table. Apply PostgreSQL snake_case names first where applicable. Stop all hosts and apply before starting the release with a dedicated SEO settings group. See the [upgrade guide](../docs/upgrade-seo-settings.md). |
| Robots.txt in SEO settings | [PostgreSql/seo-robots-txt.sql](PostgreSql/seo-robots-txt.sql) | [SqlServer/seo-robots-txt.sql](SqlServer/seo-robots-txt.sql) | Existing SEO settings row. Run after the initial SEO upgrade, with all hosts stopped, before starting the release that moves robots.txt into SEO. See the [upgrade guide](../docs/upgrade-seo-settings.md#move-robotstxt-into-seo-settings). |
| Independent owner account storage | [PostgreSql/owner-account.sql](PostgreSql/owner-account.sql) | [SqlServer/owner-account.sql](SqlServer/owner-account.sql) | Existing standard schema; integer category IDs and PostgreSQL snake_case names where applicable. Stop all hosts and back up first. Adds or validates only the account table, constraints and indexes; reruns preserve profiles and credentials. Run before the account cutover below. |
| Owner email and legacy credential cutover | [PostgreSql/owner-account-cutover.sql](PostgreSql/owner-account-cutover.sql) | [SqlServer/owner-account-cutover.sql](SqlServer/owner-account-cutover.sql) | Run the additive account script first. Preserves legacy public email as a profile only when no owner exists, removes only that General member and the obsolete credentials row, and preserves configured accounts on reruns. No legacy credentials are imported. Local setup is public until completed; see the [upgrade and rollback guide](../docs/upgrade-owner-account.md). |
| Owner avatar settings | [PostgreSql/owner-avatar.sql](PostgreSql/owner-avatar.sql) | [SqlServer/owner-avatar.sql](SqlServer/owner-avatar.sql) | Run after the account cutover, with all hosts stopped. Moves the uploaded avatar and Gravatar preference into independent account storage. See the [upgrade guide](../docs/upgrade-owner-avatar.md). |
| Owner name and description | [PostgreSql/owner-presentation.sql](PostgreSql/owner-presentation.sql) | [SqlServer/owner-presentation.sql](SqlServer/owner-presentation.sql) | Run after the owner avatar cutover. Preserves existing images and Gravatar settings. |

The previous cumulative migration scripts have been discarded. These scripts do not replace older, unrelated database upgrades.

The owner-account release gate includes fresh databases and upgraded databases with initialized or profile-only owners on both providers, stopped-host script reruns, exact unrelated-data comparisons and restart preservation. Follow the [owner-account verification checklist](../docs/upgrade-owner-account.md#verification-and-rollback); successful schema creation alone does not establish application or browser acceptance.
