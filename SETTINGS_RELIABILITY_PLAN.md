# Settings Task Tracker

## Resume Here

- Last reconciled: 2026-11-07, Asia/Taipei.
- S6 started from clean HEAD `d81240a86`; the scheduler fix and S6 records are committed in `65e13ad43`. Current HEAD is `bf7e6399a` (`Remove profile pronouns setting`), on `master`; the subsequent test cleanup below is uncommitted.
- Reliability and structural implementation through S3 is complete. S6 technical verification passed within the agreed deployment scope; final user sign-off and temporary-file cleanup remain pending.
- Maintenance review items 1, 4, 6, and the duplicate initialization-log wrapper are committed in `fe22d13ce`.
- Review items 2 and 3 are committed in `701b3be04`. Ordinary commands retain one immutable settings group; production regression coverage supersedes the S0 candidate implementation.
- Review item 5 is committed in `cacdae4f1`. Pure `BlogConfig.Parse` returns the snapshot and default JSON; initialization repairs once, then publishes the prepared snapshot/account without re-parsing. First-install transactions, commit verification, and the subsequent committed-data reload remain. No new loader interface, registry, or batch-write framework was added.
- **S6 technical checks passed:** full solution with both providers **1465 passed, 0 failed, 0 skipped**; isolated PostgreSQL host/Chrome checks passed. The user authorized fixing overdue scheduled posts missing immediate wake-up publication. The next-time query now includes overdue, nondeleted schedules with timestamps; five new regression cases and the rebuilt live-host reproduction passed.
- The user accepted the existing root-path deployment limit. Full subpath hosting is outside this work; do not add forwarded-prefix or client-URL changes without a new decision. Local authentication/TOTP was exercised live; external OIDC sign-in was not provisioned.
- Follow-up UI cleanup: removed Pronouns from the model, editor, profile, CSS and translations. The user explicitly chose to ignore legacy JSON without database migration or startup rewrites; a later General save drops the field. Per the user's test policy, removed the Pronouns-only test, old-member/interface absence assertions and redundant retired avatar-CDN cases; retained actual behavior, security, failure and migration coverage. Focused verification: **105 Configuration, 63 Auth and 363 Web tests passed** (531 total), no failures/skips; diff check passed. Full-solution/provider/browser acceptance was not repeated; S6 evidence predates these changes. The test policy is recorded in `AGENTS.md`.
- Next actions: final user sign-off and cleanup of `C:/Users/Edi/AppData/Local/Temp/moonglade-s6-062e5864aadc`. Automatic approval review rejected deletion with "blocked by policy" after target verification. Containers/volumes/network, proxy and browser session are already removed; only that temporary directory remains. Do not print its test credentials. See [S6 evidence](SETTINGS_STRUCTURE_PLAN.md). No commit, deployment, or production-data change is authorized.
- Retain this tracker and update status, verification, remaining issues, and the next action at every batch boundary. A new conversation should read this file, `AGENTS.md`, and the relevant engineering-reference sections first.
- No design question is currently pending. Ask about new requirement, business, architecture, or recovery-policy choices, with a recommendation; continue independent authorized work while waiting.

## Progress

| Batch | Result | Commit |
| --- | --- | --- |
| B0-B6 | Reliability complete: safe startup/install, defaults, validation, activation/cache behavior, audit isolation, and failed-startup host protection. | Final reliability baseline `907ac2285` |
| S0 | Compatibility exploration complete; production regression coverage now supersedes the candidate models and Razor fixtures. | Original probes `90e302d45` |
| S1 | Account and installation history separated from ordinary preferences in code. | `8db04575d` |
| S2 | Immutable ordinary values and atomic root publication; avatar CDN support removed. | `aa38e96f7` |
| S3 | Operation/render capture propagation, reader migration, and configuration-dependent cache coherence complete. | `4b3185757` |
| S4-S5 | Not applicable: uploaded avatars no longer have a CDN/file workflow. | No implementation required |
| Maintenance 1 | Review items 1, 4, 6 and duplicate log wrapper complete; focused tests and Web build passed, with provider tests skipped as recorded below. | `fe22d13ce` |
| Maintenance 2 | Review items 2 and 3 complete; Configuration/Web regression tests and Web build passed as recorded below. | `701b3be04` |
| Maintenance 3 | Review item 5 complete; full automated acceptance below includes the previously skipped database groups. | `cacdae4f1` |
| Automated acceptance | Superseded by S6 final regression: 1465 passed with both providers, no failures/skips. | Earlier evidence `d81240a86` |
| S6 | Technical verification passed; overdue-scheduler fix verified. User sign-off and policy-blocked temporary-directory cleanup pending. | `65e13ad43` |
| UI cleanup | Shared notice and Pronouns feature removed. Deletion-only test cleanup and focused verification are recorded above. | Notice `8ce2f9b67`; Pronouns `bf7e6399a`; test cleanup uncommitted |

## Rules To Preserve

### Startup And Recovery

- Target a single-instance personal blog, with PostgreSQL and SQL Server support. Initialization is allowed only after proving all mapped application tables empty, including an existing empty schema. Failed state queries do not prove emptiness. Preserve ambiguous older partial installations for manual recovery.
- First-install seeds and all eleven settings rows share one provider execution-strategy transaction. Roll back pre-commit failures and allow retry; verify uncertain commit outcomes before reseeding. Schema creation and noncritical site-icon generation remain outside that data transaction.
- With existing data, missing account settings or an empty settings table stops startup before repairs. Invalid account/manifest data also stops startup. Preserve data, log a safe diagnostic, exit nonzero, and start no HTTP or business workers.
- With valid account/other settings but no manifest, reconstruct unknown version and current reconstruction time with Warning. Never invent original installation history or default credentials for an existing site.
- Missing ordinary groups or fields use the group's current `DefaultValue`. Valid explicit false, zero, null, and empty arrays remain explicit; valid partial JSON and timestamps are not rewritten. Invalid ordinary groups recover whole-group defaults with Warning, including replacement of otherwise valid sibling fields. Do not log stored JSON or secrets.

### Ownership, Validation, And Writes

- Configuration owns nine ordinary groups; Auth owns accounts; Setup owns installation history and may reference Auth. Preserve the existing table, keys, and JSON. Physical separation or data migration requires a new decision.
- `IBlogConfig` exposes only `Capture()`. Ordinary values and nested menus are init-only records with immutable lists. Capture once before asynchronous work and propagate the same root/group through related calls. Independent subsequent operations may capture new values; do not freeze the whole HTTP request.
- Razor handlers, views, layouts, partials, and components share the ViewData capture. Preserve a capture already used to prepare page data. Internal command/query/event snapshot contexts are JSON-ignored and must not enter HTTP or durable email payloads.
- Each ordinary command retains one immutable settings group. Inside the shared process write gate, validate it, apply it to the latest root, serialize once for persistence, save, publish once, then emit the established noncancelable change event. Preserve unrelated groups and timestamps for no-op saves. Same-group saves replace the whole group; later saves win. Failed/rejected writes do not publish.
- Preserve model-owned validation: existing required/range/length/format and conditional rules, defined enums only, nonnegative feed count with zero unlimited, at most five menu entries, required titles/submenu URLs, and non-null entries/submenu lists. Disabled menus may be null. CSS is limited to 10,240 UTF-8 bytes; watermark text/font rules apply even when disabled. Blank notification display names and unknown themes retain their fallbacks; inactive legacy avatar URL data has no added length cap.
- Saved account username/hash/salt must be nonblank; populated TOTP secrets must parse and enabled TOTP requires one. Generated-but-disabled setup is valid. Do not impose new-credential form syntax on saved accounts. Manifests require a non-minimum UTC installation time while retaining arbitrary, empty, or null versions.

### Activation, Auditing, And Avatars

- FOAF/sitemap endpoint toggles and singleton email sender display-name changes require restart. Scheduler saves wake the worker; capture per wait iteration and recheck a fresh enable state before dispatch. Do not revoke already dispatched work.
- An operation's notification decisions use its captured switches; new independent operations may see changes. Previously queued email keeps its payload. Preserve password session retention and authenticator-reset sign-out of all three local schemes.
- After successful settings/avatar persistence, audit failure returns success and logs Error. Validation, persistence, cache, storage, and sign-out failures are not audit-only failures.
- Config-dependent cache keys include relevant captured inputs alongside invalidation generations: General/Feed/Image for feeds, General for sitemap, and theme ID for theme CSS. Cached tag HTML varies by hot-tag count. Preserve unrelated article caches and normal expirations.
- Uploaded avatars use the database asset and local `/avatar` route, with the bundled default when absent. Preserve PathBase and Gravatar behavior. Image CDN settings affect article images, not avatars; toggling them does not migrate avatars or write General settings. `AvatarUrl` remains inactive compatibility data. Existing copied avatar files are not automatically deleted. Do not restore avatar CDN orchestration or versioned-file cleanup.

## Verification Baseline

S6 on 2026-11-07: `d81240a86` plus the uncommitted scheduler fix and five regression cases.

```powershell
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --solution src/Moonglade.slnx --no-restore
dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore
```

- Full solution: **1465 passed, 0 failed, 0 skipped**, across all 15 assemblies, 40.068 seconds. PostgreSQL 18 and SQL Server 2022 groups ran, including initialization rollback/retry/lost acknowledgment, existing-data preservation, partial JSON/timestamps and manual upgrade suites. Focused Features/Web suites passed 120/367 cases.
- Web build: **0 warnings, 0 errors**. Full test compilation still reported three existing CS8602 warnings in unchanged `CannonServiceTests.cs` (88/188/226). `git diff --check` passed.
- Real Chrome/HTTPS/PostgreSQL host checks passed all nine settings UI saves, validation errors, menus/defaults, next-render refresh, local sign-in/TOTP/password/session/reset/rate limits, avatar/settings isolation, article watermark/original/CDN/ETag behavior, Gravatar, feeds, sitemap/theme/tag caches, endpoint/sender-name restart rules and the fixed scheduler. Seven startup preservation/repair/failure scenarios passed. Fault/race scenarios reuse passing automated tests; see [detailed evidence](SETTINGS_STRUCTURE_PLAN.md).
- Accepted limit: deployment at the URL root. URL helpers remain PathBase-aware, but a forwarded prefix alone does not configure full subpath hosting. This is documented in README and the engineering reference. No external OIDC tenant was exercised; live authentication used Local with required TOTP.
- Ignored local evidence: `output/playwright/s6/final-tests.log`, `recovery-results.json`, `public.png`, `settings-general.png`. Enrollment snapshots and console artifacts were removed. Task-created containers/anonymous volumes/network, proxy and browser session were removed; shared Docker image caches remain.
- Cleanup exception: automatic approval review blocked deletion of the verified temporary directory `C:/Users/Edi/AppData/Local/Temp/moonglade-s6-062e5864aadc`, returning only "blocked by policy". It still contains the isolated publish, synthetic images, scripts, database backup and credentials/certificate for destroyed test services. Do not print or reuse them. Manual or permitted cleanup and final user acceptance are still pending.
- No production data, configuration, credentials, deployment or commit changed. Only the user-authorized scheduler fix and its tests/documents are in the working tree.

## References And Limits

- Durable contracts and task-specific rules: [engineering reference](docs/agent-reference.md#configuration).
- Configuration and local runtime: [README](README.md#configuration).
- Test layout, provider prerequisites, and commands: [test guide](src/Tests/README.md).
- Production entry points: `Moonglade.Configuration` snapshots/writer, `Moonglade.Auth` account state/writer, `Moonglade.Setup` initialization, `Moonglade.Web/Rendering` and `Filters/BlogSettingsFilter.cs`, Syndication handlers/data source, and Web cache invalidators.
- Multi-instance synchronization is a risk to assess, not implemented scope. No deployment or real-data operation is authorized.
- Settings commands publish after their own `SaveChangesAsync`; arbitrary caller-owned outer transaction composition remains unsupported. Preserve its characterization test in `Structure/CommitBoundaryProbeTests.cs`; do not silently redesign that boundary during acceptance.
- Roll back related callers, contracts, registration, views, and cache changes together. Stored data remains compatible; the immutable settings and local-avatar behavior remain the current baseline.
