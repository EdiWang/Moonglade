# Settings Reliability Plan and Progress Tracker

## Resume Here

This file is the recovery record for the settings design review and reliability work. It must be sufficient for an AI starting a new conversation without the original chat.

1. Read `AGENTS.md`, this file, and the applicable sections of `docs/agent-reference.md` before working.
2. Inspect `git status --short`, the current branch, and recent commits. Preserve unrelated user changes. Revalidate the implementation pointers below; recorded observations are not a substitute for current source inspection.
3. Read the confirmed decisions, open decisions, batch status, and execution log. Do not interpret an old recommendation as user approval.
4. Identify the next authorized batch and its dependencies. Ask the user before executing an unauthorized batch or resolving a pending business or architecture decision. Offer a recommendation and explain its effects.
5. Update this file during work and at every batch boundary, including implementation completion, verification, commit, user acceptance, interruption, or newly discovered blockers. Do not leave the only recovery information in chat.

Current authorization: execute B4 (the user explicitly requested "Execute B4" on 2026-11-05), including implementation, verification, a local batch commit, and tracker updates under the agreed workflow. B5-B6, optional structural work, deployment, and production-data changes are not authorized.

Current next step: present the verified B4 local batch commit and await user acceptance / further instructions. B3 is commit `fe32fd43f`; B4 is the commit containing the completion entry below, titled `fix(settings): invalidate affected caches after save`. Record its actual hash at the next tracker update. B5 and later batches must not start automatically. The isolated acceptance host/database have been stopped and removed.

Retention: the user explicitly requested a repository recovery and progress file. Keep this file across batches and context compaction. Do not delete it merely because tests pass; ask before removing or archiving it. Permanent documentation must not depend on this task record.

## Goal and Scope

Improve settings reliability through independently testable, committable, reviewable batches, preserving the personal-blog use case. First complete reliability fixes; evaluate larger structural changes separately.

- Repository: `E:\GitHub\ediwang\Moonglade` on the original workstation; use the actual checkout root on another machine.
- Stack: ASP.NET Core / .NET 10, Razor Pages, LiteBus, EF Core; PostgreSQL and SQL Server are supported.
- Creation date: 2026-11-05, Asia/Taipei.
- Source baseline when this tracker was created: `f6407bd24978458af6f09c6b9cb4a22a667096e1`, branch `master`, clean working tree before creating this file. These are historical observations, not a required branch or a claim about future state.
- Preserve grouped JSON storage, existing setting keys and field names, supported providers, public URLs, authorization, antiforgery protection, and the current successful-save guarantees unless the user approves a specific change.
- Keep deployment configuration separate from administrator-editable blog settings. Do not move SMTP credentials or other deployment secrets into ordinary settings.
- Do not introduce dependencies, schema revisions, conflict UI, distributed synchronization, or a generic settings framework without a demonstrated need and explicit scope approval.
- Current implementation scope is B4 only, preserving verified B1-B3 behavior except explicitly approved activation changes. No deployment is included in any batch below.

Repository files must be English, except permitted localization resources and relevant test data. Follow `.editorconfig` (UTF-8, CRLF, final newline). Use the existing .NET stack and repository tools.

## Confirmed User Decisions

These decisions were explicitly supplied by the user in this conversation on 2026-11-05.

| ID | Decision | Execution consequence |
| --- | --- | --- |
| D1 | Target a single-instance personal blog; analyze multiple instances only as a risk. | Preserve local serialization; no distributed infrastructure in the reliability scope. |
| D2 | Ordinary invalid settings should continue to recover automatically, write defaults back, and emit Warning logs. | Keep recovery automatic. Do not log full JSON or sensitive values. Missing new settings and corrupt existing settings must be distinguished. |
| D3 | Some settings may explicitly require an application restart. | Preserve restart-dependent behavior where appropriate and make the UI/documentation clear. |
| D4 | Accept later saves overwriting earlier saves. | No optimistic concurrency version field or conflict interaction is required. Preserve transaction and in-process commit ordering. |
| D5 | With existing business data, an empty settings table or a missing local-account settings row must stop startup, preserve data, and prompt manual recovery. | Never infer permission to reseed or recreate a default account from these conditions. Check before repair writes. |
| D6 | Complete reliability fixes first; structural changes are optional later batches. | B1-B6 do not authorize immutable snapshots, account storage redesign, or other broad refactors. |
| D7 | If settings were saved but activity-log writing fails, return save success and emit an Error log. | Isolate audit failure only after successful persistence; do not hide validation, settings-save, or file-operation failures. |
| D8 | Ask questions when requirements or design choices are unresolved, provide recommendations, and do not decide for the user. | Record decisions here, including their scope and acceptance; a pending question blocks only dependent work. |
| D9 | Persist the plan and progress in the repository, with enough context for a fresh conversation; update it after every batch. | Maintain this tracker rather than relying on conversational memory. |
| D10 | When other settings and the account are valid but the manifest row is missing, preserve compatibility: reconstruct it with unknown version, reconstruction time, and Warning. | Do not invent the original installation time or treat a missing manifest like a missing account. |
| D11 | First installation must be idempotent and retryable after external dependencies recover; report the failed attempt in the console rather than permanently preventing another attempt. | A failed new installation must not become an unrecoverable partial state under the new implementation. This does not authorize clearing an established site. |
| D12 | Use a database transaction for first-install seed data and default settings; roll back pre-commit failures and retry. | Coordinate provider execution strategies, verify uncertain commit outcomes, and skip reseeding a completed installation. A persistent multi-step installer is not approved. |
| D13 | A pre-existing supported schema with every application table empty may be initialized. | Check all mapped application sets; an empty settings table alone, a failed query, or a missing table does not prove emptiness. This also permits retry after a rolled-back new installation. |
| D14 | Preserve older partial-install state that cannot be reliably identified; require manual confirmation and do not clear it automatically. | Guarantee safe retries for the new transactional installation path without guessing the provenance of legacy data. |
| D15 | For ordinary persisted settings, completely absent fields use the current whole-group DefaultValue; explicit false, zero, null, and empty arrays remain unchanged. | User accepted the listed changes to missing-field moderation/Gravatar, feed count, watermark, menus, and site metadata. Exclude account/manifest state. Do not rewrite valid stored JSON solely to materialize defaults or change HTTP request binding defaults. |
| D16 | Reuse existing required/range/length/format and script/CSS conditional rules; reject undefined enums; feed count is nonnegative with 0 unlimited and no new upper cap. Menus have at most 5 entries, non-null entries/submenu lists, required titles and submenu URLs, and no extra limits; disabled menus may have a null array. | Reject invalid writes with validation errors. Invalid ordinary persisted groups recover whole-group defaults with Warning, even if that replaces other valid sibling fields. B2 preserves explicit values only when they pass approved B3 validation. |
| D17 | Remove the generated AvatarUrl 64-character cap; allow blank notification display name and deployment fallback; preserve missing-theme fallback; custom CSS is limited to 10240 UTF-8 bytes. Keep existing watermark text/font constraints even when disabled. | Apply these compatibility exceptions consistently at HTTP, command, and persisted-loading boundaries. |
| D18 | Account username/hash/salt must be nonblank; populated TOTP secrets must parse and enabled TOTP needs a secret; generated-but-not-enabled setup remains valid. Manifest requires non-minimum UTC time; old/empty version values are preserved. | Invalid protected state stops initialization without defaults or invented history. Do not apply new-account request username/password format restrictions to old stored credentials. |
| D19 | FOAF/sitemap enable and disable, and notification sender display-name changes, take effect after application restart. | Preserve endpoint registration and email singleton lifetimes; clearly label restart requirements. |
| D20 | Wake scheduled publishing after settings save and recheck the enable flag before dispatch. | Disabling skips pending work not yet dispatched; enabling promptly rechecks. Already dispatched publication is not revoked. |
| D21 | Include the full-feed-content source defect in B4. | Read actual post content when full feeds are enabled, abstracts when disabled; verify RSS/Atom and category feeds, including formatting/CDN behavior. |

Existing repository constraints protect invalid persisted `LocalAccountSettings` and `SystemManifestSettings`: startup fails rather than replacing them with shipped defaults. Ordinary-setting recovery does not override that protection. D5 additionally covers missing account rows and empty settings tables with existing business data.

D11-D14 refine interrupted-first-install handling: do not apply D5 in a way that permanently blocks retry of a transactionally rolled-back new installation. Older partial state without reliable provenance follows D14 and is preserved for manual confirmation. The earlier proposal to require manual recovery after every interrupted first installation was rejected by the user and must not be implemented.

## Open Decisions and Compatibility Gates

Do not silently resolve these while implementing. B0 must inventory actual values, distinguish existing behavior from proposed behavior, and request only decisions that are needed.

| ID | Open point | Evidence / recommendation | Blocks |
| --- | --- | --- | --- |
| Q1 (resolved: D10) | Missing installation manifest with other settings and account valid. | User confirmed reconstruction with Warning; unknown version and reconstruction time. Current compatibility fixture needs a valid account added under D5. | No longer blocks B1. |
| Q2 (resolved: D15) | How should absent fields use defaults when property initializers and `DefaultValue` differ? | User explicitly accepted whole-group defaults for absent ordinary fields and preservation of explicit values after seeing the behavioral changes. Protected state remains separate. | No longer blocks B2. |
| Q3 (resolved: D16-D17) | Which semantic validation rules should be added or tightened? | User accepted the explicit baseline rules, whole-group recovery effects, and compatibility exceptions after reviewing them. | No longer blocks B3. |
| Q4 (resolved: D18) | Which historical account and manifest shapes remain valid? | User accepted nonblank credential fields, parseable TOTP/setup states, and non-minimum UTC installation time with version values preserved. | No longer blocks B3. |
| Q5 (resolved: D19-D20) | Which settings are immediate, cache-dependent, restart-dependent, or delayed by a background worker? | User accepted restart requirements for FOAF/sitemap/email name and wake/recheck behavior for the scheduler. Other settings retain next-operation semantics and downstream cache limits. | No longer blocks B4. |
| Q6 (resolved: D13) | Should a pre-existing supported schema with every application table empty permit first-install initialization? | User confirmed yes. Verify all application sets before initialization; retain retry after transactional rollback. | No longer blocks B1. |
| Q7 (resolved: D14) | Must older releases' abandoned partial installations recover automatically despite lacking reliable provenance? | User confirmed preservation for manual confirmation; no automatic clearing. Safe new-install retries remain required. | No longer blocks B1. |

Current recovery operates at the settings-group level. Extending validation can therefore cause an entire group to reset. Show affected historical cases in B0 and confirm changed recovery behavior before B3. Do not replace per-group recovery with field-level repair without approval.

## Investigation Context and Source Map

The review recommended retaining typed, grouped JSON storage. Its useful properties include separation of deployment and blog configuration, detached command snapshots, database-first publication, related-group persistence in one save, and existing failure/cancellation tests.

The following observations are the historical pre-B1 source-review baseline. B1-B3 address several of them; use the execution log and current source for the implemented behavior.

| Area | Current observation / concern | Main source pointers |
| --- | --- | --- |
| Startup safety | An empty `BlogConfiguration` table is treated as a new database, followed by `ClearAllData()` and seeding. With existing business data, this can delete content. This destructive path was not executed during the review. | `src/Moonglade.Setup/StartUpInitializer.cs`; `src/Moonglade.Data/BlogDbContext.cs`; `src/Moonglade.Data/Seed.cs` |
| Load and recovery | The loader mainly checks deserialization and non-null objects. Missing groups use `DefaultValue`; missing fields use property initialization. Protected invalid JSON stops startup, but missing keys follow the default path. | `src/Moonglade.Configuration/BlogConfig.cs`; `JsonExtensions.cs`; `src/Moonglade.Setup/ConfigInitializer.cs`; `AddDefaultConfigurationCommand.cs` |
| Runtime saves | The command snapshots JSON, validates target-row presence, serializes writes within the process, saves, then replaces singleton properties. Business validation is not centralized here. Related properties are published individually. | `src/Moonglade.Configuration/UpdateConfigurationCommand.cs`; `ServiceCollectionExtensions.cs` |
| HTTP and audit | Administrator APIs have inherited authorization and API validation, plus global antiforgery protection. Settings persistence precedes activity logging; a later audit failure can currently make the request fail. | `src/Moonglade.Web/Controllers/SettingsController.cs`; `BlogControllerBase.cs`; `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs` |
| Menu validation | The API accepts a JSON string and constructs a settings object after parsing; validation of that resulting object and nested data needs review. | `src/Moonglade.Configuration/CustomMenuSettings.cs`; `Menu.cs`; `SubMenu.cs`; settings controller; `src/Moonglade.Web/Pages/Components/Menu/Default.cshtml` |
| Cache and activation | Feed responses and sitemap have one-hour server caches. Settings writes do not consistently invalidate them. FOAF and sitemap routes are conditional at startup; the UI currently identifies restart requirements inconsistently. | `src/Moonglade.Web/Controllers/SubscriptionController.cs`; `PostCacheInvalidator.cs`; `src/Moonglade.Web/Handlers/SiteMapMapHandler.cs`; `src/Moonglade.Web/Extensions/WebApplicationExtensions.cs`; `src/Moonglade.Web/Pages/Admin/Settings/Advanced.cshtml` |
| Related writers | Avatar/CDN and authenticator flows also write settings; changes must reach these callers rather than only forms. File operations and database writes do not share a transaction. | `src/Moonglade.Web/Handlers/SaveAssetToCdnHandler.cs`; settings controller; `src/Moonglade.Web/Pages/SetupAuthenticator.cshtml.cs` |
| Structural concerns | The singleton exposes mutable objects. Stored keys derive from type names and update publication uses matching properties/reflection. These are deferred design concerns, not permission for a rewrite. | `BlogConfig.cs`; `UpdateConfigurationCommand.cs`; setting models |

Read the task-specific engineering reference before each batch: configuration/defaults, startup and LiteBus, EF Core, caching/side effects, authentication, and frontend/localization as applicable. Never quote or copy values from `appsettings.Development.json`.

## Batch Workflow and Status Rules

Recommended execution order: B0, B1, B2, B3, B4, B5, B6. B4 and B5 can be considered earlier after their own dependencies are met and the user authorizes that order.

For every implementation batch:

1. Confirm authorization, resolved dependencies, intended changed files/callers/tests, and a concrete acceptance checklist.
2. Inspect the working tree and nearby implementation/tests. Keep unrelated user changes intact. Recheck repository rules and the relevant reference sections.
3. Implement only that batch, including caller/fixture/resource changes needed to make it complete. Add meaningful behavior tests, not tests that merely mirror implementation.
4. Run focused checks. Build Web for cross-module or registration changes. Update affected long-lived documentation with confirmed durable behavior in the same batch.
5. Review the diff and `git diff --check`; record changed files, exact test commands/results, skipped checks and reasons, risks, open questions, and rollback notes here.
6. When that batch's implementation and commit have been authorized, create an independent local commit including its progress update. Never commit unrelated changes. Suggested messages below are proposals.
7. Present the commit and acceptance evidence to the user. Mark `Awaiting user acceptance`, not `Accepted`, until the user confirms. Do not start the next implementation batch before acceptance and authorization.
8. Record acceptance or requested fixes here. Record a commit hash in the next tracker update after it exists; do not invent a hash or amend commits solely to make a file contain its own hash.

Allowed statuses: `Not started`, `In progress`, `Blocked: <specific dependency>`, `Verified`, `Awaiting user acceptance`, `Accepted`, `Deferred`. Track implementation, verification, commit, and user acceptance separately. A passing test run is not user acceptance. On interruption, record the exact safe next action and current uncommitted state.

| Batch | Dependencies | Implementation / preparation | Verification | Commit | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0 | None | Verified: source matrices and B1 startup decisions complete | Source inventory and document checks verified; no application tests | None; implementation commit not applicable | User authorized proceeding to B1 on 2026-11-05 |
| B1 | B0 startup rules settled | Verified: startup safety and transactional installation implemented | Setup 107/107 including both providers; Configuration 45/45; Web 273/273; Web build clean | `7f0394673` | User authorized proceeding to B2 on 2026-11-05 |
| B2 | B1 accepted; affected defaults confirmed | Verified: D15 applied at persisted ordinary-settings loading | Configuration 60/60; Setup 120/120 including both providers; Web 273/273; Web build clean | `d51230912` | User authorized proceeding to B3 on 2026-11-05 |
| B3 | B2 accepted; D16-D18 confirmed | Verified: shared validation and approved compatibility rules implemented | Configuration 124/124; Setup 127/127 including both providers; Web 281/281; Auth 48/48; Email 105/105; Web build clean | `fe32fd43f` | User authorized proceeding to B4 on 2026-11-05 |
| B4 | B1-B3 accepted; D19-D21 confirmed | Verified: post-commit cache invalidation, activation hints, scheduler wake/recheck, full-feed source | Configuration 129/129; Web 297/297; Syndication 18/18; BackgroundServices 14/14; Auth 48/48; Email 106/106; Web build clean; isolated browser save/restart checks passed | Batch commit containing this row; title `fix(settings): invalidate affected caches after save` | Awaiting user acceptance |
| B5 | B1 accepted | Not started; not authorized | Not run | None | Pending |
| B6 | B1-B5 accepted | Not started; not authorized | Not run | None | Pending |
| C1-C3 | B6 accepted; separate design and authorization | Deferred | Not run | None | Not applicable |

### B0 - Confirm Behavior and Compatibility Matrices

Scope: read-only analysis and decisions, with tracker updates. Inventory each settings group: defaults, existing validation, supported historical JSON, readers/writers, caches, and activation timing. Classify first installation, genuinely empty database, existing content with missing settings, and interrupted initialization. Separate absent fields from explicit `false` or `0`.

Deliverables: complete scenario matrices, a list of proposed behavior changes and affected old values, recommendations, user answers, and batch-specific acceptance examples. Resolve Q1-Q5 only as needed for the next batch. B0 does not change application behavior and has no implementation commit.

Acceptance: every scenario needed by B1 has an approved outcome; subsequent batches have either approved rules or clearly identified pending questions. A pending unrelated validation limit does not block startup safety work.

### B1 - Make Startup and Initialization Safe

Scope: separate first-install seeding from abnormal settings loss. Remove the association between an empty settings table and permission to clear existing business data. Preserve genuine fresh creation/seeding. Apply D5 before repair writes; preserve existing content, accounts, and other data. Apply D10 to a missing manifest with a valid account. Under D11-D12, seed rows and default settings form one first-install data transaction: failures before commit roll back and permit a later retry. Coordinate both providers' execution strategies, avoid residual tracked inserts from the current recursive seed retry, and verify uncertain commit outcomes before repeating writes. D13 allows initialization of a pre-existing wholly empty supported schema; D14 preserves ambiguous older partial data for manual confirmation. Do not add a persistent installer state or infer permission to clear legacy data.

Tests: `Moonglade.Setup.Tests`, relevant configuration tests, and Web build. Cover fresh initialization, a normal existing database, content with no settings rows, a missing account row, repeated startup, and restart after interrupted initialization. Add fault injection after seed writes and during early/middle/late default writes; verify transaction rollback, safe restart after dependency recovery, lost-commit-acknowledgment handling, and no duplicate installation. Assert actual retained values and relationships, not only return codes or row counts. Verify relational paths on disposable PostgreSQL and SQL Server databases; InMemory tests do not prove relational behavior. Existing migration-container tests are not a substitute for new startup-safety coverage. Detailed cases A01-A14 are in `SETTINGS_B0_ANALYSIS.md`.

Acceptance: fresh installation works and can be retried after a rolled-back failure; an already committed installation is not duplicated after retry/restart; affected established databases stop safely with actionable diagnostics and no repair writes; normal repeated startup does not reseed. Preserve noncritical icon-generation behavior outside the data transaction. Prove failure paths only against disposable data, never a real blog database.

Suggested commit: `fix(setup): preserve existing data when settings are missing`.

### B2 - Unify Defaults and Recovery

Scope: consolidate the approved defaults without changing stored keys or JSON field names. Align new installs, missing groups, and absent fields as agreed in B0. Preserve explicit saved values, including valid `false` and `0`. Continue ordinary-group default recovery and Warning logs. Preserve valid rows and modification timestamps; avoid bulk rewriting valid JSON. Maintain protected-setting safeguards.

Tests: `Moonglade.Configuration.Tests`, `Moonglade.Setup.Tests`, and affected caller tests. Cover absent groups, absent fields, explicit values, malformed JSON, independent groups, default/runtime/persisted agreement, and a second startup after repair. Verify logs identify keys and safe reasons without full JSON. A repaired group should not produce a repair warning on every later startup.

Acceptance: the approved default source is consistent; valid data is unchanged; only qualifying anomalous groups are repaired and written back.

Suggested commit: `fix(configuration): unify settings defaults and recovery`.

### B3 - Validate Loads and Saves Consistently

Scope: share business validation across persisted loading and the settings-write boundary, including callers outside HTTP. Validate parsed menu objects and nested data. Validation reports errors without mutating its input. Reconcile form constraints with valid generated values, such as avatar URLs. Do not automatically apply every form annotation to persisted data or invent limits. Keep legitimate account-setup intermediate states and approved legacy manifests valid.

Tests: Configuration, Setup, and Web projects; add Auth, Email, or other affected projects according to the actual caller impact. Cover invalid numbers/enums, missing required data, nested menu nulls/counts, cross-field conditions, generated URLs, direct command dispatch, one invalid group in a paired write, save failure, and cancellation. Update fixtures to represent valid inputs rather than weakening validation to preserve incomplete fixtures.

Acceptance: HTTP invalid input receives a meaningful validation response; direct command callers cannot persist invalid settings; invalid writes change neither database nor runtime; ordinary invalid persisted values recover under the approved policy. Related-group transaction and detached-snapshot guarantees remain intact.

Suggested commit: `fix(configuration): validate settings consistently`.

### B4 - Align Cache Invalidation and Activation Messages

Scope: map settings to affected caches and centralize post-commit invalidation using existing infrastructure where it fits. Cover site information, feeds/category feeds, sitemap, images/CDN, appearance, and internal writers as applicable. Preserve approved restart-dependent endpoint behavior and clearly label it. Distinguish successful saving from immediate activation. Audit background-worker delay rather than promising instant behavior unsupported by its reader.

Tests: `Moonglade.Web.Tests`, `Moonglade.Syndication.Tests`, and Web build. Warm caches before saving; verify RSS/Atom, category feeds, sitemap, and appearance consume the approved new values. Verify failures do not run successful-save effects and old in-flight cache computations cannot repopulate an active generation. Check post-commit request cancellation. Exercise actual admin pages in a browser for activation/restart messages, including both enable and disable followed by restart.

Acceptance: immediate changes no longer wait for stale server caches; restart requirements are accurate and consistent; restart activates the saved value. Browser, CDN, and downstream feed-client caches are distinct from server cache invalidation and must not be claimed as purged.

Suggested commit: `fix(settings): invalidate affected caches after save`.

### B5 - Isolate Activity-Log Failures After Successful Saves

Scope: change settings-related audit behavior only, including direct save paths, image settings, password changes, and authenticator reset. After successful settings persistence, an activity-log failure emits Error and leaves the API response successful. It must not prevent required subsequent authentication actions. Do not change unrelated controllers' audit semantics or hide settings-save, validation, cache, or file-operation failures under an audit catch.

Tests: `Moonglade.Web.Tests` and Web build, with separate fault injection for settings-save failure and audit-write failure. Assert response, persisted/runtime values, safe contextual error logs, and required sign-out behavior. Confirm success is not reported when the actual settings save fails.

Acceptance: an audit-only failure does not mislead the administrator about whether settings were saved; real save failures remain visible; authentication flows and unrelated auditing retain their contracts.

Suggested commit: `fix(settings): preserve successful responses when audit logging fails`.

### B6 - Full Regression and User Acceptance

Scope: full solution tests, Web build, diff/format review, and final verification that long-lived docs reflect confirmed behavior. Add documentation only when needed; do not manufacture an empty cleanup commit. Verify each prior batch against its approved rules.

Manual scenarios: fresh installation; existing JSON with missing fields; ordinary invalid-setting recovery; empty settings table or missing account with existing data; save/refresh/restart round trips; warm RSS/Atom and sitemap caches; appearance; two stale tabs saving in sequence (later save wins); avatar/CDN; password change; authenticator reset; audit outage.

Evidence for each scenario: expected result, actual result, environment/provider, command or interaction, associated implementation commit, and remaining limits. Record all skipped or unavailable tests. Do not record real secrets or production data.

Acceptance: B1-B5 rules have supporting evidence; the user accepts the overall outcome; unresolved issues and intentional limits are explicitly recorded. Tests passing alone do not complete B6.

Suggested documentation commit, only if needed: `docs(settings): document recovery and activation behavior`.

## Optional Follow-up Batches

These are not part of authorized reliability implementation and require a separate proposal and user decision after B6.

| Batch | Candidate scope | Verification focus |
| --- | --- | --- |
| C1 | Read-only public access and atomic publication of immutable settings snapshots. | Consumers cannot bypass persistence to mutate runtime settings; related reads use a consistent snapshot; failure guarantees are preserved. |
| C2 | Separate account and installation state from ordinary blog preferences. | Login, authenticator setup/reset, old installations, module ownership, and persistence compatibility. |
| C3 | Move avatar/CDN orchestration to its owning service and clarify file/database failure handling. | Failure at each file or database step; the previous resource remains usable; diagnostics and retry behavior are defined. |

Additional deferred concern: decouple durable setting identifiers from C# type names before renaming settings types. No rename or new generic registry is approved. If any follow-up changes schema or persisted-data contracts, include manual idempotent scripts and upgrade/rollback verification for both PostgreSQL and SQL Server, following repository rules.

## Verification Commands and Operating Notes

Run from the repository root. Select affected projects rather than running every suite after every small edit. Follow `src/Tests/README.md` and the runner selected by `global.json`.

```powershell
dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
dotnet test --project src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj
dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj
dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
git diff --check
```

Final batch:

```powershell
dotnet test --solution src/Moonglade.slnx
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
git diff --check
git status --short
```

- Run test-project builds sequentially: during the earlier review, independent builds sharing project outputs collided. Sequential retries resolved the collision.
- Use normal restore when assets are stale or package files are missing. The review initially encountered missing packages with `--no-restore`; normal restore and testing succeeded. Do not change dependency versions to fix stale local assets.
- The existing database migration groups are opt-in and use disposable containers; consult the current test guide before enabling them. A skipped group is not a pass and does not verify new startup scenarios.
- No real SMTP delivery, production writes, deployment, or external-service mutation is needed for ordinary automated verification. Use mocks and disposable/local data.

## Historical Verification Evidence

During the earlier analysis in this conversation on 2026-11-05, the following existing suites passed. These results are a historical baseline only, not evidence for unimplemented batches or future source state.

| Project | Historical result | Limits |
| --- | --- | --- |
| `Moonglade.Configuration.Tests` | 45 passed | Existing tests only; no new defect-reproduction tests were added. |
| `Moonglade.Setup.Tests` | 19 passed, 8 skipped | Opt-in database-container migration tests were skipped; no destructive startup scenario was executed. |
| `Moonglade.Web.Tests` | 273 passed | Existing tests only; no actual site/browser or external service was exercised. |

The planning-only turn subsequently rechecked sources and a clean working tree, but did not rerun tests. This tracker-creation task does not rerun application suites for a documentation-only edit.

## Rollback and Data Protection

- Keep implementation batches independently revertible. Record any dependency that requires reverting later batches first. Do not revert unrelated user work.
- Reverting code does not restore settings already replaced by automatic default recovery. Use disposable databases or controlled copies for fault scenarios and retain a secure pre-test snapshot. Never put backup credentials or settings dumps in this repository.
- Do not deliberately empty settings, remove accounts, corrupt JSON, or inject storage failures into a real blog database for acceptance testing.
- A failed check is investigated and recorded; it is not a reason to weaken or delete the check. Stop only the work dependent on an unresolved issue.
- If schema/data-contract changes become necessary, pause for scope confirmation and plan both providers' manual scripts. Do not add automatic schema migration.

## Per-Batch Evidence Template

Copy this entry into the execution log when a batch begins and update it as work proceeds.

```text
Batch / status:
Started / last updated (date and timezone):
User authorization and confirmed scope:
Dependencies / relevant decision IDs:
Current branch / baseline commit:
Changed files and callers:
Implementation summary:
Checks (exact commands, results, skips, environment/provider):
Documentation updates:
Commit(s), or not committed and why:
Acceptance checklist / evidence:
User acceptance (pending, or explicit confirmation and date):
Risks / unresolved questions / rollback:
Uncommitted state / exact next action:
```

## Execution Log

### 2026-11-05 - Plan and Tracker Creation

- User authorized writing the existing plan into the repository as a durable recovery/progress record and requires updates after every batch. No implementation batch or commit was authorized by this request.
- Recorded the prior review, confirmed decisions D1-D9, unresolved compatibility gates, B0-B6, optional C1-C3, verification approach, acceptance workflow, and rollback limits.
- Rechecked repository task-record rules, configuration guidance, test commands, source baseline, and a clean starting working tree.
- B0 is not yet complete or accepted. Its detailed behavior matrices and unresolved decisions remain pending. All implementation batches are not started.
- Application source and long-lived operational documentation are unchanged. Only this new tracker is being added; it has not been committed.
- Document verification: UTF-8 without BOM, CRLF, final newline, English text, B0-B6 definitions, D1-D9 decisions, referenced repository paths, and whitespace checks. No application tests were run for this documentation-only change. Recheck the final working tree before reporting completion.
- Next action: present this file to the user. Await authorization for B0 or a specifically approved batch; do not automatically begin implementation.

### 2026-11-05 - B0 Source Inventory and User Decisions

- User explicitly authorized B0. Only the two repository task records are in scope; no application implementation or commit is authorized.
- Added `SETTINGS_B0_ANALYSIS.md`: startup/recovery scenarios S01-S16, all 19 mapped application sets, all 11 setting groups, the complete top-level default-divergence inventory, validation gates, read/write/activation/cache paths, and proposed B1 acceptance cases A01-A14.
- Confirmed D10: reconstruct a missing manifest when other settings/account are valid, preserve business data, and warn. Existing compatibility tests currently omit the account and will need their fixture corrected under D5.
- User rejected the earlier recommendation to require manual recovery for every interrupted first installation. Recorded D11-D12: first installation is idempotent and can be retried after external dependency recovery; use one database transaction for seed data/default settings, with rollback and commit-state verification.
- Q6 (pre-existing wholly empty schema) and Q7 (older ambiguous partial installations) were asked and remain pending. Do not treat the recommendations as answers. Q2-Q5 concern later batches and remain explicitly open.
- Additional observed activation issues: EmailDisplayName is captured by a singleton; a scheduler already waiting may publish without rechecking its disabled flag. These are documented B4 choices, not application changes or approved fixes.
- Source evidence includes provider retry registration and startup seed/default-save boundaries. Official EF transaction/retry guidance was checked and linked in the B0 analysis. No application tests, real database, browser, SMTP, or destructive startup path were run.
- Current state: B0 source work prepared, decisions/acceptance pending. B1-B6 and C1-C3 remain unstarted and unauthorized.
- B0 verification: checked catalogue coverage against all 11 loader assignments and all 19 DbSets, B0 scenario/acceptance identifiers, repository references, UTF-8 without BOM, CRLF/final newlines, English text, and whitespace. Only the two task-record Markdown files are changed/untracked; no implementation commit was made.

### 2026-11-05 - B0 Startup Decisions Resolved

- User confirmed D13: initialize a pre-existing supported schema when all application tables are empty.
- User confirmed D14: preserve ambiguous older partial-install data for manual confirmation; do not automatically clear it.
- Updated Q6-Q7, startup scenarios S02/S14, acceptance cases A07/A14, and the current handoff. Earlier pending-state entries above are historical snapshots superseded by this entry.
- B0 analysis and required B1 startup decisions are complete and verified. User acceptance of B0 remains pending; answering the two questions is not recorded as an explicit acceptance or B1 implementation authorization.
- Q2-Q5 remain open for their respective later batches. They do not block B1's agreed startup-safety and installation-idempotency scope.
- Only task-record documents changed. No application implementation, application tests, commit, deployment, or database mutation occurred. Next action: present B0's resolved state and wait for user acceptance / B1 authorization.

### 2026-11-05 - B1 Started

- User authorized B1 under the agreed implementation/test/commit/acceptance workflow. B0's resolved rules are the basis for implementation; later-batch questions remain open.
- Starting source baseline: `f6407bd24978458af6f09c6b9cb4a22a667096e1`; the only initial untracked files are the two requested task records. No unrelated user edits were present.
- Affected scope: startup classification and installation transaction, configuration initialization/publication, seed retry/cancellation, startup tests and disposable provider coverage, and relevant README/reference documentation. No schema or stored-format change is planned.
- Docker daemon was initially unavailable; checking whether the local Docker Desktop can start for the agreed disposable-provider verification. Do not substitute a real blog database.
- Implementation and verification are in progress. No commit or B1 acceptance yet.

### 2026-11-05 - B1 Implementation and Verification Complete

- Authorization: the user's explicit B1 execution request under the agreed local implementation/test/commit/acceptance workflow. B2-B6 and optional structural batches remain unstarted and unauthorized.
- Startup now checks every mapped application set before allowing first-install seeding. A populated database without account settings fails before default/repair writes; existing content and ambiguous older partial state are preserved. The startup path no longer calls `ClearAllData`.
- First-install seed data and all default settings share a provider execution-strategy transaction. Retries clear tracked inserts, recheck emptiness, and compare the saved settings plus the attempt's post ID before treating a possibly lost commit acknowledgment as success. Pre-commit failure rolls back; restarting an empty existing schema can retry. Schema creation and site-icon generation remain outside the installation-data transaction.
- Configuration initialization stages and validates detached settings before writing. The runtime configuration is loaded only after a successful installation commit / completed existing-store initialization. Missing account rows are never replaced during existing-install startup. Missing manifests with valid accounts are reconstructed with unknown version, reconstruction time, and Warning as approved.
- Removed the seed method's recursive same-context retry; cancellation now flows through its EF writes. No schema, setting key, JSON shape, dependency version, or migration contract changed, so no SQL upgrade script is needed for B1.
- Changed production files: `src/Moonglade.Setup/StartUpInitializer.cs`, `src/Moonglade.Setup/ConfigInitializer.cs`, and `src/Moonglade.Data/Seed.cs`.
- Tests: updated `StartUpInitializerTests.cs` and `ConfigInitializerTests.cs`; added `TestSupport/StartupTestHarness.cs` and `Database/StartupInstallationDatabaseTests.cs` in the Setup test project. The 19-set preservation theory is generated from actual DbSet declarations. Existing compatibility fixtures now include a valid account rather than depending on account auto-recreation.
- Documentation: updated README initialization/recovery behavior, `docs/agent-reference.md` implementation constraints, `src/Tests/README.md` provider-test coverage, and the B0 record's handoff. Permanent documentation does not link to temporary task records.

Verification (all from repository root):

| Check | Exact command / environment | Result |
| --- | --- | --- |
| Setup with both supported providers | Set `MOONGLADE_TEST_CATEGORY_DATABASES=1` for the command process; `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | 107 passed, 0 failed, 0 skipped. Includes 26 startup provider cases plus existing migration/provider tests. |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 45 passed, 0 failed, 0 skipped. |
| Web | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 273 passed, 0 failed, 0 skipped. |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed, 0 warnings, 0 errors. |
| Diff / formatting | `git diff --check`; explicit CRLF/final-newline checks on batch files and review of staged changes | Passed; original Seed.cs BOM retained. |

Acceptance evidence:

- A01-A02, A07: first creation and pre-existing empty schemas install successfully on both providers; two restarts preserve post IDs, all setting JSON/timestamps, and installation metadata without duplicate seeds.
- A03-A04, A06: each of the 19 mapped sets prevents empty-database classification when it contains the sole row (InMemory preservation theory). Both providers separately retain exact saved post content/timestamps and comment relationships after settings-table/account loss, without recreating defaults.
- A05: existing-install tests verify missing-manifest reconstruction and Warning, while valid historical manifests and settings timestamps remain unchanged.
- A08-A09: actual-provider write failures at seed save and default rows 1/6/11, plus cancellation during defaults, roll back seed and setting rows; a fresh context can restart and finish one installation.
- A10: provider retry after a simulated lost commit acknowledgment verifies committed settings/post state without duplicate data. Runtime settings remain unchanged at the commit callback, then load after successful completion.
- A11, A14: ordinary invalid-group recovery and protected-JSON failure-before-repair cases pass; missing-account/ambiguous-content cases preserve data instead of reseeding.
- A12: application-state query failure produces startup failure without assuming an empty database; injected CREATE TABLE failure can be retried after recovery on both providers.
- A13: the shared harness injects an unavailable icon source; otherwise successful startup remains successful, preserving the noncritical icon boundary.

Execution notes and limits:

- Docker Desktop was initially stopped and was started locally for verification. All database containers were uniquely named disposable test containers; the fixture removed them after completion. No real blog database or external service was used.
- Initial new unit fixtures required nonzero integer foreign keys and scalar snapshots rather than serializing computed theme properties; corrected the fixtures without weakening checks. The first provider run found two PostgreSQL timestamp-comparison failures: comparisons now use database-round-tripped baseline timestamps instead of finer-precision pre-save values. The final full provider run passed all 107 cases.
- Faults are injected through EF interceptors against real providers, not physical network interruption or process termination during schema DDL. Browser acceptance and deployment were not performed. Complete-solution regression remains B6 scope.
- Commit: this entry is included in the B1 implementation commit titled `fix(setup): preserve existing data when settings are missing`. Resolve the actual hash with `git log` and record it at the next tracker update; do not amend solely to embed a commit's own hash.
- User acceptance: pending. Required checks passed, but this is not user acceptance. Next action: present the local commit and this evidence, then await acceptance / further instructions. Do not start B2.
- Rollback: revert the batch code commit if needed; it does not alter existing database schema. Reverting code does not restore settings/data changed by any later operator action. Keep fault testing confined to disposable copies.

### 2026-11-05 - B2 Started, Default Choice Pending

- User explicitly authorized B2; recorded B1 commit `7f0394673` and authorization to proceed. Starting working tree was clean.
- Q2 remains a real compatibility decision. Asked whether ordinary absent fields should use current whole-group defaults, while preserving explicit false/0/null/empty arrays and excluding account/manifest recovery. Explained effects on moderation, Gravatar, feed count, watermarking, menus, and site metadata. No answer has been inferred.
- Continue independent caller/test/recovery inspection while waiting. Do not change request binding defaults, ordinary missing-field behavior, or protected-setting semantics before the needed answer.
- B2 is in progress; no application edits, tests, or new commit yet.

### 2026-11-05 - B2 Default Policy Confirmed

- User accepted the recommended Q2 policy after the explicit compatibility-effects question. Recorded D15; default changes are no longer blocked.
- Implementation will combine defaults with persisted ordinary JSON only when loading. Preserve duplicate-property/case-insensitive deserialization behavior, all explicit values, existing request-model initialization/validation, and protected account/manifest parsing.
- Valid partial JSON stays unchanged in the database, including timestamps; missing/corrupt whole groups retain the existing persistence/recovery path and Warning behavior. No schema or JSON field-name change is planned.

### 2026-11-05 - B2 Implementation and Verification Complete

- Authorization: explicit B2 execution request plus D15 confirmation after the user reviewed the effects on omitted moderation/Gravatar, feed, watermark, menu, and metadata fields. B1 is commit `7f0394673`; B3 and later batches remain unauthorized.
- Production change is limited to `src/Moonglade.Configuration/BlogConfig.cs` and `JsonExtensions.cs`. For ordinary object JSON, the loader inserts only absent top-level properties from the group's current `DefaultValue` before normal deserialization. Saved property names are compared case-insensitively; duplicate saved properties remain in their original order so existing last-value/error behavior is retained.
- Explicit false, zero, null, and empty arrays remain explicit values, including values whose semantic validation is deferred to B3. Default collection objects are independently deserialized; loading one configuration cannot mutate another's default menu.
- The shared `FromJson` methods, setting model constructors, HTTP request validation, stored keys, JSON field names, provider mappings, account/manifest parsing, and B1 transaction/rollback behavior are unchanged. No dependencies or migration scripts were added because no schema/data-format migration is required.
- Valid partial JSON is not added to the initialization/repair list, rewritten, or timestamped. Whole-group missing/corrupt values retain existing initialization/recovery behavior. Tests assert a single repair Warning across two starts, without the stored payload in log messages.
- Tests: added `PersistedSettingsDefaultsTests.cs` for all nine ordinary groups and edge cases; extended Setup initializer tests for every ordinary group, null/incompatible recovery, warning behavior, and two provider cases retaining partial JSON/timestamps across repeated startup.
- Updated README and engineering-reference behavior, test coverage documentation, and the B0 decision/handoff record. Existing pre-B1 source observations in B0 remain a historical baseline, with Q2 now marked resolved.

Verification (from repository root, sequential builds):

| Check | Exact command / environment | Result |
| --- | --- | --- |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 60 passed, 0 failed, 0 skipped. |
| Setup with both supported providers | Set `MOONGLADE_TEST_CATEGORY_DATABASES=1` for the command process; `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | 120 passed, 0 failed, 0 skipped, including B1 installation rollback/retry regressions and both new partial-JSON provider cases. |
| Web | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 273 passed, 0 failed, 0 skipped. |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed, 0 warnings, 0 errors. |
| Diff / formatting | `git diff --check`; staged whitespace and CRLF/final-newline checks on batch files | Passed. |

Acceptance evidence and limits:

- Every ordinary group loaded from `{}` matches its whole-group default and is not scheduled for a repair write. Explicit false/zero/null/empty arrays and case-insensitive names survive; duplicate known fields retain the previous deserializer's behavior, including incompatible earlier values causing ordinary-group recovery.
- InMemory tests load all nine valid partial groups twice with strict no-write mediators and preserve the exact saved JSON (including whitespace/unknown fields) plus every timestamp. PostgreSQL and SQL Server tests independently load all ordinary groups from partial JSON and compare database-round-tripped rows before/after repeated startup.
- Account/manifest partial-object tests prove B2 does not synthesize bootstrap credentials or installation history. Minimum valid protected shapes remain Q4 for B3, not a new B2 policy.
- Both supported disposable providers were exercised. Test containers were removed by the fixture; no real blog database, external service, deployment, or browser session was used. Full-solution regression remains B6 scope.
- Intentional compatibility effect: old JSON genuinely missing fields now uses the approved group defaults; a later code rollback restores the prior loading behavior. Actual automatic corrupt-group recovery writes cannot be undone merely by reverting code. Existing valid saved JSON is unchanged by B2 loading.
- Commit: this entry is part of the B2 batch commit titled `fix(configuration): unify settings defaults and recovery`. Record the actual hash at the next tracker update; do not amend solely to embed its own hash.
- User acceptance: pending. Next action: present the local commit and evidence, then await acceptance / instructions. Do not start B3 or choose Q3-Q5 rules automatically.

### 2026-11-05 - B3 Started, Validation Decisions Pending

- User explicitly authorized B3; B2 is commit `d51230912`, and the starting working tree was clean.
- Requested three decisions: baseline constraints and whole-group semantic recovery; generated avatar / blank sender-name / theme / UTF-8 CSS compatibility; minimum protected account/TOTP/manifest shapes. Recommendations and their old-data effects were stated explicitly. No answers are inferred from the execution request.
- Continue independent inspection of validation, write callers, HTTP error translation, and tests. No dependent business-rule changes or application implementation have begun yet.
- B4-B6 and optional structural work remain unauthorized. Pending questions block only the changes that need their answers.

### 2026-11-05 - B3 Rules Confirmed and Implementation Started

- User accepted all three stated rule groups. Recorded D16-D18; Q3-Q4 no longer block implementation. The earlier pending-status entry is historical.
- Reuse DataAnnotations and model-owned IValidatableObject rules in both the loader and update handler; translate settings validation exceptions to HTTP validation problems. No generic settings framework or new service abstraction is needed.
- Remove mutating CDN validation; add model rules and the approved compatibility exceptions. Reuse the already-installed Otp.NET 1.4.1 parser in Configuration for TOTP syntax, matching Auth's existing normalization, rather than inventing a decoder or changing the version.
- Update incomplete test fixtures to valid values while preserving failure/cancellation/transaction assertions. New tests must prove invalid groups recover or stop appropriately, paired writes reject atomically, and HTTP responses contain validation errors without sensitive values.

### 2026-11-05 - B3 Implementation and Verification Complete

- Authorization: explicit B3 execution request and the user's acceptance of all three proposed rule groups, recorded as D16-D18. B2 is commit `d51230912`; B4-B6 and optional structural work remain unstarted and unauthorized.
- Shared validation uses the settings models' DataAnnotations and `IValidatableObject` at persisted loading and `UpdateConfigurationCommandHandler`. Every replacement is validated before querying or changing rows. Invalid direct or paired writes change neither persistence nor runtime; existing save-failure, cancellation, detached-snapshot, and serialized-publication guarantees remain covered.
- Ordinary semantically invalid stored groups now follow the established whole-group default recovery path and Warning. This can discard valid sibling fields in that group, as approved. A second startup preserves the repaired row without another warning. Valid partial JSON, timestamps, explicit values, and B2 absent-field behavior are preserved when validation succeeds.
- Account and manifest semantic failures stop initialization before any repair writes. Credentials must be nonblank; TOTP parsing reuses the existing Otp.NET 1.4.1 package and Auth normalization without changing saved values or applying new-account form syntax to historical usernames. Generated-but-disabled secrets remain valid. Manifest timestamps must be non-minimum UTC; historical/null/empty version values are preserved.
- Added undefined-enum, nonnegative feed, nested-menu, conditional script/CSS, and UTF-8 CSS-byte validation. Validation no longer mutates CDN settings. Preserved the approved avatar, blank sender-name, unknown-theme, disabled-menu-null, and watermark compatibility rules. The notification form no longer marks the optional display name as required.
- `SettingsValidationAttribute` translates settings-controller command validation failures into HTTP 400 validation problems. Existing HTTP model validation and JSON parse errors remain in place. Script/CSS conditions moved from controller-only checks to their owning models; parsed menu graphs now reach shared validation. Authentication, authorization, antiforgery, activity-log semantics, and cache activation are unchanged.
- Production files: configuration models, loader, update handler, and Configuration project reference to the already-installed Otp.NET parser; settings controller, its scoped exception filter, and notification Razor form. No new package version, generic validation service/framework, schema revision, JSON field/key change, or SQL migration is involved.
- Tests: added Configuration semantic-load/write/compatibility tests and the nonmutating image-validation regression; added Web TestHost validation/compatibility tests; extended Setup initializer and disposable-provider tests, plus Email sender fallback coverage. Updated incomplete fixtures to valid settings while retaining failure/rollback assertions.
- Documentation: updated README recovery/validation behavior, engineering-reference constraints, test coverage, B0 resolved Q3-Q4/handoff, and this tracker. Permanent docs do not depend on temporary task records.

Verification (repository root; builds run sequentially):

| Check | Exact command / environment | Result |
| --- | --- | --- |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 124 passed, 0 failed, 0 skipped. |
| Setup with both supported providers | Set `MOONGLADE_TEST_CATEGORY_DATABASES=1` for the command process; `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | 127 passed, 0 failed, 0 skipped. Includes four new semantic recovery/protected-preservation provider cases and all B1-B2 regressions. |
| Web | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 281 passed, 0 failed, 0 skipped. |
| Auth | `dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj --no-restore` | 48 passed, 0 failed, 0 skipped. |
| Email | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj --no-restore` | 105 passed, 0 failed, 0 skipped. Null/empty/whitespace names use the deployment sender name; explicit names take precedence. |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed, 0 warnings, 0 errors. |
| Diff / formatting | `git diff --check`; staged whitespace and CRLF/final-newline checks on batch files | Passed. |

Acceptance evidence and remaining limits:

- All nine ordinary groups have semantic-recovery and invalid direct-write cases. Enum, menu graph/count, required/range, conditional script/CSS, UTF-8 byte boundaries, and approved compatibility cases pass. Protected load/write cases reject invalid credentials, secrets, and timestamps without including stored sensitive values in errors.
- Both PostgreSQL and SQL Server preserve every setting row/timestamp and the seeded post ID when account semantic validation fails, even with another group needing repair. When protected state is valid, only the invalid feed group is rewritten, with its valid sibling field also reset to the group default.
- Actual HTTP tests verify 400 validation problems for model errors and parsed-menu command errors, no activity-log command on rejection, and successful saves for blank sender names, disabled null menus, and long generated avatar URLs. Direct command tests verify unchanged persistence/runtime and rejection of an invalid second group before either row is written.
- Early test runs exposed incomplete pre-validation fixtures; these were corrected without weakening save-failure or cancellation expectations. One such fixture prevented the concurrency interceptor from being reached; its wait now has a bounded timeout. An Email test nullable-context warning was corrected; final runs and build are clean.
- Disposable provider containers were removed by their fixture (verified with no running containers after completion). No real blog database, SMTP send, production operation, or deployment was performed. Browser/form interaction and full-solution regression remain B6 scope; HTTP behavior here uses TestHost.
- Intentional risk: startup now treats semantically invalid ordinary stored groups as recoverable corruption and persists full defaults; reverting code cannot restore values already replaced. Restore such values from backup if needed. Invalid protected state instead requires manual restoration; arbitrary old usernames and version strings remain supported under D18.
- Commit: this entry is part of the B3 batch commit titled `fix(configuration): validate settings consistently`; record its actual hash at the next tracker update rather than amending to embed its own hash.
- User acceptance: pending. Present the local commit and verification evidence, then await acceptance / instructions. Do not start B4, resolve Q5, or alter audit/cache behavior automatically.
- Rollback: revert this batch commit if required. There is no schema migration to undo. A code revert does not reconstruct values already changed by automatic recovery or later saves.

### 2026-11-05 - B4 Implementation and Verification Complete

- Authorization: explicit B4 execution request and explicit answers approving D19-D21. B3 is commit `fe32fd43f`; B5-B6 and optional structural work remain unauthorized.
- Every changed settings save now publishes one `ConfigurationChangedEvent` after database persistence and runtime replacement, while still within the existing process write lock. Publication ignores request cancellation after commit. Invalid, missing-row, failed-save, pre-commit cancellation, and no-op writes do not publish. Internal avatar/CDN writers reach the same command boundary without controller-specific invalidation.
- The Web event handler refreshes feed generations for General/Feed/Image settings, sitemap for General settings, theme CSS for Appearance settings, and wakes scheduled publishing for Advanced settings. Related Image/General writes refresh feeds once. Article caches are preserved by settings-only feed changes; ordinary post changes still invalidate posts, feeds, and sitemap together.
- Added a separate feed generation to the existing `PostCacheInvalidator` and a small `ThemeCacheInvalidator`. Public/admin/sign-in/authenticator layouts all read versioned theme keys; older in-flight feed/theme factories can only populate obsolete generations. Theme create/delete invalidates only after successful commands, before activity logging. Removed the appearance action filter and the unused controller-only CurrentThemeColor reset.
- D19: FOAF/sitemap route registration and email singleton lifetimes remain unchanged. Added missing restart hints for sitemap and email display name, retaining FOAF's hint. A shared settings note distinguishes subsequent operations and downstream cache retention; the scheduler hint explains that running publication is not canceled. New text is localized in all four supported non-English resource files.
- D20: the scheduler captures its wake token before inspecting configuration, can be woken while disabled, and rechecks the enable flag/cancellation before dispatch. A settings wake restarts its timing query rather than blindly publishing. Already dispatched commands receive the host stopping token, not the settings wake token, and are not revoked by toggling the setting.
- D21: full feeds select `PostContent` rather than formatting `ContentAbstract`; summary mode still selects the abstract. The flag used for selection is also used for formatting. HTML/Markdown formatting and CDN rewriting are preserved.
- Updated command/DI and cache fixtures, including the real LiteBus registration test. The first expanded Web runs exposed a pre-existing image-test shutdown race: it stopped the background queue before asserting storage. The test now waits with a bounded completion signal for the actual original-image storage invocation before shutdown, retaining the byte/name assertion; no production queue semantics were changed.
- Documentation: updated README activation/cache behavior, engineering-reference event and generation rules, test coverage, B0 Q5/handoff, and this record. No schema, stored key, JSON field, dependency version, endpoint URL, authentication policy, or antiforgery behavior changed. No SQL upgrade script is required.

Verification (repository root; builds run sequentially):

| Check | Exact command / environment | Result |
| --- | --- | --- |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 129 passed, 0 failed, 0 skipped. |
| Web | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 297 passed, 0 failed, 0 skipped. Final run includes updated feed-generation assertions and image-test synchronization. |
| Syndication | `dotnet test --project src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj --no-restore` | 18 passed, 0 failed, 0 skipped. Includes distinct body/abstract and HTML/Markdown CDN cases. |
| Background services | `dotnet test --project src/Tests/Moonglade.BackgroundServices.Tests/Moonglade.BackgroundServices.Tests.csproj --no-restore` | 14 passed, 0 failed, 0 skipped. Three existing nullable-dereference warnings in unchanged CannonServiceTests.cs remain; no new warnings from scheduler tests. |
| Auth | `dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj --no-restore` | 48 passed, 0 failed, 0 skipped. |
| Email | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj --no-restore` | 106 passed, 0 failed, 0 skipped. Confirms an already-resolved sender name changes only after services are recreated. |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed, 0 warnings, 0 errors. |
| Localization / formatting | Parse all four resource XML files; verify each new activation key occurs once; `git diff --check`, staged whitespace and CRLF/final-newline checks | Passed. Existing BOMs retained. |

Behavior evidence:

- Warm RSS/Atom and both category variants refresh after feed-count/full-content saves. Real feeds contain one selected entry and the actual body marker. General/Image paired saves refresh metadata, author email, canonical sitemap URLs, and CDN image content without a second feed-generation advance.
- Invalid/no-op/failure paths publish no event. Commit-time cancellation still publishes after persisted and runtime values agree, and Web integration verifies that active feed keys change even when the caller has been canceled. An internal SaveAssetToCdnHandler save also advances feed keys.
- Appearance changes return new actual CSS; unrelated comment settings preserve theme/post/feed generations. Old in-flight feed/theme factories cannot refill the active generation. Theme command conflict/not-found paths preserve the theme generation.
- Scheduler tests synchronize on actual waits and dispatches: disabling a waiting worker skips dispatch and keeps it running; enabling a disabled worker dispatches due work within a bounded test timeout instead of five minutes; disabling after dispatch does not cancel the command token and the worker returns to its disabled wait.

Browser acceptance (Playwright CLI, real compiled host, disposable PostgreSQL 18):

- Used HTTPS loopback port 11584, independent images/originals directories, and a clean temporary content root containing only the committed base configuration. Development's existing TOTP bypass was enabled only for this isolated host. The repository's local Development override was not loaded, read, or copied. SMTP was disabled. No real blog database or external service was used.
- Signed in through the actual UI. Advanced settings displayed both restart hints and the scheduler explanation. Disabled FOAF/sitemap/scheduler, saved, observed the success toast, and refreshed to verify persisted off values. Both endpoints stayed HTTP 200 before restart and became HTTP 404 after restart.
- Re-enabled all three through the UI and saved. FOAF/sitemap remained HTTP 404 until the next restart, then both returned HTTP 200. Their route-registration contract is verified in both directions.
- Changed notification name to a synthetic acceptance value, saved, and refreshed. The value persisted and the restart hint remained visible. No email was sent; the Email DI test separately proves sender-name lifetime.
- Changed Word Blue to Excel Green and enabled custom CSS using the actual editor. After saving and reloading, the admin computed accent changed from `#2A579A` to `#165331`; public and sign-in HTML also contained the new accent. `/custom.css` returned HTTP 200, a content ETag, and the saved synthetic CSS marker.
- Warmed actual RSS and Atom while summary mode was active; neither contained the seeded article's body heading. Enabled full content and saved through the subscription page; both feeds then contained that body heading without restarting or waiting for cache expiry.
- Inspected an Advanced-page screenshot: the shared note and field-specific hints fit the existing layout. Temporary browser artifacts are under `C:\Users\Edi\AppData\Local\Temp\moonglade-b4-0c9add94338342c09a1b29229de53d10` on this workstation; this path is optional evidence, not required for resuming work. The browser session and host were closed, the disposable container removed, the generated database credential state deleted, and no listener remained on the acceptance port.
- Initial browser setup used a non-Development debug host and exposed unavailable debug static assets; the final acceptance used the isolated clean content root above and the real UI scripts. This was a local acceptance setup correction, not an application workaround or production change.

Limits, commit, and handoff:

- Total automated results: 612 passed. Full-solution regression remains B6. B4 did not rerun SQL Server containers because no schema/provider change was made; the new relational cache tests use SQLite and actual browser acceptance used PostgreSQL. B3's prior provider results remain historical evidence only.
- No SMTP delivery, production operation, distributed synchronization, browser/CDN/feed-reader cache purge, historical image reprocessing, or cancellation of already dispatched publication was attempted. Invalidation remains local to one process. Mutable singleton/snapshot concerns remain optional C1, and asset/file transaction concerns remain optional C3.
- Activity-log failure handling remains B5; this batch only ensures invalidation/wake occurs at successful persistence before later auditing. Do not infer B5 completion from the event boundary.
- Commit: this entry is part of the B4 implementation commit titled `fix(settings): invalidate affected caches after save`. Record its hash at the next tracker update; do not amend merely to embed a commit's own hash.
- User acceptance: pending. Present the commit and evidence, then await acceptance / further instructions. Do not start B5-B6 automatically.
- Rollback: revert this batch commit. No schema migration is involved; the code revert does not reverse later saved settings. All manual acceptance data was disposable and has been removed.
