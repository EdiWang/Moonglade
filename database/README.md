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

## Available Scripts

Run the scripts required for your installation in ascending numeric order for your database provider.

| Upgrade | PostgreSQL | SQL Server | Prerequisites and execution order |
| --- | --- | --- | --- |
| Category integer identity IDs | [PostgreSql/1.category-int-identity.sql](PostgreSql/1.category-int-identity.sql) | [SqlServer/1.category-int-identity.sql](SqlServer/1.category-int-identity.sql) | Existing GUID-based `Category.Id` and `PostCategory.CategoryId` in the standard Moonglade schema. Apply the script for your provider once before starting the release introducing integer category IDs; repeat executions are safe. See the [upgrade guide](../docs/upgrade-category-int-identity.md). |
| PostgreSQL snake_case table and column names | [PostgreSql/2.snake-case-names.sql](PostgreSql/2.snake-case-names.sql) | [SqlServer/2.snake-case-names.sql](SqlServer/2.snake-case-names.sql) (no-op) | All 19 standard PostgreSQL tables in `public`; integer category IDs are required. Apply the category ID upgrade first if needed, then the naming script before starting the release introducing snake_case mappings. SQL Server keeps PascalCase names. See the [upgrade guide](../docs/upgrade-postgresql-snake-case.md). |
| Stable activity event keys | [PostgreSql/3.activity-log-events.sql](PostgreSql/3.activity-log-events.sql) | [SqlServer/3.activity-log-events.sql](SqlServer/3.activity-log-events.sql) | Complete standard nine-column legacy activity table or ten-column target, with its original `bigint` identity and primary key. Complete the category ID upgrade first if needed, then PostgreSQL snake_case naming if applicable, then this script before starting the release introducing stable activity event keys. **Legacy activity history is deleted only at cutover**; reruns preserve new history. Unknown/partial shapes and dependencies fail before deletion. Stop all hosts and back up; see the [upgrade and rollback guide](../docs/upgrade-activity-log-events.md). |
| SEO settings | [PostgreSql/4.seo-settings.sql](PostgreSql/4.seo-settings.sql) | [SqlServer/4.seo-settings.sql](SqlServer/4.seo-settings.sql) | Existing configuration table. Apply PostgreSQL snake_case names first where applicable. Stop all hosts and apply before starting the release with a dedicated SEO settings group. See the [upgrade guide](../docs/upgrade-seo-settings.md). |
| Robots.txt in SEO settings | [PostgreSql/5.seo-robots-txt.sql](PostgreSql/5.seo-robots-txt.sql) | [SqlServer/5.seo-robots-txt.sql](SqlServer/5.seo-robots-txt.sql) | Existing SEO settings row. Run after the initial SEO upgrade, with all hosts stopped, before starting the release that moves robots.txt into SEO. See the [upgrade guide](../docs/upgrade-seo-settings.md#move-robotstxt-into-seo-settings). |
| Independent owner account storage | [PostgreSql/6.owner-account.sql](PostgreSql/6.owner-account.sql) | [SqlServer/6.owner-account.sql](SqlServer/6.owner-account.sql) | Existing standard schema; integer category IDs and PostgreSQL snake_case names where applicable. Stop all hosts and back up first. Adds or validates only the account table, constraints and indexes; reruns preserve profiles and credentials. Run before the account cutover below. |
| Owner email and legacy credential cutover | [PostgreSql/7.owner-account-cutover.sql](PostgreSql/7.owner-account-cutover.sql) | [SqlServer/7.owner-account-cutover.sql](SqlServer/7.owner-account-cutover.sql) | Run the additive account script first. Preserves legacy public email as a profile only when no owner exists, removes only that General member and the obsolete credentials row, and preserves configured accounts on reruns. No legacy credentials are imported. Local setup is public until completed; see the [upgrade and rollback guide](../docs/upgrade-owner-account.md). |
| Owner avatar settings | [PostgreSql/8.owner-avatar.sql](PostgreSql/8.owner-avatar.sql) | [SqlServer/8.owner-avatar.sql](SqlServer/8.owner-avatar.sql) | Run after the account cutover, with all hosts stopped. Moves the uploaded avatar and Gravatar preference into independent account storage. See the [upgrade guide](../docs/upgrade-owner-avatar.md). |
| Owner name and description | [PostgreSql/9.owner-presentation.sql](PostgreSql/9.owner-presentation.sql) | [SqlServer/9.owner-presentation.sql](SqlServer/9.owner-presentation.sql) | Run after the owner avatar cutover. Preserves existing images and Gravatar settings. |
| Owner OIDC binding | [PostgreSql/10.owner-oidc.sql](PostgreSql/10.owner-oidc.sql) | [SqlServer/10.owner-oidc.sql](SqlServer/10.owner-oidc.sql) | Run after the owner-account schema upgrades with all hosts stopped. Adds provider settings, account binding and the dedicated key ring. [Upgrade and restore guide](../docs/upgrade-oidc-binding.md). |

The previous cumulative migration scripts have been discarded. These scripts do not replace older, unrelated database upgrades.

The owner-account release gate includes fresh databases and upgraded databases with initialized or profile-only owners on both providers, stopped-host script reruns, exact unrelated-data comparisons and restart preservation. Follow the [owner-account verification checklist](../docs/upgrade-owner-account.md#verification-and-rollback); successful schema creation alone does not establish application or browser acceptance.
