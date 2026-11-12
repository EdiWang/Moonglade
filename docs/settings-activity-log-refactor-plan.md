# Settings and Activity Log refactor plan

## Current status

B0 through B4 were accepted by the user on 2026-11-08, and the user accepted B5 on 2026-11-09. The B5 combined automated, provider, diff and isolated browser checks verified code at `3a1cae66a0ead73f2c301db943df7dcee65df4c5` against the B0 baseline `1f5584223de01a1b7da1078268c54c6412b8f7d5`. The user separately requested a small write-path allocation optimization after accepting B5. See the progress and decision log for its verification. New local commits, production schema changes, and deployment remain unauthorized.

| Batch | Depends on | Status | Test evidence | Commit | User acceptance |
| --- | --- | --- | --- | --- | --- |
| B0. Confirm the baseline | Implementation authorization | Accepted | Configuration 310/310; Web 634/634; Activity Log Node 14/14; no failures or skips | None required | User confirmed B0 on 2026-11-08 |
| B1. Share the ordinary settings audit policy | B0 | Accepted | Configuration 310/310; Web 634/634; Web build 0 warnings/errors; no test failures/skips | `4bd7c3a5b825a4c3dab0de44289070bf6f97aa1c` (user-created) | User accepted B1 on 2026-11-08 |
| B2. Reuse audit dispatch and correlate failures | B1 accepted | Accepted | Focused 22/22; complete Web 640/640; Web build 0 warnings/errors; no final failures/skips | `4ac5e79b7292953fc685f9f94ab15bcf1672b2ab` (user-created) | User accepted B2 on 2026-11-08 |
| B3. Return typed setting changes from the detail reader | B2 accepted | Accepted | Focused 48/48; complete Web 642/642; Node 14/14; Web build 0 warnings/errors; no failures/skips | `88d9e52dbd3afc43a7b193220d07091e63babbff` (user-created) | User accepted B3 on 2026-11-08; browser evidence remains separate |
| B4. Read the event catalog directly | B3 accepted | Accepted | Focused 7/7; complete Web 642/642; Node 14/14; Web build 0 warnings/errors; no failures/skips | `3a1cae66a0ead73f2c301db943df7dcee65df4c5` (user-created) | User accepted B4 on 2026-11-08; browser evidence remains separate |
| B5. Verify the combined result | B4 accepted | Accepted | Opt-in solution 1951/1951; both providers executed; Node 14/14; Web build 0 warnings/errors; isolated browser scenarios verified | Verification record `fac660509`, code verified at `3a1cae66a0ead73f2c301db943df7dcee65df4c5` | User accepted B5 on 2026-11-09 |

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

Use the [migration verification guide](../database/README.md#verification-and-execution) for temporary PostgreSQL and SQL Server checks. Record provider results separately from the solution suite. Browser acceptance is separate from Node and HTTP fixture coverage.

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
- Limits and acceptance: automated data was synthetic InMemory/SQLite only. Provider-database, migration, full-solution, Node and browser checks were not run for B2. The user subsequently committed B2 as `4ac5e79b7292953fc685f9f94ab15bcf1672b2ab`, accepted it and authorized B3. No agent-created commit or push was performed.

## B3. Return typed setting changes from the detail reader

- Have [ActivityLogDetails](../src/Moonglade.ActivityLog/ActivityLogDetails.cs) yield validated typed setting changes so [ActivityLogController](../src/Moonglade.Web/Controllers/ActivityLogController.cs) can localize them without reading the persisted `Key`, `OldValue`, and `NewValue` properties again. Preserve ordinary detail fields, scalar JSON types, field ordering, and the distinction between no change envelope and an empty one. Keep existing localized label keys. A future label consolidation that changes the visible text requires a separate decision.
- Run the Web suite, Web build, and Activity Log browser-module tests. Verify valid stored rows, restricted-only empty changes, wrong shape, missing keys, duplicate keys, disallowed values, and the existing list failure on malformed stored data. Compare the detail payload and localized labels in all supported cultures through the HTTP fixtures. In browser acceptance, check a setting detail and an ordinary event in the existing offcanvas.
- Acceptance evidence includes a focused commit and equivalent list/detail API results and presentation for existing history, with malformed stored details still rejected safely.

### B3 implementation evidence (2026-11-08)

- Starting scope: clean `master` at the user-created, accepted B2 commit `4ac5e79b7292953fc685f9f94ab15bcf1672b2ab`. Its passing complete Web suite (640 cases) and standalone build provide the unchanged baseline. No branch switch, commit, push, deployment, or real-database operation was performed.
- Structural change: `ActivityLogDetails.ReadSettingChanges` retains the existing shared write/read validation and returns an ordered read-only list of `ActivityLogSettingChangeDetail` records. Old/new values remain detached `JsonElement` scalars, cloned before the parsing document is disposed. The internal parser returns both the original details dictionary and the validated typed list in one pass.
- Compatibility: the existing `ActivityLogItem.Details` property, raw `Changes` JSON, write model, query types and response DTOs are retained. The additive init-only `ActivityLogItem.SettingChanges` property is JSON-ignored; it does not alter serialized read-model or HTTP shapes. Existing public symbols and nullable annotations are unchanged. Both affected projects target `net10.0`, with no API manifest, conditional compilation, package-validation gate or partial production declaration governing the changed surface.
- Web now localizes `log.SettingChanges` without enumerating raw stored change members again. Ordinary events retain `changes: null`; restricted-only settings events retain `changes: []`. Fields, scalar types, ordering, event/field keys and all existing localization resource keys are unchanged. List and detail queries still use the same `FromEntity` validation, so malformed rows fail on both paths without exposing their contents.
- Focused test changes are confined to `ActivityLogModelTests.Changes.cs`: strengthen the independent nine-group/51-field and scalar-boundary reader matrices for typed values, order, read-only lists, disposed-document safety and unchanged raw serialization; add two ordinary-event cases; extend all 24 malformed-envelope cases to verify list rejection as well as detail rejection. No tests or edge-case data were removed, weakened or skipped. Final complete-suite total increased by exactly the two ordinary-event cases.

| Executed command (repository root) | Result | Exit code |
| --- | --- | --- |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 642, Failed: 0, Skipped: 0 | 0 |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\ActivityLogTests.mjs` | Passed: 14, Failed: 0, Skipped: 0 | 0 |

The focused reader/HTTP selection passed 48 cases, with no failures/skips and exit 0:

```powershell
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-method '*SettingChanges_All51B0Fields_AreExactEventAllowlistAndPersistTypedValues' '*SettingChanges_EmptyAndScalarBoundaries_RoundTripWithoutLosingTypes' '*SettingChanges_OrdinaryDetails_HaveNoChangeEnvelopeAndPreserveFields' '*SettingChanges_InvalidStoredEnvelope_ThrowsSafeFailureBeforeReadExposure' '*SettingDetails_All51LabelsAreLocalizedAndOldNewValuesRemainTypedPlainText' '*RestrictedOnlyHttp_HasEmptyChangesWhileNonSettingsKeepFields'
```

| Requirement | Evidence |
| --- | --- |
| Validated typed changes retain 51 independent field expectations, order, detached scalar values and raw compatibility | `ActivityLogModelTests.SettingChanges_All51B0Fields_AreExactEventAllowlistAndPersistTypedValues`; `SettingChanges_EmptyAndScalarBoundaries_RoundTripWithoutLosingTypes` |
| Ordinary events have no change envelope; restricted-only events have an intentional empty one | `ActivityLogModelTests.SettingChanges_OrdinaryDetails_HaveNoChangeEnvelopeAndPreserveFields`; existing `ActivityLogHttpTests.RestrictedOnlyHttp_HasEmptyChangesWhileNonSettingsKeepFields` |
| Wrong shapes, missing/repeated/disallowed keys and nonscalar values fail safely on detail and list reads | `ActivityLogModelTests.SettingChanges_InvalidStoredEnvelope_ThrowsSafeFailureBeforeReadExposure` (24 stored-shape cases); existing `Detail_InvalidStoredFieldsOrShapes_FailsExplicitlyWithoutEchoingPayload` |
| HTTP detail field order, labels in all five cultures and plain-text presentation remain unchanged | Existing `ActivityLogHttpTests.SettingDetails_All51LabelsAreLocalizedAndOldNewValuesRemainTypedPlainText` and full-suite non-settings writer-to-HTTP fixtures; actual Activity Log Node module's typed-change, empty-envelope, offcanvas and enum-label cases |

- Review and documentation: reviewed the focused production/test diff and its concrete assertions. Confirmed no edits to event keys/allowlists, writer failure behavior, configuration/Data/provider/schema code, localization resources or frontend assets. Updated `docs/agent-reference.md` with typed-read ownership and compatibility invariants, and this retained tracking record; no README or deployment changes were needed.
- Limits and acceptance: tests used synthetic InMemory/SQLite and TestServer data only. No provider-database, migration or full-solution run was performed. Node and compiled-page/HTTP evidence are not interactive browser acceptance. A disposable browser-acceptance host/database remains unconfirmed; no existing developer host was launched or used. The user subsequently committed B3 as `88d9e52dbd3afc43a7b193220d07091e63babbff`, accepted it and authorized B4. No agent-created commit/push was performed.

## B4. Read the event catalog directly

- Replace the in-memory-only `GetEventTypesQuery` with a direct read from the canonical event catalog in [ActivityLogController](../src/Moonglade.Web/Controllers/ActivityLogController.cs). Verify all callers before deleting the query and handler. Keep the ordinal group/key ordering, localization, route, and response shape. Leave the persisted list and detail queries in place.
- Run the Web suite, Web build, and Activity Log browser-module tests. Keep coverage of the exact canonical catalog, endpoint grouping/order, supported cultures, and event-key filtering. In browser acceptance, select a settings event and a System publication event and verify the filters select the expected records.
- Acceptance evidence includes a focused commit and unchanged filter options, ordering, and filtering behavior.

### B4 implementation evidence (2026-11-08)

- Starting scope: clean `master` at the user-created B3 commit `88d9e52dbd3afc43a7b193220d07091e63babbff`. Reused the accepted 642-case Web baseline without repeating unchanged pre-edit verification. No branch switch, commit, push, deployment or real-database operation was performed.
- Structural operation: `ActivityLogController.GetEventTypes` now reads `ActivityEvents.All` directly and applies the same ordinal Group/Key ordering before grouping and localization. Deleted the in-memory-only query and handler after LSP/text reference checks found the endpoint and two test callers; migrated both tests. Retained the action's `Task<IActionResult>` signature, authorization, `/api/activitylog/eventtypes` route, `ActivityEventGroup` and `ActivityEventOption` shapes. The query mediator remains for persisted list/detail reads.
- Test migration: preserved the independent exact 50-event key/group/label matrix. The controller test now pins all 12 canonical groups, all 50 options and key ordering rather than a mocked singleton. The existing five-culture HTTP test additionally pins canonical group/key order and exact group/option JSON member sets. Its existing checks still compare localized filter labels, summaries and detail headings for every event; localization resources and frontend code are unchanged.

| Executed command (repository root) | Result | Exit code |
| --- | --- | --- |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-method '*Catalog_All50ActiveEvents_HaveExactUniqueKeysGroupsAndLabels' '*GetEventTypes_ReturnsOkWithEventTypes' '*Catalog_All50Events_LocalizedFiltersSummaryAndHeadingShareIdentity'` | Passed: 7, Failed: 0, Skipped: 0 | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors | 0 |
| `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 642, Failed: 0, Skipped: 0 | 0 |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\ActivityLogTests.mjs` | Passed: 14, Failed: 0, Skipped: 0 | 0 |

| Requirement | Evidence |
| --- | --- |
| Keep all 50 exact canonical keys, groups, labels and detail policy | `ActivityLogModelTests.Catalog_All50ActiveEvents_HaveExactUniqueKeysGroupsAndLabels` retains its independent expectations |
| Preserve ordinal groups/keys and endpoint response shape | `ActivityLogControllerTests.GetEventTypes_ReturnsOkWithEventTypes`; `ActivityLogHttpTests.Catalog_All50Events_LocalizedFiltersSummaryAndHeadingShareIdentity` asserts exact JSON members and order |
| Preserve filter-label, summary and heading identity across all five supported cultures | `ActivityLogHttpTests.Catalog_All50Events_LocalizedFiltersSummaryAndHeadingShareIdentity` (en-US, zh-Hans, zh-Hant, de-DE, ja-JP) |
| Keep repeated-key filtering and System publication history behavior | Existing `ActivityLogHttpTests.List_RepeatedKeysAndInclusiveUtcDates_ReturnExactCapturedRows`; `AutomaticPublication_DetachedRenameDeleteSnapshots_KeepLocalizedFilterListDetailIdentityAndOccurrence`; Node `string event keys serialize repeatedly and server summary is used without reconstruction` and `key toggling filter reset and page changes preserve exact active filters` |

- Review and documentation: reviewed the concrete production/test diff and assertions; reference search found no remaining query/handler references in source/configuration. Canonical definitions, persisted queries/detail validation, configuration/Data/provider/schema code, resources and frontend assets are unchanged. Updated the CQRS section of `docs/agent-reference.md` with direct catalog ownership and ordinal-before-localization ordering, and this retained record. No README/deployment/configuration updates were needed.
- Limits and acceptance: automated HTTP tests used synthetic data, TestServer and in-memory SQLite, not the developer blog database. No provider-database, migration or full-solution run was performed in B4. Node and HTTP evidence are not interactive browser acceptance. No existing developer host was launched or used. The user subsequently committed B4 as `3a1cae66a0ead73f2c301db943df7dcee65df4c5`, accepted it and authorized B5. No agent-created commit/push was performed.

## B5. Verify the combined result

- Run the Solution suite, Web build, and Activity Log browser-module tests on the accepted code. For the relational compatibility gate, follow the documented provider opt-in in the isolated environment. If prerequisites are unavailable, record the exact skipped cases and ask whether an alternate acceptance gate is appropriate.
- Review the combined diff against the B0 baseline. Confirm historical keys and details, API shape, deployment instructions, project dependencies, settings side effects, and security boundaries. If an accepted architecture change affects durable project guidance, update the relevant engineering reference with its owning batch. Record the documentation disposition here.
- Demonstrate the settings and Activity Log acceptance scenarios with synthetic data. Record the final SHA, automated checks, browser evidence, user sign-off, and any open follow-up. Retain this record for future handoff.

### B5 combined verification evidence (2026-11-08)

- Authorization and checkout: the user accepted B4 and requested B5. Starting and final verified production HEAD is `3a1cae66a0ead73f2c301db943df7dcee65df4c5` on `master`, ahead of `origin/master` by four user-created commits. The starting checkout was clean. B5 changes only this retained record; no application/test/dependency/schema edit, new commit, push or deployment was needed.
- Runtime and isolation: the existing native Microsoft.Testing.Platform runner executed all 15 test projects. Docker client/server `29.9.2` with a Linux engine was available. Enabled the former provider suite for that run. The existing fixture created uniquely named, loopback-bound PostgreSQL (`postgres:18-alpine`) and SQL Server (`2022-latest`) containers and disposable databases; its normal disposal removed all its containers. A label-filtered post-run check found no residual fixture containers. No developer or production blog database was opened.

| Executed command (repository root) | Result | Exit code |
| --- | --- | --- |
| `dotnet test --solution src\Moonglade.slnx` with the former provider suite enabled | Passed: 1951, Failed: 0, Skipped: 0; all 15 projects, including both provider groups | 0 |
| `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Build succeeded; 0 warnings, 0 errors | 0 |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\ActivityLogTests.mjs` | Passed: 14, Failed: 0, Skipped: 0 | 0 |

- Compilation diagnostics: the solution test build reported the three existing CS8602 warnings in unchanged `CannonServiceTests.cs` at lines 54, 154 and 192. These are baseline non-Web test warnings, not new refactor warnings. The explicit Web build had no warnings/errors.
- Provider evidence: unlike B0-B4, this solution run did not skip relational tests. `ActivityLogDatabaseTests` executed fresh canonical schema/real-handler reads and writes, legacy upgrade/rerun history and identity preservation, complete-installation preservation and unsupported-schema rejection for both providers. `StartupInstallationDatabaseTests`, `CategoryIdentityDatabaseTests` and `SnakeCaseDatabaseTests` also ran their documented provider/prerequisite scenarios. Existing SQL upgrade scripts ran only inside those disposable test databases; no production migration or schema change was performed.

| Requirement | Combined evidence |
| --- | --- |
| Preserve the nine ordered audit groups and 51 historical field names | Direct B0 comparison of all ordered `nameof` group/field declarations matched `SettingsAuditFields` exactly; independent Configuration and Web matrices passed in the solution |
| Preserve canonical event keys, groups, labels, details and ordering | Reviewed the complete `ActivityEvents` diff; all 50 event definitions retain identity, with only shared settings-policy consumption changed. `Catalog_All50ActiveEvents_HaveExactUniqueKeysGroupsAndLabels` and five-culture HTTP catalog tests passed |
| Preserve successful business outcomes and safe failure ownership | Reviewed every Web dispatch caller and writer diff. Real-save, insert-failure/detach, trace serialization, cancellation, reset/sign-out and independent scheduled-publication regressions passed; business/cache/authentication operations remain outside the audit guard |
| Preserve typed read contracts, scalar order and malformed-list rejection | Reader/controller diff retains raw stored details and HTTP DTOs; shared validation produces detached typed changes. All 51-field, scalar-boundary, ordinary/empty-envelope and 24 malformed stored-shape cases passed |
| Preserve provider history, installation and manual-upgrade compatibility | Opt-in real-provider fixtures above executed without failures or skips; provider/entity mappings and published upgrade scripts are unchanged from B0 |
| Preserve deployment, startup, settings side effects and security boundaries | B0-to-B4 diff review confirmed README, AGENTS, Docker/deployment files, Data/providers, Features/BackgroundServices, configuration save/PATCH gates, Program/DI/pipeline, resources and frontend pages/assets are unchanged |

#### Isolated browser behavior evidence

- Used Chrome in an isolated browser context with the actual compiled General settings and Activity Logs pages and their production JavaScript modules. A session-only loopback proxy exposed the existing `ActivityLogHttpFixture` TestServer, using `Testing`, cleared configuration sources, synthetic Administrator claims, ephemeral data protection and an in-memory SQLite database with initialized ordinary settings. Its disposable port was `127.0.0.1:61186`; it is no longer running. No production host, application background workers, external notification service or developer override configuration was used.
- Synthetic history began with three existing fixture rows and one System publication created through the real `ScheduledPostsPublishedEventHandler` and activity writer. The captured publication occurrence was `2026-11-08T10:00:00Z`, with a synthetic detached post target.
- Real General-settings dialogs saved a title and then `B5 <b>x</b>`, producing exactly one `SiteTitle` old/new entry each (history totals 4 to 5 to 6). The first longer input was clipped by the existing 15-character UI limit; the short markup-containing value remained exact. Saving that identical title again left the total at 6. A Copyright-only dialog edit increased the total to 7 and returned `changes: []`, without projecting the restricted field.
- Opened both settings details through their real row buttons. The title change displayed the literal `B5 <b>x</b>` text without a `b` element; the restricted-only detail displayed the intentional no-values state. Selecting only the General settings checkbox and applying the filter showed exactly rows 7, 6 and 5. Switching to only automatic publication showed exactly row 4 with System actor and the captured UTC time.
- The System offcanvas retained its detached target/PostId, `changes: null`, and no IP address or user agent. Closing it returned focus to its initiating detail button. Browser state, rendered rows and detail DOM were checked alongside successful catalog/list/detail responses.
- Checked en-US and zh-Hans browser presentation. With an explicit zh-Hans request language for page and API requests, the actual catalog retained 12 groups/50 options and ordinal key order, while settings/System summaries and detail labels localized correctly. Existing automated HTTP tests cover all five supported cultures; no browser run is claimed for the other three.
- Browser scope limits: the existing focused TestServer fixture does not expose the generated `Moonglade.Web.styles.css` static-web-asset bundle (404), and it excludes unrelated asset controllers. The browser results establish settings/activity interaction and text contracts, not full production-host startup, complete visual styling, deployed authentication or all unrelated admin pages. A session-only proxy configuration-source startup error was corrected before acceptance checks; its executable also had a transient xUnit entry-point warning, neither affecting production code or the clean integrated test results.
- Cleanup: stopped the loopback proxy, cleared the browser language override and left its final tab at `about:blank`. Removed the session-only proxy sources/build outputs and temporary verification logs after recording these results. No acceptance host, fixture container, synthetic database or repository scratch file remains.

- Documentation disposition and final gate. B1-B4 already synchronized `docs/agent-reference.md` with each approved structural change. B5 review found no additional durable workflow or architecture change requiring README, AGENTS, or reference edits. The user accepted B5 on 2026-11-09 with the automated, provider, and bounded browser evidence recorded above. This record is retained by explicit user request.

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
| 2026-11-08 | B2 acceptance / B3 authorization | User accepted B2 and requested B3. Starting checkout is clean on `master` at the user-created B2 commit `4ac5e79b7292953fc685f9f94ab15bcf1672b2ab`. Reuse its passing Web baseline; implement only typed validated detail reading and localization, preserving storage/HTTP/list-failure contracts. New commits and B4 remain pending. |
| 2026-11-08 | B3 implemented and verified | Typed read-only detached changes now come from ActivityLogDetails; the Web controller only localizes them. Existing raw details, serialized contracts, all five culture labels and malformed-list failures are preserved. Focused reader/HTTP 48 passed; complete Web 642 passed; Activity Log Node 14 passed, all exit 0 without failures/skips. Standalone Web build had 0 warnings/errors. Engineering reference and tracking record updated. Changes remain uncommitted; browser acceptance is unclaimed. Await B3 acceptance and local commit authorization; do not start B4. |
| 2026-11-08 | B3 acceptance / B4 authorization | User accepted B3 and requested B4. Starting checkout is clean on `master` at the user-created B3 commit `88d9e52dbd3afc43a7b193220d07091e63babbff`. Reuse its passing Web baseline; replace only the in-memory event-catalog query with a direct canonical read, retaining ordering/localization/HTTP contracts. New commits and B5 remain pending. |
| 2026-11-08 | B4 implemented and verified | Endpoint reads the canonical catalog directly; redundant query/handler removed after checking callers. Exact catalog, canonical group/key ordering, five-culture localization and JSON shapes passed: focused 7, complete Web 642, Activity Log Node 14, all exit 0/no failures/skips; Web build 0 warnings/errors. Engineering reference and tracking record updated. Changes remain uncommitted; browser acceptance is unclaimed. Await B4 acceptance and local commit authorization; do not start B5. |
| 2026-11-08 | B4 acceptance / B5 authorization | User accepted B4 and requested B5. Starting checkout is clean on `master` at the user-created B4 commit `3a1cae66a0ead73f2c301db943df7dcee65df4c5`. Docker Linux engine is available; run the documented complete solution opt-in against uniquely named disposable PostgreSQL/SQL Server containers, review the B0-to-B4 diff and keep synthetic browser evidence separate. New commits, deployment and production-data changes remain unauthorized. |
| 2026-11-08 | B5 combined gate verified | Verified production SHA remains `3a1cae66a0ead73f2c301db943df7dcee65df4c5`. Full opt-in solution passed 1951 with 0 failures/skips, including both providers; Activity Log Node passed 14; Web build had 0 warnings/errors. Combined contract/deployment/failure-boundary review found no required follow-up changes. Isolated real-page browser checks covered title/no-op/restricted-only saves, literal details, settings/System filters, UTC/context and focus behavior in en-US, plus localized zh-Hans presentation; focused-host static-assets limits are recorded. Temporary host/artifacts and fixture containers cleaned up. Only this retained tracking document changed. Await final user acceptance; no new commit, push or deployment was performed. |
| 2026-11-09 | B5 acceptance | User explicitly accepted B5 and separately requested removing unused typed detail allocations during settings activity writes. |
| 2026-11-09 | Write-path follow-up verified | Started from `fac660509`. The writer and reader still use the same validation, and only reads collect typed details and clone their JSON values. The full Web suite passed 642/642 with no failures or skips, including valid and rejected envelopes and typed read round trips. This change does not affect the stored format or HTTP contracts. Browser and provider suites were not rerun. Changes remain uncommitted. |
