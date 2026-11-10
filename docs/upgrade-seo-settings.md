# Upgrade to Dedicated SEO Settings

Before starting the release that introduces `SEOSettings` on an existing database, stop all Moonglade hosts and back up the database. Test the script on a restored copy and run the script for your database provider.

- PostgreSQL uses [seo-settings.sql](../database/PostgreSql/seo-settings.sql) after the [snake_case naming upgrade](upgrade-postgresql-snake-case.md), if required.
- SQL Server uses [seo-settings.sql](../database/SqlServer/seo-settings.sql) with SQL Server 2022 or later.

The script copies the existing canonical URL prefix, meta keyword, Dublin Core switch and license URL from General, and the sitemap and JSON-LD BlogPosting switches from Advanced. Missing fields use the application defaults. The script stops if a source value is not a valid JSON object or a moved field occurs more than once, including case variants. Resolve those source records before retrying.

Both scripts insert one `SEOSettings` row and leave the source rows and activity history intact. If the target row exists, reruns leave it and its modification timestamp intact. Apply the script before starting the upgraded application, since a startup without the row initializes it to defaults. Fresh installations seed the SEO row automatically.

Administrators edit the migrated settings under **Settings > SEO**. API clients send those fields to `/api/settings/seo`. IndexNow remains a host configuration setting.
