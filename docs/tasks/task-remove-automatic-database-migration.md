# Remove Automatic Database Migration

## Original Goal

Remove the application's ability to migrate older databases automatically. Future upgrades must be performed by administrators running SQL scripts manually. Discard the existing SQL migration scripts.

## Background

Startup currently injects `MigrationManager`, which runs embedded PostgreSQL or SQL Server SQL before configuration initialization. `AutoDatabaseMigration` controls this behavior. Fresh installations use EF Core `EnsureCreatedAsync` and seed data separately.

## Scope

- Remove migration execution, startup wiring, the configuration switch, embedded resources, and both existing SQL scripts.
- Remove obsolete migration tests and update startup/service registration coverage.
- Document manual upgrades in README, repository guidance, and the test guide.
- Preserve fresh database creation, seeding, settings initialization, and stored installation metadata.

## Out of Scope

New migration scripts, database compatibility checks, schema changes, deployments, and changes to existing databases.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Remove automatic migration implementation and wiring | None | Reference search and Web build | Complete |
| 2 | Update startup regression coverage and remove obsolete tests | 1 | Setup and Web tests | Complete |
| 3 | Synchronize upgrade and development documentation | 1 | Diff review | Complete |

## Execution Order

Complete implementation removal first, then adjust tests and documentation, run verification, and review the final diff.

## Current Progress

Removed the migration manager/result types, startup step and failure mapping, DI registration, configuration switch, embedded-resource declaration, both SQL scripts, dedicated migration tests, and the now-unused Setup test friend-assembly declaration. Startup still creates and seeds fresh databases and initializes settings. Existing-database regression cases cover missing and older manifests, preserving historical post content types and persisted manifest JSON/timestamps. Updated README, AGENTS.md, the engineering reference, and the test guide. Initial working tree was clean.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-28 | Initial `git status --short` and caller/resource search | Complete | No pre-existing user changes |
| 2026-10-28 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings, 0 errors |
| 2026-10-28 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-progress` | Passed | 15 tests, 0 skipped |
| 2026-10-28 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-progress` | Passed | 251 tests, 0 skipped |
| 2026-10-28 | Source/reference search and `git diff --check` | Passed | No remaining migration runner references in source or SQL files; no whitespace errors |

## Issues and Resolutions

Removed the obsolete comment claiming the manifest default triggers an upgrade, preserving its stored-value semantics. Historical task records remain historical.

## Follow-ups

Administrators must back up databases and apply any required provider-specific SQL before starting an upgraded version. No replacement SQL is supplied by this task. No application deployment, real database operation, or PostgreSQL/SQL Server integration migration was performed. The remaining startup tests use EF Core InMemory and do not prove relational schema compatibility.
