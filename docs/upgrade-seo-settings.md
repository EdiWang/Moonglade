# Upgrade to Dedicated SEO Settings

Before starting the release that introduces `SEOSettings` on an existing database, stop all Moonglade hosts and back up the database. Test the script on a restored copy and run the script for your database provider.

- PostgreSQL uses [4.seo-settings.sql](../database/PostgreSql/4.seo-settings.sql) after the [snake_case naming upgrade](upgrade-postgresql-snake-case.md), if required.
- SQL Server uses [4.seo-settings.sql](../database/SqlServer/4.seo-settings.sql) with SQL Server 2022 or later.

The script copies the existing canonical URL prefix, meta keyword, Dublin Core switch and license URL from General, and the sitemap and JSON-LD BlogPosting switches from Advanced. Missing fields use the application defaults. The script stops if a source value is not a valid JSON object or a moved field occurs more than once, including case variants. Resolve those source records before retrying.

Both scripts insert one `SEOSettings` row and leave the source rows and activity history intact. If the target row exists, reruns leave it and its modification timestamp intact. Apply the script before starting the upgraded application, since a startup without the row initializes it to defaults. Fresh installations seed the SEO row automatically.

## Move robots.txt into SEO settings

Before starting the release that moves robots.txt to SEO, stop all Moonglade hosts and back up the database. Run the provider-specific follow-up on an existing database after the initial SEO upgrade. If the initial SEO upgrade is already complete, apply the follow-up directly.

- PostgreSQL uses [5.seo-robots-txt.sql](../database/PostgreSql/5.seo-robots-txt.sql).
- SQL Server uses [5.seo-robots-txt.sql](../database/SqlServer/5.seo-robots-txt.sql).

The follow-up copies the existing robots.txt value from Advanced into the SEO row when the SEO field is absent. It preserves the text, including whitespace, and leaves the Advanced row and the other SEO values intact. If the source field is absent, the current SEO row is preserved. A value already present in SEO, including explicit null or an empty string, is retained on reruns. Invalid or duplicate source values require review before retrying.

Run the follow-up before the upgraded app can save the SEO row. Fresh installations already store robots.txt under SEO. The public endpoint stays at `/robots.txt`.

Administrators edit the migrated settings under **Settings > SEO**. API clients send those fields to `/api/settings/seo`. IndexNow remains a host configuration setting.
