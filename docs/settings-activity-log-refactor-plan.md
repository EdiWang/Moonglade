# Settings and Activity Log refactor plan

## Current status

B0 and B1 were accepted by the user on 2026-11-08. The user committed B1 as `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c` and authorized B2. B2 dispatch consolidation and the separately implemented trace-diagnostic enhancement are complete and verified; B2 user acceptance and local commit authorization remain pending. Its changes are uncommitted on that B1 SHA. The verified B0 baseline remains `1f5584223de01a1b7da1078268c54c6412b8f7d5`. Recheck the current checkout before relying on either SHA. B3, schema changes, and deployment remain unauthorized.

| Batch | Depends on | Status | Test evidence | Commit | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0. Confirm the baseline | Implementation authorization | Accepted | Configuration 310/310; Web 634/634; Activity Log Node 14/14; no failures or skips | None required | User confirmed B0 on 2026-11-08 |
| B1. Share the ordinary settings audit policy | B0 | Accepted | Configuration 310/310; Web 634/634; Web build 0 warnings/errors; no test failures/skips | `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c` (user-created) | User accepted B1 on 2026-11-08 |
| B2. Reuse audit dispatch and correlate failures | B1 accepted | Implemented and verified; awaiting acceptance | Focused 22/22; complete Web 640/640; Web build 0 warnings/errors; no final failures/skips | Not authorized; changes uncommitted | Pending |
| B3. Return typed setting changes from the detail reader | B2 accepted | Pending | Pending | Pending | Pending |
| B4. Read the event catalog directly | B3 accepted | Pending | Pending | Pending | Pending |
| B5. Verify the combined result | B4 accepted | Pending | Pending | None required unless follow-up changes are approved | Pending |

## Decisions and scope

- Confirmed by the user on 2026-11-08. Share the ordinary settings audit field definition and allow `Moonglade.ActivityLog` to reference `Moonglade.Configuration`. Keep that dependency one way.
- Confirmed by the user on 2026-11-08. Keep the activity writer's existing failure contract. Reuse the Web dispatch path and include the request trace identifier in write failure diagnostics when available.
- The user will decide changes to behavior, module ownership, architecture, or public contracts. When a question arises during research or implementation, describe the concrete alternatives, their impact, and a recommendation. Record the answer here before doing dependent work. Continue independent work within the approved scope.
- The proposed batches preserve routes, payload shapes, persisted event keys and detail keys, existing history, and behavior on PostgreSQL and SQL Server. The scope excludes deployment, schema migrations, history cleanup, and replacing CQRS or the settings snapshot design. Repository guidance and this task exclude Java tooling.
- The user requested this record for handoff and progress tracking. Retain it after acceptance and keep permanent documentation independent of it.

## Start here after a handoff

1. Read [AGENTS.md](../AGENTS.md) and the applicable sections of the [engineering reference](agent-reference.md), especially Configuration, CQRS and LiteBus, Caching and Side Effects, and the Change Impact Checklist. Read [test commands and provider prerequisites](../src/Tests/README.md#run-tests).
2. Run `git status --short --branch` and `git log -8 --oneline` from the repository root. Inspect any user changes, compare the current code with the baseline, and preserve unrelated changes. Check the progress table and decision log for implementation authorization and the first unfinished gate.
3. Work on one batch at a time. Read the affected production callers and tests, make the smallest approved change, run the batch checks, and inspect the diff and `git status --short`. Keep business saves, cache events, and sign-out outside the audit failure boundary.
4. Once the user has authorized implementation and local commits, create one focused implementation commit per code batch after its tests pass. Present the SHA, exact commands and results, review points, and any skipped checks for user acceptance. B0 and B5 are verification gates and can finish without an implementation commit.
5. Update this record after every batch and whenever work stops at an unresolved decision or failed check. Record the actual checkout or SHA, test totals and skips where available, remaining risks, the user's explicit acceptance, and the next action. Put the implementation SHA and acceptance evidence in the tracking update after the implementation commit exists. Keep the tracking update reviewable and leave previous commits intact. Start the next batch after the current batch is accepted.
6. Use isolated synthetic data for any interactive acceptance. Record test environment and account role without credentials or connection strings. Treat an unavailable environment or missing acceptance as pending and record what is needed to resume.

## Contracts to preserve

- The eight ordinary settings pages use PATCH with explicitly editable fields, and existing POST endpoints still replace a group. Custom menu keeps its dedicated route and compares nested menu values structurally. Full and partial writes share the write gate, validate the latest candidate, commit, publish the immutable snapshot, and invalidate affected caches on persisted JSON changes.
- Audit compares effective values inside the gate. A no-op produces no activity. A restricted-only change records the event with `{"Changes":[]}`. Persisted JSON normalization may change timestamps and trigger invalidation without creating activity. PATCH eligibility and audit value visibility are separate policies. Preserve the ordered setting changes, current JSON keys and scalar types, and the exact approved projection for every group.
- Activity keys, groups, localized labels, and detached actor/target snapshots remain stable. An administrator audit failure preserves an already successful business response. The authenticator reset still signs out all three local schemes. The scheduled producer keeps the captured UTC occurrence and independently attempts one System activity per published post without HTTP actor data. Diagnostics may carry the supported event key, failure type, and available trace identifier.
- The writer detaches a failed activity insert so a later save cannot resurrect it and unrelated tracked changes remain intact. Details are validated on both write and read. The list currently rejects malformed stored details too. Preserve that failure behavior and the separate list and detail HTTP contracts.

## Verification commands

Use the repository's Microsoft.Testing.Platform runner. Batch descriptions refer to these commands by name. Record actual results as work proceeds.

| Check | Command |
| --- | --- |
| Configuration suite | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj` |
| Web suite | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` |
| Activity Log browser-module tests | `node --test src/Tests/Moonglade.Web.Tests/JavaScript/ActivityLogTests.mjs` |
| Solution suite | `dotnet test --solution src/Moonglade.slnx` |

The solution run may skip the relational provider fixtures. Follow the [provider opt-in instructions](../src/Tests/README.md#run-tests) for an isolated Docker-backed PostgreSQL and SQL Server run. Report provider evidence only when those tests actually execute. Browser acceptance is separate from Node and HTTP fixture coverage.

## B0. Confirm the baseline

- Verify the current branch, runtime, clean starting scope, and current behavior pinned by the settings, Activity Log, and scheduled-publication tests. Confirm an isolated acceptance environment before interactive checks.
- Run the Configuration and Web suites and the Activity Log browser-module tests. Log baseline failures and skips separately from later regressions. If a baseline failure affects a batch, ask whether to fix it separately or revise that batch's gate.
- Acceptance evidence includes the starting SHA, test results, known constraints, and user approval to begin B1.

### B0 baseline evidence (2026-11-08)

- Authorization: the user requested reading this plan and executing B0 only. No B1 implementation, local commit, push, deployment, or real-database operation was authorized or performed.
- Checkout: `master`, HEAD `1f5584223de01a1b7da1078268c54c6412b8f7d5`. Before verification and before the tracking edit, `git status --short --branch` showed only this untracked plan (`?? docs/settings-activity-log-refactor-plan.md`); tracked source and tests were clean. The starting and final verification SHA matched.
- Runtime: Windows x64; .NET SDK `10.0.403`, MSBuild `18.9.15`, .NET and ASP.NET Core runtime `10.0.14`, Node.js `v26.13.0`. The repository's `global.json` selects Microsoft.Testing.Platform; both .NET runs targeted `net10.0|x64` with the normal build-enabled commands.

| Executed command (repository root) | Passed | Failed | Skipped | Exit code |
| --- | --- | --- | --- | --- |
| `dotnet test --project src\Tests\Moonglade.Configuration.Tests\Moonglade.Configuration.Tests.csproj` | 310 | 0 | 0 | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 634 | 0 | 0 | 0 |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\ActivityLogTests.mjs` | 14 | 0 | 0 | 0 |

- Baseline behavior: the complete Configuration suite includes the independent ordinary-settings change matrix and write-gate cases. The complete Web suite includes settings save/audit isolation, Activity Log writer/list/detail/HTTP contracts, scheduled-publication handler/cache/HTTP cases, and subscriber registration. The Node run imports the actual Activity Log module and covers typed changes, filters, pagination, offcanvas details, and clear confirmation/error flows. These are existing tests, not new coverage or user acceptance.
- Baseline failures and skips: none in the three requested runs. The current Web total is 634; historical verification totals in the test guide are not used as current-run evidence.
- Isolation and limits: the Activity Log HTTP fixture uses `EnvironmentName = "Testing"`, cleared host configuration sources, TestServer, ephemeral data protection, a synthetic Administrator role, and an in-memory SQLite database. Existing automated settings/publication fixtures use synthetic InMemory/SQLite data, not a developer blog database. No full application or interactive browser was started; a disposable browser-acceptance host/database has not been confirmed and must be confirmed before interactive checks.
- Not executed: PostgreSQL/SQL Server provider fixtures, migrations, solution-wide tests, a standalone Web build, or browser acceptance. Those are separate later gates, not skipped cases in the requested suites. Passing these baseline tests does not establish relational-provider compatibility or browser presentation.
- Documentation disposition: only this retained tracking record is updated. No durable workflow, architecture, configuration, or runtime behavior changed, so README, AGENTS, and the engineering reference need no update.
- Acceptance: the user confirmed B0 and authorized B1 on 2026-11-08. No implementation commit was created; local commit authorization remains separate.

## B1. Share the ordinary settings audit policy

- The baseline repeats 51 approved field names across nine groups in [SettingsChangeProjection](../src/Moonglade.Configuration/SettingsChangeProjection.cs) and [ActivityEvents](../src/Moonglade.ActivityLog/ActivityEvents.cs). Put the ordered field definitions in Configuration. Consume them from the gated projection and from the matching Activity Log event definitions, with the approved one-way project reference. Keep the canonical event keys in ActivityLog and preserve the PATCH allowlists in [SettingsController](../src/Moonglade.Web/Controllers/SettingsController.cs).
- Check all nine group-to-event mappings against the existing behavior. Keep stable persisted field names and current labels, including `SiteTitle`. Treat any future removal or rename as a history compatibility decision because the current allowlist also validates stored rows.
- Run the Configuration suite, Web suite, and Web build. Use the independent expected field sets in `UpdateConfigurationCommandTests.Changes`, `ActivityLogModelTests.Changes`, and `ActivityLogHttpTests.Changes`. Cover full and partial saves, no-ops, restricted-only edits, structural menus, concurrent saves, and persisted JSON normalization. Keep the expected test data independent of the shared production definition.
- Acceptance evidence includes one production declaration per group, unchanged stored detail format and HTTP outcomes, and a focused commit. Suggested acceptance scenario uses a synthetic site title change, an identical resave, and a restricted-only edit.

### B1 implementation evidence (2026-11-08)

- Structural change: extracted the original ordered field declarations into `Moonglade.Configuration.SettingsAuditFields.Get(Type)`, which returns an `ImmutableArray<string>`. `SettingsChangeProjection` consumes that definition, and `ActivityEvents` maps the same nine settings types to their existing events using the shared fields with the existing ordinal frozen sets. Added only the approved ActivityLog-to-Configuration project reference; Configuration has no reverse dependency.
- Exact preservation check: compared all `nameof` group/field declarations in the new policy with the B0 projector from Git HEAD. All nine groups and 51 field names match exactly, including casing and order. Event keys, event groups/labels, `SiteTitle`, `Changes` member names/scalar types, and the stored-history validator are unchanged.
- Unchanged boundaries: no edits to SettingsController's PATCH allowlists or save/event mapping, configuration write-gate/persistence/cache code, activity detail validation, public HTTP contracts, authentication, scheduled publication, or test files. The independent expected field lists remain in the existing Configuration and Web fixtures; they do not reference the new production policy.
- Baseline: reused the accepted B0 suites on the same starting SHA. Before editing, the standalone Web build passed with 0 warnings and 0 errors. The existing public symbols were not renamed, removed, or moved between assemblies. Both owning projects target only `net10.0`; no public API manifest, ApiCompat/package-validation gate, conditional compilation, or partial declaration governs the extracted policy.

| Executed command (repository root) | Result | Exit code |
| --- | --- | --- |
| `dotnet test --project src\Tests\Moonglade.Configuration.Tests\Moonglade.Configuration.Tests.csproj` | Passed: 310, Failed: 0, Skipped: 0; unchanged B0 total | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors, including both owning projects | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 634, Failed: 0, Skipped: 0; unchanged B0 total | 0 |

- Automated acceptance scope: existing full/partial-save matrices, nine-group no-ops, restricted-only and mixed changes, structural menus, concurrent saves, and equivalent persisted-JSON normalization passed. Existing Activity Log writer/read and five-culture HTTP fixtures passed against their independent nine-group/51-field expectations. No test expectations were changed to obtain these results.
- Documentation: updated the Configuration section of `docs/agent-reference.md` with the shared policy owner, one-way dependency, separate PATCH policy, and history-compatibility invariant. Updated this retained record; no README/deployment/configuration changes were needed.
- Limits and acceptance: no provider-database, migration, full-solution, Node rerun, or interactive browser checks were executed for B1; B0 Node evidence remains separate. Browser acceptance still requires a confirmed disposable environment. The user subsequently committed B1 as `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c`, accepted it, and authorized B2 on 2026-11-08. No agent-created commit or push was performed.

## B2. Reuse audit dispatch and correlate failures

- Consolidate the two entry points in [BlogControllerBase](../src/Moonglade.Web/Controllers/BlogControllerBase.cs) and reuse a Web audit dispatch path for [article management](../src/Moonglade.Web/Commands/PostManagementCommands.cs) and [scheduled publication](../src/Moonglade.Web/Handlers/ScheduledPostsPublishedEventHandler.cs) where it eliminates repeated handling. Preserve the actor, target, timestamp, and request cancellation behavior of each caller. Keep the activity writer's existing catch and detach behavior.
- The Web guard handles failures that escape dispatch. The writer handles failures inside [CreateActivityLogCommand](../src/Moonglade.ActivityLog/CreateActivityLogCommand.cs). Carry an available request trace identifier into the writer's safe diagnostic without persisting it as activity data. Scheduled events can have no request trace. Ensure the same failure produces one safe diagnostic from its owning boundary. Confirm with the user if a shared dispatch path would change a caller's behavior or require a new module boundary.
- Run the Web suite and Web build. Review `SettingsControllerTests.ActivityLog`, `SettingsControllerTests.Changes`, `AssetsControllerTests.ActivityLog`, `ActivityLogControllerTests`, `PostManagementCommandTests`, `ScheduledPostsPublishedEventHandlerTests`, and `ActivityLogModelTests.B4`. Verify dispatch failure, cancellation, and a real insert failure. Check that settings remain committed, reset still signs out all schemes, failed rows stay detached, unrelated tracked writes survive, and later posts in a scheduled batch still receive independent attempts. A focused regression should cover the trace identifier on an actual failed insert and the absence of sensitive values.
- Acceptance evidence includes a focused commit and test results showing one diagnostic at the appropriate boundary with the available trace, preserved business responses, and independent scheduled attempts.

### B2 implementation evidence (2026-11-08)

- Starting scope: clean `master` at the user-created B1 commit `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c`. No branch switch, commit, push, deployment, or real-database operation was performed.
- Structural operation: introduced the internal Web-owned `ActivityLogDispatcher` and retained the single `BlogControllerBase.LogActivityAsync` entry point. Binding references identified all five callers of the redundant `LogActivityAfterSaveAsync`; they now use `LogActivityAsync`. Removed the file-local `PostActivityLogger` and reused the same guard for post saves and each scheduled publication. Existing contextual log templates remain distinct. Command construction stays inside the audit guard, not the business save, cache, asset, or sign-out boundary. The Web host is an executable, not a shipped controller-base library contract.
- Diagnostic enhancement: the separate feature implementation added optional, non-positional `[JsonIgnore]` init-only `TraceIdentifier` metadata to `CreateActivityLogCommand` and `PostOperationContext`. Existing positional constructors/deconstruction, HTTP payloads, persisted entity mappings, and activity detail shapes are unchanged. Controllers supply their request trace; post saves capture and forward it through the existing operation context. Scheduled events still supply no HTTP trace.
- Failure ownership: the writer still catches failures and detaches only its failed activity insert, preserving unrelated tracked changes and its nonthrowing failure contract. It includes an available trace only in the safe diagnostic. The shared Web guard handles escaped dispatch failures, including cancellation; it does not duplicate a writer-handled failure. No-trace writer/post/scheduled diagnostics retain their previous fields and templates. No exception object/message, actor, target, request detail, settings value, or credential is added to error diagnostics.
- Cancellation and snapshots: controller audit dispatch retains its default noncancelable token; article save audits retain their existing request token; scheduled dispatch retains `CancellationToken.None`, ordered independent attempts, System actors, and captured ID/title/UTC occurrence. The Features producer, configuration write gate, detail validator, settings allowlists, schema, and provider code are unchanged.

| Verification stage | Executed command (repository root) | Result | Exit code |
| --- | --- | --- | --- |
| Structural operation | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors | 0 |
| Structural operation | `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 634, Failed: 0, Skipped: 0; unchanged B1 baseline | 0 |
| Final combined B2 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors | 0 |
| Final combined B2 | `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 640, Failed: 0, Skipped: 0 | 0 |

The final focused diagnostic run passed 22 cases with no failures/skips, exit 0. Two intermediate focused runs each had four test-fixture failures, resolved by observing the actual shared LiteBus DbContext scope and using valid title data; these were not B1 baseline failures or production behavior changes. The exact final selection was:

```powershell
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-method 'Moonglade.Web.Tests.Controllers.SettingsControllerTests.AuditInsertFailure_KeepsCommittedSettingsTimestampAndCacheWithoutResurrection' 'Moonglade.Web.Tests.Commands.PostManagementCommandTests.SavePostCommand_AuditDispatchFailureOrCancellation_PreservesSaveAndLogsOnlySafeContext' 'Moonglade.Web.Tests.Controllers.PostControllerTests.CreateOrEdit_ValidLocalSchedule_DispatchesUtcFeatureValue' 'Moonglade.Web.Tests.Handlers.ScheduledPostsPublishedEventHandlerTests.HandleAsync_Batch_EmitsOrderedIndependentSystemCommandsWithPublicationSnapshots' 'Moonglade.Web.Tests.Handlers.ScheduledPostsPublishedEventHandlerTests.HandleAsync_OneDispatchFailsOrCancels_ContinuesWithSafeDiagnostics' 'Moonglade.Web.Tests.Controllers.ActivityLogModelTests.Writer_SuppliedUtcWriteFailsOrCancels_DetachesActivityAndLogsOnlySafeFailureType' 'Moonglade.Web.Tests.Controllers.SettingsControllerTests.SettingsSave_WhenAuditFails_PreservesCommittedValuesAndReturnsSuccess' --output Detailed --no-ansi
```

| Requirement | Evidence |
| --- | --- |
| Real insert failure receives HTTP trace, produces one safe writer diagnostic, preserves settings/cache/timestamp and unrelated tracked work, and cannot resurrect the failed row | `SettingsControllerTests.AuditInsertFailure_KeepsCommittedSettingsTimestampAndCacheWithoutResurrection` covers POST/PATCH with/without trace using real SQLite/LiteBus. It also checks a subsequent successful activity has the exact original `Changes` envelope without trace or restricted values. |
| Article trace capture/propagation and dispatch failure/cancellation preserve successful saves and exclude metadata from JSON | `PostControllerTests.CreateOrEdit_ValidLocalSchedule_DispatchesUtcFeatureValue`; `PostManagementCommandTests.SavePostCommand_AuditDispatchFailureOrCancellation_PreservesSaveAndLogsOnlySafeContext` |
| Writer insert failure/cancellation retains detach behavior, safe diagnostics, no-trace compatibility, and later unrelated saves | `ActivityLogModelTests.Writer_SuppliedUtcWriteFailsOrCancels_DetachesActivityAndLogsOnlySafeFailureType` |
| Scheduled events remain HTTP-context-free, ordered, independent and best-effort with captured UTC | `ScheduledPostsPublishedEventHandlerTests.HandleAsync_Batch_EmitsOrderedIndependentSystemCommandsWithPublicationSnapshots`; `HandleAsync_OneDispatchFailsOrCancels_ContinuesWithSafeDiagnostics`; `HandleAsync_PendingWrite_AwaitsItBeforeDispatchingNextPost` |
| Audit failures preserve settings saves, all three authenticator-reset sign-outs, local assets and clear-only response/failure boundaries | Existing complete-suite cases: `SettingsControllerTests.SettingsSave_WhenAuditFails_PreservesCommittedValuesAndReturnsSuccess`; `ResetTotp_WhenAuditFails_PreservesResetAndSignsOutAllSchemes`; `AssetsControllerTests.Avatar_Post_WhenAuditFails_PreservesSavedLocalAssetWithoutChangingSettings`; `ActivityLogControllerTests.Clear_WhenAuditDispatchFails_PreservesCommittedDeleteAndLogsSafely`; `Clear_WhenDatabaseDeleteFails_PreservesRowsAndDoesNotAudit`; `Clear_WhenAuditPersistenceFails_DoesNotResurrectOnNextSave` |

- Test changes add six trace/no-trace theory cases to the original full-suite total; existing failure assertions remain meaningful and the scheduled tests explicitly assert no trace metadata. No tests were removed or skipped.
- Documentation disposition: updated the Configuration section of `docs/agent-reference.md` for the shared guard, trace-only metadata, cancellation contracts and diagnostic ownership, and updated this retained plan. No README, deployment, configuration, dependency, or schema documentation changes were needed.
- Limits and next gate: automated data was synthetic InMemory/SQLite only. Provider-database, migration, full-solution, Node and browser checks were not run for B2. No new commit/push was created. Await B2 user acceptance and local commit authorization; do not start B3.

## B3. Return typed setting changes from the detail reader

- Have [ActivityLogDetails](../src/Moonglade.ActivityLog/ActivityLogDetails.cs) yield validated typed setting changes so [ActivityLogController](../src/Moonglade.Web/Controllers/ActivityLogController.cs) can localize them without reading the persisted `Key`, `OldValue`, and `NewValue` properties again. Preserve ordinary detail fields, scalar JSON types, field ordering, and the distinction between no change envelope and an empty one. Keep existing localized label keys. A future label consolidation that changes the visible text requires a separate decision.
- Run the Web suite, Web build, and Activity Log browser-module tests. Verify valid stored rows, restricted-only empty changes, wrong shape, missing keys, duplicate keys, disallowed values, and the existing list failure on malformed stored data. Compare the detail payload and localized labels in all supported cultures through the HTTP fixtures. In browser acceptance, check a setting detail and an ordinary event in the existing offcanvas.
- Acceptance evidence includes a focused commit and equivalent list/detail API results and presentation for existing history, with malformed stored details still rejected safely.

## B4. Read the event catalog directly

- Replace the in-memory-only [GetEventTypesQuery](../src/Moonglade.ActivityLog/GetEventTypesQuery.cs) with a direct read from the canonical event catalog. Verify all callers before deleting the query and handler. Keep the ordinal group/key ordering, localization, route, and response shape. Leave the persisted list and detail queries in place.
- Run the Web suite, Web build, and Activity Log browser-module tests. Keep coverage of the exact canonical catalog, endpoint grouping/order, supported cultures, and event-key filtering. In browser acceptance, select a settings event and a System publication event and verify the filters select the expected records.
- Acceptance evidence includes a focused commit and unchanged filter options, ordering, and filtering behavior.

## B5. Verify the combined result

- Run the Solution suite, Web build, and Activity Log browser-module tests on the accepted code. For the relational compatibility gate, follow the documented provider opt-in in the isolated environment. If prerequisites are unavailable, record the exact skipped cases and ask whether an alternate acceptance gate is appropriate.
- Review the combined diff against the B0 baseline. Confirm historical keys and details, API shape, deployment instructions, project dependencies, settings side effects, and security boundaries. If an accepted architecture change affects durable project guidance, update the relevant engineering reference with its owning batch. Record the documentation disposition here.
- Demonstrate the settings and Activity Log acceptance scenarios with synthetic data. Record the final SHA, automated checks, browser evidence, user sign-off, and any open follow-up. Retain this record for future handoff.

## Progress and decision log

| Date | Batch or decision | Evidence and next action |
| --- | --- | --- |
| 2026-11-08 | Plan drafted | Read-only analysis and repository inspection completed. Worktree was clean before this document was added. No tests run. Await implementation authorization before B0. |
| 2026-11-08 | B1 dependency | User selected a shared field definition with a one-way ActivityLog dependency on Configuration. |
| 2026-11-08 | B2 failure contract | User selected retaining the writer's existing failure contract and adding the available trace identifier to its safe diagnostic. |
| 2026-11-08 | B0 authorization | User requested executing B0 only. Scope is baseline verification and this tracking update; B1 and local commits remain unauthorized. |
| 2026-11-08 | B0 automated baseline verified | `master` at `1f5584223de01a1b7da1078268c54c6412b8f7d5`; Configuration 310 passed, Web 634 passed, Activity Log Node 14 passed, all exit 0 with no failures or skips. Only the untracked plan was present at the start; no source/test/schema changes. Provider and interactive browser evidence remain unclaimed. Await user acceptance and authorization to begin B1. |
| 2026-11-08 | B0 acceptance / B1 authorization | User explicitly confirmed B0 and requested beginning B1. The checkout still matches the B0 SHA with only the untracked plan present. The pre-edit Web build passed with 0 warnings and 0 errors. B1 is a behavior-preserving field-policy extraction; local commits and B2 remain unauthorized. |
| 2026-11-08 | B1 implemented and verified | Shared immutable Configuration field policy now feeds both the projector and the canonical ActivityLog settings events through the approved one-way reference. All nine groups/51 fields retain exact B0 spelling/order. Configuration 310 passed, Web 634 passed, both without failures/skips; standalone Web build passed without warnings/errors. Independent tests and HTTP/write/history boundaries are unchanged. Engineering reference updated. Await local commit authorization and B1 acceptance; do not start B2. |
| 2026-11-08 | B1 acceptance / B2 authorization | User explicitly accepted B1 and requested B2. Starting checkout is clean on `master` at the user-created B1 commit `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c`. Reuse its passing Web baseline. Separate behavior-preserving dispatch consolidation from the already authorized trace-diagnostic enhancement; keep commits and B3 pending. |
| 2026-11-08 | B2 implemented and verified | Shared Web guard replaces redundant controller/post/scheduled dispatch handling. Separate diagnostic enhancement propagates JSON-ignored request trace metadata to safe writer/guard diagnostics without persistence or duplicate errors. Structural Web run retained 634 passing cases; final focused selection passed 22 and complete Web suite passed 640, all final exit 0/no failures/skips; both Web builds had 0 warnings/errors. Engineering reference and tracking record updated. Changes remain uncommitted; await B2 acceptance and local commit authorization before B3. |
