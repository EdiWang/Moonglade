# Local Entra OIDC Validation

## Objective

Validate the generic OpenID Connect authentication implementation locally against the existing production Entra application registration without changing the production App Service configuration.

## Completed Work

- Added the local sign-in and signed-out callback URLs to the existing application registration while preserving all production callback URLs.
- Appended a dedicated 180-day client credential for local development.
- Configured the ignored development settings file for the generic OpenID Connect provider.
- Signed in through Entra and captured the authenticated account's standard `sub` value into the local administrator allowlist.
- Updated the identity diagnostic endpoint to support providers that expose the token issuer through the subject claim's issuer metadata instead of an `iss` claim.
- Added a focused unit test for the issuer metadata fallback.
- Verified both the administrator policy endpoint and the Admin Dashboard with the authenticated Entra account.

## Verification

- Web test project build: passed with no warnings or errors.
- Web test executable: 194 passed, 0 failed.
- Live OpenID Connect authorization-code flow with PKCE: passed.
- Authenticated administrator policy endpoint: HTTP 200.
- Admin Dashboard: HTTP 200 and rendered successfully.

## Production Boundary

The production App Service configuration was not changed. It continues to use the previously deployed Entra-specific authentication implementation until the generic OpenID Connect build is deployed and its application settings are migrated separately.
