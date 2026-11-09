# Admin Portal Visual Refresh: Execution Plan and Progress

## Current state

- Last updated: 2026-11-09.
- Phase: planning only; implementation has not been authorized.
- This document is the task plan, decision record, progress tracker, and cold-start handoff. The user explicitly requested its retention, including after completion.
- No application code, configuration, dependencies, or database data was changed during planning. No build, automated test, or implementation acceptance gate has run for this task.
- Planning checkout: `master`, commit `6615117f9b93d7d9c31519ec109b1cc00f7e2662`. The worktree was clean before this document was added. This is a source reference, not a tested baseline; B0 must record the actual execution checkout.
- Authorized now: write this plan into the repository. Not authorized now: implement a batch, make commits, change branches, push, create a PR, or deploy.
- Next action: the user reviews this plan and explicitly authorizes B0. Do not interpret approval of individual workflow choices as authorization to implement the entire plan.

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
| D7 | Accept at 1440 x 900 and 1920 x 1080 CSS-pixel desktop viewports, in light/dark mode, with zoom checks and all six existing UI languages. |

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
6. Do not read credentials from local development configuration. If access is missing, ask the user to sign in or approve an alternative. Do not restart the user's host, kill processes, or alter ports/configuration without agreement.
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
- Supported admin width remains at least 1440 CSS pixels per the [README](../../README.md). Narrower-window/zoom checks do not authorize a mobile redesign.
- Do not perform writes, uploads, deletes, moderation actions, saves, test emails, authenticator resets, or publish actions against the user's existing blog data just to verify layout. Use an agreed isolated environment and synthetic fixtures for behavioral mutation checks.

## Batch plan

Technical dependencies are explicit below. The intended delivery order is B0 through B6, with a separate user gate between batches. B2-B5 depend on B1's primitives; B4 also preserves the verification-file surface introduced by B2.

### B0 - Establish the execution baseline and settle visual details

**Prerequisites:** user authorization to start B0.

**Work**

- Record the current branch, checkout, dirty files, running host, browser access, and safe test-data environment.
- Run the baseline checks W, T, and J below. Record actual failures/skips; do not import test totals from another task.
- Capture or describe representative baseline views: dashboard, long comment list, logs, tags, category/widget grids, general settings, verification files, and both excluded editors.
- Ask the user to settle material details not yet specified: content/toolbar spacing, border/radius/shadow treatment, toolbar background/divider, and the settings tab/action arrangement. Recommend reusing existing tokens and a restrained border rather than adding stronger shadows.
- Confirm whether Menu Save moves to the top action area and whether its old bottom action is removed or retained. These exact details were not separately approved in the initial analysis.
- Define zoom test levels with the user. Recommend 100% as the visual baseline and 125%/150% as usability checks, without changing the documented desktop minimum.
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
- Execute the complete route and state matrix below. Check dark mode, cultures, zoom, focus, overflow, overlays, and editor/public regressions after all styles coexist.
- Fix only task-caused defects within the approved design. Ask before changing previously accepted design decisions or expanding scope. Re-run affected gates and identify any acceptance invalidated by a fix.
- Update durable frontend guidance only if a reusable layout contract changed. Do not rewrite unrelated documentation or make permanent docs depend on this plan.
- Record remaining limitations honestly and wait for final user acceptance.

**Checks and acceptance**

- Every matrix row has actual evidence or is explicitly blocked, not assumed passed from a build.
- All batches have user acceptance and confirmed user-created commits, where applicable.
- Final handoff records the verified checkout, commands/results, browser coverage, remaining risks, and user acceptance.
- Retain this file. No deployment, push, PR, merge, or autonomous commit is part of completion.

## Verification commands and evidence rules

Run from the repository root. Commands are planned, not executed. The root [global.json](../../global.json) selects native Microsoft.Testing.Platform; use `--project`, not a positional project path or VSTest filter syntax.

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

For each in-scope route, inspect 1440 x 900 and 1920 x 1080 CSS-pixel viewports in light/dark mode at baseline zoom. Check all six cultures for toolbar wrapping, labels, and panel overflow. Run the user-agreed zoom levels on the shared shell and representative list, grid, and settings pages. Record any sampling or unavailable state rather than claiming the whole matrix passed.

Long-page criteria: toolbar position stays constant during content scrolling; controls remain reachable by keyboard; no clipped focus, final row, pager, card edge, offcanvas, or modal; no unintended whole-workspace horizontal scroll at supported baseline viewports. Wide tables may use their own intentional horizontal scroller.

Do not capture secrets or identifying blog data in committed screenshots. Use agreed synthetic content. Textual results and decisions must remain sufficient for handoff even when session-local screenshots are unavailable.

## Progress tracker

Status vocabulary: `Pending`, `In progress`, `Blocked`, `Ready for user acceptance`, `Accepted - awaiting user commit`, `Accepted - awaiting continuation`, `Complete`. Acceptance, commit confirmation, and continuation are distinct gates.

| Batch | Technical dependencies | Implementation/check status | Evidence | User acceptance | User commit | Next gate |
| --- | --- | --- | --- | --- | --- | --- |
| B0 | Explicit B0 authorization | Pending | No execution checks run | Pending | No application commit required | User reviews plan and authorizes B0 |
| B1 | B0 | Pending | Not run | Pending | Not created | B0 acceptance and B1 authorization |
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
- 2026-11-09: User confirmed desktop acceptance at both proposed viewports, both modes, zoom checks, and all six existing cultures. Exact zoom levels remain a B0 decision.
- 2026-11-09: This plan was written at the user's request. No implementation batch has started; no execution test results or batch acceptance are recorded.

## Latest handoff

- Current phase: plan drafted; waiting for user review and B0 authorization.
- Current authorized work: planning/documentation only.
- Current branch at planning: `master`; do not change branches.
- Application changes: none.
- Verification: source/documentation inspection only; W/T/J and browser execution acceptance are pending.
- Open decisions for B0: detailed visual tokens/spacing, toolbar background/divider and settings header arrangement, Menu Save placement/duplication, exact zoom levels, and safe fixture setup.
- Immediate next action: present this plan for review. Do not begin B0 tests, change application files, or commit this document unless explicitly instructed.
