# Upgrade Owner Profile Settings

Run the provider script before starting the version that moves avatar controls to Account Settings. A fresh database uses the new schema on first start.

1. Complete any required [owner account upgrades](upgrade-owner-account.md), including the PostgreSQL naming upgrade where applicable.
2. Stop all Moonglade hosts and back up the database.
3. Run [PostgreSQL owner-avatar.sql](../database/PostgreSql/owner-avatar.sql) or [SQL Server owner-avatar.sql](../database/SqlServer/owner-avatar.sql).
4. Run [PostgreSQL owner-presentation.sql](../database/PostgreSql/owner-presentation.sql) or [SQL Server owner-presentation.sql](../database/SqlServer/owner-presentation.sql).
5. Start Moonglade and open Account Settings at `/admin/account`. Verify the name, description, Gravatar switch, uploaded image and public `/avatar` endpoint. With no public owner email, the public profile uses the uploaded image or bundled fallback.

The script copies the original avatar bytes and the General Settings Gravatar preference to the Auth-owned singleton table. It removes the former avatar asset row and the two avatar fields from General Settings in the same transaction. The inactive legacy URL is discarded. Unrelated assets, configuration fields and timestamps, account identity, and credentials are preserved. The script skips the cutover when the new table already exists, preserving avatar changes made after upgrade.

The presentation script adds name and description columns to the same account-owned row, moves the two General Settings fields, and preserves the avatar, Gravatar preference, and unrelated settings. Re-running it preserves later edits.

Test each script and a second execution on a restored copy for the target provider. Verify existing settings and assets before applying it to a live database. To roll back, stop all hosts, restore the pre-upgrade backup, and start the previous application version.
