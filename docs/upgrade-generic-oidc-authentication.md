# Upgrade from standalone OpenID Connect to local owner sign-in

Moonglade requires an initialized local owner account. Existing local accounts retain their credentials and authenticator. Old standalone OIDC settings, subject allowlists, callbacks and sessions no longer grant access. The former identity endpoint returns HTTP 404.

1. Stop the old application and back up the database and deployment configuration. If the database already has the current owner-account, avatar and presentation schema, this authentication change requires no database script. For older schemas, complete the applicable [database upgrades](../database/README.md).
2. Remove the old authentication provider selector and OpenID Connect section from application configuration and environment overrides. Keep your local TOTP and authentication rate-limit settings.
3. For a site without local credentials, restrict network access before starting the upgraded application. The public setup page allows any reachable visitor to establish the owner credentials.
4. Start the upgrade and open the site. If redirected to the setup page, confirm the prefilled public email when available and set a local password. If TOTP is required, complete authenticator verification. Setup uses the existing owner record when present.
5. Sign in again using the local account. Check the public owner profile and administrator access before reopening the site.

To roll back, stop the upgraded hosts and restore the matching old binaries, configuration and pre-upgrade database backup.
