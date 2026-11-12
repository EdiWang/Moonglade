# Account storage and first-run setup plan

## Current status

B0 through B4 are complete. The user accepted B4 on 2026-11-11 and requested task closure. The accepted checkout was `master` at `b9138db04bf350d67d3226eb8c36a8b990340517`. The B4 evidence below distinguishes the end-to-end release checks from the subsequent UI checks. Retain this record as requested.

| Batch | Dependency | Status | Verification | Implementation commit | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0. Establish the baseline | Execution authorization | Verified; progression to B1 authorized | Auth 63 passed; Setup 78 passed, 23 opt-in skips; Web 668 passed; corrected opt-in Setup 169 passed, 0 skipped | Baseline correction and plan committed in `4ad36f2d3` before B1 | Progression authorized 2026-11-11 |
| B1. Add independent account storage | B0 accepted | Accepted; development database migrated | Auth 75 passed; Setup 79 passed, 28 opt-in skips; opt-in Setup 196 passed, 0 skipped; Web build 0 warnings/errors | User-created commit `9a36a68d2` | Accepted 2026-11-11 |
| B2. Switch authentication and enable first-run setup | B1 accepted | Accepted | Auth 108 passed; Web 733 passed; full opt-in Setup 193 passed, 0 skipped; frontend 42 passed; Web build 0 warnings/errors | User-created commit `89a6dc1ff` | Accepted 2026-11-11 |
| B3. Move Owner Email editing and public consumers | B2 accepted | Accepted; development database migrated | Configuration 311, Auth 133, Web 750, Email 113, Syndication 18, full opt-in Setup 231 passed; frontend 42 passed; Web build 0 warnings/errors; development cutover/repeat verified | User-created commit `48de287c4` | Accepted 2026-11-11 |
| B4. Verify fresh installation and upgrade end to end | B3 accepted | Complete | Solution 2,211 and frontend 47 passed. Eight disposable runtime scenarios and TOTP/OIDC browser flows passed. Follow-up UI checks passed. | B4 correction and docs `5c4f3cb8e`. Final accepted checkout `b9138db04`. | Accepted 2026-11-11 |

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

For temporary migration verification, use disposable databases and follow the [migration guide](../database/README.md#verification-and-execution). Record each provider independently. The ordinary suite and TestHost results are separate from real database and browser acceptance.

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
| `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-build` with the former provider suite enabled | 167 | 2 | 0 | 2 |

The default Setup run skipped the opt-in relational theories. The opt-in run exercised both providers and expanded the theory rows. Both failures were in `ExistingContent_WithMissingSettingsOrAccount_IsNotModified`, with `accountOnly: true` on PostgreSQL and SQL Server. The preceding assertion on the legacy startup failure passed. The settings-count assertion expected 10 and saw 11. The dedicated SEO settings commit `5a0bff243` added another seeded ordinary group and left this expectation unchanged. The failure prevented the later post and comment assertions in those two cases from running.

### B0 follow-up (2026-11-11)

- The user approved correcting the existing test before B1. The test now compares the configuration keys, JSON and modification timestamps before and after startup fails. The existing assertions for startup, the saved post and the saved comment still run for both providers with either all settings removed or only the local account row removed.
- Reran `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` with the former provider suite enabled. The build-enabled run passed 169 cases with zero failures or skips and exit code 0. This rerun supersedes the two opt-in Setup failures above.
- The verified checkout is `6c0b9fcbeea7dbaa37da9201095c12d6feffbde2` plus the single test correction in the working tree. The correction and this record are uncommitted. B0 verification is complete and awaits acceptance. No B1 work has started.

### B1 independent account storage (2026-11-11)

- The user requested reading this record and starting B1. At the start of this batch, `master` was clean at `4ad36f2d3a950569dfa28205bde8da2b5af2089b`; the prior B0 correction and this record had already been committed. No branch, commit, push, deployment, or production database operation was performed during B1. Implementation and provider tests remain together in the working tree for review; commit authorization is still required.
- Added the typed singleton `OwnerAccount` table with a stable GUID ID, trimmed/public email, invariant-uppercase normalized email, revision concurrency token, optional local credentials and optional bound TOTP secret. Database checks and unique indexes enforce one owner and complete-or-absent local credentials. `LocalInitializedUtc` distinguishes initialized accounts from profiles even after the authenticator is cleared.
- Auth owns the scoped `OwnerAccountStore` and the credential-free `IOwnerProfileReader`. Storage supports creating a public profile, initializing once from an absent/profile-only account, and validating local passwords against the independent row. Initialization retains a profile's stable ID, checks its expected revision, hashes with a fresh salt and commits once. Store writes reject enclosing explicit or ambient transactions, and detach attempted writes on success/failure so a failed credential save cannot reappear in an unrelated save.
- Kept the legacy `LocalAccountSettings`, default administrator, runtime login/TOTP flow, settings writes, and General Owner Email consumers unchanged. No pending setup data, HTTP setup route/gate, cookie cutover, credential migration, or Owner Email transfer was added. Those remain B2/B3 work.
- Added additive, idempotent `6.owner-account.sql` scripts for PostgreSQL and SQL Server. Fresh creation and manual addition have matching logical columns, keys and indexes. Reruns preserve both profile-only and initialized rows. Guarded unsupported shapes fail without changing account or unrelated data. Startup checks the new table before deciding whether to seed; a missing table fails initialization rather than starting the host. Full-database clearing includes the table.
- Added SQLite Auth coverage for absent/profile-only rejection, normalization and stable identity, initialization, wrong passwords, repeated/stale completion, authenticator reset state, failed/cancelled save cleanup and enclosing-transaction rejection. Added 13 account relational cases per provider (26 total), including deterministic simultaneous writes over separate connections and subsequent unrelated saves. Upgrade snapshots compare every unrelated mapped table, including articles, comments, all settings/timestamps, legacy account JSON and manifest history.
- Adjusted the historical PostgreSQL naming fixture to exclude the new table: it represents the earlier 19-table schema, not the current 20-table model. Updated the README, database script index, engineering reference and test guide for the preparatory boundary.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --project src\Tests\Moonglade.Auth.Tests\Moonglade.Auth.Tests.csproj` (final) | 75 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` (ordinary) | 79 | 0 | 28 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --no-build --filter-class '*OwnerAccountDatabaseTests'` with opt-in (initial) | 16 | 4 | 0 | 2 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --filter-class '*OwnerAccountDatabaseTests'` with opt-in (corrected fixture, before six schema-rejection cases were added) | 20 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` with the former provider suite enabled (final) | 196 | 0 | 0 | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` | N/A | 0 | N/A | 0 |

- The first Auth build found a misplaced helper inside a `try`/`finally`; it was corrected before the passing runs. The first targeted provider run had four concurrency-test timeouts because reconstructing connections from an already-open connection string omitted its password. The fixture now clones each connection, preserving its synthetic authentication while using independent owned connections, and surfaces pre-barrier task failures. All four concurrency scenarios passed in the corrected targeted run and again in the final full Setup run.
- Final provider evidence: PostgreSQL 13/13 new account cases passed; SQL Server 13/13 passed. Each provider exercised fresh schema creation, additive reruns, profile/credential preservation, unsupported-schema rejection, singleton and normalized-email constraints, incomplete credential rejection, both initial concurrency states, startup preservation and database clearing. The complete opt-in suite also retained all pre-existing migration/startup gates. No build warnings or errors were reported. The Docker fixtures used only synthetic data and uniquely named disposable loopback databases; their containers were removed after the runs.
- Remaining gate: B1 user acceptance and an explicitly authorized implementation commit. The deployable upgrade is still the combined result after B4; do not treat these additive scripts or password-storage checks as acceptance of first-run setup, required TOTP proof, email cutover, live OIDC, or browser behavior. B2 remains pending.

### B1 acceptance and development migration (2026-11-11)

- The user accepted B1, committed it as `9a36a68d2`, and requested the local development database migration followed by B2. The checkout was clean at that commit before B2 edits. No agent-created commit, push or deployment was authorized or performed.
- The configured development PostgreSQL endpoint was not loopback, so the exact target was confirmed with the user before writing. There were no other database connections. A private full custom-format `pg_dump` backup, `development-before-owner-account-20261111-085637.dump`, was retained outside the repository. It contains real development data and must not be published or committed.
- Applied the PostgreSQL additive `6.owner-account.sql` twice using an ephemeral PostgreSQL 18 client. Both runs succeeded; row counts and server-side content digests of all 19 unrelated tables were unchanged after each run. The independent account table was empty afterward. The application was not launched against this database during migration or subsequent browser acceptance.

### B2 runtime cutover (2026-11-11)

- Removed legacy credential parsing, defaults, publication and runtime writes. New installations seed ten ordinary configuration groups and one manifest, never credentials. Existing legacy credential JSON remains inert; missing independent account data is valid setup state, while unrelated installation safeguards remain.
- Local email sign-in, password changes, reset and authenticator proof now use scoped Auth storage with stable ID/revision cookies. Initial proof is staged only in an encrypted ten-minute ticket and credentials commit after required TOTP succeeds. Added public Setup Account and centralized pending-page/API/feed gating. Account actions moved from Settings to Account; General Owner Email and all of its public consumers remain unchanged for B3.
- Automated verification and isolated browser acceptance are complete. Browser checks used synthetic credentials, disposable loopback PostgreSQL databases, an isolated HTTPS host and separate browser contexts, never the configured development database.
- The first browser pass exposed fingerprinted static assets being blocked by the gate; physical static-file middleware alone did not serve mapped fingerprint routes. The endpoint mapping now short-circuits static assets. After rerunning, the real six-digit SHA-1 TOTP proof committed one initialized owner and opened the public homepage; unsuccessful earlier submissions had written no account. Completed setup could no longer reopen the form. Password changes retained the current session, reset signed out, a new real TOTP code completed re-enrollment, and ordinary email/password plus TOTP sign-in reached the dashboard.
- Browser acceptance also exposed an antiforgery mismatch when directly opening Sign In while authenticated: sign-out removed the cookie but left the old request principal in the rendered form token. Sign In now clears that principal after sign-out. The corrected published Production flow passed authenticated GET Sign In followed by a valid POST.
- With TOTP disabled, a fresh published Production installation completed account setup directly to the public homepage and ordinary sign-in directly to the dashboard. Setup CSS and modules returned nonempty bodies with the correct MIME types; health endpoints stayed HTTP 200 and RSS was HTTP 503 before setup. Restart retained the current admin session, one initialized account without a TOTP secret, all eleven configuration rows and the single seeded post. Traditional Chinese setup and account wording rendered correctly. The initial unpublished Debug-in-Production check had unavailable static-web-asset providers; it was superseded by this actual published-runtime run.
- Live external OIDC browser authentication has not been exercised; automated bypass, callback/sign-out behavior and exact-sub allowlist checks passed. B2 remains uncommitted and awaits user acceptance; B3 has not started.
- Initial automated results: Auth 108 passed; ordinary Setup 75 passed, 1 failed, 28 opt-in skips; the 70 cases in modified PostgreSQL/SQL Server fixtures passed; Web 730 passed and 3 failed. The failures correctly exposed two production defects: an unknown-only configuration row bypassed the existing-installation guard, and GET requests cached an unlimited rate limiter under the POST partition key. The guard now recognizes only typed ordinary-group or manifest keys, and unlimited requests have their own partition namespace. Complete Web and full opt-in Setup reruns passed without weakening those regressions; the final results below supersede these intermediate failures.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --project src\Tests\Moonglade.Auth.Tests\Moonglade.Auth.Tests.csproj` | 108 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` (final) | 733 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` with the former provider suite enabled (final) | 193 | 0 | 0 | 0 |
| `node --test --test-reporter=spec 'src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs'` | 42 | 0 | 0 | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` (final) | N/A | 0 | N/A | 0 |

- Each .NET command additionally emitted xUnit TRX using `--report-xunit-trx --report-xunit-trx-filename <report> --results-directory <private-session-results>`. Parsed report counters, execution identities and discovered names agreed. Auth's passing result was retained after the final fixes, which changed only Setup and Web behavior. The full Setup run included 76 ordinary cases and 117 actual provider cases: 59 PostgreSQL and 58 SQL Server. All six provider fixture classes executed without filters or skips. Earlier targeted-provider results are not added again to these totals.
- The final Web build reported zero warnings and zero errors. The isolated published Production host, browser pages, temporary runtime files and disposable browser database were cleaned up. The private development backup remains outside the repository. No production database operation, commit, push or deployment was performed. README, the engineering reference, database guide and test guide now describe the intermediate runtime contract and remaining upgrade boundary.

| Requirement | Evidence |
| --- | --- |
| Independent email/password authentication and inert legacy credentials | `AuthenticateLocalAsync_AbsentOrProfileOnly_RejectsLegacyCredentials`; `AuthenticateLocalAsync_MalformedLegacyRow_DoesNotOverrideIndependentAccount`. |
| Required real proof before initial credentials and competing-write protection | `TotpRequired_ActualCodePrecedesCredentialCommitAndCookieCreation`; `CompleteLocalSetupAsync_ExpiryBoundary_EnforcesTenMinuteProofLifetime`; `ConcurrentInitialization_OnlyOneCommitWins_AndFailedWriteCannotReappear` on both providers. |
| TOTP disabled in every environment, genuine admin session and public homepage | `TotpDisabled_RealHttpSetupCreatesAdminCookieUnlocksHomeAndRejectsReplay` in Development, Production and Staging; published Production browser setup/sign-in passed without a TOTP step. |
| Pending pages, business/protocol 503, health and real fingerprinted assets | `PendingLocalSetup_PageRoutes_RedirectWithinPathBaseWithoutExecutingBusiness`; `PendingLocalSetup_BusinessProtocolsAndLookalikes_Return503WithoutExecution`; `PendingLocalSetup_CompiledSetupHealthAndFingerprintAssets_RemainReachable`. |
| Antiforgery and actual rate-limited proof/enrollment/verification | `AuthenticatedSignInGet_ClearsIdentityAndIssuesUsableAnonymousAntiforgery`; `InitialSetupProof_ConfiguredRateLimitRejectsActualValidCodeWithoutCommit`; `OrdinaryTotpGet_CannotDisablePostRateLimitForEnrollmentOrVerification`. |
| Revision-bound cookies and current password-change session retention | `LocalApplicationCookie_LegacyExpiredOrChangedRevision_IsRejected`; `PasswordEndpoint_RefreshesCurrentCookieAndRejectsOtherStaleSessions`. |
| Password-protected reset, preserved initialization and verified re-enrollment | `ResetTotp_ValidPassword_ClearsOnlyAuthenticatorWithoutReopeningSetup`; `OnPostAsync_ValidCode_EnablesTotpAndSignsInAdmin`; isolated browser reset/re-enrollment passed using a new real code. |
| Startup cannot reseed ambiguous data; valid missing/profile-only account survives restart | `InitStartUpAsync_AnyExistingApplicationRowWithoutConfiguration_IsPreserved` across all 20 mapped entity cases; `InitStartUpAsync_AccountAbsentOrProfileOnly_PreservesExistingInstallationAcrossRestart`; all provider startup fixtures passed. |
| OIDC bypass and exact-sub authorization | `OpenIdConnect_GateAndCookieValidationDoNotReadUnavailableLocalAccountTable`; `AdministratorPolicy_ForOpenIdConnect_RequiresAllowedSubject`; `AdministratorPolicy_ForOpenIdConnect_ProfileClaimsCannotReplaceExactSubject`. |
| Existing password/reset activities without protected values | `AccountNamedEvents_RealWriterStoresNoProtectedValuesAndPreservesSignOutPolicy`; existing audit-failure and sign-out failure regressions passed. |

- Focused test-quality review retained meaningful negative, persistence, exception and session assertions; no new presentation/resource-wording assertions or skips were introduced. No coverage percentage or empirical mutation score was claimed. Remaining gate: user acceptance of B2 before B3, with the live OIDC provider/browser check still explicitly unverified.

### B3 owner email cutover (2026-11-11)

- The user accepted B2 at the user-created `89a6dc1ff` and authorized B3. This batch moves the sole public email into Auth, removes General editing/defaults, adds revision-protected Account editing for both providers, and switches notification, Atom, FOAF and Gravatar consumers together. Owner Name and unrelated General settings remain unchanged. The configured development database has not been modified again.
- Feed keys include the captured owner email, including absence, and Atom receives that same snapshot. A postcommit, noncancelable event refreshes feed generations for profile creation, Local setup and real email changes; no-op writes leave revision and cache generation unchanged. Existing article partitions are not flushed. Queued notifications retain the recipient captured before enqueueing.
- Both providers now have additive schema and separate cutover scripts plus a permanent upgrade guide. Only legacy public email is transferred when no owner exists; legacy credentials are never imported. Cleanup removes only that top-level General member and the exact obsolete credentials row, with timestamps and unrelated data preserved. Malformed General objects, duplicate email aliases and invalid imported addresses fail before cleanup. Existing profiles/accounts and subsequent edits win on rerun.
- Isolated browser acceptance used an actually published Production host, separate synthetic PostgreSQL databases, and separate browser contexts. Local setup required a real TOTP code. Wrong-current-password editing failed visibly; a real email change signed out, old email login failed, new email login accepted the existing authenticator. Stable ID, password/salt/TOTP digests and Local initialization time were identical before and after the edit. The new revision survived restart; an exact no-op retained that revision and session.
- A loopback-only synthetic OIDC issuer exercised the real production discovery/JWKS, signed ID-token, authorization-code, PKCE, nonce, callback and exact allowed-sub flow without bypassing authorization. Fresh OIDC bypassed Local setup; missing profile omitted Atom email, FOAF mailbox hash and Gravatar while retaining local avatars. Account navigation and password-free create/update worked and kept the OIDC session. The deliberately different claim email was never used as public owner email. Both provider account pages and the new warning rendered under `zh-Hant`.
- Browser checks confirmed cold/warm Atom updates and exact current Gravatar MD5/FOAF SHA-1 fingerprints. FOAF retains its pre-existing one-hour public HTTP cache policy; fresh server responses were verified with browser cache bypass, not by claiming downstream cache purging. Account pages had no browser console errors. The General page no longer exposed an email editor.
- Automated missing-profile regressions exposed an Atom formatter error: omitting email had also removed the required author. The initial browser absence-only check had not asserted a successful response and was insufficient. Atom now keeps the post author name without email; RSS keeps its existing email-based author behavior. Repeated Production browser acceptance on a newly created OIDC database explicitly verified HTTP 200, valid Atom namespace/XML, one retained entry and author name, zero email elements, and a byte-identical warm-cache response. After public-profile creation, both cold and warm Atom remained valid HTTP 200 with that same author and the configured email, never the different OIDC claim address.
- Final automated results follow. The affected Web and Syndication suites and a fresh Web build were rerun after the Atom fix. Unchanged passing Configuration, Auth, Email and provider-enabled Setup results were retained rather than rerunning unrelated gates.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --project src\Tests\Moonglade.Configuration.Tests\Moonglade.Configuration.Tests.csproj` | 311 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Auth.Tests\Moonglade.Auth.Tests.csproj` | 133 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` (final Atom fix) | 750 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | 113 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Syndication.Tests\Moonglade.Syndication.Tests.csproj` (final Atom fix and RSS assertions) | 18 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` with the former provider suite enabled | 231 | 0 | 0 | 0 |
| `node --test --test-reporter=spec 'src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs'` | 42 | 0 | 0 | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-incremental` | N/A | 0 | N/A | 0 |

- Each .NET test command emitted `--report-xunit-trx` to private session results. TRX counters, discovered definitions and executions agreed, including all 35 new methods. Full Setup contained 76 ordinary cases, 78 PostgreSQL cases and 77 SQL Server cases, all passed. The ordinary-only Setup run had 76 passes and 36 explicit opt-in skips; those skips are superseded by the full provider run and are not counted as provider coverage. Earlier failing Atom reports remain historical evidence, superseded by the final passing reports.
- Focused gap/assertion review retained concrete persistence, negative, exception, snapshot, session and full-data-preservation assertions. No coverage percentage or empirical mutation score is claimed. Presentation, navigation, localization and Gravatar were browser-owned, not C# markup assertions.

| Requirement | Evidence |
| --- | --- |
| Sole email source; independent General writes and preserved Owner Name/audit | `LegacyGeneralEmail_OrdinarySavePreservesAccountAndOtherValues_WithoutEmailAudit`; `GeneralEmailInput_CannotOverrideAccount_AndOrdinaryWritesPreserveOwnerNameAndOtherValues`. |
| Local password protection, valid change and retained credentials/TOTP/identity | `ChangeLocalEmail_ValidPassword_PreservesCredentialsIdentityAndAuthenticator`; `ChangeLocalEmail_RejectedPasswordOrNoOp_RetainsExactRevisionAndDoesNotPublish`; actual TOTP browser sign-in before/after editing passed. |
| Real-change sign-out of every Local scheme; no-op retains sessions | `ChangedEmail_RejectsEveryOldLocalCookie_WhileNoOpRetainsIt`; `EmailEndpoint_LocalChangeSignsOutAllSchemes_AndPreservesAuthenticator`. |
| Revision, validation, write failure and real-provider concurrency | `ChangeLocalEmail_StaleIdentityOrInvalidAddress_CannotChangeAccount`; `FailedOrCancelledEmailWrite_DoesNotPublishOrReappearInLaterSave`; `ConcurrentProfileEmailWrites_FromAbsentOrExistingRow_OnlyOneRevisionWins`. |
| Authorized OIDC editing without local credentials or email-claim ownership | `EmailEndpoint_OpenIdConnectCreatesAndUpdatesProfileWithoutPassword_AndRetainsSession`; `EmailEndpoint_OpenIdConnectMatchingEmailClaimCannotGrantAdministration`; real test-issuer callback/browser editing passed. |
| Authentication/antiforgery, durable cache handoff and visible failures | `EmailEndpoint_RequiresAuthenticationAntiforgeryAndValidAddressBeforeMutation`; `OwnerChangedEvent_SeesDurablePublicProfile_WithNoncancelableHandoff`; `ProfileEventFailure_RemainsPostCommitFailure_NotMisreportedAsConcurrency`. |
| Cold/warm/category Atom, missing owner and unchanged RSS | `OwnerEmailChange_RefreshesWarmAndColdAtomAcrossCategories_WithoutChangingRss`; `Atom_SuppliedOwnerOrMissingProfile_PreservesItemsAndDoesNotAffectRss`; final Production browser assertions checked successful valid XML, author name, exact email state and item count. |
| Initial setup invalidation and old in-flight identity isolation | `InitialSetup_RefreshesPreviouslyMissingOwnerAtom_AfterDurableCommit`; `AtomCache_PinsOwnerForKeyAndQuery_OldInFlightCannotRefillNewIdentity`. |
| FOAF hash/omission and stable operation snapshot; Gravatar | `Foaf_ProfileMissingOrConfigured_UsesCurrentMailboxHashAndRetainsOwnerIdentity`; browser verified exact new-email Gravatar hashes and missing-profile local-avatar fallback. |
| Single notification capture, persisted queued recipient, missing profile and unaffected replies | `OwnerNotification_CapturesRecipientOnceBeforeQueueAwait_AndNextOperationRefreshes`; `QueuedNotification_ProfileChanges_DoNotRewritePersistedRecipientOrPayload`; `MissingOwner_SkipsOnlyOwnerDirectedMail_AndCommenterReplyRetainsRecipientAndPayload`. |
| Explicit missing-owner test-email failure and captured recipient | `TestEmail_MissingOwnerReturnsConflict_AndConfiguredOwnerIsSuppliedToNotification`. |
| Both-provider legacy/B1/B2 upgrade, preserved content/history/timestamps and idempotent configured-account reruns | `Cutover_LegacyAndAdditiveSchemas_PreservePublicProfileContentHistoryAndTimestamps`; `Cutover_AlreadyConfiguredProfileOrAccount_RerunsPreserveCredentialsAndLaterEmailEdits`. |
| Invalid source rollback, exact cleanup and 254-character email boundary | `Cutover_InvalidInput_FailsBeforeAnyLegacyOrUnrelatedDataIsRemoved`; `Cutover_LegacyAccountKeyLookalike_IsNotDeleted`; `Cutover_EmailAtMaximumLength_RemainsUsableForActualLocalSetup`. |
| Provider-specific controls, navigation, localization and operational guidance | Local/OIDC and `zh-Hant` browser verification; [permanent upgrade guide](upgrade-owner-account.md), README, AGENTS, engineering/database/test references updated. |

- All synthetic browser hosts, issuer processes, containers and runtime scratch files were removed. Temporary test research/plan/status records were removed after preserving this evidence; raw reports and the private development backup remain outside the repository. Final diff/whitespace checks passed. No development/production database operation, commit, push or deployment was performed during B3. B4 has not started; B3 awaits user acceptance.

### B3 development database cutover (2026-11-11)

- The user accepted B3 at the user-created `48de287c4`, explicitly requested development database migration, and instructed that B4 must not start. No application or browser acceptance work was started for B4.
- Verified the configured PostgreSQL development target without exposing its connection string, credentials or email. No local Moonglade host was running, and database checks rejected any other connected client before backup/migration. The source had 20 application tables and an already initialized independent owner account.
- Saved a new full custom-format backup, `development-before-owner-email-cutover-20261111-110005.dump` (1,139,280 bytes), in private session storage outside the repository. Verified its archive catalog and restored it into an isolated PostgreSQL container. Collation-independent, ordered full-row fingerprints matched every restored table to the source before applying any live changes.
- On the restored copy, ran the accepted `6.owner-account.sql` and `7.owner-account-cutover.sql` in order, then repeated both scripts. Verified the retained General JSON members and timestamp, every unrelated setting row and timestamp, unchanged schema, all 18 unrelated tables, and the complete existing owner row. Reruns produced identical data and schema. The rehearsal container and its temporary database volume were removed.
- Rechecked the accepted checkout, backup SHA-256, target identity and full development database fingerprints immediately before applying the same scripts to the development database. Both first execution and repeat passed. Configuration rows changed from 13 to 12 solely by removing the one obsolete `LocalAccountSettings` row; only the legacy General email member was removed from its retained row. All 18 unrelated tables were unchanged.
- The existing independent account's ID, email, revision, initialization time, password hash/salt and bound authenticator were preserved exactly. No legacy credentials were imported, no account was reset, and initial Local setup was not reopened.
- Private backups and verification metadata remain outside the repository. Temporary migration helpers were removed. No application startup, production migration, code commit, push or deployment was performed. B4 remains explicitly paused.

### Post-B3 review corrections (2026-11-11)

- The user authorized correction of three independently reproduced review findings before B4: a posted-email bypass of initial setup proof throttling, password dialogs that could not accept passwords longer than 32 characters even though setup allowed them, and a stale email revision after a password change.
- Initial setup and authenticator proof now share one client-IP quota independent of posted fields or setup tickets. Ordinary sign-in and TOTP partitions, configured limits, and unlimited GET behavior remain unchanged.
- Account password and authenticator-reset dialogs retain the existing password policy without the incompatible 32-character input cap. Password updates reload the email revision; email editing stays disabled during the update/refresh and after a failed reload. Failed password updates retain the prior email state, and duplicate password submissions cannot race that state.
- Targeted automated verification passed: 71 Web HTTP/rate-limit cases and all 47 native JavaScript cases (including the five-case Account module run), with no failures or skips. The Web test run rebuilt the affected source projects. HTTP regressions cover changed/empty/omitted setup email, full 40-character credentials, cookie revisions, and TOTP/reset invariants; actual-module regressions cover same-page email revision refresh, pending/duplicate submissions, failed password writes, and failed profile reloads.
- Isolated Chrome verification used the actual compiled account/setup/sign-in pages, account module and controllers over a synthetic SQLite fixture, not the development database. The dialogs accepted full 42-character passwords; password update returned 204, the following profile reload returned 200, and the same-page email change returned 204 rather than 409. Sign-in with the new email/password and authenticator reset also succeeded. This fixture does not cover the full dashboard or external integrations.
- No schema change or database migration is required. Temporary verification helpers were removed. B4 remains paused; no commit, push or deployment was performed.

### B4 end-to-end verification (2026-11-11)

- The user authorized B4. The starting checkout was clean on `master` at `7a60447dac34efd941e70ea8680842736e33d2d5`. No development/production database was used, and no commit, push or deployment was performed. See the closure entry for the accepted checkout.
- Environment: Windows x64, .NET SDK 11.0.100 building `net10.0`, Node.js 26.13.0 and Docker 29.9.3 in Linux mode. The repository-selected Microsoft.Testing.Platform runner was retained.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | ---: | ---: | ---: | ---: |
| `dotnet test --solution src\Moonglade.slnx --verbosity minimal`, with the former provider suite enabled | 2,211 | 0 | 0 | 0 |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | 47 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Auth.Tests\Moonglade.Auth.Tests.csproj --verbosity minimal`, after the diagnostic correction | 133 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-class '*LocalAccountSetupHttpTests' --filter-class '*AccountSecurityHttpTests' --filter-class '*LocalAccountRateLimitPolicyTests' --verbosity minimal`, after the correction | 71 | 0 | 0 | 0 |

- The full solution discovered all 15 test projects, including 231 Setup cases with both real providers and zero skipped cases. Its build reported three existing nullable-dereference warnings in `CannonServiceTests.cs` (lines 54, 154 and 192); these unrelated tests were not changed. The separate `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` passed with zero warnings/errors, both before and after the correction. Publishing the same Web assets for isolated Production-mode acceptance also succeeded.
- Runtime acceptance used production Web service composition, actual startup initialization, controllers, Razor, static assets and background services, with no business-service mocks. Configuration was explicitly isolated to uniquely named disposable PostgreSQL/SQL Server databases, loopback HTTPS, per-scenario key/image directories and disabled external email. A temporary synthetic HTTPS OIDC provider exercised the real OIDC handler rather than issuing test cookies.

| Disposable scenario on each provider | Observed result |
| --- | --- |
| Fresh Local | Startup created one seeded post and eleven configuration rows, but no account. Page redirects, feed 503, health/static access, invalid setup 400/no persistence, both scripts twice and restart preservation passed. |
| Upgraded Local | A representative pre-account schema retained a synthetic post, comment, current activity history, manifest, General unknown values/timestamp and unrelated settings. Both scripts created only a public profile, removed only obsolete credentials/email, and preserved all remaining state on repeat and restart. Setup remained pending. |
| Fresh OIDC | Local setup was bypassed; homepage/feed/health remained reachable without a profile. Both scripts and restart retained the absent profile and all unrelated data. |
| Upgraded OIDC | Both scripts preserved the legacy public address as a profile without credentials and retained all unrelated content, history and configuration. Repeat and restart were unchanged. |

- The eight-database matrix compared exact sorted row snapshots of every unrelated mapped table and separate retained General values/timestamps. A complete snapshot, including account credentials when present, was compared across script repeat and host restart. The opt-in provider regressions separately verified reruns after later profile/account email edits, credential preservation and real concurrent writers.
- Browser acceptance used separate contexts. Fresh PostgreSQL Local required real TOTP: an invalid password and invalid proof were rejected without credentials; valid proof committed once and opened the actual public homepage. Full 42-character password changes, immediate same-page email editing, sign-in with the new address and unchanged authenticator, password-protected reset, no reopened first-run setup, and new-authenticator enrollment all succeeded. The initialized account and replacement authenticator survived a host restart.
- Upgraded SQL Server Local prefilled the migrated address, accepted a replacement, and opened the homepage without TOTP when disabled. Ordinary password sign-in opened the actual admin dashboard directly; restart retained the account, original post/comment and eleven configuration rows.
- Fresh PostgreSQL and upgraded SQL Server OIDC completed discovery, PKCE code exchange, signed ID-token validation, user-info and the actual callback. Allowed-subject sessions created/edited public email without a password or sign-out. Provider email claims never became the public owner address. OIDC sign-out/callback returned home; a non-allowlisted identity reached Access Denied and received HTTP 403 from the account API, while its own identity bootstrap endpoint remained accessible. Profile-only state and email edits survived restart.
- Browser-rendered English, Simplified Chinese, Traditional Chinese, Japanese, French and German account/setup/sign-in surfaces were checked across these scenarios. A warm Atom feed changed to the saved owner address; a missing profile omitted Atom email and FOAF mailbox hash, and a fresh FOAF response included the hash after profile creation. Existing public FOAF browser-cache freshness was retained.
- One focused presentation diagnostic was corrected: invalid setup passwords previously named the internal `OwnerAccountStore` type. The existing DataAnnotations password rule now identifies `Password`; validation policy, rejection/no-write semantics and accepted lengths are unchanged. No C# assertion of UI wording was added. The rebuilt application passed the final browser recheck: the field was identified correctly, the rejected password created no profile/credentials, and the original seeded post and eleven configuration rows were unchanged. The entire eight-database runtime matrix also passed again after this correction.
- Cross-checked the published scripts and synchronized README, AGENTS, engineering reference, database catalog, upgrade guide and test instructions. Corrected the stale reference that still placed Owner Email editing in General Settings. External commercial identity-provider interoperability and actual outbound SMTP were not exercised; the configured-provider callback and notification business contracts were covered by the synthetic provider and existing tests respectively.
- All acceptance hosts were stopped and their disposable containers, synthetic keys, published copies and temporary runtime helper were removed. The isolated browser tabs were closed. Raw check summaries remain private; the retained plan records the release evidence. No schema/script change or development database migration was necessary.

### Task closure (2026-11-11)

- The user explicitly accepted B4 and requested task closure. B4's password-diagnostic correction and release documentation are in `5c4f3cb8e`. The accepted checkout `b9138db04bf350d67d3226eb8c36a8b990340517` also contains the Setup Account accent/TOTP layout and admin account label follow-ups.
- The Setup Account follow-up passed 37 HTTP cases and an isolated Chrome run with valid TOTP at desktop and narrow widths. The admin account label follow-up passed 754 Web cases and isolated Chrome checks of the desktop label and 320px dropdown/navigation. These checks are separate from the B4 solution and provider matrix above.
- The separately requested development account reset cleared only local credentials and initialization state and retained the owner profile. No database operation or application change was made during closure. The workspace was clean before this tracking edit.
