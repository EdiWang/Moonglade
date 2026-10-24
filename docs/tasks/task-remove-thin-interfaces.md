# Remove Thin Interfaces

## Original Goal

Remove interfaces without an independent production responsibility, retaining their concrete classes and behavior. Do not introduce test-driven production abstractions.

## Background

Eight interfaces duplicate one fixed implementation: the four startup services, image naming and validation, comment submission checks, and syndication data access. Their callers can use concrete constructor injection with unchanged lifetimes. Storage, moderation, email, authentication, configuration, and external communication contracts retain their existing boundaries.

## Scope

Remove `IStartUpInitializer`, `IConfigInitializer`, `IMigrationManager`, `ISiteIconBuilder`, `IFileNameGenerator`, `IImageUploadValidator`, `ICommentSubmissionGuard`, and `ISyndicationDataSource`. Update production consumers, service registration, tests, and developer documentation.

## Out of Scope

No behavior, validation, migration, HTTP contract, filesystem root, lifetime, or dependency changes. No new interfaces, virtual methods, factories, or production hooks for tests.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect contracts, implementations, consumers, and tests | None | Search all references | Complete |
| 2 | Replace contracts with concrete classes | 1 | Build and registration checks | Complete |
| 3 | Adapt tests to real implementations | 2 | Web, Setup, ImageStorage, Syndication tests | Complete |
| 4 | Final review and documentation | 3 | Diff and removed-symbol checks | Complete |

## Execution Order

Inspect dependencies, perform mechanical production edits, adapt affected tests at real dependencies, then verify behavior and DI registration.

## Current Progress

Removed all eight contracts and updated consumers and registrations. Production method bodies and service lifetimes are unchanged. Tests now use real concrete classes, generated filenames, persisted startup defaults, and category-filtered feed output. A registration test resolves all eight services through the real Web registration and checks singleton, scoped, and transient behavior. Site icon tests isolate process temporary paths in nonparallel collections and restore them after each test. Updated `AGENTS.md`; README and deployment instructions need no changes.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-24 | Inspect references and repository state | Complete | Clean starting tree. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore --verbosity quiet` | Passed | 251 tests, including concrete DI lifetimes, comments, SVG sanitization, original image isolation, and real icon generation. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj --no-restore --verbosity quiet` | Passed | 39 tests. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj --no-restore --verbosity quiet` | Passed | 25 tests. |
| 2026-10-24 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore --verbosity quiet` | Existing failures | 20 passed, 2 failed migration-script manifest assertions; the adapted startup test passes. |
| 2026-10-24 | Repeat Setup tests with `MigrationManager.cs` from `HEAD`, then restore refactor | Confirmed baseline failures | Same two failures: script manifest is 16.8.1 while application version is 16.9.0-preview.1; actual status is UnsupportedVersion, expected NotRequired. |
| 2026-10-24 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed | Zero warnings or errors. |
| 2026-10-24 | Removed-symbol search and `git diff --check` | Passed | No removed-interface references remain in source or tests; no whitespace errors. |

## Issues and Resolutions

Site icon generation uses a shared temporary cache. Tests now isolate temporary environment paths and run without concurrent collections, avoiding writes to an existing runtime cache without adding production hooks.

The first adapted startup test exposed a Moq callback mismatch between `Task<Dictionary<string, string>>` and `Task<IDictionary<string, string>>`; its callback now returns the query's declared contract. The concrete Atom test checks the serialized owner email instead of mutating a mocked entry list.

Two unchanged migration-script manifest tests fail on the current preview version. Repeating them with the original migration manager reproduced both failures; migration scripts and version policy remain untouched.

Automatic approval rejected cleanup of five process-level temporary test directories with `blocked by policy`; these temporary artifacts remain outside the repository. The temporary directories owned by the final test fixtures are cleaned by their normal disposal.

## Follow-ups

No remaining work for interface removal. The existing migration-script manifest assertions need a separate version-policy review. Browser testing, live database migrations, and external service delivery were not exercised; this change does not alter those paths' behavior.
