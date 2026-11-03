# PostgreSQL Snake Case Names

## Original Goal

Use snake_case table and column names with PostgreSQL, retain PascalCase names with SQL Server, and migrate the configured development PostgreSQL database.

## Background

Provider-specific contexts already own database differences. The PostgreSQL provider includes Npgsql's snake case translator. Existing databases are upgraded manually, with idempotent scripts under `database/`.

## Scope

- Convert PostgreSQL table and column mappings without changing C# names, public contracts, or SQL Server mappings.
- Supply PostgreSQL rename SQL and an explicit SQL Server no-op companion, tests, and upgrade documentation.
- Back up the development database, verify migration on a restored copy, then migrate and verify development data.

## Out of Scope

Renaming constraints, indexes, or sequences; automatic migration; production databases; new dependencies.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Update provider mappings and model tests | None | Setup tests and Web build | Complete |
| 2 | Add rename scripts and relational coverage; adapt fresh-schema category tests | 1 | Migration, rerun, collision rollback, fresh schema | Complete |
| 3 | Document provider names and upgrade order | 1, 2 | Diff review | Complete |
| 4 | Back up, restore-test, and migrate development PostgreSQL | 1, 2 | Data fingerprints, schema, keys, sequences, EF reads | Complete |

## Execution Order

Implement mappings, SQL, and tests; document the upgrade; build and run focused tests; back up and rehearse on a restored development copy; apply and verify the development migration. Never store connection credentials or database backups in the repository.

## Current Progress

- Initial working tree was clean.
- Read the EF Core, migration verification, and change impact rules, existing contexts, setup tests, and database documentation.
- Development configuration selects PostgreSQL. No local blog process was running during initial inspection.
- Production access uses EF Core rather than handwritten table/column SQL. Historical category migration fixtures intentionally retain their original identifiers.
- Implementation and development migration are complete. All 19 PostgreSQL tables and their columns now use the provider's snake_case conversion; SQL Server mappings are unchanged.
- Added model checks, relational migration/rerun tests for both providers, PostgreSQL rollback tests, and fresh-schema coverage.
- A complete custom-format backup was restored into a uniquely named disposable PostgreSQL container before migrating development. All rehearsal and test containers were removed afterward.
- Development migration and a second execution both passed data and dependency checks. EF Core subsequently read all 19 tables and post relationships successfully.
- Verified backup retained outside the repository: `C:/Users/Edi/.codex/backups/moonglade/moonglade-before-snake-case-20261103-130038.dump`.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-11-03 | Initial repository and environment inspection | Complete | No credentials printed or copied |
| 2026-11-03 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | Zero warnings and errors |
| 2026-11-03 | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` with `MOONGLADE_TEST_CATEGORY_DATABASES=1` | Passed | 60 passed, zero skipped; disposable PostgreSQL 18 and SQL Server 2022 |
| 2026-11-03 | Restore development backup; run PostgreSQL naming script twice | Passed | All 19 table row fingerprints, keys, indexes, column types, UTC values, and sequence values unchanged |
| 2026-11-03 | Run the naming script twice against development PostgreSQL 18.6 | Passed | Same checks; 342 posts, 794 comments, 11 categories preserved |
| 2026-11-03 | Temporary .NET verifier against migrated development database | Passed | Actual EF Core reads for every mapped entity and post/tag/category joins |

## Issues and Resolutions

- Docker was initially stopped; started Docker Desktop for disposable relational tests and PostgreSQL backup tools.
- Initial development credentials lacked table ownership and access to the category identity sequence. No development schema changes were attempted with that account. The user updated the local configuration to an administrator account, and access checks then passed.
- Real PostgreSQL tests caught the digit boundary in `Base64Data`: Npgsql maps it to `base64data`. The SQL conversion now matches the translator, with an explicit model assertion and full-column relational comparisons.
- The migration test explicitly populates tag associations rather than assuming the seed creates them.
- Backup comparison normalizes session time zone to UTC and sorts serialized rows independently of database collation before hashing. A restored copy and the source then matched exactly.

## Follow-ups

Production migration remains an administrator operation outside this request. Retain the verified backup for rollback. Full browser/UI testing and the complete solution test suite were not run; focused database tests, the Web build, and direct development EF Core reads passed.
