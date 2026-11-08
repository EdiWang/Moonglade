# Activity Log Redesign Plan And Tracker

## Resume Here

- Planning snapshot: 2026-11-08, Asia/Taipei. Repository baseline was clean at `4140154d4e4c54b37b057a9e9d717306fdeefca1` on `master`. Recheck the current branch, HEAD, and `git status --short` before starting. Preserve other work.
- The user accepted B1 on 2026-11-08 and requested B2 implementation. The current B1 application commit is `3226ff207417923e1894ab7dbdb2fb67b57f871b`. B2 implementation and required verification are complete and awaiting user acceptance; the user subsequently committed the cutover as `19e12ef03e954591c40adfdbb5caa3651568bfac`. After reporting a startup failure, the user explicitly authorized the configured Development database migration on 2026-11-08; that PostgreSQL upgrade is now applied and verified, with a retained whole-database backup. No production upgrade, deployment, B3, or B4 work is authorized. See the development migration record below.
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
| B2 | Awaiting acceptance | Web 461, Setup 160 with both-provider opt-in, activity provider subset 10, Node 28; zero failures/skips. Web build clean; disposable browser complete. User-authorized Development PostgreSQL migration applied and verified. | `19e12ef03e954591c40adfdbb5caa3651568bfac` (observed user commit) | User can rerun the development application; wait for B2 acceptance. B3/B4 remain gated. |
| B3 | Not started | Pending | — | Wait for B2 acceptance and relevant B0 decisions. |
| B4 | Not started | Pending | — | Wait for B3 acceptance. |
| B5 | Not started | Pending | — | Wait for B3 and B4 acceptance. |

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
