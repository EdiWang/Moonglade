# Local Account TOTP

## Original Goal

Replace the admin portal sign-in captcha for local accounts with TOTP-based verification, including authenticator app QR code setup for both new and existing blog instances.

## Background

Moonglade uses cookie authentication for local admin accounts and stores the single local account in `LocalAccountSettings` within blog configuration. The existing `/auth/signin` Razor Page validates username, password, and a stateless captcha before issuing the admin cookie. The requested design keeps the existing local account model, stores the TOTP secret in `LocalAccountSettings` without encryption, does not add a bootstrap token, and sends both new and existing users through TOTP setup after their first successful password login.

## Scope

- Add TOTP fields to `LocalAccountSettings`.
- Add a local account TOTP service and package dependency.
- Add QR code generation support for authenticator app setup.
- Remove captcha from admin local account sign-in.
- Add `/auth/setup-authenticator` for first-time TOTP enrollment.
- Add focused tests and documentation updates.

## Out of Scope

- Full ASP.NET Core Identity migration.
- Encrypted TOTP secret storage.
- Bootstrap token or first-deployer takeover protection.
- Recovery codes and self-service authenticator reset.
- Removing captcha support from public comments.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect current local authentication and captcha flow | None | Code review | Done |
| 2 | Add TOTP model fields, service, and dependencies | Task 1 | Auth unit tests | Done |
| 3 | Update sign-in and setup Razor Pages | Task 2 | Web build and page tests where practical | Done |
| 4 | Update docs and localization resources | Tasks 2-3 | Resource/build validation | Done |
| 5 | Run focused tests and build | Tasks 2-4 | `dotnet test` / `dotnet build` | Done |

## Execution Order

Start with the reusable authentication model and service, then wire the Web flow, then add tests and documentation. The setup page depends on the TOTP service and configuration fields, while sign-in depends on the temporary setup cookie scheme.

## Current Progress

TOTP fields, the TOTP service, temporary setup cookie scheme, sign-in flow, setup page, QR code generation, account page copy, localization resources, tests, and documentation updates are complete.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-18 | `git status --short` | Clean | Checked before edits |
| 2026-07-18 | `dotnet test src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj` | Passed | 31 tests |
| 2026-07-18 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 107 tests |
| 2026-07-18 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | 0 warnings, 0 errors |

## Issues and Resolutions

- Initial parallel test execution hit transient `obj` file contention. Re-running tests serially resolved it.

## Follow-ups

- Consider recovery codes or an authenticated reset UI later.
- Consider sign-in rate limiting as a separate hardening task.

## Notes

NuGet package choices confirmed on 2026-07-18: `Otp.NET` for TOTP and `QRCoder` for QR code generation.
