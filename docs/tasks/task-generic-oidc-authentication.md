# Generic OpenID Connect Authentication

## Original Goal

Replace the Microsoft Entra ID-specific admin authentication path with one configurable, standards-based OpenID Connect provider while retaining local-account authentication. Implement only a single external provider and do not add OAuth-only provider adapters or a multi-provider login selector.

## Background

Moonglade currently uses cookie authentication for the application session and `Microsoft.Identity.Web` for the Entra ID OpenID Connect challenge. The Entra-specific dependency and configuration are isolated mainly in `Moonglade.Auth`, while the Web project directly selects the OpenID Connect scheme for sign-in and sign-out. Admin Razor Pages and API controllers currently require only an authenticated principal.

The approved design replaces `Authentication:Provider=EntraID` and `Authentication:EntraID` with `Authentication:Provider=OpenIdConnect` and a vendor-neutral `Authentication:OpenIdConnect` section. It uses the ASP.NET Core OpenID Connect handler with authorization code flow, PKCE, a confidential-client secret, explicit claims mapping, and an allowlist of OIDC `sub` identifiers. The configured authority and standard token validation establish the issuer boundary. An empty allowlist is a safe bootstrap state that denies all admin access.

## Scope

- Replace the Entra ID provider enum/configuration branch with one generic OpenID Connect provider.
- Replace `Microsoft.Identity.Web` with the ASP.NET Core OpenID Connect package.
- Validate required OpenID Connect configuration at startup.
- Require an allowlisted OIDC subject for all admin Razor Pages and admin API controllers while preserving local-account administrator access.
- Update sign-in, sign-out, configuration examples, repository guidance, upgrade documentation, and focused tests.

## Out of Scope

- Multiple simultaneous external identity providers or a provider-selection UI.
- OAuth-only provider adapters such as GitHub or X.
- ASP.NET Core Identity, external-user persistence, account linking, or database schema changes.
- Calling downstream APIs or storing access/refresh tokens.
- Provider-specific group, role, or tenant authorization extensions.
- Backward-compatible support for the old `EntraID` provider/configuration keys.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Define and validate the generic OIDC configuration contract | None | Auth unit tests | Complete |
| 2 | Register cookie/OIDC handlers and administrator policy | 1 | Auth unit tests and DI inspection | Complete |
| 3 | Update Web sign-in, sign-out, and authorization boundaries | 2 | Web unit tests | Complete |
| 4 | Replace package and application configuration | 1 | Restore and build | Complete |
| 5 | Update README, AGENTS, and upgrade guidance | 1-4 | Documentation review and searches for stale Entra-specific behavior | Complete |
| 6 | Run focused tests and Web build | 1-5 | xUnit v3 runner and `dotnet build` | Complete |

## Execution Order

Define the configuration and policy contract first, then update authentication registration. Change the Web authentication entry points and authorization boundaries after the schemes and policy exist. Update packages and operator documentation before running focused tests and a Web project build.

## Current Progress

Implementation and verification are complete. Generic OIDC registration, conditional configuration validation, the OIDC subject allowlist policy, Web sign-in/sign-out changes, package replacement, documentation, and the approved `/auth/identity` bootstrap endpoint are implemented. No database change is required.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-24 | `git status --short` | Passed | Worktree was clean before implementation. |
| 2026-08-24 | `dotnet list src/Moonglade.Auth/Moonglade.Auth.csproj package --include-transitive` | Passed | Confirmed `Microsoft.Identity.Web` and its Azure/MSAL dependency graph. |
| 2026-08-24 | `dotnet build src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj` | Passed | 0 warnings and 0 errors. |
| 2026-08-24 | Auth xUnit v3 test executable | Passed | 45 tests passed. |
| 2026-08-24 | `dotnet build src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 0 warnings and 0 errors. |
| 2026-08-24 | Web xUnit v3 test executable | Passed | 190 tests passed. |
| 2026-08-24 | Auth xUnit v3 test executable after bootstrap endpoint work | Passed | 48 tests passed. |
| 2026-08-24 | Web xUnit v3 test executable after bootstrap endpoint work | Passed | 193 tests passed. |
| 2026-08-24 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings and 0 errors. |
| 2026-08-24 | `git diff --check` | Passed | No whitespace errors; Git reported expected CRLF normalization notices. |
| 2026-08-24 | Stale implementation reference search | Passed | No Entra-specific enum, scheme, or registration calls remain in production code or active documentation. |

## Issues and Resolutions

- Resolved the allowlist bootstrap requirement by allowing an empty deny-all list and exposing `/auth/identity` to authenticated OIDC users. The endpoint does not use the administrator policy and returns 404 in local mode.

## Follow-ups

- OAuth-only providers can be evaluated later through explicit adapters or OpenIddict WebIntegration if a concrete provider requirement emerges.
- Multiple simultaneous OIDC providers require a separate scheme-selection and logout design and are intentionally deferred.

## Notes

- The OIDC client secret must remain outside committed configuration files.
- The allowlist uses the stable standard `sub` claim. Human-readable `email`, `name`, and `preferred_username` claims must not authorize admin access.
- The configured authority is restricted to HTTPS and the standard handler retains issuer, audience, signature, lifetime, correlation, state, and nonce validation.
