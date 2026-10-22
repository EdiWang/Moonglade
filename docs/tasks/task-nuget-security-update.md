# NuGet Security Update

## Original Goal

Resolve NuGet vulnerability warnings using .NET 10 package versions.

## Background and Scope

The solution audit identified only `System.Security.Cryptography.Pkcs`: version 10.0.0 through `Edi.TemplateEmail.Smtp > MailKit > MimeKit`, and version 9.0.11 through `Microsoft.EntityFrameworkCore.SqlServer > Microsoft.Data.SqlClient`. Web and matching test projects inherit these versions.

Explicitly reference version 10.0.13 in the two owning projects. This is the patched .NET 10 release for both reported advisories, GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7. No changes to application logic, database mappings, or unrelated packages are required.

References: [NuGet package](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13), [high severity advisory](https://github.com/advisories/GHSA-846f-f764-76mx), [moderate severity advisory](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

## Task Breakdown and Execution Order

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Audit solution and trace dependency chains | None | NuGet audit and `dotnet nuget why` | Complete |
| 2 | Override vulnerable dependencies in owning projects | 1 | Restore and inspect resolved versions | Complete |
| 3 | Build, test, and repeat solution vulnerability audit | 2 | Web build, Email/Setup/Web tests, solution audit | Complete |

## Current Progress

The working tree was clean at task start; the prior default-editor work was already committed. Added two direct references and documented why they must remain.

Both dependency chains now resolve Pkcs 10.0.13. Verification passed, with no remaining solution vulnerability findings or Web build warnings.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-22 | `dotnet list src/Moonglade.slnx package --vulnerable --include-transitive --format json` | Baseline confirmed | Only Pkcs reported; two advisories |
| 2026-10-22 | `dotnet nuget why` for Email and SQL Server projects | Complete | Located both owning dependency chains |
| 2026-10-22 | `dotnet restore src/Moonglade.slnx --force-evaluate --verbosity minimal` | Passed | No vulnerability warnings |
| 2026-10-22 | Repeat `dotnet nuget why` for both projects | Passed | Both resolve Pkcs 10.0.13 |
| 2026-10-22 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero warnings and zero errors |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj --no-restore` | Passed | 97 tests |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | Passed | 22 tests |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | Passed | 204 tests |
| 2026-10-22 | `dotnet list src/Moonglade.slnx package --vulnerable --include-transitive --format json --no-restore` | Passed | No vulnerable packages reported across all 35 projects |
| 2026-10-22 | `git diff --check` | Passed | Focused project and documentation changes |

## Issues and Resolutions

The vulnerable package was transitive, so upgrading only the Web host would leave independently built libraries and tests vulnerable. The override belongs in each owning module.

## Follow-ups

No implementation work remains. The direct references can be removed once upstream dependencies resolve a patched version without overrides. README deployment instructions are unchanged. No live SMTP or database integration was exercised; existing Email tests include a local SMTP listener.
