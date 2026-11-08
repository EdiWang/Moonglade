# Activity Log Redesign Plan And Tracker

## Resume Here

- Planning snapshot: 2026-11-08, Asia/Taipei. Repository baseline was clean at `4140154d4e4c54b37b057a9e9d717306fdeefca1` on `master`. Recheck the current branch, HEAD, and `git status --short` before starting. Preserve other work.
- The user accepted B4 on 2026-11-08 and requested B5 integrated acceptance. B5 is technically complete and awaiting user acceptance: 1,944 .NET and 32 Node cases passed, zero failures/skips; both disposable providers, final Web build, and integrated browser/fault flows passed. The recorded detail-label casing issues and activity clear-confirmation localization are corrected. Started from clean `master` at `66953e371660dec6461b33b7abe911e76714dcb9`, the observed user B4 commit; no B5 commit was made. B2's separately authorized Development PostgreSQL upgrade and retained whole-database backup remain untouched. No real database operation, production upgrade, deployment, or automatic commit is authorized.
- In a fresh conversation, read this file, [AGENTS.md](AGENTS.md), and the applicable sections of the [engineering reference](docs/agent-reference.md). No Java, JDK, or JRE may be used. Repository text is English outside localization resources.
- Start with the earliest unaccepted batch in the progress table. Check the last recorded evidence and open decisions before touching code. If the work is already committed, verify that commit before resuming acceptance.
- Ask the user about new requirement, behavior, architecture, ownership, public contract, or data-policy choices. Explain the alternatives, impact, and recommendation. Continue independent work already authorized. Record each answer and the affected acceptance criteria here before starting dependent work.
- Retain and update this tracker after every batch, as explicitly requested. Keep long-lived operational instructions in the appropriate permanent docs without linking them back here. Store no credentials, protected values, or real request data in this file or test evidence.

## Confirmed Requirements

1. Activity Log is an administrator-facing operation history. Record administrator actions and automatic post publication. Exclude public visitor comment submissions. Record **one entry per automatically published post** and identify the actor as System. Administrator comment moderation and replies remain distinct from public submissions.
2. Ordinary writes are best effort. If the activity entry fails, log a safe diagnostic through the application logger and preserve the successful business outcome. This applies to automatic publishing and the clear-all operation as well.
3. Support **Clear all** only. Remove per-entry deletion from the page and the server. After a successful clear, attempt to write a new entry with the actor, time, and deleted count. If that write fails, keep the clear committed and report the failure only through the application logger. This supersedes the earlier suggestion to couple the two steps in one transaction.
4. Skip setting saves with no actual value changes. For actual changes, show old and new values for explicitly allowed fields. If none of the changed fields can display values, retain the update entry and show a localized message that no change values are available. Protected values such as passwords, hashes, salts, TOTP secrets, and tokens must never enter stored activity details, responses, or diagnostic logs.
5. Old activity rows may be discarded during upgrade. Preserve every other application data set. Support fresh install and manual, idempotent upgrades on both PostgreSQL and SQL Server.

## Proposed Design

- Persist a stable event key, such as `settings.comments.updated` or `post.published`, plus UTC time, actor, optional object data, and optional structured details. A single definition per event supplies its localized wording and filter group. The final key names, grouping, and detail contract are B0 review items.
- Present **Time**, **Actor**, and **Activity**, plus access to details. For an article, keep its stable ID and title at the time of the action. A settings event can identify its own group. Put IP and User Agent in details when available and omit request context for System activity. Derive the displayed summary and filter labels from the same event definitions.
- Keep `/admin/activitylogs` and the existing Razor Pages, Bootstrap, Alpine, and LiteBus structure. The goal is to remove the duplicate persisted `Operation` and enum-derived display text. Identify compatibility needs for `/api/activitylog` and its numeric filter values before changing the API contract.
- Compare effective typed settings from the actual save operation. Use the same pre-change and saved values inside the configuration write boundary, including for partial saves, concurrent saves, and legacy JSON normalization. Return or pass only the approved field-level differences to the activity writer after a successful save. Preserve configuration validation, snapshot publication, timestamps, cache invalidation, and the existing account sign-out behavior.
- Continue to use the existing activity module and provider-neutral EF mappings. An exact event inventory, a field-by-field details allowlist, and the publishing handoff are reviewable outputs of B0. Examples in this file do not approve any particular field, wire format, or new administrator event.

## Starting Points

| Area | What to inspect |
| --- | --- |
| Write and read model | [`Moonglade.ActivityLog`](src/Moonglade.ActivityLog), [`ActivityLogEntity`](src/Moonglade.Data/Entities/ActivityLogEntity.cs), [`ActivityLogConfiguration`](src/Moonglade.Data/Configurations/ActivityLogConfiguration.cs), and [`BlogDbContext`](src/Moonglade.Data/BlogDbContext.cs). Today the writer stores numeric `EventId`, free-form `Operation`, `TargetName`, and optional JSON. The query derives filter groups from numeric ranges. |
| HTTP and UI | [`ActivityLogController`](src/Moonglade.Web/Controllers/ActivityLogController.cs), [`ActivityLogs.cshtml`](src/Moonglade.Web/Pages/Admin/ActivityLogs.cshtml), [`admin.activitylogs.mjs`](src/Moonglade.Web/wwwroot/js/app/admin.activitylogs.mjs), and Web resources. Preserve the administrator authorization and antiforgery checks. The current detail view displays raw JSON, and the clear and individual-delete routes each write a separate activity entry. |
| Activity producers | [`BlogControllerBase`](src/Moonglade.Web/Controllers/BlogControllerBase.cs), controller callers, and [`PostManagementCommands`](src/Moonglade.Web/Commands/PostManagementCommands.cs). Inventory all `CreateActivityLogCommand`, `LogActivityAsync`, and `LogActivityAfterSaveAsync` call sites, including callers in tests. [`CommentController`](src/Moonglade.Web/Controllers/CommentController.cs) explicitly passes a visitor name for public comment activity today. |
| Settings and protected account operations | [`SettingsController`](src/Moonglade.Web/Controllers/SettingsController.cs) and [`UpdateConfigurationCommand`](src/Moonglade.Configuration/UpdateConfigurationCommand.cs). Both full and partial saves use the shared configuration gate. Current settings activity contains selected after-values or no metadata. Use the existing failure and concurrency tests as the baseline. |
| Scheduled publishing | [`ScheduledPublishService`](src/Moonglade.BackgroundServices/ScheduledPublishService.cs) and [`PublishScheduledPostCommand`](src/Moonglade.Features/Post/PublishScheduledPostCommand.cs). The worker currently receives only a count. Identify the successfully published posts from the write path and preserve scheduler wake-up, cache invalidation, and the existing publication behavior. |
| Installation and manual upgrade | [`StartUpInitializer`](src/Moonglade.Setup/StartUpInitializer.cs) checks the log table when determining database emptiness. The DbContext also clears it during application data cleanup. Follow [database script rules](database/README.md) and [migration verification](docs/agent-reference.md#migration-verification). PostgreSQL uses `activity_log` and snake_case columns after its naming upgrade. SQL Server uses PascalCase. |

## Batch Workflow

1. Record the starting commit, batch dependencies, working tree status, test environment, and any open question. Inspect the affected callers and tests. Keep the change local to the batch.
2. Run the specified checks on disposable data. Record exact commands, results, counts or representative scenarios, and any skips. A failing or unavailable required check keeps the batch open unless the user accepts the specific gap. Run `git diff --check` and inspect `git status --short` before committing and finishing.
3. Record the completed changes and test evidence in this tracker and include the tracker update in the authorized batch commit. Record the implementation commit ID and acceptance evidence in a subsequent tracker update. Mark the batch **Awaiting acceptance** until the user confirms the result, then record the decision and mark it **Accepted**. Do not advance a dependent batch without acceptance unless the user changes the gate.
4. Leave the next action, any blocker, and the rollback point in the progress row. Commits must build and run together with their own test fixtures. B2 introduces a manual schema prerequisite for existing installations. B5 covers integrated acceptance. Deployment requires separate authorization.

## B0 — Baseline And Design Review

**Depends on** authorization to start the delivery plan. No application changes.

- Inventory existing producers, event grouping, settings fields, API callers, localized UI resources, and relevant tests. List which administrator events carry forward and which existing visitor event is excluded. Recommend whether to add any newly discovered administrator events and ask before expanding coverage.
- Present a reviewable event inventory, settings allowlist by group, and API compatibility plan. Check whether the authenticated endpoints have consumers beyond the repository before proposing any response or filter change. Record known consumers or the limits of that check.
- Describe the scheduler result boundary and propose how to identify committed posts without adding a dependency in the wrong direction. Confirm the proposed schema and two-provider cutover, including what the upgrade discards and how rollback works.
- **Test** the current Web and Configuration projects and run a Web build. If feasible, capture the existing Activity Log behavior on an isolated host with synthetic data. Record baseline failures and avoid mixing unrelated fixes into this task.
- **Commit** the reviewed decisions and baseline evidence in the tracker, once authorized.
- **Accept when** the user approves the inventory, allowlist, and any contract or ownership choices raised by the review. Record the authorized implementation scope and point to the next batch.

## B1 — Clear-Only Management And Visitor Exclusion

**Depends on** B0 acceptance. Keep the existing storage model for this batch.

- Remove the single-entry delete API, handler, UI control, and related strings. Keep the protected Clear all action with confirmation. Record its deleted count after the delete completes. Isolate post-clear activity failures so the response still reports that the clear succeeded. An unsuccessful delete must not produce a successful-clear entry.
- Stop public comment submissions from writing activity entries. Preserve comment validation, moderation, persistence, and notifications. Keep administrator comment activity.
- **Test** Web HTTP and controller behavior for successful clear and the remaining entry, failed clear, failed activity dispatch or persistence after clear, authorization and antiforgery, and successful public comment submission with its ordinary side effects. Exercise the management flow on a disposable host and confirm cancellation from the UI leaves the history intact.
- **Commit** clear-only behavior, visitor exclusion, affected tests and localization, and tracker evidence as one batch.
- **Accept when** the user can clear seeded history and see the new clear entry on success, with no per-entry control. Show fault-test evidence that a failed activity write leaves the clear committed. Show that a visitor comment still works without producing an activity entry.

## B2 — Canonical Event Model, Read View, And Database Cutover

**Depends on** B1 acceptance. Update all active writers, readers, storage mappings, test fixtures, resources, and the UI together to avoid a partial contract cutover.

- Replace persisted duplicate wording with the approved event key. Use the same definitions for filter groups, activity text, and detail headings. Keep meaningful targets and existing approved details. Render the concise table and a readable detail view, with an intentional empty state when details are absent. Preserve date filtering, pagination, local display of UTC times, and clear-only behavior.
- Add guarded manual upgrades for PostgreSQL and SQL Server and document prerequisites in `database/README.md` and the appropriate upgrade guide. Discard legacy activity rows only while transitioning the old schema. A second run against the upgraded schema must preserve activity rows written after the first run. Fresh databases must get the final schema through normal setup.
- **Test** Web and Setup projects, Web build, relevant Node tests, and both providers. On each provider, verify fresh initialization and an upgrade from the supported old schema seeded with legacy activity and unrelated data. Check a second run after inserting a new activity row, assert unrelated data is preserved, and exercise listing, filtering, detail, and clear against the new schema.
- **Commit** the coordinated cutover, both upgrade scripts, docs, and verification evidence together. Record the previous application commit and the database restore requirement for rollback.
- **Accept when** the new list shows a single activity description per row and filters still identify the event precisely. The upgrade loses only permitted legacy activity rows, and reruns preserve new entries on both providers. Demonstrate the UI using a disposable host.

## B3 — Setting Changes

**Depends on** B2 acceptance and the B0 allowlist and ownership decisions.

- Connect the settings save outcome to activity logging. Cover full and partial updates, the exact effective before and after values from that save, no-op suppression, and real changes with no permitted values to show. Keep credential actions as explicit events with safe details. Preserve current behavior for failed validation, persistence, cache events, and sign-out unless a change is separately approved.
- **Test** Configuration and Web projects and Web build. Cover allowlisted values, non-allowlisted or protected values, no-op saves, overlapping partial saves, and post-save activity failure. Verify the successful save and runtime configuration separately from the activity entry.
- **Commit** settings changes, tests, and progress evidence.
- **Accept when** an administrator can inspect a real safe change, see no row for a no-op, and see an update without values for a restricted-only change.

## B4 — Automatic Publication

**Depends on** B2 acceptance and the B0 publishing handoff decision. B3 has no code dependency on this batch. The proposed delivery order puts B4 after B3 acceptance.

- Emit one System activity for each post actually published by the scheduler. Use a stable post ID and the title at publication. Preserve the existing scheduled-selection rules, publication timestamps, and cache updates. An activity write failure must not change the publication outcome, and a subsequent scheduler pass over already published posts must not produce repeat entries.
- **Test** Features and Web projects and Web build. Exercise a scheduler pass with multiple due posts plus ineligible posts, a subsequent pass, and failure to write an activity entry. Verify the publication and cache outcomes separately from the activity entries.
- **Commit** scheduler changes, tests, and progress evidence.
- **Accept when** multiple automatic publications produce individual System entries and each details view identifies the intended article.

## B5 — Integrated Acceptance And Handoff

**Depends on** B3 and B4 acceptance. No deployment is in scope.

- Run the full solution and relevant Node tests, the Web build, and required provider suites. Use an isolated host and synthetic data for browser acceptance. Verify fresh setup, upgraded setup, settings changes, System publishing, visitor exclusion, filters, detail, localization, administrator access, and clear-all success and failure behavior. Record what was checked through the UI versus by fault injection.
- Review the documented upgrade order, backup/rollback, and the cutover limits. Confirm no production or external service was contacted. Resolve regressions in the owning batch, retest, and repeat affected acceptance before final sign-off.
- **Commit** final verification records and any needed durable documentation corrections in the authorized closeout commit.
- **Accept when** the user signs off on the integrated result and any explicitly documented verification gap. Retain this tracker and record the final commit and next action. Applying scripts, deploying, or changing a real database requires separate authorization.

## Verification Commands

From the repository root. For provider checks, use the repository's disposable fixtures with Docker. `MOONGLADE_TEST_CATEGORY_DATABASES=1` currently enables the existing provider fixture. If the activity upgrade tests use a different opt-in, record the exact command in the B2 evidence. Merely enabling the existing fixture does not exercise a new activity upgrade.

```powershell
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs
dotnet test --solution src/Moonglade.slnx
git diff --check
```

## Progress

| Batch | Status | Tests and acceptance | Commit | Next action |
| --- | --- | --- | --- | --- |
| Planning | Complete | The user requested B0 execution. | Included in B0 | See B0 evidence below. |
| B0 | Accepted | User approved the review package on 2026-11-08. Web 370 passed, Configuration 102 passed, Web build clean. Isolated browser baseline unavailable. | Tracker introduction commit (`git log --diff-filter=A -1 -- ACTIVITY_LOG_PLAN.md`) | B0 closed. |
| B1 | Accepted | User accepted B1 on 2026-11-08. Web 382 passed; Node 22 passed; Web build clean; SQLite HTTP/fault and browser verification complete. | `3226ff207417923e1894ab7dbdb2fb67b57f871b` | B1 closed. |
| B2 | Accepted | User accepted B2 on 2026-11-08 after the Development migration. Web 461, Setup 160, activity provider subset 10, Node 28; zero failures/skips. Web build and disposable browser passed. | `19e12ef03e954591c40adfdbb5caa3651568bfac` (observed user commit) | B2 closed. |
| B3 | Accepted | User accepted B3 on 2026-11-08. Configuration 310, Web 607, Node 31; zero failures/skips. HTTP subset 63 passed; Web build and isolated browser passed. | `7ed70bc12460676fcb8ec01ff8c605f06bf9d8f8` (observed user commit) | B3 closed. |
| B4 | Accepted | User accepted B4 on 2026-11-08. Features 132, BackgroundServices 15, Web 630; zero failures/skips. Web build and isolated browser passed. | `66953e371660dec6461b33b7abe911e76714dcb9` (observed user commit) | B4 closed. |
| B5 | Awaiting acceptance | 1,944 .NET and 32 Node passed, zero failures/skips. PostgreSQL 45 and SQL Server 40 included; native reports validated. Final Web build and integrated/repeated localized browser/fault checks passed. | Not committed | User integrated sign-off; application rollback point `66953e371660dec6461b33b7abe911e76714dcb9`. No deployment or real database operation. |

### Batch Evidence To Append

For each batch, record the date, starting commit, decisions and their source, implementation commit, commands and outcomes, disposable provider or host details, acceptance evidence, unresolved risks, rollback point, and next action. Keep partial results if work stops mid-batch. Distinguish technical verification from user acceptance.

## B0 Evidence And Review Package

### Baseline

- 2026-11-08, Asia/Taipei. Started on `master` at `4140154d4e4c54b37b057a9e9d717306fdeefca1` with only this tracker untracked. SDK `10.0.403`. No application code, production configuration, or database data changed. The starting commit remains the rollback point for application code.
- `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` passed 370 tests, zero failures or skips. Existing coverage includes the activity controller, settings and avatar activity-failure isolation, and post cache invalidation.
- `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj` passed 102 tests, zero failures or skips. Existing coverage includes concurrent configuration updates and persistence/publication ordering.
- `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` succeeded with zero warnings or errors. No live baseline host was started. `docker info` could not connect to the Docker Desktop Linux engine. The user's screenshot is the visual baseline. No provider migration or browser acceptance was performed in B0.

### Proposed Event Inventory

Keys in each row use the shown prefix followed by one of the listed suffixes. Groups and labels are explicit and localized, including `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`. Old enum numbers are listed for compatibility review.

| Filter group | Legacy IDs | Proposed keys |
| --- | --- | --- |
| Posts | 200–209 | `post.` + `schedule.cancelled`, `schedule.postponed`, `restored`, `permanently-deleted`, `recycle-bin.cleared`, `created`, `updated`, `deleted`, `published`, `unpublished` |
| Posts | New in B4 | `post.automatically-published`, one per committed article and shown as System |
| Pages | 400–405 | `page.` + `created`, `updated`, `deleted`, `restored`, `permanently-deleted`, `recycle-bin.cleared` |
| Categories | 100–102 | `category.` + `created`, `updated`, `deleted` |
| Comments | 301–303 | `comment.` + `approval-toggled`, `deleted`, `replied` |
| Tags | 600–602 | `tag.` + `created`, `updated`, `deleted` |
| Themes | 700–701 | `theme.` + `created`, `deleted` |
| Settings | 800–808 | `settings.` + `general.updated`, `content.updated`, `comments.updated`, `notifications.updated`, `subscription.updated`, `images.updated`, `advanced.updated`, `appearance.updated`, `custom-menu.updated` |
| Account | 809, 830 | `account.` + `password.updated`, `authenticator.reset` |
| Images | 810 | `image.uploaded` |
| Site assets | 820–825 | `asset.` + `avatar.updated`, `site-icon.updated`, `verification-file.created`, `verification-file.updated`, `verification-file.deleted`, `verification-file.toggled` |
| Widgets | 850–852 | `widget.` + `created`, `updated`, `deleted` |
| Activity log | 901 | `activity-log.cleared` |

- Remove visitor `CommentCreated` (300) and individual `ActivityLogDeleted` (900). `Default` (0) has no producer. Carry forward the 49 remaining produced types and add the automatic-publication type in B4. Keep the current distinction between editor saves and explicit publish/unpublish actions. No new administrator producers are proposed for gaps such as Webmention moderation, test email, or authentication sessions.
- Carry forward useful identifiers, display names, counts, status flags and other small structured values. Use IDs and captured titles where available from the operation result, with a safe ID fallback when no display name is available. The recommendation is to omit copied comment reply bodies, category notes, and site verification `ContentBytes` from new entries. Retain the verification filename, ID when available, and ordinary status/type information. Render names and details as plain text.

### Proposed Settings Value Allowlist

Only the fields below may have values stored. All other changes still count toward the decision to log a settings update. A mixed update shows allowed differences without naming or disclosing restricted fields. A restricted-only update shows the localized empty-values explanation. Treat booleans, numbers and enum choices as typed data with localized display labels. Display approved short public labels as plain text.

| Settings group | Exact proposed fields |
| --- | --- |
| General | `LogoText`, `SiteTitle`, `SideBarOption`, `WidgetsProfile`, `HotTagAmount`, `WidgetsCategoryList`, `WidgetsSubscriptionButtons`, `UseGravatarAsProfilePicture`, `DefaultLanguageCode`, `UseDublinCoreMetaData` |
| Content | `DefaultEditor`, `PostListPageSize`, `PostTitleAlignment`, `ShowCalloutSection`, `ShowPostFooter`, `DocumentOutline`, `EnableViewCount`, `MaximumPageNumbersToDisplay` |
| Comments | `CommentProvider`, `CommentOrder`, `EnableComments`, `EnableGravatar`, `EnableMarkdownLinks`, `RequireCommentReview`, `CloseCommentAfterDays`, `EnableWordFilter`, `WordFilterMode`, `EnableCommentSectionOnMobile` |
| Notifications | `EnableEmailSending`, `SendEmailOnCommentReply`, `SendEmailOnNewComment` |
| Subscription (`FeedSettings`) | `FeedItemCount`, `UseFullContent` |
| Images | `IsWatermarkEnabled`, `KeepOriginImage`, `WatermarkFontSize`, `WatermarkColorA`, `WatermarkSkipPixel`, `EnableCDNRedirect` |
| Advanced | `EnableWebmention`, `EnableOpenSearch`, `EnableFoaf`, `EnableOpml`, `EnableSiteMap`, `EnablePostScheduler`, `WarnPostExternalLink`, `WarnCommentExternalLink`, `EnableJsonLdBlogPosting` |
| Appearance | `ThemeId`, `EnableCustomCss` |
| Custom menu | `IsEnabled` |

- Excluded examples include email addresses and display names, URLs, custom markup and scripts, filter keywords, watermark text, robots text, CSS, the menu graph, and legacy `AvatarUrl`. Account and installation settings are outside this allowlist. Existing password and authenticator actions remain named events without protected values.
- Configuration owns the effective comparison under `ConfigurationWriteGate`, using structural equality for nested menu data and typed values for ordinary fields. Keep it separate from the existing raw persisted-JSON equality test that governs timestamp/cache behavior. Propose a small internal save outcome carrying an effective-change flag and only safe projected differences for Web to record. Preserve existing HTTP settings responses and existing account behavior.

### Proposed API And Storage Contract

- Repository search found one runtime client, the admin page module `admin.activitylogs.mjs`, and controller tests. No tracked integration or documentation was found that calls these routes. Deployment logs and off-repository clients were unavailable, so confirm whether any scripts or integrations need the current contract.
- The user confirmed there are no external consumers. Keep the admin page and API route paths, pagination/date inputs, authorization, and antiforgery. Update UI and endpoints together in B2. `/eventtypes` returns grouped `{ key, label }` entries. `/list` uses repeated `eventKeys` string filters and returns event key, localized summary, actor, and time. `/{id}/metadata` returns a structured detail object with contextual fields and optional settings changes. An existing row with no extra details has a successful response and an empty state. An unknown ID returns 404. Validate unsupported filter keys rather than silently dropping them. `DELETE /clear` retains its existing success response. B1 removes `DELETE /{id}` as already agreed.
- The coordinated cutover removes the numeric `eventTypes` filter, stringified metadata, and superseded list fields without a compatibility adapter.
- Keep the existing activity table and generated long ID. Proposed columns are required `EventKey`, UTC occurrence time, and actor kind, plus optional actor display snapshot, target ID, target display snapshot, IP, User Agent, and structured details serialized as JSON text. Use explicit Administrator/System actor kinds so a login name cannot be mistaken for automation. Keep targets detached from live entities so records survive target deletion. Use a deterministic newest-first order by time then ID. Event definitions stay in `Moonglade.ActivityLog`, with presentation/localization in Web.
- For B2, stop the old host before the manual upgrade. Use the installed provider schema after any required PostgreSQL category ID and naming scripts. Verify a recognizable legacy shape, then clear only the old activity rows and change only the activity table in a transaction. A complete new shape is a no-op, including after new rows are written. Reject unknown or partial shapes before deleting rows. Test fresh install, both provider upgrades, and a second run. Document a pre-upgrade backup and rollback to the matching old application and schema. A whole-database restore after reopening writes can discard unrelated post-upgrade changes. No production upgrade or restore is authorized by this plan.

### Proposed Scheduled Publication Handoff

- Features already owns selection and database persistence. It returns only an EF affected-entry count to `ScheduledPublishService`, after dispatching `PostChangedEvent`. Returning a list from the existing command is smaller on the happy path, but a cache-event exception after commit would prevent the worker from getting those posts.
- Recommend a dedicated in-process notification from Features for committed scheduled posts, carrying only ID, captured title and publication UTC time. Attempt the existing cache notification as today. Attempt the activity handoff after that attempt even if the cache notification fails, while preserving the original cache error behavior. Web handles the notification and writes per-post System entries through the existing ActivityLog writer. Isolate dispatch and individual activity-write errors and keep them outside the publishing transaction. Features keeps no dependency on ActivityLog or Web. Use the existing process only. No queue, durable delivery, or multi-host coordination is proposed.
- Add `Moonglade.BackgroundServices.Tests` to B4's focused checks alongside Features and Web. Verify committed posts with an activity failure, a second scheduler pass, and a failed post-commit cache notification. Preserve the current publication and cache semantics throughout.

### B0 Decision Status

- On 2026-11-08 the user accepted B0 and authorized committing this tracker. The user explicitly approved the field-by-field settings allowlist and confirmed no external Activity Log API consumers. The accepted review package carries forward existing administrator events, adds one entry per automatically published post, omits the proposed copied metadata, and confines the manual schema upgrade to the activity table. See the sections above for the accepted API contract, storage cutover, and scheduled handoff.
- B0 sign-off alone did not authorize implementation or real database operations. The subsequent user request to start B1 authorizes that batch's implementation and disposable verification only.

## B1 Evidence

- 2026-11-08, Asia/Taipei. User requested reading this tracker and starting B1. Started on `master` at `0a892c2cd6420e3f34d433c4a59a7f84d7170b8f` (the accepted B0 tracker commit), with a clean working tree and SDK `10.0.403`. This commit is the application rollback point.
- Scope: remove the individual-delete route, command/handler, page control, JavaScript action, and dedicated localized strings; stop public comment activity dispatch; preserve administrator comment actions and the existing storage/read contract. Retain legacy enum values until B2 so old history remains readable/filterable.
- Clear dispatch remains outside audit isolation: delete failures must propagate. After a successful clear, reuse the existing post-save audit guard so mediator exceptions or cancellation do not change the HTTP 204 outcome. The real activity writer already logs persistence failures without throwing.
- `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` passed with zero warnings and errors.
- Browser verification used a temporary loopback Kestrel host at `http://127.0.0.1:54660/admin/activitylogs`, an in-memory SQLite database, the production Activity Log controller and LiteBus handlers, and the real compiled Razor page/layout/Bootstrap/Alpine assets. Authentication was a synthetic administrator fixture; production authentication and other application services were not started. Configuration sources were replaced rather than using Development overrides. This does not prove PostgreSQL/SQL Server behavior or full-host startup.
- At 1600 by 1000 CSS pixels, all three seeded rows exposed only Detail. Opening Clear all and choosing Cancel preserved IDs 1, 2, and 3 and sent no delete request. Confirming Clear all returned HTTP 204, reloaded one `ActivityLogsCleared` row for `synthetic-admin` with UTC time displayed locally, and showed the existing success toast. Opening Detail showed `DeletedCount: 3`. The removed individual-delete route returned 404; a clear request without antiforgery returned 400 and preserved the remaining row. Browser snapshots and screenshots were inspected in the session. The temporary browser tab and host were stopped after verification.
- The first full Web regression run discovered 382 tests: 381 passed and one failed. A real INSERT failure left the attempted activity entity tracked as Added; after removing the fault, another save on that DbContext recreated the failed audit. The writer now detaches only that attempted entity on failure, preserving unrelated tracked changes. The regression checks committed clear, HTTP 204, the error diagnostic, later persistence of an unrelated setting, and no resurrected audit. This fixes a failure-handling defect directly coupled to B1 rather than weakening the test.
- Final `dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` passed 382 tests, zero failures and skips (exit 0). The following `dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet` passed with zero warnings and errors.
- `node --test src\Tests\Moonglade.Web.Tests\JavaScript\ActivityLogTests.mjs` passed all four discovered cases (parent plus three behavior subtests), with no failures, cancellations, or skips. The complete JavaScript wildcard run passed 22 cases. New tests use the real activity module and pagination with dependency hooks; no extra JavaScript packages were added.
- HTTP tests use the production local authorization policy and actual Razor antiforgery token with synthetic authentication claims, not real sign-in credentials. Comment tests use real domain persistence, moderation, and the database email outbox; no SMTP delivery is attempted. Existing administrator approval/delete/reply and captured-policy tests pass.
- The test pipeline performed bounded assertion-quality and test-gap reviews. Final manual review confirmed fault ordering, exact counts, UTC timestamps, protected routes, state preservation, notification payloads, and realistic legacy filter values. All four edited localization resources parse as XML. `git diff --check` passed; edited files retain CRLF/final newlines.
- Verification limits: Docker Linux engine is unavailable (`docker info --format '{{.ServerVersion}}'` failed to connect to its named pipe), so no PostgreSQL/SQL Server run was performed. B1 makes no schema change. Full-host startup, real local/OIDC sign-in, and deployment are not claimed by the isolated fixture checks. No existing database or external service was contacted.
- No commit or deployment has been performed. The temporary preview project and binaries were removed after stopping its host and browser tab. Next action: user acceptance and explicit commit authorization; B2 remains blocked on B1 acceptance.
- Subsequent acceptance: the user confirmed B1 acceptance and requested B2 on 2026-11-08. At B2 start, the checkout was clean on `master` at `3226ff207417923e1894ab7dbdb2fb67b57f871b` (`Make activity history clear-only and resilient`). The existing commit was observed, not created by the B2 session.

### B1 Requirement Evidence

All C# names below are in `src/Tests/Moonglade.Web.Tests/Controllers`; Node names are in `JavaScript/ActivityLogTests.mjs` in that project.

| Requirement | Evidence |
| --- | --- |
| Clear empty or seeded history; exactly one administrator entry with time and count | `ActivityLogControllerTests.Clear_WithRealHandlers_ReplacesHistoryWithCountedAdministratorAudit` (0 and 3 rows). Browser confirmed three rows became one with `DeletedCount: 3`. |
| Failed delete preserves history and produces no successful-clear entry | `ActivityLogControllerTests.Clear_WhenDatabaseDeleteFails_PreservesRowsAndDoesNotAudit`. |
| Failed activity dispatch or cancellation does not undo clear or change HTTP success | `ActivityLogControllerTests.Clear_WhenAuditDispatchFails_PreservesCommittedDeleteAndLogsSafely` (exception and cancellation). |
| Failed activity persistence is logged; clear stays committed without a later stale insert | `ActivityLogControllerTests.Clear_WhenAuditPersistenceFails_DoesNotResurrectOnNextSave`. |
| Administrator authorization, antiforgery, and existing response/filter shape | `ActivityLogHttpTests.Clear_AnonymousOrNonAdministrator_IsDeniedWithoutMutation`, `Clear_AdminWithoutValidAntiforgery_IsDeniedWithoutMutation`, and `Clear_AdminWithPageToken_ClearsHistoryAndReturnsLegacyReadShape`. |
| Public comments preserve validation, moderation, review, persistence and notifications without visitor activity | `CommentControllerTests.Create_WithRealPersistence_PreservesModerationReviewAndOutboxWithoutAudit`, `Create_WhenEmailIsInvalid_ReturnsValidationProblem`, and `Create_WhenSubmissionGuardRejectsRequest_ReturnsValidationProblem`. |
| Administrator comment operations retain their activity entries | Existing `Approval_WhenCommentExists_ReturnsOkAndWritesActivityLog`, `Delete_WhenCommentsExist_ReturnsOkAndWritesActivityLog`, and `Reply_WhenCommentExists_ReturnsOkAndWritesActivityLog`. |
| UI cancellation preserves history; confirmation reloads; failed clear reports error without success | `cancelled clear keeps history and pagination without clear or refresh`, `confirmed clear waits for delete then reloads page one and reports success`, and `failed clear preserves history and page and surfaces error without success`. |
| Remove individual deletion without changing the B1 storage model | Controller route and command/handler deleted; Razor/JavaScript control and four cultures' dedicated strings removed. Browser confirmed Detail-only rows and 404 for the former individual-delete URL. |

## B2 Evidence

- 2026-11-08, Asia/Taipei. User accepted B1 and authorized proceeding to B2. Started from a clean `master` at `3226ff207417923e1894ab7dbdb2fb67b57f871b`; this is the pre-cutover application rollback point. Existing installations additionally require restoring the matching pre-upgrade activity schema/history backup to roll back.
- B2 implements the accepted stable key catalog for the 49 existing administrator events, explicit Administrator/System actor kinds, detached target snapshots, UTC occurrence time, and approved structured details. The automatic-publication producer remains B4 work; settings comparisons and no-op suppression remain B3 work.
- All active producers, readers, DTOs, Razor, JavaScript filters, localized labels, and fixtures switch together. Existing API paths stay; numeric `eventTypes` is rejected in favor of repeated `eventKeys`, and metadata becomes a structured object rather than JSON text. Empty-details rows remain distinguishable from nonexistent rows. Missing target display names fall back to safe IDs; unnamed administrators use the localized Administrator label.
- Ordinary activity dispatch/persistence is best effort. Diagnostics identify the canonical event, failure type, and available HTTP trace, without logging target values, request context, protected details, or potentially sensitive exception messages. Failed inserts remain detached without clearing unrelated tracked changes.
- SQL cutover is manual, transaction-protected, and legacy-shape-only. Old activity rows may be discarded once; a recognized final shape must preserve subsequent history on rerun. Both providers must reject missing/unknown/partial shapes before deleting rows. No existing installation was upgraded at the initial B2 handoff; the later explicitly authorized Development upgrade is recorded below.
- Docker initially had no available Linux engine. The user chose to start Docker Desktop and complete both provider checks rather than defer verification. A subsequent `docker info --format '{{.ServerVersion}}'` succeeded with server `29.9.2`. Actual disposable PostgreSQL/SQL Server activity checks passed **10/10, zero failures/skips**. This is not authorization to apply scripts to an existing installation.
- Both providers verified fresh ten-column schemas and UTC handling, the supported nine-column legacy cutover, preservation of real synthetic post/category/configuration sentinels, identity continuation, post-cutover rerun preservation, listing/filter/detail/clear, and rejection of unknown/partial/wrong-length schemas. Tests reconnect after the standalone SQL Server maintenance session to model a fresh application connection and its normal affected-row counts.
- Browser verification used the actual compiled Razor/layout/assets and production activity handlers on a temporary loopback Kestrel host with in-memory SQLite and a synthetic administrator. No startup worker, developer configuration, real credentials, existing database, or external business service was used. English and Simplified Chinese showed concise rows, typed/plain-text details, distinct actor kinds even for an administrator named `System`, and the explicit System empty-details state. Page two had four of fourteen rows; UI event-key/date filtering returned the twelve matching posts with local-date boundaries converted to UTC.
- Browser Cancel preserved fourteen rows. Confirmed clear returned HTTP 204 and left exactly one `activity-log.cleared` Administrator entry with `DeletedCount: 14`; its structured detail modal displayed the count. The isolated host intentionally omitted generated manifest/favicon/scoped-CSS endpoints, causing ancillary resource 404s but no activity JavaScript errors. The browser and host were closed, the listener was checked stopped, and their specific scratch artifacts were removed.
- Final Web/Setup/Node verification and the Web build passed. Web: **461 passed, zero failed/skipped**. Full Setup with the disposable-provider opt-in: **160 passed, zero failed/skipped**. The activity-only provider subset additionally passed **10/10**, five per provider; this is a subset of the full Setup run, not ten additional unique cases. Node: **28 passed, zero failed/skipped**. Web build: **zero warnings/errors**. No required verification gap remains.
- Regression checks corrected the Japanese System label to meaningful localized wording, added available stable IDs for avatar/site-icon targets, and verified unnamed-actor and safe-ID presentation fallbacks. The test README and permanent architecture/upgrade instructions are synchronized. Focused read-only test quality review covered 41 C# test methods and 362 assertion callsites, finding no assertion-free or trivial-only tests; no empirical mutation-testing result is claimed.
- Final commands ran from the repository root and exited zero:

```powershell
dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj
dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --filter-class '*ActivityLogDatabaseTests'
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
git diff --check
```

- Disposable provider containers were removed by the fixtures. Browser/preview and test-pipeline scratch records were removed after evidence was retained here. Changed Markdown local links, CRLF/final-newline conventions, and the working tree were checked. Changes were local and uncommitted at the initial B2 handoff; the later user commit and authorized Development migration are recorded below. No deployment was performed.

### B2 Requirement Evidence

Web C# names below are under `src/Tests/Moonglade.Web.Tests/Controllers`; provider names are under `src/Tests/Moonglade.Setup.Tests/Database`.

| Requirement | Evidence |
| --- | --- |
| Exact catalog of 49 active events with shared localized identities | `ActivityLogModelTests.Catalog_All49ActiveEvents_HaveExactUniqueKeysGroupsAndLabels`; `ActivityLogHttpTests.Catalog_All49Events_LocalizedFiltersSummaryAndHeadingShareIdentity` covers five cultures. |
| Required UTC, generated IDs, snapshots, typed scalar/array details and string boundaries | `ActivityLogModelTests.Writer_SqliteRoundTrip_PreservesGeneratedIdUtcSnapshotsAndTypedDetails`, `Writer_FlatGuidAndIntegerArrays_PreserveTypeOrderAndEmptyPartition`, and `Writer_StringLimits_PreserveAtBoundaryAndTruncateOnlyExcessAfterTrim`. |
| Reject unsupported events/actors/restricted details without persisting or logging contents | `ActivityLogModelTests.Writer_InvalidEventsActorsAndRestrictedDetails_RejectBeforePersistenceAndLogNoContents`. |
| System omits request context; administrator named System retains its actual kind | `ActivityLogModelTests.WriterAndReader_SystemOmitsRequestContext_AdministratorNamedSystemRemainsAdministrator`; disposable browser demonstrated both icon/actor-kind presentations. |
| Deterministic time/ID pagination, inclusive dates and repeated event-key filters | `ActivityLogModelTests.List_TimeThenIdDescending_PaginatesInclusiveDatesAndRepeatedKeyFilters`; browser page-two and event/date-filter checks. |
| Detached target snapshots survive deletion; display/actor fallbacks remain meaningful | `ActivityLogModelTests.Detail_TargetSnapshotSurvivesCategoryDeletion_EmptyDetailsStillReturnRow`; `ActivityLogControllerTests.List_MissingTargetDisplay_UsesSafeStableIdAndPrefersCapturedTitle`; `ActivityLogHttpTests.BlankActorAndTargetDisplay_UseLocalizedAdministratorAndStableIdFallback`. |
| Empty-detail 200 versus missing 404; readable context/fields; protected endpoints | `ActivityLogHttpTests.Metadata_ExistingEmptyRowIs200_MissingIs404_WithReadableContextAndTypedFields`, `ReadEndpoints_AnonymousAndNonAdministrators_AreDenied`, and `Clear_AdminWithoutValidAntiforgery_IsDeniedWithoutMutation`. |
| B1 clear-only and best-effort behavior remains intact | `ActivityLogControllerTests.Clear_WhenAuditPersistenceFails_DoesNotResurrectOnNextSave`; retained dispatch/delete/HTTP/visitor tests; browser Cancel preserved 14 and confirmed clear left one count-bearing entry. |
| Both-provider fresh initialization and actual new-schema handlers | `ActivityLogDatabaseTests.FreshSchema_EnsureCreatedAndSeed_UsesCanonicalShapeAndRealHandlers`. |
| Legacy-only deletion, unrelated-data and identity preservation, rerun safety | `ActivityLogDatabaseTests.LegacyUpgrade_DiscardsOnlyOldHistory_RerunPreservesNewRowsIdentityAndUnrelatedData`. |
| Reject unsupported schemas before destructive work | `ActivityLogDatabaseTests.UnsupportedSchema_RejectsBeforeDeletingLegacyOrUnrelatedSentinels`, for unknown/partial/wrong-length shapes on both providers. |
| Node uses server summaries, repeated string keys and typed plain-text detail values | `JavaScript/ActivityLogTests.mjs`: `string event keys serialize repeatedly and server summary is used without reconstruction`; `structured detail loads supplied heading context and typed values as plain text`; retained cancellation/confirmation/failure cases. |

### Authorized Development Database Migration

- On 2026-11-08, the user reported a development startup failure and explicitly requested applying the migration to the configured Development database. The checkout was clean on `master` at `19e12ef03e954591c40adfdbb5caa3651568bfac`; its committed files include the canonical catalog and both activity upgrade scripts. This authorization covers the configured development target only, not production or another deployment.
- The maintenance helper resolved the same base JSON, Development override, and environment-variable configuration without printing or editing credentials. The launch profile has no database override. The PostgreSQL database had the exact supported nine-column legacy activity table and 198 activity rows; integer category IDs and snake_case prerequisites were complete. No application process or other database client was active.
- Created a complete PostgreSQL custom-format database backup with the already installed local PostgreSQL 18 client image. The 1,601,209-byte archive was actually restored into a temporary local container with networking disabled and no exposed ports. All 19 restored tables matched source row counts and full-row SHA-256 fingerprints. The published activity script then passed rehearsal, preserving all 18 unrelated tables and producing the ten-column target. The isolated container was removed.
- Applied the unchanged `database\PostgreSql\activity-log-events.sql` to the authorized Development database, then executed it again successfully. The one-time cutover removed the 198 legacy activity rows. All 18 other tables retained their exact row counts and full-row fingerprints; the activity table, primary key, identity sequence object, and sequence state were preserved. The second execution made no further data changes.
- The actual PostgreSQL EF activity-list handler and column reads for every application entity succeeded without starting the Web host or background workers. A fresh connection confirmed the persisted ten-column canonical shape, required UTC time, and zero activity rows. Application startup itself remains for the user to rerun; no unrelated application behavior was changed.
- The restore-tested whole-database backup is retained outside the repository and session cleanup in the user's local application-data `Moonglade\DatabaseBackups` directory as `moonglade-before-activity-events-20261108T041110521Z.dump`. Its durable copy was verified with SHA-256. Do not delete it as scratch. Restoring it would also restore the pre-upgrade activity schema and require the pre-cutover application; a whole-database restore after new writes would overwrite those later writes.
- The maintenance helper and duplicate session backup were removed after verifying the durable backup. Only this retained tracker changed in the repository; no application code, credentials, automatic migration runner, production database, Git commit, or deployment was changed by this maintenance operation.

## B3 Evidence

- 2026-11-08, Asia/Taipei. The user accepted B2 and authorized B3 from a clean `master` at `a566629054d24ffa2032e649c87d5a7d363ce1ff`, the B3 application rollback point. The accepted B0 nine-group, 51-field projection and configuration ownership decisions remain unchanged.
- Full and partial ordinary saves now return a safe `ConfigurationSaveResult` from the shared configuration write gate. The effective previous/saved comparison covers excluded values too and uses structural menu/submenu equality. Approved scalar differences alone leave Configuration. Persisted-JSON equality independently retains its existing timestamp, publication, and cache-event semantics, including equivalent legacy rewrites and stale runtime refreshes. A postcommit cache-event failure still throws rather than fabricating a successful result or audit.
- SettingsController consumes that committed result, suppresses no-op entries, and keeps restricted-only updates with an empty change envelope. Credentials remain separate named events, with existing sign-out and failure behavior. ActivityLog validates exact, unique, per-event field changes and typed old/new primitives before serialization and on reads; old B2 scalar details remain readable.
- The metadata API keeps `fields` and adds nullable `changes` with localized labels and typed old/new values. Razor/Alpine render a before/after table as text, localize booleans and enum choices, and distinguish a real update with no permitted display values (`[]`) from legacy/non-settings details (`null`). All four locale resources parse without duplicate keys and cover all new UI keys and approved detail labels.
- Final verification passed Configuration 310, Web 607, Node 31, and the focused HTTP subset 63, with zero failures/skips; the HTTP 63 are included in Web 607, not additional unique tests. The first complete Web run passed 602 and failed five localized-label cases: `EnableCustomCss` used a lookup differing in case from the existing `Enable Custom CSS` resource key. That production lookup is corrected; the affected HTTP class and full Web rerun passed unchanged assertions in all five cultures. All 51 approved labels plus 12 new keys pass exact-case checks against all four resources, and the final Web build has zero warnings/errors.
- A disposable loopback host used compiled production Web/Razor/assets, the actual Configuration and ActivityLog handlers, the production configuration cache-event handler, one synthetic administrator, nine synthetic configuration rows, and in-memory SQLite. No real database, account, email transport, hosted worker, or startup initializer was used, and no appsettings file was copied into its output.
- Real antiforgery-protected PATCH and POST saves returned HTTP 204. Starting with one synthetic legacy row, a mixed General save produced four approved changes and raised history to two; repeating the PATCH kept two. A restricted-only Description change produced a third row with `changes: []`; a full Feed save produced a fourth row with exact integer and boolean differences, and repeating that full save kept four. API details contained neither excluded field names nor synthetic excluded values.
- English and zh-Hans browser detail checks showed committed old/new values, localized boolean/sidebar choices, literal `<b>B3</b>` text without a corresponding HTML element, the empty-values explanation despite visible request context, and legacy scalar fields without a new change table. Switching between details removed prior change rows. The isolated host omitted the generated scoped-CSS static-assets manifest (one CSS 404), and Bootstrap emitted its existing transient modal-focus warning on closing; no activity-script exception was observed.
- README, AGENTS, and the engineering reference now document the save-result, projection, no-op, restricted-only, and compatible detail contracts. No entity, database mapping, manual migration script, or deployment behavior changed in B3; no further Development migration is needed or was performed.
- Focused quality review covered 32 new C# test methods and three Node subtests, with no assertion-free or trivial-only tests. Change-survival review was static, not empirical mutation testing. Broader exact-case inspection also identified existing non-settings detail lookups (`Widget type`, `Accent color`, `File name`, `Content type`) differing from resource-key casing; those unchanged B2 labels are outside the B3 settings scope and remain for integrated review rather than an unrelated refactor.
- Final commands ran from the repository root and exited zero. Configuration and Node results were retained after the label-only correction because neither their production dependencies nor test inputs changed:

```powershell
dotnet test --project src\Tests\Moonglade.Configuration.Tests\Moonglade.Configuration.Tests.csproj
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-class '*ActivityLogHttpTests'
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet
git diff --check
```

- The browser pages and attached loopback host were closed, its listener was verified stopped, and the specific preview/test-pipeline scratch was removed after retaining evidence here. Changed-file CRLF/final-newline conventions, exact-case resource keys, synchronized documentation links, and final working-tree scope were checked. At its initial handoff B3 awaited acceptance without an agent commit. The user subsequently committed and accepted B3, as recorded in Progress, and authorized B4.

### B3 Requirement Evidence

Method names below are in the owning Configuration or Web test project; Node names are in `Moonglade.Web.Tests/JavaScript/ActivityLogTests.mjs`.

| Requirement | Evidence |
| --- | --- |
| Every ordinary field, exact 51-field B0 allowlist, full/partial projections | `UpdateConfigurationCommandTests.Save_AllOrdinaryFields_ProjectsOnlyExactB0AllowlistedChanges`. |
| Nine-group no-op suppression, runtime refresh, unchanged timestamp/cache | `UnchangedSave_AllNineGroups_ReturnsNoEffectiveChangesAndRefreshesRoot`; `SettingsControllerTests.NoOp_FullAndPatchAllGroups_ReturnNoContentWithoutAuditOrCache`; browser repeated full/PATCH saves. |
| Restricted-only empty changes and mixed safe-only values | `MixedSave_AllGroups_ProjectOnlyChangedSafeValues`; `RestrictedOnly_FullAndPatch_WritesEmptyChangesWithoutRestrictedNamesOrValues`; `MixedSave_PersistsOnlySafeChangedFieldWithoutProtectedValues`; browser mixed/restricted-only General updates. |
| Effective comparison remains separate from persisted JSON | `RawUnchangedButEffectiveDifferent_UsesCapturedCurrentValuesWithoutCacheEvent`; `LegacyEquivalentJson_NormalizesRawStorageAndCacheWithoutEffectiveAudit`; `LegacyNormalizationHttp_RefreshesRawTimestampAndActualCacheWithoutActivity`. |
| Structural nested menu equality and null/empty distinctions without graph disclosure | `NestedMenus_CompareStructurallyAndExposeOnlyEnableFlag`; `DisabledMenu_NullAndEmptyPartitionsAreComparedWithoutExposingGraph`. |
| Save-specific gated old/new values during overlapping writes | `OverlappingSameFieldSaves_ReturnTheirActualGatedBeforeAndAfter`; `OverlappingDifferentGroupPatches_ReturnOwnValuesAndRetainLatestOtherGroup`; strengthened `QueuedPatchesToSameGroup_PreserveBothFieldChanges`. |
| Immutable captures and completion ordering | `Save_UpdatesDatabaseAndRuntimeFromRetainedImmutableValue`; `Save_ResultWaitsForRuntimeAndCacheCompletion`. |
| Approved scalar boundaries and precommit rejection preserve state | `ApprovedScalarBoundaries_PreserveValuesAndRuntimeWithoutReportDataInNames`; `Patch_InvalidShapeOrField_RejectsWithoutExposingSubmittedValue`; `Patch_RejectedBeforeCommit_PreservesRootRowsTimestampsAndCache`. |
| Postcommit cache failures remain visible without fabricated audit success | `CacheFailureAfterCommit_ThrowsInsteadOfReturningSuccessAndKeepsSavedState`; `PatchCacheFailure_PropagatesCommittedFailureWithoutFabricatingAudit`. |
| Audit failure/cancellation preserves committed save and actual cache behavior | `AuditDispatchFailure_PreservesActualFeedAndSitemapInvalidation`; retained `SettingsSave_WhenAuditFails_PreservesCommittedValuesAndReturnsSuccess`. |
| Failed audit inserts cannot resurrect history on a later save | `AuditInsertFailure_KeepsCommittedSettingsTimestampAndCacheWithoutResurrection`. |
| Activity boundary independently pins safe keys/types and rejects malformed envelopes | `SettingChanges_All51B0Fields_AreExactEventAllowlistAndPersistTypedValues`; `SettingChanges_InvalidWriterInput_RejectsBeforePersistenceAndLogsNoProtectedContents`; `SettingChanges_InvalidStoredEnvelope_ThrowsSafeFailureBeforeReadExposure`. |
| Legacy Fields versus new empty Changes remain distinguishable | `LegacyB2ScalarSettingsDetails_RemainReadableWithoutInventedBeforeValues`; `RestrictedOnlyHttp_HasEmptyChangesWhileLegacyAndNonSettingsKeepFields`; browser legacy/new detail switching. |
| Empty HTTP 204 responses, administrator authorization, valid/missing/invalid CSRF | `SettingsHttp_FullAndPatch_Return204AndExactSafeChangeThenSuppressNoOp`; `SettingsHttp_UnauthorizedOrMissingCsrf_PreservesStateWithoutAudit`; browser real antiforgery-protected full/PATCH saves. |
| All 51 labels localize and typed values render as text | `SettingDetails_All51LabelsAreLocalizedAndOldNewValuesRemainTypedPlainText`; Node `B3 settings changes retain typed old and new values as plain text` and `all six settings enum discriminators localize every approved choice without rewriting ordinary strings`; English/zh-Hans browser checks. |
| Named account events remain safe and preserve sign-out policy | `AccountNamedEvents_RealWriterStoresNoProtectedValuesAndPreservesSignOutPolicy`; retained password/authenticator audit-failure tests. |

## B4 Evidence

- 2026-11-08, Asia/Taipei. The user accepted B3 and requested B4. Started with a clean `master` at `7ed70bc12460676fcb8ec01ff8c605f06bf9d8f8`; that commit remains the application rollback point. No agent commit, push, real database operation, migration, deployment, or B5 work occurred.
- Implemented the B0-approved new `post.automatically-published` event, bringing the canonical catalog to 50. Its only allowed detail is `PostId`. Added the exact automatic-publication label in all four localization resources; System activity retains no administrator or request context.
- Features still owns selection and database persistence and still returns the EF affected-entry `int`. The original due predicate, separate cutoff/execution UTC samples, publication date, route generation, schedule clearing, and LastModified behavior remain unchanged. A positive successful save captures a read-only detached batch of post ID, title, and actual publication UTC.
- The existing `PostChangedEvent` cache attempt remains first. A `finally` handoff attempts `ScheduledPostsPublishedEvent` even when that cache operation fails, preserving its original exception and worker error behavior. Both postcommit notifications use `CancellationToken.None`. A failed handoff is safely logged without undoing publication or changing the successful return.
- The discovered Web subscriber awaits an independent System activity command for each captured post, continues after individual dispatch/write failures, and performs no publication, cache, or outbound notification work. The activity command accepts optional occurrence UTC; existing callers still default to current UTC, and the shared UTC guard rejects local/unspecified values without resurrecting failed inserts.
- Delivery is best-effort in-process, not durable or globally exactly-once. A later normal scheduler pass excludes already-published posts and therefore emits no repeat history. No queue, retry, cross-host coordination, uniqueness constraint, new schema, migration script, or provider-specific behavior was introduced.
- Dedicated automated verification added 38 cases across 20 methods. Features 132, BackgroundServices 15, and Web 630 passed: 777 total, zero failures/skips, all final commands exit zero. Focused Features 15, worker 7, and Web 234 are subsets, not additional independent cases. The final parent Web build has zero warnings/errors. BackgroundServices test compilation retains three pre-existing CS8602 warnings in unchanged `CannonServiceTests.cs` at lines 54, 154, and 192; new B4 tests are warning-free.
- Assertion-quality and static change-survival review strengthened the actual pre-query cancellation, supplied-UTC writer cancellation, exact single-post row count, and audit-only dispatch assertions. No assertions were weakened, skipped, or removed to fit production. No production defect was found. No empirical mutation score or measured coverage percentage is claimed.
- The browser gate used an isolated loopback host with compiled production Web/Razor/assets, actual Features/ActivityLog handlers, actual cache invalidators and scheduled-event subscriber, validated synthetic configuration, synthetic administrator authentication, and in-memory SQLite. No application startup initializer, hosted worker, real account, appsettings copy, real database, email, or external integration was used.
- Eight synthetic posts included three due posts (before and exactly at cutoff) and five unchanged future/null-schedule/draft/deleted/already-published sentinels. The actual bus command returned 3, committed all three publications at `2026-11-08T04:55:02Z`, generated their expected `2026/11/8` routes, cleared schedules, preserved LastModified, advanced post/feed/sitemap generations to 1, and wrote exactly three System entries at that same occurrence UTC.
- The fixture subsequently renamed and soft-deleted one published post; its captured literal `<b>Due alpha</b>` activity target remained unchanged. Another actual scheduler pass returned 0 with identical post state, activity IDs/count, and cache generations. Automatic/manual/exact-inclusive-time/before-occurrence API filters returned 3/0/3/0; the catalog contained exactly one new key.
- All three actual English detail modals identified their own post ID, captured title, and publication time without stale target data. zh-Hans list/filter/detail/System/Post ID labels agreed. The literal markup stayed plain text, with PostId-only fields and no request context, post body, or renamed title in captured history. Scoped CSS loaded; missing synthetic favicons and the existing transient Bootstrap close-focus warning were fixture/UI limits, with no activity-script exception. Failure isolation is proved by automated fault injection, not a browser-induced database failure.
- README, AGENTS, the engineering reference, and the test README are synchronized. The previously recorded non-settings label casing issues remain unchanged for B5 integrated review. B4 requires no further Development migration; the separately retained restore-tested B2 backup remains untouched.
- The task-owned browser pages were closed and all three disposable preview ports were verified stopped. Preview and test-pipeline scratch were removed after retaining evidence here. Final changed-file UTF-8/CRLF/final-newline, exact resource-key uniqueness, documentation-link, diff-scope, `git diff --check`, branch, HEAD, and status checks passed. At its initial handoff B4 awaited acceptance without an agent commit. The user subsequently committed and accepted B4, as recorded in Progress, and authorized B5.

### B4 Verification Commands

All commands ran from the repository root with the configured Microsoft.Testing.Platform runner. Final owning-suite results were retained after the pipeline's last test correction; production and tests did not change afterward.

```powershell
dotnet test --project src\Tests\Moonglade.Features.Tests\Moonglade.Features.Tests.csproj --filter-class '*PublishScheduledPostCommandTests'
dotnet test --project src\Tests\Moonglade.BackgroundServices.Tests\Moonglade.BackgroundServices.Tests.csproj --filter-class '*ScheduledPublishServiceTests'
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-class '*ScheduledPostsPublishedEventHandlerTests' '*ActivityLogModelTests' '*PostCacheInvalidationTests' '*ScheduledPublicationRegistrationTests' '*ActivityLogHttpTests'
dotnet test --project src\Tests\Moonglade.Features.Tests\Moonglade.Features.Tests.csproj
dotnet test --project src\Tests\Moonglade.BackgroundServices.Tests\Moonglade.BackgroundServices.Tests.csproj
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet
git diff --check
```

### B4 Requirement Evidence

Exact method names below are in the owning Features, BackgroundServices, or Web test project. Real SQLite/cache outcomes and browser checks are separate from direct dispatcher assertions.

| Requirement | Evidence |
| --- | --- |
| Original due/ineligible boundaries, distinct execution UTC, rollover routes, LastModified, affected-entry int, repeat suppression | `HandleAsync_MixedEligibility_UsesCutoffAndExecutionTimeAndDoesNotRepeat`; strengthened `HandleAsync_DueScheduledPost_PublishesPostAndGeneratesRouteLink`. |
| Empty, failed, cancelled and zero-row saves never notify or persist publication | `HandleAsync_EmptySelection_DoesNotSaveOrNotify`; `HandleAsync_UnsuccessfulSave_DoesNotNotifyOrPersistPublication`; `HandleAsync_ZeroRowSave_DoesNotNotify`; `ScheduledPublication_UncommittedSqlitePass_LeavesPostCachesAndActivityUntouched`. |
| Noncancelable postcommit notifications and detached read-only snapshots | `HandleAsync_PostCommitCancellation_UsesNoncancelableNotifications`; `HandleAsync_CacheSubscriberMutatesPosts_HandsOffDetachedReadOnlySnapshot`. |
| Cache failure still audits; handoff failure preserves successful return and original cache exception | `HandleAsync_NotificationFailures_PreservePublicationAndOriginalCacheException`; `ScheduledPublication_RealCacheFailsAfterCommit_StillPersistsAuditAndRethrowsOriginalFailure`. |
| Per-post System-only commands, canonical key, captured target/time, safe details and diagnostics, sequential failure/cancellation continuation | `HandleAsync_Batch_EmitsOrderedIndependentSystemCommandsWithPublicationSnapshots`; `HandleAsync_PendingWrite_AwaitsItBeforeDispatchingNextPost`; `HandleAsync_OneDispatchFailsOrCancels_ContinuesWithSafeDiagnostics`. |
| Supplied/default UTC, non-UTC rejection, failed-entity detachment and no resurrection | `Writer_PublicationOccurrence_PreservesSuppliedUtcOrDefaultsToCurrentUtc`; `Writer_NonUtcSuppliedOccurrence_RejectsSafelyAndDoesNotResurrectOnLaterSave`; `Writer_SuppliedUtcWriteFailsOrCancels_DetachesActivityAndLogsOnlySafeFailureType`. |
| Real publication, actual cache/feed/sitemap outcomes and repeat isolation, independently from audits | `ScheduledPublication_RealBusMixedBatch_PreservesPublicationCachesAndExactlyOneAuditPerPost`; browser three-post pass and unchanged repeat. |
| Failed activity insert leaves publication/cache committed, continues the batch and never resurrects on a later save | `ScheduledPublication_OneSqliteActivityInsertFails_ContinuesAndNeverResurrectsFailedEntry`. |
| Detached rename/delete targets, five-culture filter/list/detail identity and occurrence, plain text | `AutomaticPublication_DetachedRenameDeleteSnapshots_KeepLocalizedFilterListDetailIdentityAndOccurrence`; actual English three-detail and zh-Hans browser checks. |
| Catalog 50 and exact localized identities preserve previous events | `Catalog_All50ActiveEvents_HaveExactUniqueKeysGroupsAndLabels`; `Catalog_All50Events_LocalizedFiltersSummaryAndHeadingShareIdentity`. |
| Actual Web LiteBus composition discovers the subscriber | `WebServices_ActualLiteBusDiscovery_DispatchesScheduledPublicationToSystemWriter`. |
| Worker int/failure/shutdown policies and existing wake/toggle behavior | `Worker_RealScheduledCommand_RetainsIntResultAndPostCommitFailurePoliciesAndStopsPromptly`; retained `DisableWhileWaiting_SkipsPublicationAndRemainsRunning`, `EnableWhileDisabled_WakesAndPublishesDueWorkWithoutFiveMinuteDelay`, `DisableAfterDispatch_DoesNotCancelPublicationAlreadyRunning`. |

## B5 Evidence

- 2026-11-08, Asia/Taipei. The user accepted B4 and requested integrated acceptance. Started with clean `master` at `66953e371660dec6461b33b7abe911e76714dcb9`, the observed user B4 commit and application rollback point. No agent commit, push, deployment, or real database operation is authorized or was performed.
- Corrected the four recorded non-settings detail lookups to their existing exact resource keys: `Widget Type`, `Accent Color`, `File Name`, and `Content Type`. The event catalog stays at 50 and field/storage contracts are unchanged. A five-culture actual writer/HTTP regression now checks all 26 non-settings detail keys, typed values, and plain-text Razor bindings.
- Added a two-provider complete-installation legacy activity upgrade/rerun witness using the actual `StartUpInitializer`. It preserves all 18 unrelated mapped tables, eleven configuration JSON/timestamp rows, runtime settings, account and installation manifest state, activity identity continuity, and category identity generation without repair warnings.
- Initial full-solution verification passed 1,944 .NET cases in all fifteen projects, with zero failures/skips and fifteen validated native xUnit XML reports. The opt-in actually executed PostgreSQL 45 and SQL Server 40 cases; these 85 provider cases are included in Setup 162 and the solution total. Initial Node verification passed 31 cases. Focused Web HTTP 73 and activity-provider 12 are subsets, not additional unique cases.
- The provider groups comprise ActivityLog 6/6, StartupInstallation 16/16, CategoryIdentity 18/17, and SnakeCase 5/1 (PostgreSQL/SQL Server). SQL Server's naming case verifies its companion no-op. Category/naming prerequisites are separate witnesses, not a single arbitrary historical upgrade chain. Tests used cached `postgres:18-alpine` and SQL Server 2022 images, uniquely named fixture databases/containers, and loopback ports; all fixture containers were removed.
- Reviewed README, the engineering reference, `database/README.md`, and the activity upgrade/rollback guide. The published order remains stop every writer, restore-test a whole-database backup, category IDs if needed, PostgreSQL naming if needed, then the complete activity script before the matching new host. Fresh setup requires no manual activity script; reruns preserve new history. Rolling back binaries alone is incompatible after cutover; an authorized whole-database restore with old binaries can discard unrelated later writes. The separately retained B2 Development backup remains untouched. No script or schema change is needed for B3-B5.
- Integrated browser checks used the actual Web service composition, request pipeline, endpoints, compiled Razor and modules, but replaced authentication with synthetic role claims and the database with request-local connections to isolated shared-memory SQLite. Configuration sources were cleared and replaced with synthetic values; ephemeral data protection was used and all business hosted services were removed. No startup initializer, developer settings file, real account, SMTP delivery, external integration, or actual database was used.
- The verified browser fixture initially held eight posts and empty history. Administrator/anonymous/non-administrator API and page checks produced the intended 200/401/403 outcomes; missing CSRF, obsolete/unknown filters, and reversed date ranges returned 400, and missing detail returned 404. Rejected requests left state unchanged.
- Seeded twelve canonical entries, then the actual scheduler published three due posts at `2026-11-08T05:30:02Z`, including exact-cutoff eligibility. It wrote three independent System entries, preserved LastModified and five ineligible rows, generated the expected `2026/11/8` routes, and advanced post/feed/sitemap generations to 1. Captured targets survived a subsequent synthetic rename/soft deletion. A repeat command returned 0 with identical posts, activity IDs/count and cache keys.
- Actual activity-module pagination produced 10/5 nonoverlapping rows. Its event/date controls selected exactly three automatic entries; all three actual English details identified their own post/occurrence and rendered literal markup as text. Five-culture APIs shared all 50 filter/heading identities and localized the corrected field labels. Standard culture-cookie selection made actual zh-Hans list/detail requests agree; widget/theme/avatar/System/safe-change/restricted-only details were checked.
- The actual General settings change modal saved `<b>B5</b>` as SiteTitle, produced one exact safe old/new change, and advanced only feed/sitemap generations. Repeating that PATCH returned empty HTTP 204 with no audit or cache/state changes. The actual Description change modal produced a restricted-only entry with `changes: []`, no restricted field/value disclosure, and no stale old/new rows when switching details.
- Two anonymous submissions used the actual public comment controller, guard, domain save and database outbox. Review-enabled behavior intentionally retained the original text as pending review; disabling review through the actual settings PATCH allowed masking, yielding an approved masked comment. Two outbox rows were enqueued without external delivery, and neither visitor submission produced activity. Only the administrator review-setting change added history.
- Actual clear cancellation preserved all eighteen rows. A synthetic SQLite delete trigger caused the actual UI request to fail, with unchanged history/business state and a visible danger toast. Successful clear retained one Administrator record with DeletedCount 18. A synthetic post-clear insert failure retained the committed empty history and success notice; clearing empty history then retained one record with DeletedCount 0. Posts, configuration, comments, outbox and cache keys were unchanged throughout. These database fault observations are separate from ordinary browser success and from automated test counts.
- Browser review also found that the activity clear confirmation inherited hardcoded English `Confirm Delete`/`Delete` from the generic modal helper. The activity page/module now uses the existing localized `Clear all` resource for both title and action through the existing `showConfirmModal`; danger styling/icon, confirmation, callback/error/finally ordering and the generic shared helper remain unchanged.
- After that UI-only correction, the affected Web 635 and Node 32 suites passed with zero failures/skips/cancelled/todo cases. The refreshed Web XML accounted for all 635 results and all 73 HTTP cases. The unchanged non-Web 1,309 results and 85 provider cases were retained from the original full run, not needlessly rerun or double-counted. Latest combined evidence is 1,944 .NET plus 32 Node, not a claim of a second full-solution run. The final parent Web build has zero warnings/errors.
- Repeated the affected actual UI on a fresh isolated fixture after the final build. The modal title and action both displayed the zh-Hans translation of `Clear all`, with localized confirmation body/cancel, danger class and trash icon, and no request before confirmation. Cancellation and a delete fault preserved all twelve new synthetic rows; the fault showed a visible danger toast. Success recorded DeletedCount 12, a failed post-clear insert left committed empty history with a success notice, and an empty clear recorded DeletedCount 0. Business state and cache keys remained unchanged. This repeat closes the UI correction rather than substituting older browser evidence.
- Seven .NET cases across two new methods and one Node subtest were added. Bounded assertion-quality and static test-gap review found no weakened or assertion-free/trivial-only cases; there is no empirical mutation score or measured coverage claim. Successful final commands emitted no warnings; previously recorded unchanged BackgroundServices CS8602 warnings were not fixed, and an incremental run is not a warning-free clean rebuild of every unchanged source.
- All task-owned pages and attached hosts were closed, the preview port was verified stopped, and Docker inventory was empty. Specific preview/pipeline scratch and native reports were removed only after preserving their commands, counts, validated outcomes and exact requirement evidence here. Final UTF-8/CRLF/final-newline, resource identity, local documentation-link, `git diff --check`, scope, branch, HEAD and status checks passed. Only focused activity localization, owning regression tests and tracker/test documentation changed.
- Upgrade instructions required no change; the README/AGENTS/engineering/database guides still describe the approved contracts and order. Remaining design limits are deliberate: best-effort in-process activity delivery, no durable retry or global exactly-once guarantee, only the documented standard legacy schema, and no authorization to deploy or restore a real database. B5 is **Awaiting acceptance**, not Accepted or committed; the next action is user integrated sign-off.

### B5 Verification Commands

Commands ran from the repository root. The provider opt-in was set in each process requiring it. Native xUnit export was already available; no reporter dependency, coverage tool, or persistent runner configuration was added. Reports were parsed and reconciled before scratch cleanup.

```powershell
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-class '*ActivityLogHttpTests'
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --project src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj --filter-class '*ActivityLogDatabaseTests'
dotnet test --solution src\Moonglade.slnx --report-xunit-xml --results-directory C:\Users\Edi\.copilot\session-state\a368261e-ed07-496f-9970-8268fbd4ddb8\files\b5-tests\results --results-directory-layout per-module
dotnet test --project src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --report-xunit-xml --results-directory C:\Users\Edi\.copilot\session-state\a368261e-ed07-496f-9970-8268fbd4ddb8\files\b5-tests\results-followup --results-directory-layout per-module
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
dotnet build src\Moonglade.Web\Moonglade.Web.csproj --no-restore --verbosity quiet
git diff --check
```

| Project | Final passed |
| --- | ---: |
| Auth | 63 |
| BackgroundServices | 15 |
| Configuration | 310 |
| Email | 107 |
| Features | 132 |
| ImageStorage | 38 |
| IndexNow.Client | 9 |
| Moderation | 13 |
| Setup | 162 |
| Syndication | 16 |
| Theme | 67 |
| Utils | 282 |
| Web.Middleware | 26 |
| Web | 635 |
| Webmention | 69 |
| **Total .NET** | **1,944** |
| **Node** | **32** |

All fifteen .NET projects had nonzero discovery and zero final failures/skips. Fifteen original XML files and one refreshed Web XML were valid, current-run and error-free; individual Pass records matched module counters and totals. Provider outcomes were counted from those actual records. Focused runs and browser scenarios are not added to these totals.

### B5 Requirement Evidence

Method names below are in the owning test projects. Browser observations are identified separately; actual startup/provider witnesses are not replaced by InMemory, generated DDL, Razor TestServer or Node checks.

| Requirement | Evidence |
| --- | --- |
| Fresh setup and actual restarts on both providers | `FreshInstallationAndRestarts_AreIdempotent`; `FreshSchema_EnsureCreatedAndSeed_UsesCanonicalShapeAndRealHandlers`. |
| Standard activity cutover/rerun and actual startup of a complete upgraded installation | `LegacyUpgrade_DiscardsOnlyOldHistory_RerunPreservesNewRowsIdentityAndUnrelatedData`; new `CompleteLegacyInstallation_UpgradeAndRerun_StartUpPreservesSettingsContentAndIdentity`, both providers, including all 18 unrelated tables and configuration/account/manifest/identity preservation. |
| Reject unsupported activity schemas before deletion | `UnsupportedSchema_RejectsBeforeDeletingLegacyOrUnrelatedSentinels`, unknown/partial/wrong-length cases on both providers. |
| Category/naming prerequisites and rollback guards | `LegacyMigration_PreservesContentLinksKeysAndSequenceAcrossEditsDeletesAndReruns`; `LegacyMigration_InvalidPreconditions_RollsBackSchemaContentAndLinks`; `Migration_PreservesAllRowsDependenciesAndIdentity_AndCanRunTwice`; `Migration_InvalidSchema_RollsBackEveryRename`. Separate prerequisite witnesses, not an arbitrary historical-script chain. |
| Actual full/PATCH settings, no-op suppression and gated effective changes | `Save_AllOrdinaryFields_ProjectsOnlyExactB0AllowlistedChanges`; `NoOp_FullAndPatchAllGroups_ReturnNoContentWithoutAuditOrCache`; `SettingsHttp_FullAndPatch_Return204AndExactSafeChangeThenSuppressNoOp`; `OverlappingSameFieldSaves_ReturnTheirActualGatedBeforeAndAfter`; actual General change-modal save/repeat. |
| Restricted-only/protected-safe details and post-save failure isolation | `RestrictedOnly_FullAndPatch_WritesEmptyChangesWithoutRestrictedNamesOrValues`; `SettingChanges_InvalidWriterInput_RejectsBeforePersistenceAndLogsNoProtectedContents`; `AuditInsertFailure_KeepsCommittedSettingsTimestampAndCacheWithoutResurrection`; `PatchCacheFailure_PropagatesCommittedFailureWithoutFabricatingAudit`; actual restricted-only modal/detail switching. |
| Per-post System publishing, publication/cache state and repeat isolation | `HandleAsync_MixedEligibility_UsesCutoffAndExecutionTimeAndDoesNotRepeat`; `ScheduledPublication_RealBusMixedBatch_PreservesPublicationCachesAndExactlyOneAuditPerPost`; actual three-post pass/five exclusions and unchanged repeat. |
| Publication cache/audit failures do not undo committed publication | `ScheduledPublication_OneSqliteActivityInsertFails_ContinuesAndNeverResurrectsFailedEntry`; `ScheduledPublication_RealCacheFailsAfterCommit_StillPersistsAuditAndRethrowsOriginalFailure`; `HandleAsync_NotificationFailures_PreservePublicationAndOriginalCacheException`. |
| Detached System targets, actual bus and worker discovery | `AutomaticPublication_DetachedRenameDeleteSnapshots_KeepLocalizedFilterListDetailIdentityAndOccurrence`; `WebServices_ActualLiteBusDiscovery_DispatchesScheduledPublicationToSystemWriter`; `Worker_RealScheduledCommand_RetainsIntResultAndPostCommitFailurePoliciesAndStopsPromptly`; English/zh-Hans actual details after synthetic rename/delete. |
| Visitor exclusion preserves moderation, review, persistence and outbox | `Create_WithRealPersistence_PreservesModerationReviewAndOutboxWithoutAudit`; `Create_WhenMaskFilterEnabled_DispatchesMaskedPayloadWithoutVisitorAudit`; actual anonymous reviewed/masked submissions and two queued outbox rows with no visitor activity. |
| Catalog50, inclusive date/key filters, pagination and rejected inputs | `Catalog_All50ActiveEvents_HaveExactUniqueKeysGroupsAndLabels`; `List_TimeThenIdDescending_PaginatesInclusiveDatesAndRepeatedKeyFilters`; `List_RepeatedKeysAndInclusiveUtcDates_ReturnExactCapturedRows`; `List_InvalidOrObsoleteFilters_Return400ValidationProblemWithoutMutation`; actual 10/5 pagination and three-row UI filter. |
| Readable empty/missing details, all localized labels and plain text | `Metadata_ExistingEmptyRowIs200_MissingIs404_WithReadableContextAndTypedFields`; new `NonSettingsDetails_AllowedLabels_LocalizeFromWriterAndRemainTypedPlainText`; `SettingDetails_All51LabelsAreLocalizedAndOldNewValuesRemainTypedPlainText`; Node `structured detail loads supplied heading context and typed values as plain text`; actual English/zh-Hans detail switching. |
| Administrator access and antiforgery | `ReadEndpoints_AnonymousAndNonAdministrators_AreDenied`; `Clear_AnonymousOrNonAdministrator_IsDeniedWithoutMutation`; `Clear_AdminWithoutValidAntiforgery_IsDeniedWithoutMutation`; `SettingsHttp_UnauthorizedOrMissingCsrf_PreservesStateWithoutAudit`; actual rejected browser requests left state unchanged. |
| Localized clear confirmation with no early request and unchanged danger presentation | Node `localized clear confirmation uses page title action and body with unchanged danger presentation`; five-culture compiled `data-clear-all` checks in `NonSettingsDetails_AllowedLabels_LocalizeFromWriterAndRemainTypedPlainText`; repeated actual zh-Hans title/action/body/danger UI. |
| Successful/empty/cancelled clear and awaited ordering | `Clear_WithRealHandlers_ReplacesHistoryWithCountedAdministratorAudit`; `Clear_AdminWithPageToken_ClearsHistoryAndReturnsCanonicalReadShape`; Node `cancelled clear keeps history and pagination without clear or refresh` and `confirmed clear waits for delete then reloads page one and reports success`; actual cancellation and DeletedCount 18/12/0 observations. |
| Delete failure and failed post-clear audit preserve the correct outcome | `Clear_WhenDatabaseDeleteFails_PreservesRowsAndDoesNotAudit`; `Clear_WhenAuditDispatchFails_PreservesCommittedDeleteAndLogsSafely`; `Clear_WhenAuditPersistenceFails_DoesNotResurrectOnNextSave`; Node `failed clear preserves history and page and surfaces error without success`; actual browser-driven SQLite delete/insert faults with observed danger/success notices. |
| Upgrade/backup/rollback limits, build and isolation | Reviewed unchanged README/AGENTS/engineering/database/upgrade guides; final Web build zero warnings/errors; no real DB, deployment, outbound integration or agent commit; stopped owned host/pages and removed only owned test scratch/containers after evidence retention. |
