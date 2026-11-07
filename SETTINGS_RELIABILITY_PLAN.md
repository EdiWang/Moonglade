# Settings Task Tracker

## Resume Here

- Last reconciled: 2026-11-07, Asia/Taipei.
- Committed application baseline: `4b3185757` (`Keep settings consistent per operation and render`), on `master`. Documentation cleanup was already uncommitted when the maintenance batch below started; preserve those edits and deletions.
- Reliability and structural implementation through S3 is complete and committed. Final functional acceptance is pending.
- Maintenance review items 1, 4, 6, and the duplicate initialization-log wrapper are implemented and verified, but uncommitted. Removed ineffective legacy cache-key deletions, required an explicit snapshot in the feed data source, removed the ordinary loader's unused `isNew`, and inlined the initialization command call. Paired writes, S0 probes, and parsing/publication structure remain unchanged.
- Next acceptance batch: **S6**, defined in [the acceptance plan](SETTINGS_STRUCTURE_PLAN.md). S6 has not started and requires explicit authorization. This maintenance batch does not authorize S6 execution, commits, deployment, or production-data changes.
- Retain this tracker and update status, verification, remaining issues, and the next action at every batch boundary. A new conversation should read this file, `AGENTS.md`, and the relevant engineering-reference sections first.
- No design question is currently pending. Ask about new requirement, business, architecture, or recovery-policy choices, with a recommendation; continue independent authorized work while waiting.

## Progress

| Batch | Result | Commit |
| --- | --- | --- |
| B0-B6 | Reliability complete: safe startup/install, defaults, validation, activation/cache behavior, audit isolation, and failed-startup host protection. | Final reliability baseline `907ac2285` |
| S0 | Binding, immutable-value, rendering, and commit-boundary compatibility checks complete. | `90e302d45` |
| S1 | Account and installation history separated from ordinary preferences in code. | `8db04575d` |
| S2 | Immutable ordinary values and atomic root publication; avatar CDN support removed. | `aa38e96f7` |
| S3 | Operation/render capture propagation, reader migration, and configuration-dependent cache coherence complete. | `4b3185757` |
| S4-S5 | Not applicable: uploaded avatars no longer have a CDN/file workflow. | No implementation required |
| Maintenance | Approved review items 1, 4, 6 and duplicate log wrapper complete; focused tests and Web build passed, with provider tests skipped as recorded below. | Uncommitted |
| S6 | Final isolated-host/browser regression and acceptance; not started. | Await authorization |

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
- Inside the shared process write gate, apply submitted groups to the latest root, validate, persist, publish once, then emit the established noncancelable change event. Preserve unrelated groups and timestamps for no-op saves. Same-group saves replace the whole group; later saves win. Failed/rejected writes do not publish.
- Preserve model-owned validation: existing required/range/length/format and conditional rules, defined enums only, nonnegative feed count with zero unlimited, at most five menu entries, required titles/submenu URLs, and non-null entries/submenu lists. Disabled menus may be null. CSS is limited to 10,240 UTF-8 bytes; watermark text/font rules apply even when disabled. Blank notification display names and unknown themes retain their fallbacks; inactive legacy avatar URL data has no added length cap.
- Saved account username/hash/salt must be nonblank; populated TOTP secrets must parse and enabled TOTP requires one. Generated-but-disabled setup is valid. Do not impose new-credential form syntax on saved accounts. Manifests require a non-minimum UTC installation time while retaining arbitrary, empty, or null versions.

### Activation, Auditing, And Avatars

- FOAF/sitemap endpoint toggles and singleton email sender display-name changes require restart. Scheduler saves wake the worker; capture per wait iteration and recheck a fresh enable state before dispatch. Do not revoke already dispatched work.
- An operation's notification decisions use its captured switches; new independent operations may see changes. Previously queued email keeps its payload. Preserve password session retention and authenticator-reset sign-out of all three local schemes.
- After successful settings/avatar persistence, audit failure returns success and logs Error. Validation, persistence, cache, storage, and sign-out failures are not audit-only failures.
- Config-dependent cache keys include relevant captured inputs alongside invalidation generations: General/Feed/Image for feeds, General for sitemap, and theme ID for theme CSS. Cached tag HTML varies by hot-tag count. Preserve unrelated article caches and normal expirations.
- Uploaded avatars use the database asset and local `/avatar` route, with the bundled default when absent. Preserve PathBase and Gravatar behavior. Image CDN settings affect article images, not avatars; toggling them does not migrate avatars or write General settings. `AvatarUrl` remains inactive compatibility data. Existing copied avatar files are not automatically deleted. Do not restore avatar CDN orchestration or versioned-file cleanup.

## Verification Baseline

Maintenance checks on 2026-11-07:

- Ran `dotnet test --project <path> --no-restore` for `src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` (398 passed), `src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj` (107 passed), `src/Tests/Moonglade.Syndication.Tests/Moonglade.Syndication.Tests.csproj` (18 passed), and `src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` (75 passed, 17 skipped). Total: **598 passed, 0 failed, 17 skipped**. The duplicate ordinary-loader true/false theory is now one case because its unused flag was removed.
- Cache tests now warm production keys with captured-setting fingerprints/theme IDs and check active-generation isolation instead of requiring old General entries to be deleted. Existing concurrent query/render tests passed.
- `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore`: **0 warnings, 0 errors**. `git diff --check` passed.
- Full-solution, opt-in Docker provider tests, and S6 browser/real-host acceptance were not rerun. The 17 Setup skips are the opt-in disposable-database cases; no provider-specific or schema behavior changed. No commit, deployment, or real-data operation was performed.

Prior S3 implementation checks, retained as the full-solution baseline:

- Full solution: **1492 passed, 0 failed, 0 skipped**, with disposable PostgreSQL and SQL Server groups.
- Focused Web suite: **398 passed**, including the final supplied-snapshot assertions and actual cached Razor rendering.
- Web build: **0 warnings, 0 errors**. Earlier solution compilation reported three nullable warnings in unchanged `CannonServiceTests.cs` (88/188/226).
- Coverage includes startup rollback/retry/lost acknowledgment, defaults/validation, protected state, save failure/cancellation/audit, local avatars, scheduler and restart rules, and paused feed/render/image/comment operations across publication.
- Browser/manual real-host acceptance remains S6. Passing automated tests is not final user acceptance.

## References And Limits

- Durable contracts and task-specific rules: [engineering reference](docs/agent-reference.md#configuration).
- Configuration and local runtime: [README](README.md#configuration).
- Test layout, provider prerequisites, and commands: [test guide](src/Tests/README.md).
- Production entry points: `Moonglade.Configuration` snapshots/writer, `Moonglade.Auth` account state/writer, `Moonglade.Setup` initialization, `Moonglade.Web/Rendering` and `Filters/BlogSettingsFilter.cs`, Syndication handlers/data source, and Web cache invalidators.
- Multi-instance synchronization is a risk to assess, not implemented scope. No deployment or real-data operation is authorized.
- Settings commands publish after their own `SaveChangesAsync`; arbitrary caller-owned outer transaction composition remains unsupported. Preserve its characterization test in `Structure/CommitBoundaryProbeTests.cs`; do not silently redesign that boundary during acceptance.
- Roll back related callers, contracts, registration, views, and cache changes together. Stored data remains compatible; the immutable settings and local-avatar behavior remain the current baseline.
