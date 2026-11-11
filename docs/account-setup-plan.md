# Account storage and first-run setup plan

## Current status

B1 storage implementation and verification are complete on 2026-11-11, awaiting user acceptance and commit authorization. The user authorized progression from the verified B0 baseline to B1. Auth passed 75 cases; ordinary Setup passed 79 with 28 explicit opt-in skips; full provider-enabled Setup passed 196 with no skips; Web built without warnings or errors. Legacy runtime authentication and Owner Email consumers remain active. B2 has not started.

| Batch | Dependency | Status | Verification | Implementation commit | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0. Establish the baseline | Execution authorization | Verified; progression to B1 authorized | Auth 63 passed; Setup 78 passed, 23 opt-in skips; Web 668 passed; corrected opt-in Setup 169 passed, 0 skipped | Baseline correction and plan committed in `4ad36f2d3` before B1 | Progression authorized 2026-11-11 |
| B1. Add independent account storage | B0 accepted | Implemented and verified; awaiting acceptance | Auth 75 passed; Setup 79 passed, 28 opt-in skips; opt-in Setup 196 passed, 0 skipped; Web build 0 warnings/errors | Uncommitted; explicit commit authorization pending | Pending |
| B2. Switch authentication and enable first-run setup | B1 accepted | Pending | Pending | Pending | Pending |
| B3. Move Owner Email editing and public consumers | B2 accepted | Pending | Pending | Pending | Pending |
| B4. Verify fresh installation and upgrade end to end | B3 accepted | Pending | Pending | Verification gate | Pending |

Intermediate commits are for development and review. The deployable upgrade is the combined result after B4. Administrators apply the final SQL script for their provider before starting that version.

## Confirmed requirements

- The single local account signs in with Owner Email. The independent username and shipped default administrator are removed. Owner Email has one authoritative persisted value in the independent account table. Account data and operations belong to Auth, separate from ordinary settings and their writer.
- On a fresh Local installation or an upgraded Local installation awaiting its new account, any visitor can open Setup Account and create the first administrator. There is no deployment token or installation key. The operator accepts that any reachable visitor may complete setup while it is pending.
- While Local setup is pending, redirect browser page requests to Setup Account, including the homepage and direct links to content. Temporarily return HTTP 503 for business APIs, feeds, and protocol endpoints. Serve setup, its assets, and both health endpoints. Honor path base, HTTPS, localization, antiforgery, and authentication rate limiting.
- Setup requires an email and password. When `Authentication:Totp:Required` is true, require a valid authenticator code before committing credentials. When false, skip TOTP during setup and local sign-in in every environment. The default remains true. After setup succeeds, establish the local admin session and redirect to the public homepage. Later authenticator resets use the existing password-protected re-enrollment flow.
- `Authentication:Provider=OpenIdConnect` bypasses first-run Local setup even on a fresh database. Retain OIDC callbacks, sign-in, sign-out, and the exact `sub` allowlist. OIDC profile or email claims do not determine blog ownership or administrative access.
- Owner Email is public. Keep its existing uses in Atom, FOAF, Gravatar, and notification delivery. Move editing from General Settings to Account. A Local administrator verifies the current password to change it and signs in again with the new email. Keep the bound authenticator. OIDC administrators edit the public address through their authorized OIDC session without a local password.
- Existing sites can reset local credentials. Preserve articles, comments, activity history, the installation manifest, and unrelated settings and data. Preserve an existing OIDC site's public Owner Email. For upgrades, a provider-independent SQL script can preserve the public address as an owner profile without credentials for either mode. A Local operator still completes setup and may confirm or replace that address.
- Keep one active authentication provider and one site owner in this release. Multi-user management, linking third-party identities, and offering multiple sign-in methods together are future work. Repository rules exclude the Java toolchain.

## Implementation boundaries

- Use one typed account table in the existing `BlogDbContext`, with provider-neutral EF mappings, PostgreSQL snake_case, and SQL Server PascalCase. Give the account a stable ID independent of its email. Enforce normalized-email uniqueness and one owner in the database. Keep local credentials optional for an OIDC owner profile. A profile alone does not complete Local setup. A completed Local account stays initialized after an authenticator reset.
- Store credentials only in the account table. Keep the stable ID available for future external identities keyed by issuer and subject. Leave the future association table until that feature is in scope.
- Keep the existing install/manifest and empty-database safeguards. Account absence is a valid setup state. Schema failure and unrelated initialization failures still fail before starting the host. Include the new table in startup emptiness checks and other existing full-database operations.
- Use database-enforced concurrency for initial setup. A failed, expired, or abandoned attempt leaves no usable credentials. A competing completion cannot replace an existing account. Keep pending setup data protected and short-lived, and keep passwords, hashes, salts, and TOTP secrets out of logs, audit details, public data, and redirect URLs.
- Treat local session validation as part of cutover. Reject pre-upgrade local cookies and prevent stale setup or TOTP sessions from completing against a changed account. After a successful email change, existing local sessions must require sign-in with the new address. Preserve OIDC session behavior.
- Expose the public owner address through a read boundary that carries no credentials. Keep General Settings validation and writes independent. Preserve feed and notification operation consistency and refresh cached Atom output after an address change. On a fresh OIDC site before an address is entered, omit email-derived output and skip owner-directed notifications until an address is available.
- Administrators run idempotent PostgreSQL and SQL Server scripts manually. Fresh schema creation remains part of normal startup. Use only disposable databases and synthetic credentials in verification. Production database operations, pushes, and deployment require separate authorization.

## Batches

### B0. Establish the baseline

- Read [AGENTS.md](../AGENTS.md), the applicable [engineering reference](agent-reference.md) sections, and the [database rules](../database/README.md). Review the auth pages, account settings endpoints, setup initialization, owner email readers, and current tests.
- Record the exact starting SHA, working tree, available .NET runtime, Docker/provider-test availability, and an isolated browser test environment. Run the Auth, Setup, and Web suites to identify existing failures and skips.
- **Acceptance:** Baseline and available checks are recorded. Any baseline failure that changes the planned gates has a user decision. This batch requires no source commit.

### B1. Add independent account storage

- Add the account mapping, stable ID, email normalization and constraints, and Auth-owned read/write operations. Define profile-only and initialized account states. Keep legacy runtime behavior active in this preparatory batch so it can be reviewed and reverted independently.
- Add schema creation for existing databases to both manual scripts. Defer the transfer of Owner Email and removal of the old credential row to the final cutover script work in B3. Extend the model, startup emptiness, and database maintenance coverage for the new table.
- **Verify:** Auth and Setup suites, opt-in provider tests for fresh creation, additive script rerun, unique owner and normalized email constraints, and failed or concurrent writes. Compare pre-upgrade content and settings before and after the additive script.
- **Acceptance:** Both providers create the same logical schema, and an empty or profile-only table cannot authenticate a local administrator. A concurrent write cannot create a second owner. Commit the storage and provider tests together.

### B2. Switch authentication and enable first-run setup

- Remove runtime reads and writes of the old credential record and the default account. Switch Local sign-in, password change, authenticator reset, TOTP enrollment and verification to the Auth account operations. Move account management out of the ordinary settings controller and preserve existing password and reset behavior through the new owner. Keep the existing password and authenticator activity events and exclude credentials from audit payloads.
- Add Setup Account and the pending-setup gate in the centralized Web pipeline. Commit the initial email and credentials once, after required proof succeeds. Apply the effective TOTP setting consistently. Cover fresh and upgraded databases, invalid or abandoned proof, concurrent completion, restart, direct routes, old cookies, and OIDC bypass. The General Settings owner-email consumers are switched together in B3. The intermediate build is for review only.
- **Verify:** Auth, Setup and Web suites, Web build, HTTP boundary tests for page redirects, 503 responses, health/static access, antiforgery, rate limiting and OIDC bypass. Use an isolated browser for Local setup with TOTP on and off, including an actual authenticator code, then verify ordinary sign-in and reset.
- **Acceptance:** Required proof gates account creation and the homepage. The disabled-TOTP scenario creates an account and goes to the homepage without an authenticator step. Setup can no longer claim the owner after completion. OIDC still reaches the site and follows the same allowlist policy. Commit the runtime cutover and first-run flow together.

### B3. Move Owner Email editing and public consumers

- Make the account email the only source for application reads and writes. Remove Owner Email from the General model, defaults, form, and POST/PATCH field handling. Change the email notification, Atom, FOAF, profile and Gravatar callers together. Preserve other General settings, including Owner Name. Review feed caching and in-flight operation snapshots.
- Add Owner Email editing to the Account page for both providers. Verify the current password in Local mode, save through Auth, and enforce reauthentication after a real change. Keep TOTP enrollment intact. Update localization and verify admin navigation for both providers.
- Complete the manual SQL scripts and upgrade guide. Transfer only the public owner address to a profile without local credentials, remove that field from the old General JSON while preserving its other values, and remove only the obsolete `LocalAccountSettings` row. Do not import old credentials. Guard reruns so they preserve a configured account and subsequent edits. Document provider prerequisites, backup, stopped-host cutover, and rollback by restoring a backup.
- **Verify:** Configuration, Auth, Setup, Web, Email and Syndication suites, Web build, and provider tests for upgraded and already-upgraded databases. Use warm and cold Atom caches, notifications, FOAF and Gravatar to check the effective address. Verify password rejection, valid email changes, stale session rejection, unaffected TOTP, and OIDC account editing.
- **Acceptance:** Login and all public/notification consumers use the same address. A setup or email change leaves unrelated data intact. Both scripts preserve existing OIDC email and leave Local installations awaiting new credentials. Commit the email cutover, upgrade scripts, and durable documentation together.

### B4. Verify fresh installation and upgrade end to end

- Run the solution, opt-in PostgreSQL and SQL Server fixtures, Web build, and existing frontend behavior tests. Use separate disposable databases for fresh Local, upgraded Local, fresh OIDC, and upgraded OIDC. Run the published upgrade twice on each provider and compare unrelated data before and after. Verify restart and an unsuccessful setup submission without reseeding or overwriting data.
- Use isolated browser acceptance to check the setup screens and redirect behavior, password entry and validation, TOTP on/off, login, email editing, reset/re-enrollment, direct links, and localized rendering. Exercise OIDC using a test provider when available, including the allowlist and callback. Record browser coverage and any skipped live OIDC or provider checks explicitly.
- Update durable instructions in `README.md`, `AGENTS.md`, `docs/agent-reference.md`, `database/README.md`, and the relevant upgrade and test documentation where the new contract affects them. Cross-check the upgrade steps against the actual scripts.
- **Acceptance:** Review evidence from both providers, browser scenarios, data-preservation comparisons, and the final diff. Record the exact accepted SHA and any explicitly accepted residual verification gap. A missing required environment leaves its gate pending. Commit any focused corrections after rerunning their checks.

## Verification commands

Run from the repository root. Record actual totals, skips, warnings and exit codes.

| Check | Command |
| --- | --- |
| Auth | `dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj` |
| Setup | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj` |
| Web | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` |
| Email | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj` |
| Syndication | `dotnet test --project src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj` |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` |
| Solution | `dotnet test --solution src/Moonglade.slnx` |
| Existing frontend behavior | `node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs` |

For provider runs, start Docker and set `$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'` before the Setup or solution command. See the [test guide](../src/Tests/README.md#run-tests). Record each provider independently. The ordinary suite and TestHost results are separate from real database and browser acceptance.

## Resume and update this record

1. Read this file, the latest user instructions, and repository rules. Run `git status --short --branch` and `git log -8 --oneline`. Preserve changes belonging to the user and confirm authorization for the next unfinished batch and its commit.
2. Work on one batch at a time. Trace affected callers and tests, run its checks, then review the diff and status. Once local commits are authorized, make one focused implementation commit per code batch after checks pass. Record the implementation SHA here after it exists.
3. Update the status table and add a dated log entry after each batch, and before pausing on a failed check or unresolved decision. Record the batch, SHA, actual commands and results, skips, observed data or browser evidence, remaining risk, and next step. Include the user's acceptance only after they give it. Wait for acceptance of the current batch before starting the next.
4. If research or implementation raises a decision about behavior, ownership, architecture, or a public contract, ask the user with alternatives, consequences and a recommendation. Record their answer here before doing dependent work. Continue independent authorized work. The user asked to retain this file for handoff. Keep durable documentation independent of it.

## Progress and decision log

- **2026-11-11, planning:** Confirmed public setup without an installation key, page and business-endpoint gating, the all-environment TOTP toggle, public Owner Email, manual legacy account removal, independent account persistence, unified email ownership, and editing under Account. Checked the repository structure, relevant implementation, and test instructions. No implementation or verification run has started. Next action is B0 after execution authorization.

### B0 baseline evidence (2026-11-11)

- Authorization covered B0 only. The starting and final source checkout was `master` at `6c0b9fcbeea7dbaa37da9201095c12d6feffbde2`. The plan was the only working-tree change. No source or tests were edited and no commit was created.
- Environment: Windows x64, .NET SDK 10.0.403, MSBuild 18.9.15, .NET and ASP.NET Core runtimes 10.0.14, Node.js 26.13.0, and the repository's Microsoft.Testing.Platform runner. Docker 29.9.3 was available in Linux x86_64 mode. The existing fixture ran `postgres:18-alpine` and `mcr.microsoft.com/mssql/server:2022-latest` on loopback with disposable test data and removed its containers.
- Browser prerequisites: installed Edge 157.0.4341.42 and Chrome 157.0.8103.39, a trusted ASP.NET Core localhost development certificate, and port 10210 available at the time of the check. For later acceptance, use a loopback-only host, separate browser context, explicit isolated settings, and a disposable database. Browser flows and an OIDC test provider were not exercised during B0. Scope PathBase checks to the new setup routes and redirects because the current host assumes deployment at the URL root.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj` | 63 | 0 | 0 | 0 |
| `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` | 78 | 0 | 23 | 0 |
| `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | 668 | 0 | 0 | 0 |
| `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-build` with `MOONGLADE_TEST_CATEGORY_DATABASES=1` | 167 | 2 | 0 | 2 |

The default Setup run skipped the opt-in relational theories. The opt-in run exercised both providers and expanded the theory rows. Both failures were in [ExistingContent_WithMissingSettingsOrAccount_IsNotModified](../src/Tests/Moonglade.Setup.Tests/Database/StartupInstallationDatabaseTests.cs#L133), with `accountOnly: true` on PostgreSQL and SQL Server. The preceding assertion on the legacy startup failure passed. The settings-count assertion expected 10 and saw 11. The dedicated SEO settings commit `5a0bff243` added another seeded ordinary group and left this expectation unchanged. The failure prevented the later post and comment assertions in those two cases from running.

### B0 follow-up (2026-11-11)

- The user approved correcting the existing test before B1. The test now compares the configuration keys, JSON and modification timestamps before and after startup fails. The existing assertions for startup, the saved post and the saved comment still run for both providers with either all settings removed or only the local account row removed.
- Reran `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` with `MOONGLADE_TEST_CATEGORY_DATABASES=1`. The build-enabled run passed 169 cases with zero failures or skips and exit code 0. This rerun supersedes the two opt-in Setup failures above.
- The verified checkout is `6c0b9fcbeea7dbaa37da9201095c12d6feffbde2` plus the single test correction in the working tree. The correction and this record are uncommitted. B0 verification is complete and awaits acceptance. No B1 work has started.

### B1 independent account storage (2026-11-11)

- The user requested reading this record and starting B1. At the start of this batch, `master` was clean at `4ad36f2d3a950569dfa28205bde8da2b5af2089b`; the prior B0 correction and this record had already been committed. No branch, commit, push, deployment, or production database operation was performed during B1. Implementation and provider tests remain together in the working tree for review; commit authorization is still required.
- Added the typed singleton `OwnerAccount` table with a stable GUID ID, trimmed/public email, invariant-uppercase normalized email, revision concurrency token, optional local credentials and optional bound TOTP secret. Database checks and unique indexes enforce one owner and complete-or-absent local credentials. `LocalInitializedUtc` distinguishes initialized accounts from profiles even after the authenticator is cleared.
- Auth owns the scoped `OwnerAccountStore` and the credential-free `IOwnerProfileReader`. Storage supports creating a public profile, initializing once from an absent/profile-only account, and validating local passwords against the independent row. Initialization retains a profile's stable ID, checks its expected revision, hashes with a fresh salt and commits once. Store writes reject enclosing explicit or ambient transactions, and detach attempted writes on success/failure so a failed credential save cannot reappear in an unrelated save.
- Kept the legacy `LocalAccountSettings`, default administrator, runtime login/TOTP flow, settings writes, and General Owner Email consumers unchanged. No pending setup data, HTTP setup route/gate, cookie cutover, credential migration, or Owner Email transfer was added. Those remain B2/B3 work.
- Added additive, idempotent `owner-account.sql` scripts for PostgreSQL and SQL Server. Fresh creation and manual addition have matching logical columns, keys and indexes. Reruns preserve both profile-only and initialized rows. Guarded unsupported shapes fail without changing account or unrelated data. Startup checks the new table before deciding whether to seed; a missing table fails initialization rather than starting the host. Full-database clearing includes the table.
- Added SQLite Auth coverage for absent/profile-only rejection, normalization and stable identity, initialization, wrong passwords, repeated/stale completion, authenticator reset state, failed/cancelled save cleanup and enclosing-transaction rejection. Added 13 account relational cases per provider (26 total), including deterministic simultaneous writes over separate connections and subsequent unrelated saves. Upgrade snapshots compare every unrelated mapped table, including articles, comments, all settings/timestamps, legacy account JSON and manifest history.
- Adjusted the historical PostgreSQL naming fixture to exclude the new table: it represents the earlier 19-table schema, not the current 20-table model. Updated the README, database script index, engineering reference and test guide for the preparatory boundary.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --project src\Tests\Moonglade.Auth.Tests\Moonglade.Auth.Tests.csproj` (final) | 75 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` (ordinary) | 79 | 0 | 28 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --no-build --filter-class '*OwnerAccountDatabaseTests'` with opt-in (initial) | 16 | 4 | 0 | 2 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --filter-class '*OwnerAccountDatabaseTests'` with opt-in (corrected fixture, before six schema-rejection cases were added) | 20 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` with `MOONGLADE_TEST_CATEGORY_DATABASES=1` (final) | 196 | 0 | 0 | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` | N/A | 0 | N/A | 0 |

- The first Auth build found a misplaced helper inside a `try`/`finally`; it was corrected before the passing runs. The first targeted provider run had four concurrency-test timeouts because reconstructing connections from an already-open connection string omitted its password. The fixture now clones each connection, preserving its synthetic authentication while using independent owned connections, and surfaces pre-barrier task failures. All four concurrency scenarios passed in the corrected targeted run and again in the final full Setup run.
- Final provider evidence: PostgreSQL 13/13 new account cases passed; SQL Server 13/13 passed. Each provider exercised fresh schema creation, additive reruns, profile/credential preservation, unsupported-schema rejection, singleton and normalized-email constraints, incomplete credential rejection, both initial concurrency states, startup preservation and database clearing. The complete opt-in suite also retained all pre-existing migration/startup gates. No build warnings or errors were reported. The Docker fixtures used only synthetic data and uniquely named disposable loopback databases; their containers were removed after the runs.
- Remaining gate: B1 user acceptance and an explicitly authorized implementation commit. The deployable upgrade is still the combined result after B4; do not treat these additive scripts or password-storage checks as acceptance of first-run setup, required TOTP proof, email cutover, live OIDC, or browser behavior. B2 remains pending.
