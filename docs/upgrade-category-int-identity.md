# Upgrade Category IDs to Integer Identities

The release introducing this change replaces GUID category keys with
database-generated `int` IDs starting at 100 and incrementing by 1. Article IDs
remain GUIDs. Fresh installations create the new schema automatically and obtain
the default category ID from the database; no upgrade script is needed.

## Existing Databases

For PostgreSQL, run this upgrade before the [snake_case naming upgrade](upgrade-postgresql-snake-case.md). This historical script requires the original PascalCase identifiers and must not be run against a schema already renamed to snake_case.

Use the script matching your database provider:

| Provider | Script | Source schema |
| --- | --- | --- |
| PostgreSQL | [`database/PostgreSql/1.category-int-identity.sql`](../database/PostgreSql/1.category-int-identity.sql) | `public."Category"."Id"` and `public."PostCategory"."CategoryId"` are `uuid`. |
| SQL Server | [`database/SqlServer/1.category-int-identity.sql`](../database/SqlServer/1.category-int-identity.sql) | `dbo.Category.Id` and `dbo.PostCategory.CategoryId` are `uniqueidentifier`. |

The scripts require the standard category primary key, the
`(PostId, CategoryId)` association primary key, one foreign key from the
association table to the category, and its single-column category index.
They preserve the existing key/index names and category foreign-key actions,
including the named NOT NULL constraints introduced in PostgreSQL 18.
Unexpected additional dependent indexes, check constraints, foreign keys, or
partially converted column types cause an error rather than silently discarding
custom schema. Have a database administrator adapt the script for such a schema.

1. Back up the database and restore a representative copy for verification.
2. Stop every application instance accessing the database. Do not mix the old
   GUID-based application with the integer-based application.
3. Execute the complete provider script against the restored copy using an
   account permitted to alter tables, keys, indexes, and identity generation.
   PostgreSQL requires table-owner privileges or a superuser; ordinary table
   read/write grants are not sufficient. Use a separate maintenance connection
   if needed rather than changing the runtime connection. Each script manages
   its own transaction; run it as a standalone script.
4. Compare category names, slugs, notes, category counts, and article/category
   associations with the backup. Verify that category pages and feeds still
   resolve through the same slugs and that article URLs and timestamps are
   unchanged.
5. Run the same script again, including after test inserts, edits, and deletions.
   Existing integer IDs and content must remain unchanged, and future inserts
   must continue the identity sequence without resetting it.
6. Have an administrator apply the verified script to the backed-up live
   database, then start the upgraded application. Restart all instances so no
   in-memory cache contains the old category IDs.

The application does not execute these scripts or check schema compatibility.
Rollback requires restoring the pre-upgrade backup and the matching old
application version. Do not launch an older GUID-based application against the
converted database.

## Mapping and Compatibility

The database assigns new integer IDs to existing categories. The script uses
each old GUID to map all existing article associations to the corresponding new
integer, then replaces the old key columns in the same transaction. Empty
category tables are supported; their first subsequent insert receives 100.
Migration numbering has no display-order or business meaning.

Category slugs, display names, notes, article content, article GUIDs, and
timestamps are not changed. Existing public category pages and RSS/Atom
subscriptions retain their slug-based URLs. Article JSON exports continue to
represent categories by their display names.

Administrative `GET`, `PUT`, and `DELETE /api/category/{id}` now accept positive
integers instead of GUIDs. Category lists and post-editor metadata return numeric
IDs, and article-edit `selectedCatIds` arrays contain numbers, not GUIDs or
numeric strings. Existing GUID requests are not supported; reload open admin
pages and update external management clients after upgrading. Historical
activity-log GUIDs are retained as historical metadata, not live foreign keys.

Identity values may have gaps after deletes or failed writes. Do not reseed to
reuse deleted IDs, infer category counts from the ID, or use IDs for sorting.

## Migration Verification

The migration was verified against PostgreSQL 18 and SQL Server 2022. Verification covered populated and empty legacy schemas, mapping and key preservation, generated IDs, repeat execution, and rollback on invalid prerequisites. See the [migration verification guide](../database/README.md#verification-and-execution) when changing the scripts.
