# Site Verification Files

## Original Goal

Add a Docker-friendly way for blog administrators to make site ownership verification files available from the website root without editing files inside `wwwroot`.

## Background

Moonglade is an ASP.NET Core Razor Pages and controller-based application. Static files are served before endpoint routing, so real files under `wwwroot` should keep priority. The selected approach stores small text verification files in the database and maps strict root-level virtual file endpoints for public access.

## Scope

- Add persistence for root-level site verification files.
- Add feature-layer commands and queries for managing verification files.
- Add a public root-level GET/HEAD endpoint for enabled verification files.
- Add authorized admin API, Razor UI, and JavaScript for list/create/update/delete/enable-disable.
- Add cache invalidation, activity logging, tests, and documentation.

## Out of Scope

- Writing to the container image's `wwwroot` directory.
- Supporting subdirectories or arbitrary binary files.
- Adding external object storage for generic files.
- Changing existing static file priority or public protocol endpoints.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Record task decisions | User confirmation | Task file exists | Done |
| 2 | Add data entity/configuration and DbContext wiring | 1 | Build/tests compile model | Done |
| 3 | Add feature commands, queries, validator, and DTOs | 2 | Feature tests | Done |
| 4 | Add public root endpoint with cache validators | 3 | Web endpoint tests | Done |
| 5 | Add admin API, UI, and JS | 3 | Web tests/build | Done |
| 6 | Update localization and docs | 4, 5 | Build and resource checks | Done |
| 7 | Run verification | 2-6 | dotnet build/test | Done |

## Execution Order

Implement the persistence and feature layer first, then map the public endpoint and admin API. Add UI and localization after API shape is stable. Finish with focused tests, documentation, and verification commands.

## Current Progress

Decisions confirmed by user:

- Text files only: `.txt`, `.html`, `.htm`, `.xml`, `.json`.
- Maximum file content size: 64 KB.
- Root-level file names only, limited to ASCII letters, digits, dots, underscores, and hyphens.
- Existing `wwwroot` static files keep priority.
- Admin page path: `/admin/settings/verification-files`.
- Support both file upload and manual file name/content entry.

Implemented so far:

- Added `SiteVerificationFileEntity`, EF configuration, provider-specific PostgreSQL timestamp override, `DbSet`, and clear-all-data cleanup.
- Added feature-layer validation constants, DTOs, list/get/public queries, and create/update/delete/toggle commands.
- Added public root-level GET/HEAD handler with cache headers, ETag, Last-Modified, and 304 support.
- Added authorized admin API, `/admin/settings/verification-files` UI, Alpine module, localized resource entries, activity log events, and SQL Server/PostgreSQL migration script blocks.
- Added Feature and Web tests for verification file management and public handler behavior.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-09 | Initial repository inspection | Passed | Read `AGENTS.md`, endpoint pipeline, existing asset/image patterns, and test structure. |
| 2026-08-09 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | Final build passed with 0 warnings and 0 errors. |
| 2026-08-09 | `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore` | Passed | 95 tests passed after switching delete test to SQLite for `ExecuteDeleteAsync`. |
| 2026-08-09 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | Passed | 163 tests passed. |
| 2026-08-09 | `dotnet test src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | Passed | 18 tests passed after adding provider migration script blocks. |

## Issues and Resolutions

- EF Core InMemory does not support `ExecuteDeleteAsync`; the delete handler test uses SQLite in-memory, matching existing repository patterns for set-based delete tests.

## Follow-ups

None yet.

## Notes

The endpoint must avoid a broad root catch-all. It should only match one segment with an allowed extension and should preserve existing routes such as `/robots.txt`, `/sitemap.xml`, `/manifest.webmanifest`, `/opensearch`, `/admin`, `/api`, `/auth`, `/image`, and existing static files.
