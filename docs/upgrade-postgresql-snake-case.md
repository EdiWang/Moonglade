# PostgreSQL Snake Case Upgrade

PostgreSQL uses snake_case table and column names, such as `blog_theme.theme_name` and `comment.ip_address`. SQL Server continues to use PascalCase names, such as `BlogTheme.ThemeName` and `Comment.IPAddress`. C# entities, properties, and public contracts retain their existing names.

## Existing Databases

Before starting the release introducing these mappings:

1. Stop all application instances and background workers using the database.
2. Back up the database and rehearse the upgrade on a restored copy.
3. If PostgreSQL category IDs are still UUIDs, complete the [category integer ID upgrade](upgrade-category-int-identity.md) first. That older script requires the original PascalCase identifiers; do not run it after the snake_case upgrade.
4. Run [`database/PostgreSql/snake-case-names.sql`](../database/PostgreSql/snake-case-names.sql) against the existing PostgreSQL database.
5. Verify content, relationships, and writes, then start the upgraded application.

Use a maintenance connection with table-owner privileges or a PostgreSQL superuser for the rename; ordinary read/write grants do not permit it. The backup account also needs access to all tables and sequences. Do not change the application's normal database role or grants as part of this naming upgrade.

The PostgreSQL script targets all 19 standard Moonglade tables in the `public` schema. It renames tables and columns in one transaction, without recreating tables or transforming stored values. It preserves table identities, keys, indexes, identity sequences and their current values, UTC values, and existing data. Names of constraints, indexes, and sequences are intentionally retained. Missing required tables, conflicting names, and category IDs that have not been upgraded cause the entire transaction to fail. A lock timeout also rolls back the transaction; stop concurrent database access before retrying.

Conversions follow Npgsql's name translator, including `IPAddress` to `ip_address` and `Base64Data` to `base64data`.

The script can run repeatedly against an upgraded database or a fresh current schema. Repeated runs preserve the schema and data. SQL Server requires no naming migration; [`database/SqlServer/snake-case-names.sql`](../database/SqlServer/snake-case-names.sql) is an explicit no-op companion.

Older application binaries and custom SQL using the original PostgreSQL identifiers will not work after the rename. Update external queries to the new names. Use the pre-upgrade backup if rollback is needed. Do not run an older application against the upgraded database.

## Fresh Installations

Fresh PostgreSQL databases are created and seeded with snake_case table and column names automatically. No SQL script is needed. SQL Server schema creation is unchanged.

## Automated Verification

Model tests verify provider-specific names, acronym handling, generated queries, and fresh-schema DDL without database connections. The opt-in relational tests use uniquely named disposable PostgreSQL and SQL Server containers; they never migrate configured development or production databases:

```powershell
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
Remove-Item Env:MOONGLADE_TEST_CATEGORY_DATABASES
```

Docker must be running. Tests cover legacy content and relationship preservation, identity continuation, repeated execution, collision/missing-table rollback, the SQL Server no-op, and fresh-schema seeding and writes.
