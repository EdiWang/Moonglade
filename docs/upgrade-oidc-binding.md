# Upgrade for OIDC account binding

OIDC settings and the external account binding belong to the initialized local owner. The old standalone provider settings and subject allowlist are not imported. Sites that used standalone OIDC first follow the [local account cutover](upgrade-generic-oidc-authentication.md).

## Database upgrade

1. Stop all application hosts and back up the database. Complete any required owner account, avatar and presentation upgrades.
2. Run [PostgreSQL 10.owner-oidc.sql](../database/PostgreSql/10.owner-oidc.sql) or [SQL Server 10.owner-oidc.sql](../database/SqlServer/10.owner-oidc.sql) before starting the application. The scripts add the singleton configuration and dedicated Data Protection key tables. Repeating the scripts preserves configuration, bindings, keys and existing owner data.
3. Start the application and sign in with the local owner account. Fresh installations create the required tables during setup.

## Configure and bind

Register a confidential web client with the provider for OpenID Connect authorization code flow with PKCE. In **Account Settings > Third-party sign-in**, copy the displayed redirect URI to the provider's registration. Enter the display name, public HTTPS authority, client ID and secret, then select **Continue**. The next page verifies the local password and applicable authenticator code before saving. The draft belongs to the current owner session and expires after ten minutes or when the application instance restarts.

Select **Bind account** and authenticate with the provider. Review the returned account before confirming. The binding retains the verified issuer and subject. When saving and binding in one short-lived flow, the original password and authenticator proof carries forward. Disable retains the saved configuration and binding. Delete removes both after confirmation. Leave the client secret blank to keep the saved value when editing a configuration. Unbind before changing the authority or client ID.

Provider discovery, authorization and backchannel endpoints must use public HTTPS. The server does not follow backchannel redirects.

## Sign in and revoke access

An enabled, bound provider appears on the sign-in page using its configured display name. OIDC sign-in matches the verified issuer and subject to the binding and opens the admin dashboard as the local owner. Moonglade TOTP still applies to local password sign-in when required. Sign-out ends the Moonglade session only. Disabling, unbinding, and deleting revoke sessions from the binding and preserve local sessions. Re-enabling permits a new OIDC sign-in.

## Restore and rollback

Restore the database on the replacement host. The database includes the encrypted client secret and the keys required to decrypt it. Treat database backups as credentials. A separate key volume or master secret is unnecessary for the saved configuration and binding.

To roll back, stop the upgraded hosts and restore the matching application and pre-upgrade database backup.
