# Upgrade LiteBus to 7.0.0

## Original Goal

Upgrade all LiteBus packages to 7.0.0.

## Background and Scope

The Data project references LiteBus and the Web host references its Microsoft DI extension, previously at 6.0.2. Other modules and tests consume these dependencies transitively. Preserve application behavior, configuration, and database schemas; adapt existing test doubles to the updated interface.

## Task Breakdown and Execution Order

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect references, registration, callers, and existing tests | None | Repository search and upstream interface source | Complete |
| 2 | Upgrade both references and resolve required compatibility changes | 1 | Restore and Web build | Complete |
| 3 | Run affected tests and verify resolved LiteBus versions | 2 | Web, Features, Setup tests and dependency graph inspection | Complete |
| 4 | Sync engineering reference and review final diff | 3 | Git diff and status | Complete |

## Current Progress

Both direct references and all 24 resolved LiteBus packages are at 7.0.0. Production code compiled without changes. Eleven existing ICommandMediator test doubles now implement the three new methods; these unused methods fail explicitly with NotSupportedException. Existing SendAsync behavior and assertions remain unchanged. The engineering reference records the new version.

## Verification Log

| Date | Command or check | Result |
| --- | --- | --- |
| 2026-10-28 | Initial Git status and LiteBus reference/caller search | Clean tree; two direct references at 6.0.2 |
| 2026-10-28 | `dotnet restore src/Moonglade.Web/Moonglade.Web.csproj --verbosity minimal` | Passed |
| 2026-10-28 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity minimal` | Passed; zero warnings and errors |
| 2026-10-28 | `dotnet list src/Moonglade.Web/Moonglade.Web.csproj package --include-transitive --no-restore` | All 24 LiteBus packages resolved to 7.0.0 |
| 2026-10-28 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --verbosity quiet` | 251 passed; includes real DI dispatch and command/query/event cache invalidation tests |
| 2026-10-28 | `dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --verbosity quiet` | 112 passed |
| 2026-10-28 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --verbosity quiet` | 15 passed |
| 2026-10-28 | `git diff --check` and final Git status | Passed; only intended package, test, and documentation changes |

## Issues and Resolutions

The first Web and Features test builds failed because LiteBus 7 adds TrySendAsync overloads and EvaluateAsync to ICommandMediator. Updated all eleven direct test implementations using the return types in the upstream v7.0.0 interface and installed package contracts. All three affected test projects subsequently passed.

## Follow-ups

No implementation work remains. The application was not launched and the full test suite was not run. No deployment or production changes were performed.

## References

- [LiteBus v7.0.0 ICommandMediator source](https://github.com/litenova/LiteBus/blob/v7.0.0/src/LiteBus.Commands.Abstractions/ICommandMediator.cs)
