# Upgrade to Generic OpenID Connect Authentication

Moonglade no longer has a Microsoft Entra ID-specific authentication mode. Deployments that previously used `Authentication:Provider=EntraID` must migrate to the single generic OpenID Connect provider before upgrading. Local-account deployments require no authentication configuration changes.

This change does not modify the database.

## Prerequisites

Configure Moonglade as a confidential web client in an OpenID Connect provider that publishes HTTPS discovery metadata. The provider must support authorization code flow and PKCE.

Register both application callback URLs, replacing the host with the public blog origin:

- `https://blog.example.com/signin-oidc`
- `https://blog.example.com/signout-callback-oidc`

Create a client secret and store it in the deployment's secret-management system. Do not add the secret to `appsettings.json`, deployment templates, source control, or task records.

## Replace the Authentication Configuration

Remove the old Entra-specific settings:

```json
"Authentication": {
  "Provider": "EntraID",
  "EntraID": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "...",
    "TenantId": "...",
    "ClientId": "...",
    "CallbackPath": "/signin-oidc"
  }
}
```

Replace them with the generic configuration:

```json
"Authentication": {
  "Provider": "OpenIdConnect",
  "OpenIdConnect": {
    "Authority": "https://identity.example.com/",
    "ClientId": "moonglade",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "NameClaimType": "name",
    "Scopes": [ "openid", "profile", "email" ],
    "AllowedSubjects": []
  }
}
```

Supply the client secret through the standard configuration key `Authentication:OpenIdConnect:ClientSecret`. For an environment-variable configuration provider, the equivalent key is:

```text
Authentication__OpenIdConnect__ClientSecret
```

Moonglade fails startup when the OIDC authority, client ID, client secret, callback paths, name claim type, or required `openid` scope is invalid. The authority must be an absolute HTTPS URL without a query or fragment.

## Bootstrap the Administrator Allowlist

An empty `AllowedSubjects` list allows OIDC authentication but denies all Admin Portal and admin API access. Use this safe state to discover the first administrator identity:

1. Start Moonglade with the empty list.
2. Open `/auth/signin` and complete sign-in at the configured provider.
3. Open `/auth/identity` in the same browser session.
4. Copy the returned `subject` value exactly into `Authentication:OpenIdConnect:AllowedSubjects`.
5. Restart Moonglade so the updated startup authentication configuration is applied.
6. Sign in again and verify `/admin` access.

The identity endpoint returns only the authenticated user's own `issuer`, `subject`, and `displayName`. It does not grant administrator permission. When local authentication is active, the endpoint returns HTTP 404.

Add one exact `sub` value for each administrator:

```json
"AllowedSubjects": [
  "administrator-subject-1",
  "administrator-subject-2"
]
```

Do not authorize by `email`, `name`, or `preferred_username`; those claims are display values and can change. Some providers use pairwise subject identifiers, so changing the provider or client ID can change the user's `sub` and require an allowlist update.

## Microsoft Entra ID

Microsoft Entra ID remains supported through its standard OIDC endpoints. Use a tenant-specific v2 authority:

```text
https://login.microsoftonline.com/{tenant-id}/v2.0
```

Keep the existing application client ID if possible, add a Web client secret, and register the two callback URLs. The generic client uses authorization code flow with PKCE; the legacy implicit ID-token option is not required for this flow and should remain disabled unless another application still needs it.

Do not use the `common` or `organizations` authority as a shortcut for a single-tenant blog. A tenant-specific authority keeps issuer validation scoped to the intended directory.

## Operational Notes

- Moonglade stores only its encrypted application session cookie. Access and refresh tokens are not persisted in the cookie.
- Provider sign-out is attempted through the OIDC metadata logout endpoint. The local Moonglade cookie is also cleared.
- Forwarded headers must preserve the original HTTPS scheme and host when Moonglade runs behind a reverse proxy, otherwise callback URLs can be generated incorrectly.
- For emergency rollback, set `Authentication:Provider` to `Local` and use the configured local account. Local-account TOTP requirements still apply.
