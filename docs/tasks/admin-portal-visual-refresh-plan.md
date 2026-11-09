# Admin Portal Visual Refresh: Execution Plan and Progress

## Current state

- Last updated: 2026-11-09.
- Phase: B0 accepted. B1 is ready for user visual acceptance. The user explicitly approved retaining the known image-test failure as a B1 test exception rather than expanding this batch.
- This document is the task plan, decision record, progress tracker, and cold-start handoff. The user explicitly requested its retention, including after completion.
- No application code, configuration, dependencies, or blog data was changed by AI during B0. The final Web build passed with zero warnings/errors; Web tests passed 662/662 and Node tests passed 32/32, with no failed or skipped tests.
- Planning checkout: `master`, commit `6615117f9b93d7d9c31519ec109b1cc00f7e2662`. B0 execution checkout: `b2c47ecf94540bbe8b3d01918cd98180b0776f3c` (`Document admin portal visual refresh plan`). The worktree was clean at B0 start. Only this plan differs between those commits; application source is unchanged.
- Authorized: B1 shared-frame/reference-page changes and checks. The user also permits stopping/starting this repository's local development host and signing in with the README's default local account without repeated permission. Keep the existing blog read-only. Not authorized: B2, Git commits, branch changes, push, PR, or deployment.
- Next action: the user reviews B1's appearance. Keep the final image-test failure visible as an approved exception, not a passing suite. Do not commit or begin B2 without separate instructions.

## Goal and reference context

Improve the admin portal's visual consistency using the user's research data management screenshots as a reference for surface hierarchy and toolbar placement, not as a request to reproduce its branding or features.

The reference has a blue top bar, a white left navigation area, a light-gray workspace, and white content surfaces. Table pages use a large white panel. Card pages retain separate white cards. Page-level controls occupy a consistent area above the content.

The screenshots were supplied in the original conversation and are not repository assets. A fresh conversation must not depend on access to those attachments: the decisions and acceptance criteria below are authoritative. If a visual detail cannot be recovered from this record, ask the user rather than inventing it.

### Confirmed design decisions

| ID | Decision confirmed by the user on 2026-11-09 |
| --- | --- |
| D1 | Retain a light-gray page background and give the left navigation a distinct white surface in light mode. |
| D2 | Keep the page toolbar below the top bar and visible while only the main content scrolls. |
| D3 | Organize surfaces by content type: a shared white panel for lists/tables, independent cards for card grids, and larger business-group panels for settings. |
| D4 | Panels follow content height; do not stretch sparse lists or empty states to fill the remaining viewport. |
| D5 | Put primary actions and state switches on the toolbar's left; put counts, search, and filtering on its right. |
| D6 | Exclude both full-screen editors from this redesign. Preserve their focused layout and existing bottom actions. |
| D7 | Accept at 1440 x 900 and 1920 x 1080 CSS-pixel desktop viewports, in light/dark mode and all six existing UI languages, at 100% browser zoom only. During B0 the user explicitly removed zoom checks from the earlier scope. |
| D8 | Use 24px outer main-area padding and 24px panel padding; use 16px gaps between panels and from the toolbar to content. Preserve existing button sizes and allow toolbar wrapping. |
| D9 | Use 8px panel/card radii, 1px theme-aware borders, and no panel/card shadows. This applies only to in-scope admin surfaces, not excluded editors or public pages. |
| D10 | Keep the toolbar on the workspace background: light gray in light mode, with the corresponding dark-mode surface. Do not add a separate white toolbar card or retain the existing toolbar bottom divider. |
| D11 | Prefer one settings header flow: primary page actions on the left followed by classification tabs; allow natural wrapping when necessary. Ordinary settings tabs do not acquire a Save action. |
| D12 | Move Menu Save to the fixed top toolbar and remove the old bottom Save. Retain the existing explicit-save semantics and form association. |
| D13 | The existing development site is read-only for AI's UI checks. Browser writes require a separate local test instance and disposable database; confirm its actual setup before provisioning or writing fixtures. |

Retain the existing font, theme accent colors, and blue top-bar design. Preserve application behavior. A surface/layout refresh is not authorization to change save semantics, add features, redesign navigation information architecture, or introduce a frontend framework.

### Confirmed execution and ownership rules

1. Continue on the current branch; do not create or switch branches. It was `master` when this plan was written. If a later session finds a different branch, ask the user before assuming it is the intended execution branch.
2. Execute one batch at a time after explicit authorization.
3. AI implements the authorized batch, performs its checks, updates this tracker, and stops for user acceptance.
4. The user performs Git commits. AI must not commit, amend, stage unrelated work, push, create a PR, or merge without a separate explicit request.
5. After acceptance, record that acceptance separately from the commit. The user commits and then tells AI when to continue. Confirm the user's commit from Git history rather than inventing a SHA.
6. Do not begin the next batch simply because tests passed or the user committed. Wait for the user's continuation instruction.
7. During research or implementation, ask about unresolved design, scope, behavior, or architectural choices. Explain concrete alternatives, their effects, and a recommendation. Record the answer before proceeding with dependent work; do not decide for the user.
8. Retain this document. Do not add links to this temporary task record from permanent documentation.

## Start here in a new conversation

1. Read [AGENTS.md](../../AGENTS.md), this entire document, and applicable parts of the [engineering reference](../agent-reference.md), especially Frontend and Localization, Local Runtime, and the Change Impact Checklist. Before changing settings/menu markup, also read Configuration. Read the [test guide](../../src/Tests/README.md#run-tests).
2. From the repository root, run `git status --short --branch` and `git --no-pager log -8 --oneline`. Preserve user changes. Compare the checkout with the progress table, latest handoff, and user-reported commits.
3. Find the first unfinished gate, not merely the next batch number. Check implementation authorization, self-checks, user acceptance, commit confirmation, and permission to continue independently.
4. Read the affected Razor/CSS/module code and nearby tests again before editing. The file map below is a starting point, not permission to assume implementation details remain unchanged.
5. The local development app was running at `https://localhost:10210/admin` during analysis. Browser tools used a separate Chrome profile; the user signed in manually. Do not assume the server or authenticated browser session still exists.
6. Do not read credentials from local development configuration. The user authorizes local host stop/start and the README's default local login for this task; verify the process belongs to this checkout before stopping its specific PID. Do not copy credentials into records. If login fails or TOTP blocks access, ask rather than resetting/bypassing authentication. Port/configuration changes still require agreement.
7. Resume only the explicitly authorized batch. Before stopping, update the progress table and handoff even if a test failed, the user declined a choice, or the batch remains incomplete.

Suggested cold-start instruction:

> Read AGENTS.md and docs/tasks/admin-portal-visual-refresh-plan.md. Summarize the current gate and next authorized action. Preserve user changes. Do not implement, commit, or start a subsequent batch without the recorded authorization and current user instruction.

## Investigation findings and implementation map

Read-only inspection covered all primary navigation pages, nine settings tabs, article draft/scheduled/recycle-bin pages, the page recycle bin, account/about pages, and the blank article/page editors. These observations are not implementation test results.

| Surface | Relevant implementation | Observed inconsistency or constraint |
| --- | --- | --- |
| Shared frame | [_LayoutAdmin.cshtml](../../src/Moonglade.Web/Pages/Shared/_LayoutAdmin.cshtml), [_AsideAdmin.cshtml](../../src/Moonglade.Web/Pages/Shared/_AsideAdmin.cshtml), [admin.css](../../src/Moonglade.Web/wwwroot/css/admin.css) | Navigation has no independent background. The toolbar section already exists, but content-scroll behavior depends on a page flag. |
| Shared background | [base.css](../../src/Moonglade.Web/wwwroot/css/base.css) | The light-gray token is shared with public pages. Prefer admin-scoped changes rather than changing the public site's base styles. |
| Toolbars | [_PostManageHeader.cshtml](../../src/Moonglade.Web/Pages/Admin/_PostManageHeader.cshtml), [_PageManageHeader.cshtml](../../src/Moonglade.Web/Pages/Admin/_PageManageHeader.cshtml), [_SettingsHeader.cshtml](../../src/Moonglade.Web/Pages/Admin/Settings/_SettingsHeader.cshtml) | Shared headers coexist with per-page toolbars. Actions appear on different sides. A global 25px important margin overrides local spacing classes. |
| Lists | [Admin pages](../../src/Moonglade.Web/Pages/Admin) | Articles/comments/mentions use individual bordered cards with different gaps and shadows. Recycle bins use the same article-entry styling. |
| Tags | [Tags.cshtml](../../src/Moonglade.Web/Pages/Admin/Tags.cshtml) | Only this ordinary admin page opts into `ScrollAdminBody` at the analysis baseline. Its grouped tags already scroll separately from the toolbar. |
| Tables | [ActivityLogs.cshtml](../../src/Moonglade.Web/Pages/Admin/ActivityLogs.cshtml), [VerificationFiles.cshtml](../../src/Moonglade.Web/Pages/Admin/Settings/VerificationFiles.cshtml) | Tables lack a complete white outer panel; verification-file New is in the body rather than the common toolbar area. |
| Card grids | [BlogPage.cshtml](../../src/Moonglade.Web/Pages/Admin/BlogPage.cshtml), [Category.cshtml](../../src/Moonglade.Web/Pages/Admin/Category.cshtml), [Widgets.cshtml](../../src/Moonglade.Web/Pages/Admin/Widgets.cshtml) | Preserve card grids, but standardize their border, radius, gaps, footers, and placement beneath the toolbar. |
| Settings/menu | [Settings pages](../../src/Moonglade.Web/Pages/Admin/Settings), [Menu.cshtml](../../src/Moonglade.Web/Pages/Admin/Menu.cshtml), [Menu.cshtml.css](../../src/Moonglade.Web/Pages/Admin/Menu.cshtml.css) | Settings mix field-level cards and grouped rows; headings sit outside panels. Menu uses a separate style and a bottom Save action. |
| Dashboard/helpers | [Dashboard.cshtml](../../src/Moonglade.Web/Pages/Admin/Dashboard.cshtml), [Dashboard.cshtml.css](../../src/Moonglade.Web/Pages/Admin/Dashboard.cshtml.css), [LocalAccount.cshtml](../../src/Moonglade.Web/Pages/Admin/LocalAccount.cshtml), [About.cshtml](../../src/Moonglade.Web/Pages/Admin/About.cshtml) | Dashboard has its own surface rules. Account has two centered action cards. About already has grouped white tables. Do not add empty toolbars. |
| Excluded editors | [EditPost.cshtml](../../src/Moonglade.Web/Pages/Admin/EditPost.cshtml), [EditPage.cshtml](../../src/Moonglade.Web/Pages/Admin/EditPage.cshtml) | Both use the shared layout's sidebar-free branch. Shared CSS changes must not affect their sizing, scrolling, editor controls, or bottom actions. |

Browser measurements confirmed that the comments toolbar scrolled above the viewport while the tags toolbar stayed at the same top position during content scrolling. Most toolbar locations already align initially; the issue is not a missing shared layout.

## Scope and invariants

- Limit implementation to the Web presentation layer and directly relevant tests/documentation. Reuse Razor, Bootstrap, Alpine.js, CSS isolation, existing theme tokens, and current modules.
- Prefer a few shared surface/toolbar/list rules over page-specific overrides or a new component framework. Do not make the entire main region white and then retain redundant nested card borders/shadows.
- Preserve routes, API payloads, page state, filters, sorting, pagination, form ownership, validation, antiforgery, event bindings, and element IDs/references needed by existing modules.
- Preserve immediate settings writes and their existing edit dialogs. Do not add a page-level Save or convert settings to a staged form. Menu retains its explicit save behavior.
- Do not change publishing, moderation, authentication, account reset, verification-file delivery, activity history, caches, or database behavior. No schema changes or migrations are expected.
- Preserve automatic theme selection and readable dark surfaces; white in D1/D3 describes light mode, not a forced white background in dark mode.
- Preserve public pages and both excluded editors. Shared tag/background rules may also affect public pages, so scope selectors carefully.
- Keep all six existing cultures: `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, `ja-JP`, and `fr-FR`. Avoid new text unless needed; changed resource keys require every existing translation.
- Supported admin width remains at least 1440 CSS pixels per the [README](../../README.md). Only 100% browser zoom is in scope; do not add zoom acceptance or a mobile redesign without new authorization.
- Do not perform writes, uploads, deletes, moderation actions, saves, test emails, authenticator resets, or publish actions against the user's existing blog data just to verify layout. Use an agreed isolated environment and synthetic fixtures for behavioral mutation checks.

## Batch plan

Technical dependencies are explicit below. The intended delivery order is B0 through B6, with a separate user gate between batches. B2-B5 depend on B1's primitives; B4 also preserves the verification-file surface introduced by B2.

### B0 - Establish the execution baseline and settle visual details

**Prerequisites:** user authorization to start B0.

**Work**

- Record the current branch, checkout, dirty files, running host, browser access, and safe test-data environment.
- Run the baseline checks W, T, and J below. Record actual failures/skips; do not import test totals from another task.
- Capture or describe representative baseline views: dashboard, long comment list, logs, tags, category/widget grids, general settings, verification files, and both excluded editors.
- Settle content/toolbar spacing, border/radius/shadow treatment, toolbar background/divider, and the settings tab/action arrangement with the user. These choices were resolved as D8-D11 during B0; do not reopen them without a concrete conflict.
- Confirm Menu Save placement and whether the bottom action remains. Resolved as D12: top only, preserving explicit-save behavior.
- Confirm zoom scope. Resolved during B0: 100% only; the user explicitly declined zoom support/checks.
- Agree how to obtain empty, single-item, long-content, multi-page, disabled/error, and expanded states without changing real blog data.

**Checks and acceptance**

- W/T/J results and baseline environment are recorded, or blockers are explicit.
- Proposed visual details and safe interactive test setup have user answers before dependent changes begin.
- The user accepts the baseline/decisions. No application changes or application commit are required for B0.

### B1 - Implement the shared frame and one reference page

**Technical dependency:** B0. **Start gate:** B0 accepted and user explicitly authorizes B1.

**Work**

- Add the light/dark navigation surface and scope shared styles to the ordinary admin shell.
- Make the toolbar a non-scrolling region below the top bar and make the body the content scroller, including pages without a toolbar.
- Define the approved shared surface, toolbar-group, divider, and spacing rules. Keep natural content height; a scroll viewport must not force its white child panel to stretch.
- Use Activity Logs as the first real white-panel/toolbar sample. Preserve its table, filter/detail offcanvas, clear confirmation, and pagination.
- Do not blindly enable the existing scroll flag everywhere without checking flex sizing, focus, grid gutters, dropdowns, modals, and offcanvas behavior.

**Checks and acceptance**

- W and T pass; run J only if JavaScript changes.
- On a long content page, the toolbar remains visible from the first to the last record. Only the intended content region scrolls vertically; no duplicate main-content scrollbar or clipped final row/pager.
- Logs and the shared navigation are reviewed at both viewports and in both modes. Tab navigation does not move focus underneath a fixed overlay.
- Spot-check dashboard, tags, settings, a card grid, public home, and both excluded editors for shared-layout regressions.
- User accepts the actual reference-page appearance before its pattern spreads. Suggested user commit subject: `Unify admin frame and activity log surfaces`.

### B2 - Unify list and table pages

**Technical dependency:** B1. **Start gate:** previous delivery gate closed and user authorizes B2.

**Work**

- Migrate published articles, drafts, scheduled articles, both recycle bins, comments, mentions, and tags to the approved surface rules.
- Use list separators rather than repeated independent card shadows. Keep comments/replies and tag-letter groups semantically distinct within the containing panel.
- Complete consistent logs/table/pager treatment and move verification-file New into the agreed toolbar composition without losing its settings navigation.
- Place primary actions/state switches on the left and counts/search/filter controls on the right, including conditional bulk actions.
- Keep loading, filtered-empty, empty, single-item, and multi-page states inside the appropriate content surface. Do not discard existing status or warning messages.

**Checks and acceptance**

- W and T pass. Run J if modules change; pagination/activity modules are existing regression targets.
- Read-only browser checks cover every affected route, page-size/pagination controls, filter drawers, selection-dependent controls, long titles, multiline replies, and the first/last rows.
- Mutation scenarios run only with agreed disposable fixtures. Check restore/delete/publish/moderation behavior without altering real records.
- Sparse panels remain content-height; pagination is visually part of the list and is not clipped.
- User accepts lists, table surfaces, empty states, and toolbar grouping. Suggested user commit subject: `Unify admin list and table surfaces`.

### B3 - Align independent card grids

**Technical dependency:** B1. **Start gate:** previous delivery gate closed and user authorizes B3.

**Work**

- Align Pages, Categories, and Widgets with the approved card style and toolbar composition.
- Preserve separate cards and gray grid gutters; do not add an unnecessary outer white panel.
- Standardize card padding, border, radius, footer treatment, and button alignment without changing widget previews or hiding meaningful content.
- Preserve the existing grid model unless a layout change is separately approved. Long descriptions, images, empty results, and uneven content heights must remain usable.

**Checks and acceptance**

- W and T pass; J only if modules change.
- Browser checks cover all three grids in both modes/viewports, long content, a single card, and empty results. Images/previews must fit without overlapping controls.
- New/edit drawers open and close correctly; fixture-only behavioral checks preserve existing actions.
- User accepts all three grids. Suggested user commit subject: `Align admin card grid styling`.

### B4 - Group settings and align menu management

**Technical dependencies:** B1, B2. **Start gate:** previous delivery gate closed and user authorizes B4.

**Work**

- Group related settings into larger panels with headings inside and fine row dividers, retaining useful column/group distinctions.
- Cover General, Content, Comments, Image, Subscription, Appearance, Notification, Advanced, and Verification Files; preserve the B2 file table treatment.
- Align settings navigation and optional page actions in the approved fixed header arrangement. Do not add empty action rows.
- Preserve field/edit-dialog identifiers, required-field relationships, conditional sections, validation, and immediate PATCH/save behavior.
- Align menu enablement, nested menu cards, ordering controls, and explicit Save using the B0 decision. Prevent any toolbar relocation from breaking submit/form association.
- Preserve theme previews, image/avatar dialogs, notification capability states, and code/text editors embedded in settings.

**Checks and acceptance**

- W and T pass. Run J when JavaScript changes; add/update meaningful owning tests only when bindings, form ownership, or other behavior needs regression coverage.
- Browser-check all nine tabs and menu management. In isolated fixtures, exercise an edit dialog, toggle, select/radio control, validation failure, and nested-menu save.
- Confirm one approved interaction still makes the same write as before; regrouping must not introduce duplicate event submissions or delayed settings writes.
- Do not expose configuration secrets in snapshots, console captures, logs, or this record.
- User accepts grouping, retained save semantics, and menu action placement. Suggested user commit subject: `Group admin settings into consistent panels`.

### B5 - Harmonize dashboard and auxiliary pages

**Technical dependency:** B1. **Start gate:** previous delivery gate closed and user authorizes B5.

**Work**

- Align Dashboard's metrics and business panels with the common surface rules; keep its useful multi-panel hierarchy and current data.
- Align its New Post placement with the common action region without inventing a redundant toolbar.
- Align Account and About surfaces without adding empty toolbar space or changing their information/action structure.
- Do not redesign account actions or alter authentication/reset behavior.
- Consolidate superseded page-specific styles only where directly replaced by this work.

**Checks and acceptance**

- W and T pass; J only if modules change.
- Review Dashboard with empty/non-empty sections and long titles, Account with keyboard focus, and About's grouped tables.
- Verify public home and both editor layouts remain unchanged by shared selectors; do not click account-reset actions against the user's account.
- User accepts the three pages. Suggested user commit subject: `Align dashboard and auxiliary admin surfaces`.

### B6 - Perform integrated acceptance and close the record

**Technical dependencies:** B2, B3, B4, B5. **Start gate:** all implementation batches accepted/commits confirmed and user authorizes B6.

**Work**

- Run W, T, and J once against the combined checkout. Review cumulative presentation changes against the recorded B0 baseline.
- Execute the complete route and state matrix below. Check dark mode, cultures, focus, overflow, overlays, and editor/public regressions at 100% zoom after all styles coexist.
- Fix only task-caused defects within the approved design. Ask before changing previously accepted design decisions or expanding scope. Re-run affected gates and identify any acceptance invalidated by a fix.
- Update durable frontend guidance only if a reusable layout contract changed. Do not rewrite unrelated documentation or make permanent docs depend on this plan.
- Record remaining limitations honestly and wait for final user acceptance.

**Checks and acceptance**

- Every matrix row has actual evidence or is explicitly blocked, not assumed passed from a build.
- All batches have user acceptance and confirmed user-created commits, where applicable.
- Final handoff records the verified checkout, commands/results, browser coverage, remaining risks, and user acceptance.
- Retain this file. No deployment, push, PR, merge, or autonomous commit is part of completion.

## Verification commands and evidence rules

Run from the repository root. B0 executed W, T, and J; results are recorded below. Later batches need their own results and must not reuse B0 as post-change evidence. The root [global.json](../../global.json) selects native Microsoft.Testing.Platform; use `--project`, not a positional project path or VSTest filter syntax.

| ID | Purpose | Command |
| --- | --- | --- |
| W | Compile Web/Razor/shared layout | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj` |
| T | Existing Web behavior/rendering regression suite | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` |
| J | Existing frontend helper regression suite | `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` |

- W/T do not establish visual correctness. J does not substitute for actual rendered browser inspection.
- Use existing tooling. Do not add a screenshot framework, new test project, dependency, or CI job without discussing the need and obtaining approval.
- CSS-only batches need compilation and browser checks, not tests that merely assert a CSS class exists. If markup/form/event changes need new behavior tests, inspect the existing harness and use the owning Web project.
- Do not automatically run the full solution or provider/database fixture suites for this presentation-only task. Escalate only if an actual change/failure warrants it; explain the reason. No provider compatibility claim may be based on skipped fixtures.
- Record the exact command, checkout, exit code, warnings, and test discovery/pass/fail/skip totals when available. Record failures separately from unavailable infrastructure.
- Do not restore/install preemptively. If missing dependencies block a chosen check, restore the required scope and record that prerequisite.
- If the user's running host locks build output, stop and ask how to coordinate verification; do not kill their host or silently replace it.

### Browser route and state matrix

| Group | Routes | Required representative states |
| --- | --- | --- |
| Dashboard | `/admin` | Empty/non-empty recent sections, metrics, long titles, New Post location |
| Articles | `/admin/post`, `/admin/post/draft`, `/admin/post/scheduled`, `/admin/post/recycle-bin` | Loading, empty/filter-empty, single/many records, status tabs, filter drawer, pager/page size, conditional actions |
| Pages | `/admin/page`, `/admin/page/recycle-bin` | Card grid, single/empty states, recycle list, badges and actions |
| Taxonomy | `/admin/category`, `/admin/tags` | Long names/descriptions, card grid, letter groups, search, drawer, long scroll |
| Interaction | `/admin/comments`, `/admin/mention` | Multiline content, replies, bulk selection, long scroll, filters and pagination |
| Widgets/menu | `/admin/widgets`, `/admin/menu` | Text/image/link previews, nested menu, ordering, disabled/empty states, agreed Save location |
| Logs | `/admin/activitylogs` | Table, empty results, filter/detail drawers, long detail, pagination and confirmation appearance |
| Settings | `/admin/settings/general`, `/admin/settings/content`, `/admin/settings/comment`, `/admin/settings/image`, `/admin/settings/subscription`, `/admin/settings/appearance`, `/admin/settings/notification`, `/admin/settings/advanced`, `/admin/settings/verification-files` | All tab layouts, grouped rows, dialogs, validation, capability messages, theme previews and verification table |
| Auxiliary | `/admin/account`, `/admin/about` | Focus/hover, card/panel boundaries, readable tables, no artificial empty toolbar |
| Excluded regression surfaces | `/admin/post/edit`, `/admin/page/edit`, public home and a public article | Existing sizes/scrolling, editor toolbar/footer, overlays, no admin style leakage |

For each in-scope route, inspect 1440 x 900 and 1920 x 1080 CSS-pixel viewports in light/dark mode at 100% browser zoom. Check all six cultures for toolbar wrapping, labels, and panel overflow. Do not run 125%/150% zoom checks; the user explicitly excluded them during B0. Record any sampling or unavailable state rather than claiming the whole matrix passed.

Long-page criteria: toolbar position stays constant during content scrolling; controls remain reachable by keyboard; no clipped focus, final row, pager, card edge, offcanvas, or modal; no unintended whole-workspace horizontal scroll at supported baseline viewports. Wide tables may use their own intentional horizontal scroller.

Do not capture secrets or identifying blog data in committed screenshots. Use agreed synthetic content. Textual results and decisions must remain sufficient for handoff even when session-local screenshots are unavailable.

## Progress tracker

Status vocabulary: `Pending`, `In progress`, `Blocked`, `Ready for user acceptance`, `Accepted - awaiting user commit`, `Accepted - awaiting continuation`, `Complete`. Acceptance, commit confirmation, and continuation are distinct gates.

| Batch | Technical dependencies | Implementation/check status | Evidence | User acceptance | User commit | Next gate |
| --- | --- | --- | --- | --- | --- | --- |
| B0 | Explicit B0 authorization | Complete | W: 0 warnings/errors after approved host stop; T: 662 passed; J: 32 passed; no test failures/skips; representative read-only baseline recorded below | Accepted explicitly on 2026-11-09 | Planning commit `b2c47ecf9` confirmed; no B0 application commit required | Closed; user explicitly authorized B1 |
| B1 | B0 | Ready for user acceptance | W: 0 warnings/errors; J: 32 passed; browser checks passed; latest T: 661 passed, 1 failed, 0 skipped; the user explicitly approved the known image-test exception for B1 | Pending; only the test exception has been approved | Not created | User visual acceptance, user-managed commit, and explicit continuation instruction |
| B2 | B1 | Pending | Not run | Pending | Not created | Prior delivery closed and B2 authorization |
| B3 | B1 | Pending | Not run | Pending | Not created | Prior delivery closed and B3 authorization |
| B4 | B1, B2 | Pending | Not run | Pending | Not created | Prior delivery closed and B4 authorization |
| B5 | B1 | Pending | Not run | Pending | Not created | Prior delivery closed and B5 authorization |
| B6 | B2, B3, B4, B5 | Pending | Not run | Pending | Verification/documentation only unless fixes are needed | All implementation gates closed and B6 authorization |

### Required update after every batch or interruption

1. Update the row with the actual status; never equate self-check completion with user acceptance.
2. Add a dated execution entry containing scope/files changed, actual checkout, exact commands/results, browser route/state/theme/culture coverage, skipped checks, risks, and unresolved questions.
3. Record explicit user acceptance when it arrives. Before that, use `Ready for user acceptance`.
4. The user commits. On the next interaction, record the confirmed SHA and subject without amending their commit. A later tracking update can record a previously created commit; do not fabricate a self-referencing SHA.
5. Record whether the user authorized the next batch. Keep the latest handoff actionable even when the next step is only to wait.
6. Keep unrelated work out of batch commits. Keep prerequisites in place until their dependents are removed: B4 depends on B2, and all implementation batches depend on B1. Scope boundaries make rollback reviewable, not necessarily conflict-free when commits touch shared files. Any Git rollback is user-directed, never automatic.

### Execution entry template

```text
Date / batch:
Authorization:
Starting checkout and dirty work:
Implemented scope and files:
Checks: exact command; exit code; passed/failed/skipped or build result:
Browser: viewport, zoom, mode, culture, routes/states actually checked:
Evidence location (optional; no secrets):
Unresolved choices, defects, skipped checks, and risks:
User acceptance:
Confirmed user commit:
Permission to continue:
Next action:
```

## Decision and execution log

- 2026-11-09: Initial analysis completed read-only. User confirmed D1-D7, including content-height panels, fixed toolbars, content-type-specific surfaces, and exclusion of both editors.
- 2026-11-09: User confirmed self-check then user acceptance, user-managed commits, and an explicit continuation gate between batches.
- 2026-11-09: User chose to continue on the current branch without creating another branch.
- 2026-11-09: User initially confirmed desktop acceptance at both proposed viewports, both modes, zoom checks, and all six existing cultures. During B0, the user explicitly narrowed this to 100% standard visual acceptance with no zoom support/checks; D7 and the verification matrix now reflect that answer.
- 2026-11-09: The user committed the plan as `b2c47ecf94540bbe8b3d01918cd98180b0776f3c`. Git history confirmed that commit at B0 start; AI did not create it.
- 2026-11-09: User explicitly authorized B0 only. No application implementation batch has started.
- 2026-11-09: User confirmed D8-D12: 24px outer/panel padding, 16px gaps, 8px radii, 1px borders, no shadows, workspace-colored toolbar without a bottom divider, naturally wrapping action/tab flow, and Menu Save at the top only.
- 2026-11-09: User confirmed D13. The existing site is read-only; separate local disposable data will be required for later browser mutation scenarios. No test instance/database was provisioned during B0.
- 2026-11-09: The first build encountered a running-host file lock. The user explicitly authorized stopping that specific host process and chose to restart it personally after checks. AI stopped only the verified process; the user restarted the site and manually authenticated the new browser session.
- 2026-11-09: User explicitly accepted B0 and authorized B1. The user additionally authorized autonomous local host stop/start and README default-account login. This replaces the earlier per-restart permission restriction, not the read-only blog-data policy or user-managed commit gate.
- 2026-11-09: B1 shared-frame/reference-page changes were implemented and browser-checked. The final Web build passed and JavaScript tests passed. Two full Web runs failed the same unchanged image-storage test; isolated and intervening full reruns passed. The last full run failed, so B1's test gate remains blocked pending the user's scope decision.
- 2026-11-09: The user chose to keep B1's visual scope and explicitly record the image-test exception before visual acceptance. Image-test investigation/fixes are deferred; this decision does not accept B1's appearance, authorize a commit, or authorize B2.

### B0 execution record - 2026-11-09

**Authorization and checkout**

- User instruction: execute B0. Authorization does not extend to B1.
- Branch: `master`, one local planning commit ahead of `origin/master` at execution start.
- Verified checkout: `b2c47ecf94540bbe8b3d01918cd98180b0776f3c`.
- Starting worktree: clean. Git comparison with the earlier analysis checkout showed only this plan added; no application-source changes.
- Runtimes observed: .NET SDK `10.0.403`, Node.js `v26.13.0`.
- Changes made by AI: this tracker only. No application/test source, configuration, dependency manifests, database data, commits, or branch changes.

**Automated evidence**

| Check | Exact command | Exit code | Actual result |
| --- | --- | --- | --- |
| W, initial attempt | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj` | 1 | File-copy failures MSB3027/MSB3021: running local Web host locked output DLLs. Summary: 190 warnings, 38 errors. This was not a passing baseline and is retained here rather than hidden by the retry. |
| W, retry after approved host stop | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj` | 0 | Build succeeded: 0 warnings, 0 errors. |
| T | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 0 | Passed: 662, Failed: 0, Skipped: 0; total discovered/executed: 662. |
| J | `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | 0 | Passed: 32, Failed: 0, Skipped: 0; total: 32; cancelled: 0, todo: 0. |

No separate dependency installation or restore was needed; the build's normal restore reported projects up to date. No full-solution run, provider fixture run, or browser write scenario was attempted.

**Host and browser coordination**

- The initial live site responded at `https://localhost:10210/admin` and was authenticated. Its loaded Dashboard was measured before the stop.
- The locking host was verified as PID `30216`, executable under this checkout's Web Debug output. After explicit user permission, AI stopped only that process. This PID is historical evidence, not standing permission to stop any current process.
- AI did not restart the site. The user restarted it; the HTTPS listener subsequently belonged to PID `17680`. Do not reuse either historical PID for future process control.
- Browser page IDs reset during this work. AI rediscovered the page, navigated to the restored site, and the user manually signed in. No local credentials/configuration were read or copied.
- The restarted host's binary revision was not independently fingerprinted; the browser baseline is associated with the unchanged source checkout and user-restarted local host, not a deployment certification.
- Browser changes were limited to navigation, viewport sizing, content scrolling, and temporary dark-media emulation. No forms were filled/saved, destructive actions invoked, or fixtures created. Blank editor auto-save was off.
- Dark-media emulation was reset to the browser default afterward. The browser was left on Dashboard at 1920 x 1080.

**Fresh read-only baseline coverage**

All fresh measurements used English (`en-us`) and 100% standard zoom. No screenshots containing blog data or configuration were committed; these textual descriptions are the durable baseline.

| Viewport and mode | Routes actually measured | Recorded observations |
| --- | --- | --- |
| 1440 x 900, light | `/admin` | 190px sidebar with transparent background; main/panel padding 16px; four metric cards and four business panels; 8px panel radii; no horizontal main-area overflow. |
| 1440 x 900, light | `/admin/comments` | Five independent 8px-radius comment cards with shadows and 16px bottom margins. Toolbar has 25px bottom margin; at the bottom of the 131px scroll range, its top moves from about 69.19px to -61.81px. The whole main area scrolls. |
| 1440 x 900, light | `/admin/activitylogs` | Eight visible table rows; wrapper is transparent, unbordered, radius 0, padding 0. Toolbar controls are Filter and Clear all; toolbar height 47px with 25px bottom margin. |
| 1440 x 900, light | `/admin/tags` | 24 letter groups, no containing white panel. Content scroll range 1218px; toolbar top remains about 69.19px. Main overflow is hidden while the body scrolls. |
| 1440 x 900, light | `/admin/category`, `/admin/widgets` | Category grid: four columns, 11 cards. Widget grid: three columns, five cards. Both use 24px gutters, 6px card radii and shadows. The visible image preview stays within its card. |
| 1440 x 900, light | `/admin/settings/general` | One-row settings header; 15 setting rows with mixed full/bottom-only/no rounding. Three business-group headings sit outside setting rows. Main content has 58px vertical overflow. |
| 1440 x 900, light | `/admin/settings/verification-files` | One visible table row; transparent/unbordered wrapper. New is outside the shared toolbar, about 140.80px from the viewport top. |
| 1440 x 900, light | `/admin/menu` | Two menu cards. The only form Save is outside the toolbar, about 530.50px from the viewport top. No save interaction performed. |
| 1440 x 900, light | `/admin/post/edit` | Sidebar absent; properties column 280px. Save/Preview/Publish start around y=854px; action region height 38px. Auto-save off; no content entered. Body fits the viewport. |
| 1440 x 900, light | `/admin/page/edit` | Sidebar absent; bottom footer starts at y=845px and is 55px high. Save/Preview start around y=854px. No content entered. Body fits the viewport. |
| 1920 x 1080, light and dark | `/admin` | Light workspace RGB 250/250/250 and white panels; dark workspace RGB 22/23/25 and panels RGB 33/37/41. Sidebar remains transparent. Theme preference stays auto; media emulation changes the rendered theme. No horizontal main-area overflow in either mode. |

No horizontal main-area overflow was observed on the ordinary admin routes measured above. Editor body dimensions matched their viewport. These measurements characterize existing behavior; they do not mean the requested redesign is already implemented.

Earlier read-only analysis covered the remaining ordinary pages and all nine settings tabs against the same application source. B0 did not repeat the complete B6 matrix: six-language checks, all route/mode/viewport combinations, loading/failure states, expanded replies, and synthetic write scenarios remain future gates. They must not be reported as passed from these samples.

**Safe fixtures and remaining gates**

- The safe-data policy is agreed, not provisioned: keep the current site read-only; confirm a separate local instance and disposable database before creating synthetic states or testing writes.
- Use existing empty/single/long/multi-page states for safe read-only samples when available. Create missing fixture states only in the subsequently approved isolated environment. Do not simulate success/failure states by changing production modules.
- No design question from the B0 list remains open. If a concrete implementation conflict arises, ask rather than overriding D1-D13.
- B0 self-check status: completed and subsequently accepted explicitly by the user; B1 continuation permission received.
- No B0 application commit is required. The user owns any commit of this tracking update. No application files were changed.

### B1 execution record - 2026-11-09

**Authorization and checkout**

- User explicitly accepted B0 and instructed AI to advance to B1 only. Local host stop/start and README default-account login are authorized; commits and subsequent batches are not.
- Starting branch/HEAD: `master`, `b2c47ecf94540bbe8b3d01918cd98180b0776f3c`. The only starting dirty file was this tracker's uncommitted B0 update; those results and decisions were preserved.
- No dependency, configuration, localization-resource, database/schema, or business-handler changes were made. Browser activity was read-only: navigation, filters, pagination, drawers, and cancellation of the clear confirmation. No clear/save/publish action was confirmed and no fixtures were written.

**Implemented scope**

- `_LayoutAdmin.cshtml` now gives every ordinary admin page the same keyboard-focusable `.admin-main-body` scroller. The toolbar remains outside it; the full-screen editor branch is unchanged. The obsolete Tags-only scroll flag was removed.
- `admin.css` gives the sidebar a white/light or corresponding dark surface, removes its 5px outer margin, applies 24px main padding and 16px toolbar spacing, and adds content-height `.admin-panel` and wrapping toolbar/group primitives. Scrollport padding preserves Bootstrap grid gutters. The native scroller focus outline is inset 2px to keep it inside the clipped workspace.
- All ten existing toolbar roots delegate bottom spacing/dividers to the shared CSS instead of `pb-2 border-bottom mb-3`. Other pages' content surfaces and action order were not migrated; those remain B2-B5 work.
- Activity Logs is the reference panel: 24px padding, 8px radius, a 1px theme-aware border, no shadow, an internal footer divider, and theme-aware table headings. Clear all is on the left and Filter on the right, using native buttons and existing localized labels/bindings.
- The log footer uses the existing local `x-show.important` pattern so Bootstrap's `d-flex !important` cannot leave a ghost footer in empty results. Keep it visible when existing logs are refreshing: hiding the focused page-size select during loading was observed to lose keyboard focus. The final `hasLogs` condition preserves that control throughout a populated refresh while keeping empty results compact.
- Shared pagination now scrolls `.admin-main-body`, with matching assertions in `AdminPaginationTests.mjs` and `ActivityLogTests.mjs`.
- The Frontend and Localization section of `docs/agent-reference.md` documents the reusable shell/surface contract without linking to this temporary tracker.

**Automated evidence and approved test exception**

| Check / order | Exact command | Exit code | Actual result |
| --- | --- | --- | --- |
| J | `node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | 0 | Passed: 32, Failed: 0, Skipped: 0; cancelled: 0, todo: 0. No JavaScript changed after this run; subsequent formatting only normalized line endings. |
| W, initial and subsequent builds, including final source | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj` | 0 | Every B1 Web build passed with 0 warnings and 0 errors. The last build includes the focus-safe footer, scroller focus inset, and scoped CRLF normalization. |
| T, initial | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 0 | Passed: 662, Failed: 0, Skipped: 0. |
| T, after the table-header update | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 2 | Passed: 661, Failed: 1, Skipped: 0. The image test below failed. |
| T, isolated diagnostic | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-build --filter-method '*Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage'` | 0 | Passed: 1, Failed: 0, Skipped: 0; the intended single test was discovered. |
| T, full retry and later intermediate full run | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 0 | Both runs passed: 662, Failed: 0, Skipped: 0. These successes do not replace the final failing result. |
| T, final source after footer/focus correction and formatting | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | 2 | Passed: 661, Failed: 1, Skipped: 0. The same image test failed again. This remains the latest full result, covered only by the explicit B1 exception below. |

Failing test: `Moonglade.Web.Tests.Controllers.ImageControllerTests.Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage`, `src/Tests/Moonglade.Web.Tests/Controllers/ImageControllerTests.cs:88`. Its assertion could not find the asynchronously kept original image after its background-service stop. That source, the image controller, and storage implementation were not changed. The cause has not been established; do not label it fixed, silently skip it, or repeatedly rerun until green. Ask before expanding B1 to investigate/fix it or accepting a test-gate exception.

The user subsequently explicitly selected keeping B1's scope, recording this test exception, and proceeding to visual acceptance. No image-test repair is authorized or required within the current B1 scope. This is a known failed test with an approved exception, not a successful final Web suite or general permission to ignore future failures.

Requirement coverage for the focused pagination change:

- `changing pages scrolls the admin body after loading the new page` asserts the exact scroller and load/URL/scroll order.
- `key toggling filter reset and page changes preserve exact active filters` covers the Activity Logs composition with that scroller and unchanged filter state.
- Both passed in the J command above. Real comments pagination was also checked in the browser: page 2 reset the content scroll position from 129px to 0 without moving the toolbar.

No separate package installation/restore, full-solution run, or provider/database suite was needed. No image-test repair has been attempted. The owning Web suite was not reported as passing after its final failure.

**Browser evidence**

All checks used 100% standard zoom. Real existing data and read-only GET/filter interactions supplied the states; no test database was provisioned.

| Coverage | Actual observations |
| --- | --- |
| Activity Logs populated sample: both desktop viewports, light/dark, all six cultures | All 24 combinations passed 11 grouped DOM/style checks for 24px padding, 16px gap, surfaces/borders/radius/shadow, toolbar left/right alignment, natural panel height, focusable body-only scrolling, and no document/content horizontal overflow. All used eight real rows. Panel height was about 536.61px, not viewport-stretched. |
| Theme surfaces | Light sidebar/panel: RGB 255/255/255 on RGB 250/250/250 workspace. Dark sidebar/panel: RGB 33/37/41 on RGB 22/23/25 workspace. Table headings follow the theme rather than remaining white in dark mode. |
| Empty and one-row filters, final compiled markup | Real event filters yielded zero and one row. Empty panel: about 105.61px with footer hidden. One-row panel: about 200.61px with page-size control visible. Neither stretched to the scroll viewport. |
| Loading and populated refresh, final compiled markup | Temporary Slow 3G emulation exposed a real empty-to-one-row load: loading panel about 71.61px, footer hidden, toolbar top unchanged at about 77.19px. A populated page-size refresh kept the footer visible and select focused during loading and afterward. Native ArrowDown changed 10 to 20 without losing focus; a throttled select change to 30 also retained focus and returned the same one-row filter. |
| Long comments, 1440 x 900 light | Body scroll range 129px; toolbar top stayed about 77.19px from top to bottom. Main/document vertical ranges were zero. Final card and pager remained visible, pager bottom about 875.88px. Ctrl+End worked in the focused body. Real page 2 navigation reset only body scrolling to zero. |
| Long Tags | At 1440 x 900 light: 24 groups and 1216px body range. At 1920 x 1080 dark: 24 groups and 960px range. Toolbar top stayed about 77.19px at both extremes; no second main/document scrollbar or horizontal overflow. |
| Logs overlays and keyboard, 1440 x 900 light | Filter drawer occupied the full 400 x 900 area at the right; bottom actions remained visible. Keyboard boundary controls were reachable without focusing underlying content. Detail drawer occupied 480 x 900; Escape restored focus to its Detail trigger below the toolbar. Clear confirmation fit fully inside the viewport with danger styling and was cancelled, never confirmed. |
| Scroller focus | Tab from Filter reached the body. Its native 1px focus outline uses a -2px offset and was visually checked inside the workspace rather than being clipped at its perimeter. |
| German General Settings, 1440 x 900 light | All nine classification tabs fit in one row. Fifteen settings rows remained reachable; 113px content scroll did not move the header. No fields were read/edited for this check. |
| Dashboard, Categories, Posts, 1440 x 900 light | No extra empty toolbar on Dashboard or horizontal overflow. Category grid retained 11 cards/four columns with card edges at x=214px and x=1416px, so negative Bootstrap gutters were not clipped. Posts retained its existing actions and fitted the body viewport; no toolbar dropdown existed to test on that page. |
| Public home, 1440 x 900 light | Public layout rendered without horizontal overflow and did not load the admin stylesheet. No public article was opened or content edited. |
| Excluded blank editors, 1440 x 900 light | Both remained sidebar-free and fitted the viewport. Post properties stayed 280px wide; actions stayed at y=854px with 38px height and auto-save off. Page footer stayed at y=845px with 55px height; Preview stayed at y=854px. No editor content was entered/saved. |

The populated 24-combination geometry matrix preceded the final footer-focus correction; populated appearance was unchanged, and the final compiled markup was then rechecked for populated, empty, loading, one-row, and keyboard page-size behavior. This is not a claim that the complete B6 route/state matrix passed. Logs had no natural multi-page/long-history sample; real comments/tags and the focused Node tests supplied the corresponding shared-shell/pagination evidence. Write/error fixtures, actual destructive confirmation, all other routes in all cultures, and the public-article regression remain later checks.

Browser screenshots were inspected but none containing existing blog data were committed. A browser file-based probe was rejected by the workspace-root guard; no file-based result is claimed. Permitted inline DOM measurements supplied the reported matrix. Temporary probes and five extra culture tabs were removed.

**Environment, ownership, and next gate**

- The final compiled host is running for review at `https://localhost:10210/admin/activitylogs`, launched with `dotnet run --project .\src\Moonglade.Web\Moonglade.Web.csproj --no-build --launch-profile 'Moonglade.Web'`. Its detached tool session is `admin-b1-review-host`; the last observed listener PID is `17640`. Reverify ownership/current PID before future stops.
- The existing authenticated browser session survived restarts, so no README credentials or developer configuration were read.
- Temporary theme/network/viewport emulation was removed. The browser was returned to the unfiltered log page at 1920 x 1080 with the existing automatic theme preference. No dialogs/drawers or temporary audit helper remain.
- Application/documentation changes remain uncommitted. No branch change, staging, commit, push, PR, deployment, or B2 work occurred.
- B1 visual acceptance: pending. The specific image-test exception is explicitly approved; extra image-test implementation scope is not. B1 is ready for visual review. Do not treat successful intermediate retries or the exception decision as visual acceptance.

## Latest handoff

- Current phase: B0 accepted. B1 ready for user visual acceptance with the explicitly approved image-test exception.
- Current authorized work: B1 only; do not begin B2.
- Verified baseline: `b2c47ecf94540bbe8b3d01918cd98180b0776f3c` on `master`; do not change branches.
- Application changes: shared ordinary-admin scroller/sidebar/spacing, Activity Logs reference panel and toolbar, shared pagination scroller, and corresponding Node assertions. Legacy toolbar divider/spacing classes were removed without migrating other pages' content or action order.
- Verification: B1 W passed with 0 warnings/errors; J passed 32/32; browser evidence is recorded above. Latest B1 T: Passed: 661, Failed: 1, Skipped: 0 in the unchanged original-image storage test. The user explicitly approved recording this B1 exception; the suite is not reported as passing.
- Decisions: D1-D13 govern implementation. In particular, use 24px padding/16px gaps, 8px radii/1px borders/no shadows, gray toolbar without a bottom divider, naturally wrapping settings actions/tabs, and top-only Menu Save. Only 100% browser zoom is in scope.
- Environment: local host stop/start and README default-account login are authorized. The final build is running in detached session `admin-b1-review-host` (last listener PID `17640`). Verify ownership before stopping. Browser is on the unfiltered logs page at 1920 x 1080 with emulation restored to default.
- Remaining setup: no isolated fixture instance/database has been created. Confirm the concrete environment before any later browser write scenario; current development blog remains read-only.
- Immediate next action: stop for the user's B1 visual review. Do not create a commit, claim B1 visually accepted, investigate/fix the deferred image test, or advance to B2 without separate instructions.
