# Settings Structural Plan (B7 / C1-C3)

## Resume And Authorization

This is a retained task plan, requested as a continuation of the settings review. Read `AGENTS.md`, `SETTINGS_RELIABILITY_PLAN.md`, and applicable engineering-reference sections before continuing.

- Date: 2026-11-06, Asia/Taipei. Assessment baseline: `907ac228573e151b3dfcc3f334a15d72959cc2e9` on `master`, initially clean working tree.
- B5 is `8da96cf82`; B6's startup-safety correction is already committed as `907ac2285`. The previous tracker's statements that B6 was uncommitted were stale, not missing implementation.
- Current authorization: the user explicitly authorized S3 after its operation/render consistency scope was explained. S2 is committed as `aa38e96f7`; S3 began with a clean tree at that commit. S6, commits, push, deployment, and production-data changes remain separately gated. Avatar CDN work stays superseded by D31.
- D25: separate code responsibilities without changing the existing database table, keys, or JSON. D26: consistency within one captured business operation/render, not automatic request-wide freezing. D27 originally proposed versioned avatar files; D31 supersedes that avatar-CDN scope by removing the feature. D28 confirms existing ordinary types as init-only records with ImmutableList menus, adding mutable request DTOs only for demonstrated incompatibility.
- D29 confirms the S1 ownership/dependency direction below. Later snapshot and avatar workflow batches remain separately gated; planning recommendations do not authorize all of B7.
- Current next action: present completed S3 and obtain acceptance / further instructions. Full solution: 1492 passed with both disposable providers, no failures/skips; Web build clean. S6 is not authorized.
- Keep this file and the reliability tracker across conversations. Permanent project documentation must not depend on these temporary task records.

## B5/B6 Assessment

| Area | Current evidence | Assessment |
| --- | --- | --- |
| Settings/avatar audit isolation | B5 helper catches audit dispatch only, after successful writes. Settings/image/password/reset and avatar use it; site-icon/unrelated audit paths retain their existing semantics. Fault tests check actual saved state, Error context, sign-out, and failures outside the catch. | No blocking defect found in the reviewed scope. |
| Startup failure | B6 throws after Critical logging before `Program` maps endpoints or calls `RunAsync`. Host-boundary tests assert workers never start and settings/scheduled content remain unchanged; success leaves host startup with the caller. | D23 is implemented and committed. |
| Current full regression | On 2026-11-06: `MOONGLADE_TEST_CATEGORY_DATABASES=1`; `dotnet test --solution src/Moonglade.slnx --no-restore`: 1412 passed, 0 failed, 0 skipped; both disposable provider groups ran. | Matches the recorded final B6 count at the actual committed HEAD. |
| Current Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet`: 0 warnings, 0 errors. | Passed. |
| Existing limitations | Three pre-existing nullable warnings in unchanged `CannonServiceTests.cs`. Earlier B6 host/browser scenarios are recorded evidence, not newly repeated browser tests. No new SMTP/deployment/physical network-failure verification. | Distinguish technical readiness from user functional acceptance. |

The present code is a suitable reliability baseline for planning. Structural changes are optional improvement work, not a prerequisite to operating the current single-instance blog. This assessment does not certify future designs or silently mark final functional acceptance complete.

## Source Findings And Intended Reach

| Finding | Source / impact | Planned response |
| --- | --- | --- |
| Public readers can replace groups or invoke loading | `IBlogConfig` in `Configuration/BlogConfig.cs` exposes eleven setters and `LoadFromConfig`; the update handler uses reflected per-property assignment. | Separate read access from initialization/publication. Publish one prepared ordinary snapshot after commit. |
| Read-only properties alone would be insufficient | Models remain mutable; `CustomMenuSettings.Menus` is an array and each `Menu.SubMenus` is a mutable list. | Make the published graph immutable, including nested values, not merely its root reference. |
| Rendering currently changes global state | `_Layout.cshtml` and `_LayoutAdmin.cshtml` assign a fallback to `GeneralSettings.AvatarUrl`; profile/admin/general views consume the mutated result. | Resolve fallback during rendering and pass the resolved value, without persistence or singleton mutation. Preserve existing root/path behavior. |
| Read migration is cross-module | Current inventory: 40 production files explicitly reference `IBlogConfig`, and 33 Razor files reference `BlogConfig` (overlapping inventories, not additive totals). Email, syndication, workers, Auth, Setup, Features, and middleware all consume settings. | Re-run the inventory for every batch; capture once for related reads and propagate the render snapshot to layouts/partials/components. |
| Protected state is exposed as ordinary preferences | Account and manifest models sit in Configuration, share the loader/list/update mechanisms, and appear on the general reader. | Give credentials and installation history explicit readers/writers and ownership while preserving legacy storage. |
| Install safety crosses all eleven rows | `ConfigInitializer` stages all validation before repair writes; `StartUpInitializer` seeds and initializes in one provider-retried transaction, then publishes after commit. | Preserve the complete preflight and transaction; do not initialize each new owner independently or publish partial installation state. |
| Avatar workflow is split | `AssetsController.Avatar`, `SettingsController.Image`, `SaveAssetToCdnHandler`, and the asset persistence event handler coordinate different parts. The CDN handler deletes a stable file before uploading its replacement. | One explicit application workflow for avatar/CDN changes, with existing asset/storage boundaries and controlled sequencing. |
| A naive outer transaction would break publication timing | `UpdateConfigurationCommandHandler` treats its `SaveChangesAsync` as the commit boundary, then updates runtime and publishes cache events. | Do not wrap this handler in an outer transaction and call publication complete before that transaction commits. Stage relevant rows and commit once, or explicitly separate preparation/commit/publication where necessary. |

The existing post-save command/event pattern, grouped JSON, concrete startup services, provider-neutral storage boundary, and versioned caches remain useful. Do not introduce a settings framework, a provider registry, a new project, per-read JSON cloning, or a distributed transaction to perform this work.

## Proposed Target And Alternatives

### C1: Immutable Ordinary Settings And Atomic Publication

Recommended direction:

1. Ordinary readers capture a root snapshot containing the nine ordinary groups. Only the owning initialization/write path can publish a replacement. Prepare and validate the complete candidate before the database commit; after confirmed commit, replace the root reference once, then run the established post-commit invalidation/wake event.
2. Readers retain one snapshot for the logical operation. Feed generation captures General/Feed/Image together; image delivery captures Image/General together. Workers capture per operation/iteration, not once for their lifetime. Email's intentional restart-bound sender name is not made dynamic accidentally.
3. One page render passes its captured snapshot through layouts, partials, and components. A separately captured operation later in the same request may see the newly committed snapshot. Removing only setters, or reading `Current` again for each field, does not satisfy D26.
4. Prefer making the existing ordinary value models immutable when MVC binding, validation, Razor helpers, and historical JSON tests prove this works. Use immutable nested menu values/collections. If an actual binding incompatibility requires a mutable request model, add it only at that boundary and reuse the same validation rules; avoid blanket duplication of every settings model.
5. Exclude credentials and installation history from the ordinary snapshot after S1. Retain their separate validation/lifecycle safeguards. Published snapshots must not be serialized into diagnostics or frontend data as a convenience.

Alternatives considered:

| Alternative | Benefit | Limit / reason not preferred |
| --- | --- | --- |
| Get-only root with mutable children | Small signature change | Callers can still mutate fields/menus; not C1 completion. |
| Deep-copy every read | Existing mutable request models remain usable | Recurring allocation/serialization and awkward related-read consistency; unnecessary for a personal blog. |
| Separate immutable type for every stored/request model immediately | Strong type separation | Extensive duplicate fields/mappings before demonstrating a need. Keep as fallback for proven boundary incompatibility. |
| Request-wide frozen scoped settings | Automatic same-request consistency | Not the user's D26 choice; a post-save operation in the same request may intentionally capture current state. |

`record`/`init` alone provides only shallow immutability; an exposed array can still change. The collection representation must be proven with the current serializer and validators. Official references: [C# records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record), [immutable JSON types](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/immutability), [supported collection types](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-types).

### C2: Protected-State Responsibility Separation Without Migration

Ownership confirmed in D29: Configuration owns ordinary preferences; Auth owns account operations and restricted account reads; Setup owns installation-history initialization/reconstruction. Data retains the existing entity/table/provider mappings. Reuse existing projects and real application-boundary contracts; do not add interfaces solely for tests.

- Preserve `LocalAccountSettings` and `SystemManifestSettings` keys, all JSON fields, timestamp behavior, and valid old usernames/versions. Namespace/file moves must not rename storage keys. A new general registry is not necessary merely to move code.
- Ordinary templates and preference consumers should no longer receive credentials or a manifest initializer. Password/TOTP operations use the account boundary; startup uses the protected-state boundary; installation history is not made an administrator preference.
- Admin account views receive only the username/status they need, not hash/salt/secret objects. The dedicated authorized authenticator-enrollment flow must still render its setup key/QR as intended; separating ordinary readers must not break enrollment.
- Keep all validation/preflight checks before any ordinary repair. Missing/invalid protected state must fail the host before workers or repair writes. Missing manifest reconstruction still means unknown version and reconstruction time with Warning.
- Keep fresh seed data, ordinary defaults, account, and manifest on the same scoped database context and first-install transaction. Verify provider retries and lost commit acknowledgment against all expected rows; splitting code ownership must not split this transaction.
- Review the dependency graph before moving types. Auth references Configuration; D29 approves the acyclic Setup-to-Auth edge implemented in S1. Configuration must not acquire a reverse reference to Auth/Setup just to preserve old properties.
- Preserve commit/runtime ordering and last-save-wins within each affected state domain. Do not replace the existing serialization guarantee with unrelated per-request locks or claim a cross-domain atomic read that is no longer provided. Any required shared write gate must have a justified lifetime and remain process-local.
- Do not move SMTP/deployment secrets into this storage, rewrite credentials, create new default accounts on existing sites, or add schema migration. Physical separation remains a separate future proposal needing both providers' manual scripts and rollback/recovery decisions.

### C3: Avatar/CDN Application Workflow And Failure Safety (Superseded)

D31 removes avatar CDN support during S2 instead of making its file workflow more elaborate. The following original proposal/fault matrix is retained as historical design context only. Do not implement versioned avatar files, compensation, or orphan cleanup from it. Uploaded avatars now use the existing database asset and local route. Any new remaining C3 scope requires a revised proposal and authorization.

Recommended application composition remains in the existing Web workflow layer, alongside current orchestration. Lower-level asset persistence and `IBlogImageStorage` keep their ownership. Exact service/command names are proposals to finalize in S0; a new generic storage framework is not needed.

Proposed sequence under D27:

1. Validate and capture the operation's configuration. Generate an application-owned versioned filename when a new CDN file is required; do not overwrite/delete the currently referenced file during preparation.
2. Write the candidate file, then prepare any asset-row and related General/Image-row changes. Commit related database changes coherently before publishing their runtime state and cache events. Local-avatar mode still preserves the existing asset read endpoint and its fallback.
3. Publish confirmed committed settings once, with B4's ordering/cancellation guarantees; then invalidate affected avatar/feed outputs. Run B5 audit isolation separately, after the real save succeeds.
4. Only then consider cleaning up a provably application-owned previous version which is no longer referenced. Never treat an arbitrary legacy/external CDN URL as permission to delete a storage file. No automatic scan or mass cleanup of historical files is authorized.
5. Cleanup failure after a successful save keeps the new saved state and success response, logs Error with safe operation/file context, and leaves a possible orphan. It does not revert working settings or hide an actual upload/database failure.
6. On a known pre-commit failure, retain the old active resource. A candidate may be removed only when non-commit and lack of reference are established. If commit outcome is uncertain, retain candidates and diagnose/reconcile; never blindly delete a file that a committed row might reference. No durable reconciliation queue is assumed.

Important integration constraints:

- Avatar upload and image-delivery settings must use the same workflow; remove the old CDN event path only after every caller is migrated, without double upload/save/publication. Keep site-icon handling and its audit semantics outside C3.
- Avatar-byte cache invalidation must follow the actual successful asset commit and protect against older in-flight reads repopulating the active entry. Keep the existing client-cache distinction; regenerating server state cannot purge a cached browser response.
- Do not rely on event subscriber discovery order as the workflow specification. Define the sequence explicitly, including asset persistence and settings publication, using the existing command/service patterns.
- Do not add an outer transaction around the present update command without changing its publication boundary. Prefer preparing related tracked rows and one real commit where feasible; if an explicit transaction is needed, publication belongs after its commit, not an inner `SaveChanges`.
- Concurrent avatar/CDN operations must not overwrite a candidate or delete a version another operation uses. Prove the cleanup/reference ordering for the single-instance case; do not invent distributed locking. Preserve the approved last-save-wins policy rather than adding conflict UI.
- A filesystem and a relational database do not become atomic through a method extraction. Retained candidates/orphans are an intentional safety tradeoff. Orphan retry cadence, retention period, automatic cleanup, and any client-visible reconciliation response remain separate decisions if implementation needs them.
- Preserve `/avatar`, `/image/{filename}`, authentication, request/response shapes, image validation, supported filesystems, and cloud-neutral delivery. D27 explicitly allows generated avatar CDN URLs to change with file versions; it does not authorize changing general uploaded-image filenames or published post URLs.

## Proposed Implementation Batches

S labels are sub-batches of this structural plan, not renamed historical B1-B6 batches. S0/S1 are committed and S2 is authorized; S3-S6 implementation is not. Each batch includes focused tests, relevant long-lived documentation, a reviewed independent commit only when authorized, a tracker update, and user acceptance before its dependent batch.

| Batch | Purpose / concrete scope | Dependency and verification | Acceptance / rollback |
| --- | --- | --- | --- |
| S0 - Boundary and compatibility proof | Freeze a representative legacy JSON/HTTP/validation baseline; demonstrate immutable scalar/menu binding and serialization; trace one captured snapshot through a render/partials; settle owner/dependency and actual-commit publication boundaries. Use disposable data and small targeted experiments only after authorization. | B7 plan reviewed. Configuration/Web/Setup/Auth focused checks, Web build; compare serialized JSON field names, explicit null/false/0/empty arrays, menu limits, avatar fallback and render propagation. | A written concrete S1-S5 design with any incompatibility/ownership choices returned to the user. No production migration or broad refactor hidden in a prototype. Revert experimental code independently. |
| S1 - C2 protected-state boundaries | Move account/manifest readers and writers off the ordinary consumer contract; update account/password/TOTP and startup callers together; retain the eleven-row installation transaction and storage shape. Keep old key/type names where needed for compatibility. | S0 ownership graph approved. Auth, Configuration, Setup, Web; both real providers for install/repair ordering/retry; Web build. | Normal templates cannot access credentials; existing account/manifest JSON and timestamps round-trip; missing/invalid state stops before repairs/workers. Existing rows need no migration. Revert the whole boundary commit. |
| S2 - C1 immutable values and publication | Implement immutable ordinary values/nested menus, narrow read access, and one root replacement after successful persistence. Convert writers to detached candidates; remove render-time avatar mutation using a display fallback. Temporary read-only group access may bridge S3, explicitly without claiming operation consistency. | S1 accepted. Configuration/Web/Middleware/Syndication and affected tests; barrier-based paired publication and retained-old-snapshot tests; failure/cancellation/event-order checks; Web build. | No published child can be mutated; a related General/Image save exposes all-old or all-new root state. Valid saved JSON is not bulk rewritten. Reverting code preserves storage compatibility. C1 is not complete until S3. |
| S3 - C1 reader/render migration | Capture once per logical operation; propagate the same snapshot through each render's descendants; migrate the inventoried libraries/workers/views; remove temporary per-group recapture compatibility access. | S2 accepted. Paused render/feed/image operations across a concurrent save; next-operation refresh; public/admin/auth rendering, background loops, sender-name restart behavior, cache generations; full solution and Web build. | Related reads cannot combine generations; a new operation after save can see the new version, including in the same HTTP request. No request-wide freeze or stale lifetime capture. C1 complete after acceptance. Rollback must restore compatible readers and publication together if reverting S2 as well. |
| S4 - Former C3 workflow consolidation | Superseded by D31: the avatar CDN handler and image-setting avatar migration were removed in S2. | No execution of the old plan. | Do not reintroduce the removed avatar/file/settings workflow. |
| S5 - Former C3 versioned resources | Superseded by D31: avatars no longer create image-storage copies. | No execution of the old avatar file plan. | Existing copied files remain untouched; no automatic deletion or new cleanup policy. |
| S6 - Structural regression and acceptance | Full solution with both providers, isolated real-host/browser scenarios, docs/diff checks, retained-record updates, and explicit remaining limits. | S1-S3 accepted; former S4/S5 avatar-CDN scope superseded. Repeat relevant B6 scenarios plus immutable/menu/render/account-boundary and local-avatar save/audit/cache cases; inspect actual URLs, bytes, settings, timestamps, and logs. | Present evidence and obtain final user acceptance. No automatic deployment. Keep physical storage migration and new cleanup policies out of this batch. |

Current remaining sequence is S3 acceptance, then separately authorized S6 acceptance work; former S4/S5 avatar-CDN implementation is superseded by D31. Each active batch still requires its own authorization. Any new C3 scope must be proposed separately.

## Required Acceptance Matrix

| Concern | Test or scenario that must remain meaningful |
| --- | --- |
| Snapshot integrity | Try supported consumer mutation paths, including menu entries/submenus; modifying request/detached data cannot change a published or retained snapshot. An array behind an `IReadOnlyList` cast is not sufficient proof. |
| Atomic publication | Pause around database persistence and root replacement for paired Image/General writes. Each captured root is coherent; failed/rolled-back writes publish nothing; post-commit request cancellation cannot suppress invalidation. |
| Captured readers | Pause a feed or page subtree while another save completes. Every read within that captured operation/render remains on one revision; the next capture sees the new revision. Check worker and singleton service lifetimes. |
| Compatibility | Preserve all B2/B3 explicit-value/default rules, duplicate/case-insensitive JSON behavior, undefined-enum validation, UTF-8 CSS limit, disabled null menus, nested menu validation, legacy account/TOTP and manifest values. No opportunistic validation tightening. |
| Installation | Both providers: seed plus all expected defaults in one transaction; rollback/retry and lost acknowledgment are safe. Protected failures precede ordinary repair and workers. Missing manifest reconstruction keeps D10 semantics. |
| Authentication | Login, password changes, TOTP secret-generation/activation/reset, OIDC mode, rate limits, and three-scheme sign-out retain their contracts. Ordinary views/serialization cannot expose credential state. |
| Candidate upload fails | Old file and old saved state remain usable; no success/audit-only masking. |
| Database write fails before commit | Old active reference remains; file compensation cannot remove any referenced resource. Runtime/cache publication is absent. |
| Commit outcome uncertain | Do not assume rollback or delete a candidate. Preserve files, inspect durable references with a fresh context when possible, and surface a real persistence failure unless success has actually been established. |
| Commit succeeds; request cancels | New state and its file remain usable; required post-commit publication/cache work still completes. |
| Commit succeeds; old-file cleanup fails | Keep new state, return success, and log Error without secrets; old orphan may remain. |
| Audit fails after success | B5's success and sign-out behavior remains; a real asset/file/database/cache failure is not reclassified as audit-only. |
| Concurrent operations | Versioned paths do not collide; cleanup cannot delete an active candidate/version. Stored/runtime last-save-wins and the retained ordinary snapshot are coherent. |
| Rollback/legacy resources | Existing stable filenames/external URLs and public/original root isolation remain usable; do not delete files of uncertain ownership or require a data migration just to revert code. |

## Execution And Remaining Gates

- Re-run `git status --short` and current references first. The baseline 1412 count is current assessment evidence, not a permanent expected count after adding tests.
- Run focused owning suites per batch. Cross-module/startup registration changes require a Web build. Set `MOONGLADE_TEST_CATEGORY_DATABASES=1` only for disposable-provider checks; normal skipped-provider runs cannot prove relational safety.
- Preserve B1-B6's group-level recovery, explicit restart settings, scheduler pre-dispatch checks, last-save-wins, audit Error behavior, and failed-startup nonzero exit. Do not silently redesign these while changing structure.
- G1: fulfilled by entry into B7/S0. G2-M: D28 confirms existing init-only ordinary models and immutable menus. G2-O: D29 confirms Auth/Setup ownership and the Setup-to-Auth dependency, and authorizes S1. D30 authorizes S2 after scope review; S3 still requires acceptance/authorization. G3: review S4/S5 commit/compensation and any new recovery/cleanup policy before implementation.
- Exact orphan retention, automatic retries/deletion, a durable recovery journal, physical account/manifest tables, distributed state, new credential/session rules, and source-file/type renames that change keys remain unapproved. Ask only when a concrete batch needs such a choice; keep independent work moving.
- Update this file and the reliability tracker at each batch boundary with actual commits, commands/counts, skipped checks, faults found, rollback limits, and user acceptance. Passing technical checks is not user acceptance.

## Progress

| Item | Status |
| --- | --- |
| B5/B6 code/test reassessment | Verified at `907ac2285`, 2026-11-06 |
| User directions D24-D32 | Confirmed, including S3 operation/render migration authorization |
| B7 structural plan | S0/S1 committed; S2 authorized and implemented; dependent batches remain gated |
| S0 compatibility experiments | Committed as `90e302d45`; D28/D29 resolve model and ownership gates. Historical S0 results remain in its evidence report. |
| S1 protected-state boundaries | Committed as `8db04575d`; preserved by S2 regression. User subsequently authorized S2 after scope review. |
| S2 immutable values/publication | Committed as `aa38e96f7`, including D31 avatar-CDN removal; preserved by S3 regression |
| S3 operation/render migration | Technical checks complete: 1492 passed with both providers; Web build clean. User acceptance pending. |
| S6 acceptance work | Not started; requires separate authorization |
| Former S4/S5 avatar CDN work | Superseded by D31; do not execute the historical proposal |
| Physical table migration / production rollout | Not authorized |
| New local commit | S3 changes are uncommitted; no new commit requested |
