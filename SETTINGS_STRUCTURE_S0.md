# S0 Settings Compatibility And Boundary Evidence

## Resume Here

- Date: 2026-11-06, Asia/Taipei. Starting HEAD: `aa58da8f9` (`Plan structural settings improvements`), `master`, clean working tree.
- Authorization: the user requested entering B7 after reviewing `SETTINGS_STRUCTURE_PLAN.md`. This turn executes S0 test-only experiments and records the results; it does not authorize S1-S6 production implementation or a new commit.
- Production application files, stored keys, JSON fields, dependency versions, database schema, authentication policies, and live resources are unchanged.
- Current state: technical checks passed. The user confirmed the ordinary-model direction (D28). The separate Auth/Setup ownership/dependency question is still pending; do not infer its answer from the model confirmation.
- Next action: obtain the remaining ownership decision and S0 acceptance / S1 execution authorization. Preserve this report and both plan trackers across conversations. Do not begin S1 automatically.

## Experimental Scope

The experiments live only in `src/Tests/Moonglade.Web.Tests/Structure` and compiled Razor fixtures under that test project's `Views`/`Pages` folders. They use actual ASP.NET Core TestHost, MVC/Razor engines, the current JSON helpers and validators, and disposable in-memory SQLite. They are not new application routes or implementations.

The Web test project now uses the built-in Razor SDK with `AddRazorSupportForMvc` so test views are compiled. No package, project, runtime-compilation dependency, or production view was added. Its probe host retains production Assets controller metadata for real `/avatar` link generation but excludes production compiled views and other controllers from the isolated fixture. Existing Web tests still exercise their own application composition.

The models deliberately cover representative ordinary settings, not a completed conversion of every group: Feed, nested menus, and a render-only avatar URL. Actual production models remain mutable until authorized S1-S3 work.

## Results

| Question | Experiment / actual result | Consequence |
| --- | --- | --- |
| Can init-only properties bind? | A non-positional sealed record binds JSON and form posts through actual MVC. Missing FeedItemCount remains 0 at the request boundary; explicit 0/false and positive values bind. Negative values return HTTP 400 with the field validation key. | Do not alter request constructors/defaults to imitate persisted-loading defaults. |
| Can immutable values use the existing persisted default merge? | The actual generic `FromJsonWithDefaults` helper, invoked reflectively only in the test, loads immutable Feed/Menu candidates. `{}`, case-insensitive properties, repeated known properties, unknown fields, explicit null/false/0/empty lists, and populated nested menus match the current loader's canonical JSON for the valid cases tested. | Keep the existing persistence boundary and serializer options; no new JSON format is needed. This does not authorize rewriting partial saved JSON. |
| Are current menu constraints compatible? | ImmutableList graphs preserve null/empty distinctions, nested entries and round trips. Existing MaxLength works for 5 versus 6 menus; required titles/submenu URLs, null entries, null submenus, and enabled-null menus reject without mutation. | ImmutableList is a viable candidate for existing annotations and nullable menu arrays. |
| Is ImmutableArray categorically incompatible with MaxLength? | Separate .NET 10 probes accept lengths 0/1 and reject 2 under MaxLength(1). | No such incompatibility was found. The fuller nullable/nested HTTP proof used ImmutableList; do not report an unsupported-array limitation. |
| Does changing an immutable menu affect retained state? | Replacing a nested submenu creates a new graph; the old snapshot retains the old title. Attempting IList.Add against the original immutable collection throws NotSupportedException. | Immutability must extend through the graph; a read-only wrapper around mutable elements would not prove this. |
| Can current Razor helpers read init-only properties? | Actual label/input tag helpers render the existing display name, name, value 0 and range metadata from the immutable Feed candidate. | No blanket mutable form DTO layer is justified by this experiment. Other settings must still pass their real binding/validation tests during conversion. |
| Does one capture propagate through rendering? | Both an MVC view and an actual Razor Page capture once. A barrier pauses the partial while another snapshot is published; root, partial, component and layout all render the original Feed/Menu pair. The next render uses the new pair. | A capture passed through ViewData/model is sufficient for the tested render tree. Do not recapture separately in each partial/component/layout. |
| Does avatar fallback require mutating the store? | Null and empty avatar values render the actual Assets avatar route with `/blog` PathBase; a saved CDN URL is retained. Both old and current captured values remain unchanged after rendering. | Resolve null/empty display fallback in the render context; do not persist it or change explicit saved values. |
| Is a whole request implicitly frozen? | Two operations in the same test HTTP action capture before and after publication and observe 20 then 7. | D26 can be met without request-wide caching or a global ambient context. |
| Is the current update handler safe inside an outer transaction? | In SQLite, the existing handler updates runtime and emits its event while the outer transaction is still open. Rolling the transaction back restores stored Feed count 20 but leaves runtime count 1. | It is not a safe outer-transaction commit boundary. The passing characterization test documents unsupported composition, not an approved production behavior. |
| Can prepared related rows use one actual save? | An asset row is staged on the same scoped context, then the current settings command saves. Success publishes only after a fresh context can read both rows and CurrentTransaction is null. An injected settings trigger failure rolls back both rows and leaves runtime/event state unchanged. | Preparing rows and one SaveChanges is a feasible route for S4/S5. This is not yet an end-to-end avatar implementation or provider/uncertain-commit proof. |
| Can Setup reference Auth without a cycle? | Source analysis of all 20 production project-reference graphs, with the proposed Setup-to-Auth edge added only in memory, remained acyclic. No project reference was changed. | The proposed edge is technically feasible; ownership still needs the user's separate answer. |

An initial Razor Page fixture picked up the production `_ViewStart` and failed before the render barrier because its unrelated services were absent. The fixture was corrected to isolate compiled views; failures before the barrier now surface directly instead of being hidden by a timeout. The final MVC and Razor Page cases pass. This was a test-host composition correction, not evidence of an immutable-model incompatibility.

## Verification

All commands ran from the repository root; builds/tests were sequential.

| Check | Command | Result |
| --- | --- | --- |
| Web including new probes | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 375 passed, 0 failed, 0 skipped; includes all 44 new S0 cases. |
| Configuration | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 129 passed, 0 failed, 0 skipped. |
| Auth | `dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj --no-restore` | 48 passed, 0 failed, 0 skipped. |
| Setup, ordinary run | `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj --no-restore` | 58 passed, 0 failed, 17 explicitly skipped provider groups/theories. |
| Web build | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | 0 warnings, 0 errors. |
| Documents/diff | English task records, CRLF/final-newline checks, `git diff --check` | Passed. |

Total current focused results: 610 passed, 0 failed, 17 skipped. The earlier 1412/1412 full-provider run remains baseline evidence at `907ac2285`; it was not rerun after these test-only additions. S1 requires actual PostgreSQL/SQL Server installation/retry tests after ownership changes. No browser, production database, file deletion/upload, external service, or SMTP operation occurred in S0.

## Concrete Design For The Next Batches

The names below are proposed interfaces/files, not application changes. Preserve D1-D28 and existing successful-save contracts.

### S1: Protected State And Initialization

Proposed ownership awaiting its separate answer:

| Owner | Proposed responsibility / shape | Compatibility and dependency |
| --- | --- | --- |
| Auth | Own LocalAccountSettings and account reads/commands. A limited read contract supplies credential state to authorized authentication operations; ordinary settings readers do not expose it. A concrete owner publishes validated state after commit. | Keep the type's simple name and legacy key/JSON fields; namespace/file moves must not rename rows. Auth already references Configuration/Data/Utils. |
| Setup | Own SystemManifestSettings and installation metadata validation/default/reconstruction. Do not add an unused manifest singleton/read interface merely for symmetry if there is no runtime consumer. | Keep the legacy key/time/version rules. Proposed Setup-to-Auth reference coordinates account preflight/install and is acyclic. |
| Configuration | Own the nine ordinary preferences, their validator/default loader, runtime publication, and existing change-event contract. | No reverse dependency on Auth/Setup. Reuse JSON helpers and existing scoped Data context; no generic provider registry. |
| Web | Bind account requests, display safe account status, preserve sign-out and B5 audit behavior, and compose application workflows. | Ordinary views receive no credential model. Dedicated authenticator enrollment still renders its authorized setup key/QR; admin status uses only needed username/enabled fields. |

Required sequencing:

1. Read the legacy rows and preflight account and manifest rules before ordinary repair writes. Missing account in a nonempty database still stops. Missing manifest retains approved reconstruction semantics, rather than inventing historical time.
2. Prepare all eleven expected setting rows and repair/default information before writes. Fresh seeds plus every default still share the current first-install transaction/context and execution strategy. Do not let extracted owners commit or publish independently during seeding.
3. Publish loaded state only after that transaction commits. On existing-store initialization, protected failure must still prevent any repairs and D23 must still prevent host/worker startup.
4. Account updates preserve credential validation, TOTP setup intermediates, last-save-wins/write ordering, timestamp and change-event behavior. Do not silently introduce a second unsynchronized write path or remove a change notification while extracting ownership.
5. Keep the ordinary write lock's guarantee when extracting writers. If one concrete shared write gate is needed, justify its process-wide singleton lifetime without injecting a scoped DbContext into it. Do not create interfaces purely to mock this coordination.

Only ordinary-record conversion is approved by D28. Do not automatically turn credential models into records whose generated ToString exposes secret fields, or serialize protected state for diagnostics. Determine any protected read representation within the approved ownership boundary.

### S2/S3: Ordinary Snapshot And Reader Migration

- Proposed read shape: an ordinary reader captures a `BlogSettingsSnapshot` containing the nine immutable groups. Initialization and publication are available only through the concrete owner/write boundary, not the public consumer interface. The exact name is not an added public contract yet.
- Prepare a complete candidate before persistence. Inside the existing serialized write boundary, apply only the submitted groups to the latest root, persist, then exchange the root once and run post-commit events without request cancellation. Do not build a whole root from a stale request capture and overwrite unrelated groups that were not saved. Same-group last-save-wins remains full-group replacement; this is not a new field merge/conflict policy.
- Prefer non-positional init-only records and ImmutableList menu graphs, as confirmed by the user. Preserve the current annotations/JSON options, nullable disabled menus, and request-versus-persisted default distinction. Test each remaining group during conversion; add a boundary-specific request DTO only for an actual incompatibility.
- Root handler/view operations capture once. Pass the capture through ViewData/model to the tested render descendants and into any related queries. If an entry handler already captured state used to prepare the page, do not silently replace it with another capture in a result filter. Standalone renders need their own explicit capture; a later operation may capture fresh state.
- Resolve avatar display fallback with the existing route helper and PathBase, including empty strings. Do not store request-specific URLs in the immutable global snapshot and do not change the saved null/empty value merely to render a fallback.
- Migrate all relevant ordinary consumers, not just the settings controller: middleware, syndication, email, worker iterations, page models, layouts, partials, and components. A hosted singleton must not keep its initial ordinary capture forever; the sender display-name restart exception remains intentional.
- S2 proves immutable publication but may retain temporary read-only compatibility access. S3 removes repeated per-group recapture paths and proves coherent operations/renders under concurrent writes before claiming C1 complete.

### S4/S5: Avatar/CDN Workflow And Actual Commit

- Use one explicit Web application workflow for both avatar upload and image-delivery changes, composing existing asset/storage/settings ownership. No new project or generic storage abstraction is justified. Remove the old CDN event path only after both callers are migrated and tests show no double save/upload/event; site icons remain excluded.
- The current two paths use different fixed avatar names: a full AssetId in SettingsController.Image and a six-character prefix in SaveAssetToCdnHandler. Do not treat either legacy pattern, or an arbitrary CDN URL, as blanket permission to delete a file. New versions must have distinct application-owned candidate names.
- The proven low-overhead commit route is to stage related asset/configuration rows on one scoped context and perform one real save before publication. The existing asset event handler currently saves immediately, so it cannot be reused unchanged as a staging API. Finalize the owning preparation method/command during S4; do not add an outer transaction around the current configuration command and assume its events wait for that commit.
- If an explicit outer transaction is genuinely required, separate prepare/commit/publication first and test that runtime/cache events occur only after outer CommitAsync. Do not accept inner-SaveChanges publication or a rollback leaving runtime on the rolled-back version.
- Preserve old active resources until confirmed success. Then clean only a provably owned, no-longer-referenced old version. Cleanup failure keeps save success and new state plus Error. On uncertain commit, retain candidates and inspect durable references when possible; do not guess rollback or delete a possibly referenced file.
- Prove file ownership/reference ordering under concurrent avatar/CDN operations; preserve single-instance last-save-wins and local/public/original root isolation. Existing and externally owned filenames are not auto-migrated or mass-cleaned. Any orphan retention/retry/journal policy remains separately gated if needed.
- Keep B4 cache generations and B5 audit isolation after the real save; avatar-byte reads also need successful-commit invalidation resistant to stale in-flight factories. File/asset/settings/cache/sign-out failures must not be swallowed as audit-only failures.

## Gates And Handoff

| Gate | Status |
| --- | --- |
| G1: enter B7 / authorize S0 experiments | Fulfilled by the user's execution request |
| G2-M: existing ordinary models become init-only records with ImmutableList menus; DTOs only for demonstrated incompatibility | Confirmed by the user's first answer; D28 |
| G2-O: Auth owns accounts, Setup owns manifest, Configuration owns ordinary preferences; Setup references Auth | Pending separate answer; not implied by G2-M |
| S0 acceptance / S1 implementation | Not yet authorized by a subsequent batch request |
| S4/S5 exact workflow/commit/recovery protocol | S0 evidence and proposals recorded; revisit the concrete implementation before authorizing those batches |
| Local commit / push / deployment | No new authorization; S0 changes remain uncommitted |

When evolving these test-only candidates, migrate useful compatibility/concurrency cases to the real implementation and remove redundant prototype code rather than maintaining two settings systems. The outer-transaction characterization must change when the production boundary becomes safe or explicitly rejects the composition. Retain this evidence record and the plan as requested.
