# Recover Invalid Configuration JSON

## Original Goal

Fix startup failure when an existing ordinary blog configuration row contains malformed JSON, while preserving valid settings and security.

## Background and Scope

BlogConfig already falls back to defaults, but ConfigInitializer forwards the existing key to an unconditional INSERT, causing a primary-key collision. A JSON null also produces a null runtime setting. The only production caller of AddDefaultConfigurationCommand is startup initialization.

Recover invalid ordinary settings in the same row, preserve valid rows and timestamps, keep missing-setting initialization, and reject corrupt account/manifest values without resetting credentials or installation history. Do not add interfaces, another defaults list, provider-specific SQL, or dependencies.

## Task Breakdown and Dependencies

| No. | Task | Dependency | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add regression coverage | None | Fail against existing behavior | Complete |
| 2 | Repair loader and default persistence | 1 | Configuration and startup tests | Complete |
| 3 | Verify real database startup and restoration | 2 | PostgreSQL and SQL Server LocalDB | Complete |
| 4 | Review and document behavior | 3 | Build, diff, cleanup | Complete |

## Execution Order and Current Progress

Completed on 2026-10-25. BlogConfig accepts only non-null deserialized settings, handles JsonException without swallowing unrelated failures, and blocks invalid account/manifest recovery. The startup default command finds an existing row and replaces its value, inserting only missing rows. The same default JSON snapshot continues to supply runtime and persisted values. Recovery warnings include keys only.

Both real database providers passed startup recovery and restoration. README and AGENTS.md describe the behavior; no schema, dependency, public API, or test-oriented production abstraction was added.

## Verification Log

| Date | Check | Result | Evidence |
| --- | --- | --- | --- |
| 2026-10-25 | Regression tests before fix | Reproduced | Seven Configuration cases and four startup initialization cases fail against the original implementation. |
| 2026-10-25 | Moonglade.Configuration.Tests | 47 passed | Includes invalid text, null/SQL-null/empty values, arrays, primitive values, incompatible fields, protected settings, and replacing an existing row. |
| 2026-10-25 | ConfigInitializerTests | 10 passed | Repairs the same row, preserves all valid settings/timestamps, performs no repeat write, and performs no writes when protected settings are invalid. |
| 2026-10-25 | Full Moonglade.Setup.Tests | 29 passed, 2 existing failures | Existing provider migration-script manifest assertions still expect NotRequired but receive UnsupportedVersion (script 16.8.1 versus application 16.9 preview). These files were not changed. |
| 2026-10-25 | Full Moonglade.Web.Tests | 251 passed | Existing consumers and configuration command registration remain compatible. |
| 2026-10-25 | Web build | Passed | Zero warnings and errors. |
| 2026-10-25 | SQL Server 2025 LocalDB recovery | Passed | Existing isolated `moonglade_tag_qa_20261024`: malformed FeedSettings, null AdvancedSettings, incompatible ContentSettings, and missing NotificationSettings recover together. |
| 2026-10-25 | PostgreSQL recovery | Passed | Existing isolated `moonglade_qa_20261024`: same four cases recover through actual provider registration. Original development database was not modified. |
| 2026-10-25 | Actual row preservation, both providers | Passed | Configuration row count remains 11; seven unaffected rows retain byte-equivalent JSON and original timestamps, including credentials and manifest. |
| 2026-10-25 | HTTP and runtime smoke | Passed | Home, readiness, and RSS return 200 on both providers. Headed Edge confirms runtime content defaults match persisted PageSize 10 and HTML editor. |
| 2026-10-25 | Second startup, both providers | Passed | All 11 rows and timestamps remain identical; logs report no configuration keys to initialize. |
| 2026-10-25 | Protected failure, actual databases | Passed | Invalid LocalAccountSettings in SQL Server and invalid SystemManifestSettings in PostgreSQL produce clear startup errors. An additional bad ordinary setting is not written, and all 11 rows/timestamps remain unchanged. |
| 2026-10-25 | Restoration and cleanup | Passed | Original 11 rows/timestamps restored exactly in both isolated databases. All three QA configuration tables per database removed. Final startup succeeds without repair, home/readiness return 200, and temporary hosts/browser are stopped. |

Temporary scripts, logs, browser state, and `recovered-content-settings.png` are outside the repository under `%LOCALAPPDATA%/Temp/moonglade-config-recovery-20261025`. No credentials or stored sensitive JSON were printed or copied into repository records. Browser plugin was unavailable; installed Playwright CLI drove headed Microsoft Edge. Email delivery was disabled and indexing targets were isolated. No external delivery was exercised.

## Issues and Resolutions

Ordinary JSON corruption can safely use the existing defaults. Corrupt LocalAccountSettings must not enable the shipped default password, and corrupt SystemManifestSettings must not silently reset upgrade state. These cases retain a clear startup error requiring restoration.

Initial SQLCMD invocation inherited conflicting SQL authentication environment options. Test commands removed those options only from their child-process environment and used the authorized LocalDB integrated authentication. No global environment or credentials were changed.

## Follow-ups

Semantic validation of syntactically valid JSON and the separately observed sidebar widget cache TTL are outside this task.
