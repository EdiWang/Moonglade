# Remote Development PostgreSQL Migration

## Original Goal

Move the local development `moonglade` database from SQL Server LocalDB to PostgreSQL 18 on the user's development server and switch the local Development application configuration to it.

## Background

The LocalDB database already contained the five-reply cleanup and the v16.7 cascade constraint. The remote PostgreSQL server had no `moonglade` database. An unrelated `elfdev` database was present and had to remain untouched.

## Scope

Copy all application tables and data into a new remote `moonglade` database, preserve identity sequences, verify the transfer, and update the ignored local Development configuration. Use SSH key authentication and avoid placing database credentials or content in the repository.

## Out of Scope

No production database, original BACPAC, existing remote database, or filesystem image root was changed.

## Task Breakdown

| Task | Dependency | Result |
| --- | --- | --- |
| Inspect LocalDB and remote PostgreSQL | None | 19 source tables, no reply orphans; PostgreSQL 18.6 running with no `moonglade` database. |
| Copy and verify locally | Inspection | Current EF Core PostgreSQL schema created; 6,522 complete rows copied and compared across all columns. |
| Restore to remote staging database | Local verification | Custom-format dump transferred over SSH and restored successfully. |
| Validate and rename | Remote restore | All 19 row counts matched; constraints and reply cascade were valid; staging database renamed to `moonglade`. |
| Switch Development connection | Remote validation | A dedicated non-superuser role was granted application table and sequence permissions; the ignored local configuration now selects PostgreSQL. |
| Run application smoke checks | Configuration switch | Database readiness, home page, and date-filtered search returned HTTP 200. |
| Remove temporary data copies | Final validation | Uploaded and local dump files removed; temporary local PostgreSQL container removed. |

## Execution Order And Dependencies

The current PostgreSQL EF model created an isolated local PostgreSQL 18 schema. A temporary C# transfer program matched table and column names, inserted tables in foreign-key order, converted SQL Server UTC timestamps and dates, and reset the three identity sequences. Per-row SHA-256 comparisons covered every column. A custom-format PostgreSQL dump was then copied to the development server over SSH, restored into a new staging database, checked, and renamed to `moonglade`.

The local `appsettings.Development.json` is ignored by Git. Only its database connection and provider entries were changed. A random password for the dedicated application role is stored there; the server administrator credential was not copied. The Development application was launched with external update checks and email delivery disabled for smoke testing, then stopped.

## Status And Verification Log

Completed on 2026-09-20. The source LocalDB database and unrelated remote databases remain available.

| Check | Result |
| --- | --- |
| SSH server identity | Matched the fingerprint already present in the local OpenSSH known-hosts file. |
| Local transfer | 19 tables and 6,522 rows; all-column row comparison passed. |
| Dump integrity | Local and remote SHA-256 values matched before restore. |
| Remote restore | Completed without errors into a new staging database. |
| Remote table counts | All 19 counts matched the source. |
| Foreign keys | Six foreign keys, zero unvalidated constraints, zero orphan replies; reply foreign key uses cascade deletion. |
| Manifest and sequences | Manifest reports 16.7.0; identity sequence values match the transferred table maxima. |
| Final database | `moonglade` exists and contains 74 replies. TCP port 5432 is reachable from the development machine. |
| Application role | Non-superuser; application table read/write and identity-sequence usage permissions verified. |
| Application smoke | `/health/ready`, `/`, and a date-filtered `/search` request each returned HTTP 200. |

## Issues And Resolutions

PuTTY `plink` could not use the configured SSH key. OpenSSH succeeded with strict host-key checking against the existing trusted fingerprint. No SSH password was requested. The remote PostgreSQL server currently has SSL disabled, so the application database connection uses the internal network without TLS; enable PostgreSQL TLS or an SSH tunnel if transport encryption is required.

## Follow-Ups

Image files are stored outside the database and were not included in this transfer.
