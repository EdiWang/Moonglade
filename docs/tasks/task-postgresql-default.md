# PostgreSQL Default

## Original Goal

Make PostgreSQL the default database throughout the application setup and guidance.

## Background

Compose and the Development override already select PostgreSQL. The base application configuration still selects SQL Server, and the README presents SQL Server as the primary development database. Both providers are supported by `AddMoongladeDatabase`.

## Scope

Update base configuration, the default-facing README and agent guidance, and the optional Azure SQL deployment script so it selects its provider explicitly.

## Out of Scope

Changing database schema or data and removing SQL Server support.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Select PostgreSQL in base configuration | None | Parse configuration | Done |
| 2 | Preserve explicit provider selection in the Azure SQL example | 1 | Inspect script settings | Done |
| 3 | Align setup guidance | 1-2 | Review diff | Done |
| 4 | Verify build, Compose, and deployment script | 1-3 | CLI checks | Done |

## Execution Order

Change the base configuration, make the SQL Server deployment example explicit, align documentation, then verify the resulting configuration and build.

## Current Progress

Base configuration, Azure deployment settings, and documentation updated and verified.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-09-23 | Parse `appsettings.json` and assert provider/host | Passed | PostgreSQL on localhost |
| 2026-09-23 | `docker compose config --quiet` | Passed | Compose configuration valid |
| 2026-09-23 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings, 0 errors |
| 2026-09-23 | Parse Azure deployment script | Passed | No PowerShell syntax errors |
| 2026-09-23 | Assert Azure provider override | Passed | Explicit `SqlServer` setting present |
| 2026-09-23 | `git diff --check` | Passed | No whitespace errors |

## Issues and Resolutions

The optional Azure deployment script relied on the old SQL Server default. It now selects `SqlServer` explicitly.

## Follow-ups

None.
