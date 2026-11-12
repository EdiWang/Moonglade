# Upgrade to independent owner accounts

This upgrade replaces legacy local credentials and moves the public Owner Email out of General Settings. Back up and test on a restored database before deployment. Application startup does not run these scripts.

## Preparation and execution order

1. Stop **all** old and new application hosts, including background workers. Take a complete database backup and retain the old application files and configuration.
2. Complete any applicable [category identity](upgrade-category-int-identity.md), [PostgreSQL naming](upgrade-postgresql-snake-case.md), [activity event](upgrade-activity-log-events.md), and [SEO](upgrade-seo-settings.md) upgrades. The account scripts do not replace them. Do not rerun an activity-history reset as part of the account upgrade.
3. Run the additive account script for your provider, then the matching cutover script in the same database:

| Provider | First: schema and validation | Second: public-email and credential cutover |
| --- | --- | --- |
| PostgreSQL | [`6.owner-account.sql`](../database/PostgreSql/6.owner-account.sql) | [`7.owner-account-cutover.sql`](../database/PostgreSql/7.owner-account-cutover.sql) |
| SQL Server | [`6.owner-account.sql`](../database/SqlServer/6.owner-account.sql) | [`7.owner-account-cutover.sql`](../database/SqlServer/7.owner-account-cutover.sql) |

4. Verify the resulting account/profile, remaining General Settings, and preserved content before starting the upgraded application. Both scripts are transactional and safe to rerun with hosts stopped. Run SQL tools with stop-on-error behavior; disconnect or roll back an open transaction after a reported error before retrying.
5. Start the new application and complete the local account setup below. Do not run old binaries against the upgraded database.

Fresh installations need neither script: startup creates the schema and ordinary settings, but never seeds administrator credentials or a default public email.

## What the scripts change

- The additive script creates or validates the singleton account table, constraints and indexes. It does not import data.
- The cutover requires a valid General Settings JSON object. If its top-level `OwnerEmail` field exists (case-insensitive) and no owner exists, the script trims and validates it and creates a **public profile only**. Password hash, salt, TOTP secret and Local initialization remain absent.
- An existing account or profile always wins. The cutover never replaces its email, stable ID, revision or credentials, including on a rerun after an administrator edits the address.
- Only the obsolete email member is removed from General JSON. Other members and their values, including unknown or duplicate unrelated members, remain; insignificant JSON whitespace may change. The configuration row's modification timestamp is retained.
- Only the exact `LocalAccountSettings` configuration row is deleted. Legacy usernames, password hashes, salts and authenticator secrets are **not imported**. Articles, pages, comments, current activity history, installation metadata and unrelated rows remain unchanged.

Malformed/non-object General JSON and duplicate email aliases stop the transaction without cleanup. When no account exists, an invalid email value also stops it. Resolve the reported source data deliberately before retrying. An already configured owner takes precedence over an invalid obsolete email value. A General object with no email field is a valid already-cut-over state.

## Local authentication

Pre-cutover credentials and cookies no longer authenticate. Already initialized independent accounts keep their credentials. If no initialized independent account exists, `/auth/setup-account` is public and any reachable visitor can claim the first administrator. **Restrict access operationally until you complete setup.** There is no installation token.

Enter an email and new password. A migrated public address can be confirmed or replaced. When `Authentication:Totp:Required=true` (the default), verify a new authenticator code before any credentials are committed. When false, setup and sign-in skip TOTP in every environment. Setup signs in and opens the public homepage. A profile alone does not complete setup.

Later, edit the public email at `/admin/account`, not General Settings. A real Local change requires the current password and invalidates all Local sessions, including intermediate setup/verification tickets. Sign in with the new email and the **existing bound authenticator**. Passwords, stable identity and initialization state are retained.

## Sites previously using standalone OpenID Connect

Follow the [local sign-in cutover](upgrade-generic-oidc-authentication.md) to retire the old OIDC deployment settings and establish local credentials. The current authentication cutover adds no database changes for sites that already use independent owner accounts.

## Verification and rollback

Check local sign-in and account editing and confirm the public address in Atom, FOAF, Gravatar and notification recipients. Repeat the scripts with hosts stopped and confirm subsequent email edits and existing credentials remain intact.

For release acceptance, use separate disposable databases for fresh installations and upgraded sites with initialized accounts or profile-only owners on **both** providers. Compare every unrelated mapped table and the retained General values/timestamp across the two scripts and their repeat execution. Check a rejected setup submission and restart without reseeding, then complete setup with TOTP required and disabled. A successful account setup or email edit must survive restart; an authenticator reset must not reopen initial account setup. See the [test guide](../src/Tests/README.md#owner-account-release-acceptance).

For rollback, stop every upgraded host and **restore the complete pre-upgrade backup**, then restore the matching old application and configuration. These scripts intentionally have no reverse operation that reconstructs deleted legacy credentials. An old binary or a partial General JSON restoration is not a rollback.
