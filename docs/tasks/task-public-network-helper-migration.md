# Public Network Helper Migration

## Original Goal

Move Moonglade's public-IP and Webmention network-safety logic into Edi.AspNetCore.Utils, consolidate the overlapping helpers, and make Moonglade consume only the shared library implementation.

## Background

Moonglade currently implements public-address classification, DNS validation, redirect validation, and socket pinning in `Moonglade.Webmention`. Edi.AspNetCore.Utils contains narrower and inconsistent IP checks in `ClientIPHelper` and `SecurityHelper`. The approved business definition of a public IP is an ordinary Internet address; loopback, private, link-local, documentation, reserved, multicast, transition, tunneling, and other special-use addresses are not public.

The user approved a breaking Edi.AspNetCore.Utils 2.0.0 release, full migration of generic network-safety components, trusted forwarded-header handling, and protection for both incoming and outgoing Webmention traffic. The user will publish the NuGet package; this task only changes code and verifies with a locally packed package.

## Scope

- Add a public `IsPublicIPAddress` API covering IPv4, IPv6, and IPv4-mapped IPv6.
- Refactor `ClientIPHelper` to use ASP.NET Core's processed remote IP rather than raw forwarding headers.
- Make `UseSmartXFFHeader` trust only explicitly configured proxies.
- Replace the old private-IP implementation used by `SecurityHelper`.
- Add generic public-HTTP URL validation, DNS resolution, and socket-pinned HTTP handler components to Edi.AspNetCore.Utils.
- Protect incoming source fetches and outgoing Webmention discovery/submission in Moonglade.
- Remove Moonglade's local public-IP and safe-handler implementation.
- Update package documentation, Moonglade documentation, and tests.

## Out of Scope

- Publishing Edi.AspNetCore.Utils to NuGet.
- Changing Webmention protocol response contracts or post URL behavior.
- Adding organization-specific blocked public network configuration.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Record scope and inspect both HTTP call chains | None | Code inspection | Complete |
| 2 | Implement shared IP and HTTP safety components | 1 | Edi.AspNetCore.Utils tests | Complete |
| 3 | Refactor client IP and forwarded-header trust | 1 | Unit/integration tests | Complete |
| 4 | Migrate incoming and outgoing Webmention flows | 2 | Moonglade.Webmention tests | Complete |
| 5 | Pack locally and verify Moonglade against 2.0.0 | 2-4 | Build and focused/full tests | Complete |
| 6 | Synchronize long-lived documentation | 2-5 | Documentation review | Complete |

## Execution Order

Implement and test the helper library first, then create a local 2.0.0 package. Update Moonglade to consume that package through a temporary local NuGet source used only for verification. Finish with regression tests and documentation sync.

## Current Progress

Implementation is complete. Edi.AspNetCore.Utils 2.0.0 now owns public IPv4/IPv6 classification, public HTTP URL validation, DNS resolution, socket-pinned safe HTTP handlers, processed client-IP access, and explicit forwarded-proxy trust. Moonglade uses those services for incoming source checks and redirects plus outgoing target discovery, redirects, endpoint validation, and submission. The duplicate Webmention implementation and its helper-specific tests were removed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-15 | `dotnet test src/Edi.AspNetCore.Utils.Tests/Edi.AspNetCore.Utils.Tests.csproj --no-restore` | Passed | 364 tests |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Webmention.Tests/Moonglade.Webmention.Tests.csproj --no-restore` | Passed | 84 tests |
| 2026-08-15 | `dotnet test src/Edi.AspNetCore.Utils.Tests/Edi.AspNetCore.Utils.Tests.csproj --no-restore` | Passed | 349 refactored tests |
| 2026-08-15 | `dotnet pack src/Edi.AspNetCore.Utils/Edi.AspNetCore.Utils.csproj -c Debug` | Passed | Created local 2.0.0 package only; nothing was published |
| 2026-08-15 | Moonglade restore using a temporary local 2.0.0 source and isolated package cache | Passed | Confirmed the unpublished package is consumable |
| 2026-08-15 | `dotnet test src/Tests/Moonglade.Webmention.Tests/Moonglade.Webmention.Tests.csproj --no-restore` | Passed | 69 tests after moving shared helper tests to the package |
| 2026-08-15 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings, 0 errors |
| 2026-08-15 | `dotnet test src/Moonglade.slnx --no-restore` | Partial | 1,101 passed; 2 unrelated relational migration tests could not start because Docker was unavailable |
| 2026-08-15 | IANA special-purpose registries and ASP.NET Core 10 forwarded-header guidance review | Passed | Classification and trusted-proxy behavior align with authoritative guidance |

## Issues and Resolutions

- Existing helper tests classified documentation ranges as public. They were replaced with ordinary-Internet expectations and broader IPv4/IPv6 coverage.
- `SocketsHttpHandler.UseProxy` defaults to enabled. The shared safe handler disables proxy use so validation applies to the actual destination.
- An explicitly configured but entirely invalid proxy list could become unsafe if both trust lists were cleared. Validation now occurs first; defaults are retained unless at least one explicit proxy address is valid.
- Outgoing discovery previously relied on automatic redirects. It now follows at most five redirects manually, validates every hop, and resolves relative endpoints against the final public page.
- The full solution test run could not execute two Docker-backed migration tests because Docker Desktop was unavailable. All 1,101 non-container tests passed.

## Follow-ups

- The user must publish Edi.AspNetCore.Utils 2.0.0 before other environments can restore the updated Moonglade package reference normally.

## Notes

- IPv6 details remain internal to the helper. Consumers use the same `IsPublicIPAddress` API for both address families.
- The safe handler is authoritative for DNS rebinding protection because it connects directly to a checked address.
