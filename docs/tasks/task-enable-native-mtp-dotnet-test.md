# Enabling Native Microsoft Testing Platform

## Original Goal

Determine whether the repository should enable .NET 10 native `dotnet test` support for Microsoft Testing Platform (MTP), and enable it if appropriate.

## Background

All repository test projects target .NET 10 and reference xUnit v3. MTP v2 rejects the legacy VSTest `dotnet test` path on .NET 10 unless the repository opts into the native MTP runner. Microsoft documentation encourages MTP users to transition to this mode.

## Scope

- Add the repository-level native MTP runner configuration.
- Update test commands in the developer guidance.
- Verify native `dotnet test` works for representative affected projects.

## Out of Scope

- Changing test frameworks, test packages, or test code.
- Pinning the .NET SDK version.
- Adding test-result reporting or coverage extensions.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Confirm .NET 10 and MTP compatibility | None | Inspect SDK, packages, and official documentation | Complete |
| 2 | Enable native MTP runner | 1 | Review `global.json` | Complete |
| 3 | Verify native test commands | 2 | `dotnet test --project` | Complete |

## Execution Order

Confirm compatibility, enable the runner at repository scope, then run representative xUnit test projects through native `dotnet test`.

## Current Progress

Native MTP configuration is enabled and verified by three representative test projects.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-09-07 | Official .NET documentation | Passed | .NET 10 supports native MTP when `global.json` sets `test.runner` to `Microsoft.Testing.Platform`; MTP users are encouraged to transition. |
| 2026-09-07 | Test project inspection | Passed | All test projects use xUnit v3 on `net10.0`. |
| 2026-09-07 | `dotnet test --project` | Passed | Utils (283), Webmention (69), and Features (95); 447 tests total. |

## Issues and Resolutions

The previous VSTest-based `dotnet test` command failed because MTP v2 no longer supports that path on .NET 10. Native MTP configuration resolves the incompatibility.

## Follow-ups

No follow-up work is planned.

## Notes

Native MTP test commands use `dotnet test --project <test-project-path>`.
