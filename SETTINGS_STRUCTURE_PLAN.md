# Settings Final Acceptance Plan

## Status And Scope

S6 technical verification passed on 2026-11-07 within the agreed root-path deployment scope. User sign-off and removal of one temporary directory remain pending. Verification started from clean HEAD `d81240a86`; the authorized scheduler fix and regression tests are uncommitted. No commit, deployment, schema migration, production-data operation, or old-avatar cleanup is authorized.

The user accepted the existing root-path deployment limit: URL helpers retain PathBase-aware behavior when a host supplies PathBase, but this host does not mount `/blog` through configuration or forwarded-prefix headers. Full subpath deployment requires separate host-pipeline/client-URL work and is outside S6. This limit is documented in README and the engineering reference.

## Verification Results

| Checkpoint | Evidence | Status |
| --- | --- | --- |
| Environment | Published Debug build on an isolated .NET 10 container, PostgreSQL 18, generated localhost HTTPS certificate, separate public/original image directories and container-local authentication keys. `appsettings.Development.json` was excluded. Chrome 156.0.8078.80 via Playwright CLI 0.1.18; desktop public/admin screenshots reviewed. Mailpit accepted local test mail only. | Pass |
| Automated regression | After the scheduler fix, the full solution with PostgreSQL and SQL Server enabled passed **1465 tests, 0 failed, 0 skipped** across 15 assemblies, in 40.068 seconds. Features: 120 passed; Web: 367 passed. Web build: **0 warnings, 0 errors**. Full test compilation retains three existing CS8602 warnings in `CannonServiceTests.cs` (88/188/226). | Pass |
| Startup and recovery | Fresh install produced eleven setting rows; restart preserved valid JSON/timestamps. Seven live cases passed: valid partial JSON with inactive legacy avatar URL, corrupt ordinary settings, missing manifest, missing account, empty settings, invalid account, and invalid manifest. Failure cases exited nonzero before HTTP/business workers, preserving setting/post hashes. Provider tests passed rollback/retry, uncertain-commit verification, existing empty schemas, and both manual upgrade suites. | Pass |
| Settings and rendering | All nine setting groups loaded through their real pages and saved through the UI with HTTP 204. Public title refreshed on the next render; ordinary HTML exposed no credential fields. Invalid feed count, enum, parsed menu, oversized UTF-8 CSS, and CDN URL returned validation problems. Disabled null menus and later menu restoration refreshed correctly. Paused-operation/render regressions passed in the automated suite. | Pass |
| Authentication and audit | Local sign-in, QR/manual TOTP enrollment, subsequent two-step login, password change with retained session, wrong-password rejection, TOTP reset clearing all three authentication cookies, and enrollment with a new secret passed. Anonymous admin API access returned 401; repeated invalid sign-ins returned 429 with Retry-After. Existing audit/persistence/cache/sign-out fault tests passed. The live host used Local authentication; external OIDC sign-in was not exercised or provisioned, while existing OIDC/configuration tests passed. | Pass |
| Local avatars and article images | Avatar upload returned 200, changed image bytes and preserved all setting JSON/timestamps. Default/local avatars and inactive legacy CDN data rendered correctly at the root. Article upload retained a separate original with a different hash after watermarking; public image delivery/ETag returned 200/304. Article CDN returned 301 while avatar remained local HTTP 200. Gravatar source switching passed. Full subpath hosting is the accepted limit above; URL-helper PathBase regressions passed. | Pass |
| Activation and caches | FOAF/sitemap toggles were verified in both directions across restarts. Local SMTP messages retained the old sender until restart, then changed from Moonglade to S6 Sender to S6 Final. RSS/Atom, all/category feeds refreshed count, summary/full body, CDN formatting and metadata; sitemap canonical prefix refreshed twice. Theme changes/fallback, custom CSS ETag 200/304 and tag counts 0/1/10 passed. The fixed scheduler preserved due work while disabled and published promptly after enabling; already-dispatched work and disable-before-dispatch remain covered by automated tests. | Pass |
| Closeout | Diff and documentation reviewed; containers, anonymous database volumes, temporary network, proxy process and browser session removed. One temporary directory could not be deleted because automatic approval review rejected removal with "blocked by policy". No source changes beyond the authorized scheduler fix. User acceptance remains pending. | Blocked |

## Defect And Fix

The real-host scheduler check reproduced an overdue post remaining scheduled after enabling the worker. The next-time query filtered to future timestamps, so a wake could still select the five-minute fallback delay. The user authorized fixing this in S6.

`GetNextScheduledPostTimeQueryHandler` now selects the earliest nondeleted scheduled post with a timestamp, including overdue work. This also avoids selecting deleted or undated rows indefinitely. Four query regression cases and a SQLite/LiteBus worker integration case were added. The latter uses the actual query and publication command instead of returning an artificial past timestamp from a mock. The rebuilt/re-published live host passed the original reproduction after the fix.

No broad scheduler redesign, new dependency, loader abstraction, or change to in-flight publication cancellation was introduced.

## Reproduction Commands

From the repository root, with Docker running:

```powershell
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --solution src/Moonglade.slnx --no-restore
dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore
```

The live environment used only synthetic test data and `.invalid` mail/domain values. No real SMTP destination or external identity provider was contacted. Browser/host fault cases used the disposable PostgreSQL database; both providers were exercised by the automated suite.

## Evidence And Remaining Work

- Ignored local artifacts: `output/playwright/s6/final-tests.log`, `recovery-results.json`, `public.png`, and `settings-general.png`. Screenshots contain synthetic settings only; enrollment snapshots and console artifacts were removed.
- All task-created Docker containers/volumes/network, the local proxy process and Playwright session were removed. Docker image caches were retained.
- Automatic approval review rejected recursive deletion of `C:/Users/Edi/AppData/Local/Temp/moonglade-s6-062e5864aadc` with only "blocked by policy" as the reason, including after the exact resolved directory was verified. The directory still contains the isolated publish, image files, scripts, test database backup and test-only credentials/certificate. Those credentials belong to destroyed disposable services. Do not print or reuse them; remove the directory manually or when permitted.
- No new product decision is pending. Final user sign-off is pending; the already accepted root-path limit is not an authorization to implement subpath hosting. Commit and deployment remain unauthorized.
- Retain and update [the tracker](SETTINGS_RELIABILITY_PLAN.md) when cleanup, sign-off, or an authorized commit changes status. Do not silently rerun completed checks without a new change or unresolved concern.
