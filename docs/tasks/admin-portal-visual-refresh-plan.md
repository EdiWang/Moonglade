# Admin Portal Visual Refresh: Execution Plan and Progress

## Current state

- Last updated: 2026-11-10.
- Phase: B0-B4 and R1 accepted. B5 is ready for user acceptance under confirmed D21/D22. B6 is not authorized.
- Latest refinement: D27 increases Activity Logs sizing between the preceding two revisions, using explicitly authorized page-scoped Bootstrap overrides. Final W passed with zero warnings/errors; 24 normal and 24 DOM-stress samples verified 44px ordinary rows and 35px pagination/page-size controls.
- Latest refinement: D26 supersedes D25's padded/integrated treatment with a compact Bootstrap-first table and external pagination. Final W passed with zero warnings/errors; 24 normal and 24 DOM-stress samples passed, with 40px normal rows, a 16px external-pager gap, and native refresh/detail/pagination checks.
- Latest refinement: D25 restyles the Activity Logs table without striping, assigns useful column widths, and uses accent detail buttons. Web build passed with zero warnings/errors; 24 normal and 24 DOM-stress samples passed across all six cultures, both approved sizes, and both themes; native detail/pagination checks passed.
- Latest refinement: D24 makes all ordinary-admin action-toolbar buttons/button-style links `btn-sm`. Web build passed with zero warnings/errors; 64 English route/viewport/theme samples verified small-button sizing, no overflow, and retained Menu form ownership.
- Latest refinement: D23 reduces Settings item gaps to 5px. Web build passed with zero warnings/errors; 40 English route/viewport/theme samples verified actual 5px gaps and preserved Account spacing/internal padding.
- Latest B5 checks: final W passed with zero warnings/errors; final T passed 663/663, zero failures/skips. Browser checks passed 72 normal samples, 72 DOM-only stress samples, and 48 boundary samples, followed by final-host keyboard/hover/contrast and native-page checks. J was not required because production JavaScript did not change.
- Latest R1 checks: W passed with zero warnings/errors; T passed 663/663; final J passed 34/34. The 360-sample navigation layout matrix, four final overflow stress cases, 16 boundary samples, native action dialogs, and actual keyboard navigation passed. All files pass UTF-8/CRLF/final-newline and whitespace checks.
- Earlier revised B4 checks: W passed with zero warnings/errors; T passed 663/663; all 240 fresh rendered samples passed, including disclosure states/interaction. No image-test exception was needed. Production JavaScript had not changed at that earlier checkpoint, so J was not required then; R1 now changes JavaScript and includes J.
- This document is the task plan, decision record, progress tracker, and cold-start handoff. The user explicitly requested its retention, including after completion.
- No application code, configuration, dependencies, or blog data was changed by AI during B0. The final Web build passed with zero warnings/errors; Web tests passed 662/662 and Node tests passed 32/32, with no failed or skipped tests.
- Planning checkout: `master`, commit `6615117f9b93d7d9c31519ec109b1cc00f7e2662`. B0 execution checkout: `b2c47ecf94540bbe8b3d01918cd98180b0776f3c` (`Document admin portal visual refresh plan`). The worktree was clean at B0 start. Only this plan differs between those commits; application source is unchanged.
- Authorized: B5 Dashboard/Account/About presentation work and its checks. Local test-data fixture permission, host stop/start, and README default-account login remain authorized. Not authorized: B6, Git commits, branch changes, push, PR, deployment, external email/service operations, account-reset operations, or unrelated image-test fixes.
- Starting B5 checkout: clean `master`, user-created commit `1dbcc3ee12e838b1a6c5c8d5b6106f339cef42a2` (`Refresh admin settings and secondary navigation`), containing accepted B4/R1.
- Next action: wait for user acceptance of B5 and the toolbar/table refinements; B5 and the Settings spacing user-created commits are confirmed. Do not start B6 without explicit authorization.

## Goal and reference context

Improve the admin portal's visual consistency using the user's research data management screenshots as a reference for surface hierarchy and toolbar placement, not as a request to reproduce its branding or features.

The reference has a blue top bar, a white left navigation area, a light-gray workspace, and white content surfaces. Table pages use a large white panel. Card pages retain separate white cards. Page-level controls occupy a consistent area above the content.

The screenshots were supplied in the original conversation and are not repository assets. A fresh conversation must not depend on access to those attachments: the decisions and acceptance criteria below are authoritative. If a visual detail cannot be recovered from this record, ask the user rather than inventing it.

### Confirmed design decisions

| ID | Decision confirmed by the user |
| --- | --- |
| D1 | Retain a light-gray page background and give the left navigation a distinct white surface in light mode. |
| D2 | Keep the page toolbar below the top bar and visible while only the main content scrolls. |
| D3 | Organize surfaces by content type: a shared white panel for lists/tables and independent cards for card grids. The B4 feedback supersedes larger Settings column panels with independent setting cards and related expandable groups (D15/D18). |
| D4 | Panels follow content height; do not stretch sparse lists or empty states to fill the remaining viewport. |
| D5 | Put primary actions and state switches on the toolbar's left; put counts, search, and filtering on its right. |
| D6 | Exclude both full-screen editors from this redesign. Preserve their focused layout and existing bottom actions. |
| D7 | Accept at 1440 x 900 and 1920 x 1080 CSS-pixel desktop viewports, in light/dark mode and all six existing UI languages, at 100% browser zoom only. During B0 the user explicitly removed zoom checks from the earlier scope. |
| D8 | Use 24px outer main-area/panel padding and 16px panel/toolbar gaps. The approved Settings revision uses 16px vertical/24px horizontal item padding and, after D23, 5px card gaps instead; preserve existing control sizes and toolbar wrapping. |
| D9 | Use 8px panel/card radii, 1px theme-aware borders, and no panel/card shadows. This applies only to in-scope admin surfaces, not excluded editors or public pages. |
| D10 | Keep the toolbar on the workspace background: light gray in light mode, with the corresponding dark-mode surface. Do not add a separate white toolbar card or retain the existing toolbar bottom divider. |
| D11 | Superseded by R1: page classifications are secondary navigation in the existing blue top bar, not controls mixed into the action toolbar. Actions stay in the fixed content toolbar; pages with no actions do not retain an empty toolbar. Ordinary settings still have no Save action. |
| D12 | Move Menu Save to the fixed top toolbar and remove the old bottom Save. Retain the existing explicit-save semantics and form association. |
| D13 | B0-B3 kept the existing development site read-only. During B4 the user explicitly confirmed that all local development data is test data and authorized changing/creating it without restoration. Local fixture writes are now permitted; this does not authorize production changes, deployment, or external email/service operations. |
| D14 | Card action footers keep the body surface and use inset separators aligned with the 24px body padding. Cards in the same row follow that row's tallest content and align their footer actions; do not stretch cards to the viewport. Preserve four columns for Pages/Categories and three for Widgets. |
| D15 | Supersedes the rejected whole-column B4 treatment: Settings use a left-aligned, maximum-width single column of independent Windows 11-style cards, with meaningful related options in expandable groups. Subscription follows this single-column layout. Theme previews remain a dedicated selector surface and Verification Files retains its table, within the same width limit. |
| D16 | Menu uses one white main panel for enablement and the menu list, retaining the nested menu/submenu hierarchy. Save moves to the fixed toolbar only, preserving explicit form submission. |
| D17 | R1 separates Appearance's theme New from classification navigation: keep New in the content action toolbar, with the original fewer-than-ten-user-themes condition and no duplicate panel button. All nine Settings classifications now belong in the top navigation. |
| D18 | Groups for enabled features initially expand; inactive groups initially collapse. Arrow buttons and setting controls remain independent: folding never saves/changes a setting, and toggling a setting does not automatically fold/unfold its group. Preserve immediate PATCHes and existing edit dialogs. Menu is not redesigned again with this Settings revision. |
| D19 | R1 covers four Post states, Pages/recycle bin, and nine Settings classifications (15 routes). Put secondary navigation inside the existing top bar, right of the site title and aligned with the main content, using text links, hover surfaces, and selected underlines instead of pills. Exclude editor/dialog tabs. |
| D20 | Secondary navigation stays on one line and scrolls horizontally when needed without displacing global/account controls. Keep the active destination visible after layout/font changes; preserve normal links, routes, and keyboard navigation. |
| D21 | B5 Dashboard fixes its existing title/overview plus New Post row in the common action region, with New Post retained on the right. The user explicitly chose this Dashboard-only exception to D5. Do not duplicate the title/header in the scrolling content. |
| D22 | B5 Account changes to the same Windows 11 Settings style, not the previously proposed large cards: left-aligned single column capped at 1120px, separate icon/title/description action rows, 8px gaps and 16px vertical/24px horizontal padding. Preserve full-row native-button activation, existing dialogs/bindings, and danger distinction; add no switches, disclosure groups, or authentication behavior. |
| D23 | On 2026-11-10 the user requested 5px spacing between Settings items. Apply it to adjacent independent cards and expandable groups; retain section spacing, internal padding, joined group rows, and Account's separate D22 8px action-row spacing. |
| D24 | On 2026-11-10 the user requested smaller toolbar buttons using `btn-sm`. Apply Bootstrap's small size to all ordinary-admin action-toolbar buttons and button-style links, including filters, conditional/bulk actions, Dashboard New Post, and Menu Save. Preserve content/dialog controls, navigation, and excluded editor toolbars. |
| D25 | On 2026-11-10 the user requested an Activity Logs table suited to the large white card: remove alternating row highlighting, allocate appropriate column widths, and use accent-colored Actions buttons. Keep existing data, filters, pagination, and detail behavior. |
| D26 | The user's reference feedback supersedes D25's generous cell padding and integrated footer: use a denser Activity Logs table and place pagination/page-size controls outside the white card. Prefer Bootstrap components/utilities over custom overrides and accept differences from the Fluent Web Components v3 reference; do not introduce Fluent or copy its pagination features. Keep D25's unstriped surface, column allocation, accent actions, and existing behavior. |
| D27 | The user found D26's native 40px rows and 31px pagination controls too small and explicitly authorized Bootstrap overrides. Increase sizing moderately with page-scoped styles, retaining external pagination and all behavior. The implemented intermediate sizes are roughly 44px ordinary rows and 35px pagination/page-size controls, without fixed row heights. |

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

- Present ordinary settings as Windows 11-style independent cards in one left-aligned, maximum-width column. Use expandable groups only for functionally related options, retaining useful section headings.
- Cover General, Content, Comments, Image, Subscription, Appearance, Notification, Advanced, and Verification Files; preserve the B2 file table treatment.
- Align settings navigation and optional page actions in the approved fixed header arrangement. Do not add empty action rows.
- Preserve field/edit-dialog identifiers, required-field relationships, conditional sections, validation, and immediate PATCH/save behavior.
- Align menu enablement, nested menu cards, ordering controls, and explicit Save using the B0 decision. Prevent any toolbar relocation from breaking submit/form association.
- Preserve theme previews, image/avatar dialogs, notification capability states, and code/text editors embedded in settings.

**Checks and acceptance**

- W and T pass. Run J when JavaScript changes; add/update meaningful owning tests only when bindings, form ownership, or other behavior needs regression coverage.
- Browser-check all nine tabs and menu management. Using authorized local test-data fixtures under D13, exercise an edit dialog, toggle, select/radio control, validation failure, and nested-menu save.
- Confirm one approved interaction still makes the same write as before; regrouping must not introduce duplicate event submissions or delayed settings writes.
- Verify enabled groups initially expand, inactive groups initially collapse, disclosure controls work by mouse/keyboard without writes, and changing a switch does not automatically change disclosure state.
- Do not expose configuration secrets in snapshots, console captures, logs, or this record.
- User accepts grouping, retained save semantics, and menu action placement. Suggested user commit subject: `Group admin settings into consistent panels`.

### B5 - Harmonize dashboard and auxiliary pages

**Technical dependency:** B1. **Start gate:** previous delivery gate closed and user authorizes B5.

**Work**

- Align Dashboard's metrics and business panels with the common surface rules; keep its useful multi-panel hierarchy and current data.
- Align its New Post placement with the common action region without inventing a redundant toolbar.
- Align Account and About surfaces without adding empty toolbar space or changing their information/action structure.
- Account's visual layout follows the user's later D22 Windows 11-style instruction; retain its existing action meanings, native buttons, dialogs, and authentication/reset behavior.
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
| B1 | B0 | Complete | W: 0 warnings/errors; J: 32 passed; browser checks passed; final T: 661 passed, 1 failed, 0 skipped, with the explicit B1 image-test exception | Accepted explicitly on 2026-11-09 | `e84a9cd570661a8b63b084240681e0c17192aaf3`, `Refresh admin shell and activity log layout`, confirmed from Git | Closed; user explicitly authorized B2 |
| B2 | B1 | Complete | W: 0 warnings/errors; T: 662 passed, 0 failed/skipped; J: 32 passed, 0 failed/skipped; browser evidence and unavailable fixture scenarios recorded below | Accepted explicitly on 2026-11-09 | `3580bf019dda97eaedf8ee792b936e597080ba45`, `Refresh admin list panels and toolbars`, confirmed from Git | Closed; user explicitly authorized B3 |
| B3 | B1 | Complete | W: 0 warnings/errors; T: 661 passed, 1 failed, 0 skipped, with explicit B3 image-test exception; J not required; 72 rendered route/culture/viewport/mode probes plus native-page checks; fixture gaps below | Accepted explicitly on 2026-11-09 | `5d5d39a1e85604cb769e59adee10eabeac25a2b0`, `Refresh admin page, category, and widget cards`, confirmed from Git | Closed; user explicitly authorized B4 |
| B4 | B1, B2 | Complete | Revised W: 0 warnings/errors; revised T: 663 passed, 0 failed/skipped; J not required; 240 fresh rendered samples passed, including disclosure independence, plus native/write checks below | Accepted explicitly on 2026-11-09 | `1dbcc3ee12e838b1a6c5c8d5b6106f339cef42a2`, `Refresh admin settings and secondary navigation`, confirmed from Git | Closed; user explicitly authorized B5 |
| R1 | B1, B2, revised B4 | Complete | W: 0 warnings/errors; T: 663 passed; J: 34 passed; 360 layout samples, 4 final overflow cases, 16 boundary samples, native dialogs/keyboard passed | Accepted explicitly on 2026-11-09 | Same confirmed user-created commit `1dbcc3ee12e838b1a6c5c8d5b6106f339cef42a2` as B4 | Closed; user explicitly authorized B5 |
| B5 | B1 | Ready for user acceptance | Final W: 0 warnings/errors; final T: 663 passed, 0 failed/skipped; J not required; 72 normal + 72 DOM-stress + 48 boundary samples passed, followed by final keyboard/hover/contrast checks; later D23 spacing refinement verified separately below | Pending explicit acceptance | User-created `da7dea0fabe8e6f0c33db20571d373eea06e1f21`, `Refresh admin dashboard and account surfaces`, confirmed from Git | User acceptance and explicit B6 authorization |
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
- 2026-11-09: User explicitly accepted B1 and authorized B2. Git confirms the user-created B1 commit `e84a9cd570661a8b63b084240681e0c17192aaf3` (`Refresh admin shell and activity log layout`). B2 starts on `master` with a clean worktree.
- 2026-11-09: B2 presentation changes, automated checks, and read-only browser checks completed. The current blog was not used for fixture writes. B2 is awaiting visual acceptance; no commit or B3 work has been performed.
- 2026-11-09: User explicitly accepted B2 and authorized B3. Git confirms the user-created B2 commit `3580bf019dda97eaedf8ee792b936e597080ba45` (`Refresh admin list panels and toolbars`). B3 starts on `master` with a clean worktree, aligned with `origin/master`.
- 2026-11-09: Before B3 application edits, the user confirmed D14: white card footers with inset separators and equal card heights within each content-driven row.
- 2026-11-09: B3 Web tests reported 661 passed, 1 failed, 0 skipped (exit 2). The unchanged `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` test failed at `ImageControllerTests.cs:88`. The user explicitly chose a B3-specific exception and continuation to visual acceptance. No image-code change or retry was performed; this is not a clean test run or B3 visual acceptance.
- 2026-11-09: B3 card styling, read-only browser checks, documentation, and cleanup completed. Fixture-only cases remain unverified. B3 awaits visual acceptance; no commit or B4 work has been performed.
- 2026-11-09: User explicitly accepted B3 and authorized B4. Git confirmed user commit `5d5d39a1e85604cb769e59adee10eabeac25a2b0` (`Refresh admin page, category, and widget cards`). B4 starts on clean `master`, aligned with `origin/master`.
- 2026-11-09: Before B4 application edits, the user confirmed D15-D17: one panel per existing settings business column, one Menu main panel retaining nested structure, and conditional theme New in the fixed settings toolbar.
- 2026-11-09: The user superseded D13's local read-only restriction: the existing local development environment contains test data, and AI may modify/create it without restoration. B4 may exercise real local settings/menu writes and fixtures; do not extend this permission to production, deployment, or external service operations.

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
- B1 visual acceptance: subsequently confirmed explicitly by the user on 2026-11-09, separately from the earlier test-exception decision. The user committed B1 as `e84a9cd570661a8b63b084240681e0c17192aaf3` and explicitly authorized B2. Image-test implementation scope remains excluded.

### B2 execution record - 2026-11-09

**Changes and boundaries**

- Added shared list-item separators, wrapping content/action groups, and integrated panel footers to the existing admin stylesheet. Panels retain 24px padding, 8px radii, theme-aware 1px borders, and natural content height; individual entries no longer have redundant card borders/shadows.
- Migrated published posts, drafts, scheduled posts, both recycle bins, comments, mentions, and tag-letter groups into containing panels. Kept selection inputs, status badges, reply hierarchy, IDs, event bindings, and scheduler-warning conditions.
- Kept existing loading/empty state predicates. List rows hide during loading; populated pagination footers remain visible so the focused page-size select survives refresh. Activity Logs now uses the same footer styling without changing its accepted geometry.
- Moved article/page primary actions beside the state navigation. Comments/mentions keep conditional bulk actions on the left and counts/Filter on the right. Counts reuse the existing localized `item(s)` resource.
- Tags retain editable chips and letter groups. A nonmatching filter now displays the existing localized `no items to show` message inside the panel, separately from the original first-tag invitation. No tag getter or write logic changed.
- Verification Files now has a containing table panel. Its New action precedes all nine settings links in one wrapping list within the fixed toolbar. The drawer, form ownership, table actions, and file handling remain unchanged.
- Scoped the existing empty-post illustration's height to its actual content inside panels, preventing its caption from overflowing. Wrapping content and responsive image bounds avoid clipping without truncating information.
- Updated the engineering reference. No JavaScript, controller/handler, database, configuration, localization-resource, dependency, or test source changes were needed. The Pages grid was not migrated early; only its shared toolbar composition changed. Other settings grouping, Menu Save, Dashboard/account/about, and both editors remain outside B2.

**Automated verification**

All commands ran from the repository root without installing dependencies.

| Check | Command | Result |
| --- | --- | --- |
| Initial W | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj` | Passed; 0 warnings, 0 errors |
| T | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed: 662, Failed: 0, Skipped: 0 |
| J | `node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | Passed: 32, Failed: 0, Skipped: 0 |
| Final W | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` | Passed; 0 warnings, 0 errors, after final-newline normalization |
| Formatting | `git --no-pager diff --check` and changed-file encoding/newline checks | Passed; UTF-8, existing BOMs preserved, CRLF, final newlines |

T passed on its B2 run; no image-test repair, skip, waiver, or retry-until-green was used. B1's historical intermittent failure and specific exception remain recorded above.

**Read-only browser evidence**

All checks used 100% zoom and real existing data. The baseline matrix covered these ten routes in English at both 1440 x 900 and 1920 x 1080, in light and dark modes: 40 route/mode/viewport combinations.

| Route | Existing sample | Panel height at 1440 / 1920 |
| --- | --- | --- |
| `/admin/post` | Four published posts, page size 4 | 728.11 / 641.67px |
| `/admin/post/draft` | Empty source, existing illustration/caption | 316.41 / 316.41px |
| `/admin/post/scheduled` | Empty source; scheduler enabled | 316.41 / 316.41px |
| `/admin/post/recycle-bin` | One post | 98.80 / 98.80px |
| `/admin/page/recycle-bin` | One page | 98.80 / 98.80px |
| `/admin/comments` | Five comments | 830.69 / 830.69px |
| `/admin/mention` | Ten mentions | 1066.28 / 1066.28px |
| `/admin/tags` | 24 letter groups | 2275 / 2118px |
| `/admin/activitylogs` | Eight rows | 536.61 / 536.61px |
| `/admin/settings/verification-files` | One file's metadata row | 182.11 / 182.11px |

- Every baseline combination had 24px panel padding, 8px radius, no row/panel shadows, a 16px toolbar-to-panel gap, and no document/body horizontal overflow or document vertical scrollbar. Light panels were RGB 255/255/255; dark panels were RGB 33/37/41. Settled panels had 25px bottom inset, including their 1px border.
- Verification Files additionally passed all six cultures at both sizes/modes: 24 combinations total, including the four English baseline cases. New and all nine settings links remained visible, with no overflow and the 16px content gap. All six fit one row at the supported minimum width. New drawers opened and closed in every culture; their width remained 560px. No existing verification-file contents were opened or copied.
- Real nonmatching post/comment/mention/tag filters kept empty results inside their panels. Comment/mention/tag empty panels measured 105.61px; the illustrated post empty panel measured 316.41px. Empty pagination footers had `display: none`.
- Filtering on an existing post title returned one real result: a 235.63px panel, hidden page links, and a visible right-aligned page-size control.
- An actual empty-source draft GET under Slow 3G produced a 71.61px loading panel, then returned to its contained illustration. Real populated page-size refreshes on posts, comments, mentions, and logs kept the select focused and the footer visible during and after loading. Old list rows hid while loading; no synthetic list data or fake success/failure states were injected.
- Real pagination reset only the content scroller: posts page 2 reset 1041px to 0; mentions page 2 reset 1300px to 0; comments' last page reset 5900px to 0 and rendered 33 rows on page 20. The toolbar stayed at y=77.19px. Scrolling to the end left the integrated footer inside the viewport, around y=851px at 900px height, with no clipping.
- Native checkbox/keyboard selection exposed left-aligned conditional bulk actions for comments and mentions. Their confirmation dialogs opened at 500px width and were cancelled. Page-recycle empty-all and post-recycle row-delete confirmations were also opened/cancelled, leaving their existing rows intact. Restore/publish/approval actions were not invoked.
- The blank reply form expanded within a comment without horizontal overflow (about 971px textarea width at 1440). One existing reply was checked in the first 40 comments; it was a single-line reply, not evidence for a multiline fixture. No reply content was entered or sent.
- Tags' New drawer opened/closed at 400px width without editing any contenteditable chip. Post filters remained 400 x 1080; comment/mention filters remained 400 x 900. Clear filters restored populated lists and zero active-filter counts.
- Boundary checks: Pages retained three separate cards and no outer panel, including at 1440 dark; its New/Export actions moved left. German General Settings retained nine links and no verification-only New action. Public home did not load admin CSS. Both blank editors had no ordinary-admin scroller or new list helpers, no viewport overflow, and their application Preview buttons remained at y=854px through 892px at 1440 x 900; article auto-save was off.
- No console errors/warnings were observed in the inspected interactive, localized verification, settings, grid, or editor checks. These samples do not claim the full B6 route/state/culture matrix.

**Unavailable scenarios and remaining gates**

- D13 remains in force: no separate local test instance/database has been agreed or provisioned, and the existing development blog stayed read-only.
- Nonempty drafts/scheduled posts, the disabled-scheduler warning's rendered state, empty recycle bins, empty/multiple verification-file rows, maximum-length/unbroken titles, and multiline replies were not naturally available in the samples. Existing post headlines up to 89 characters and a real single-line reply were checked; do not relabel them as maximum-length or multiline coverage.
- Browser write/error scenarios remain unverified: restore/delete/publish/moderation, submitted replies, tag edits, and verification-file saves/toggles/uploads require the agreed disposable setup. No real records, settings, or files were created/changed/deleted to obtain a visual fixture.
- The automated suites passed, but unavailable fixture states and the remaining cross-route culture/public-article checks are still later validation work, not assumed passed from W/T/J.

**Runtime and delivery**

- The final compiled host is running at `https://localhost:10210` from this checkout. Detached session: `admin-b2-final-host`; last verified listener PID: `23592`, with the expected Web Debug executable path and HTTP 200 readiness. Reverify ownership/current PID before stopping.
- Browser page 2 was returned to unfiltered `/admin/post`, page 1, page size 4, at 1920 x 1080. Theme preference remains `auto`, currently light. Temporary emulation/probes were removed, all nine additional tabs were closed, and no modal/drawer remains.
- Changes are uncommitted on `master`, based on `e84a9cd570661a8b63b084240681e0c17192aaf3`. No staging, commit, push, PR, branch change, deployment, or B3 work occurred.
- B2 visual acceptance was subsequently confirmed explicitly by the user. The user committed B2 as `3580bf019dda97eaedf8ee792b936e597080ba45` and explicitly authorized B3.

### B3 execution record - 2026-11-09

**Authorization and checkout**

- The user accepted B2 and explicitly authorized B3. Git confirmed B2 commit `3580bf019dda97eaedf8ee792b936e597080ba45` (`Refresh admin list panels and toolbars`) on `master`, aligned with `origin/master`, with a clean starting worktree.
- Before application edits, the user confirmed D14 through the clarification form: white action footers with inset separators and content-driven equal heights within each row.
- Only this tracker was dirty during initial inspection. No existing user edits were overwritten.

**Implemented scope**

- `admin.css`: added scoped card-grid/card/preview helpers. Grids have 16px gutters; independent cards have 24px body padding, 8px radii, theme-aware 1px borders, and no shadows. Same-row cards align their footers, whose inset borders, transparent background, 16px top padding, and 8px button gaps keep actions aligned with the body.
- `BlogPage.cshtml`, `Category.cshtml`, `Widgets.cshtml`: adopted those helpers while retaining four/four/three columns. Removed shadow utilities, redundant Page card margins, and fixed-height slug/note usage. Page headings may wrap around publication badges; meaningful text remains visible.
- Categories/Widgets now use the shared wrapping toolbar composition. Action-only anchors became explicit non-submit buttons; navigation remains anchors. Existing Edit/Delete resources label icon-only actions. Routes, bindings, IDs, sorting, badges, preview rendering, forms, and API behavior are preserved.
- `docs/agent-reference.md` records the reusable card convention. This retained tracker records decisions, evidence, remaining gates, and handoff.
- No JavaScript, C# business logic, tests, resource files, dependencies, configuration, schema, or database content changed. Settings/Menu, Dashboard/account/about, public pages, and both editors were not redesigned.

**Automated checks**

Commands ran from the repository root:

```powershell
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
git --no-pager diff --check
```

- W: exit 0, zero warnings/errors. The verified prior host PID `23592` was stopped before the build to avoid its file lock.
- T: exit 2; **Passed: 661, Failed: 1, Skipped: 0**, total 662. `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` failed `Assert.True` at `ImageControllerTests.cs:88`.
- That test was unchanged, failed intermittently in B1, and passed in B2. The user explicitly approved a **B3-only exception** after seeing this run's result. It was not retried, fixed, weakened, or treated as passing; the B1 exception was not silently reused.
- J: not required or run because no JavaScript module changed. B2's 32/32 remains historical evidence only.
- Whitespace checks passed. All six changed files were checked as valid UTF-8 with CRLF and final newlines, preserving existing Razor BOMs.

**Read-only browser evidence**

- All three actual Razor routes were rendered at 1440 x 900 and 1920 x 1080 CSS pixels, in light/dark mode, across `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, `ja-JP`, and `fr-FR`, at 100% zoom: **72 route/culture/size/mode samples**.
- The matrix used sequential, temporary same-origin frames sized to each accepted viewport, loading the real pages/modules/data and waiting for Alpine and images. Geometry/style probes checked each card, row, footer, preview, toolbar, and document; these were rendered-document checks, not 72 manually reviewed screenshots.
- Separate native-page screenshots were visually inspected for all three routes in both themes: Widgets at 1440 x 900, Categories at 1920 x 1080 light / 1440 x 900 dark, and Pages at 1920 x 1080 light / 1440 x 900 dark.
- All matrix probes passed: 16px toolbar/grid gaps; 24px body/inset-footer alignment; 8px radii/1px theme borders/no shadows; equal heights and footer positions within rows; preserved columns; no card/preview/workspace horizontal overflow, double document scrolling, or last-card clipping.
- Real samples contained three Pages, eleven Categories, and five Widgets. Page publication badges represented both states. Category descriptions up to 58 characters wrapped naturally; uneven descriptions kept row actions aligned. No sparse grid stretched to viewport height.
- Approximate row heights were Pages 216px; Categories 230/208/208px; Widgets 311/320px at 1440 and 371/320px at 1920. Only each row's content determined its height.
- All three existing widget types were present. The existing 1440 x 540 image fitted at approximately 340 x 129px at the narrow viewport; links and the existing one-button group remained within their card bodies. Normalized preview HTML matched the unchanged production renderer across the matrix.
- Categories: New opened with blank fields via Enter; Edit opened and loaded the matching record via Space. The 400px drawer, reachable Submit, and focus containment were checked at 1440 x 900 dark. Escape closed both without submission. Delete confirmation was opened and cancelled.
- Widgets: New and Edit for LinkList/ButtonLink/ImageLink opened and closed at 1920 x 1080 light and 1440 x 900 dark. The 500px drawer fitted, existing values/type content loaded, Submit stayed reachable, and no horizontal clipping occurred. Delete confirmation was cancelled; the final click-based check waited for Bootstrap's transition before dismissal.
- Pages: public/edit URLs and the New editor route were checked against existing IDs/slugs; publication badges remained correct. Delete confirmation opened and was cancelled. No editor save or export was invoked.
- Network checks for the native grid/drawer/confirmation interactions showed only successful GETs; no POST/PUT/DELETE was issued. No real records/settings/files were changed.
- Boundaries at 1440 x 900 dark: Posts and Activity Logs retained their single panels without new card helpers; General retained nine tabs and its toolbar stayed fixed during a real 57px content scroll. Menu still had its existing body Save, leaving the top-only move for B4. Both blank editors had no ordinary-admin scroller, toolbar, or new card helpers and fitted without document overflow. Public home had no admin stylesheet or new card helpers.
- A blank-editor frame emitted Chrome's user-gesture/beforeunload diagnostic during probe cleanup; no editor behavior was changed. The final normal Widgets navigation had no console errors/warnings.

**Unverified fixture cases and remaining risk**

- D13 remains in force: no disposable instance/database has been agreed or provisioned. Single-card and fully empty grids, very long/unbroken or maximum-length titles/slugs/notes, many-row scrolling grids, disabled Widgets, multi-button wrapping, and unusually sized/custom-class images were not naturally available and remain unverified fixture cases.
- Existing multi-line descriptions and all three preview types are real evidence, not proof of those unavailable edge cases. New/edit/delete confirmations do not prove create/update/delete submission or failure handling; browser writes still require the agreed disposable setup.
- The image test remains an unrelated unresolved test failure with the explicit B3 exception, not a repaired or green automated gate.

**Runtime and delivery**

- Host: `https://localhost:10210`, detached session `admin-b3-host`, last verified listener PID `24716`, expected Web Debug executable from this checkout, `/health/ready` HTTP 200. Reverify ownership/current PID before any stop.
- Only browser page 2 remains, on unfiltered `/admin/widgets`. Emulation was cleared, returning to the user's native 1920 x 889 viewport; theme preference remains `auto`, currently light. No temporary frames/helpers, dialogs, drawers, or backdrops remain.
- Six files are uncommitted on `master`: the shared CSS, three Razor pages, engineering reference, and tracker. No staging, commit, push, PR, branch change, deployment, or B4 work occurred.
- User acceptance: pending. User commit: not created. Permission to continue: B3 only; wait for explicit acceptance/continuation.

### 2026-11-09 - B4 execution and delivery

**Authorization and decisions**

- The user accepted B3 and explicitly authorized B4. Git confirmed B3 commit `5d5d39a1e85604cb769e59adee10eabeac25a2b0`, `Refresh admin page, category, and widget cards`; `master` was clean and aligned with `origin/master` at B4 start.
- D15-D17 were confirmed before implementation: retain business columns as independent content-height panels, retain Menu hierarchy inside one main panel, and move conditional Appearance New before the nine tabs.
- The user explicitly superseded the old read-only restriction for the existing local development environment: its data is test data and may be changed/created without restoration. No separate instance was required. This did not authorize production, deployment, external email, or third-party operations.
- After seeing the final test failure below, the user explicitly approved a **B4-only exception** and continuation to visual acceptance. B1/B3 exceptions were not reused.

**Implemented scope**

- Added shared settings grid/panel/detail helpers in [admin.css](../../src/Moonglade.Web/wwwroot/css/admin.css): existing columns become larger 24px-padded panels with 16px gaps, 8px radii, theme-aware 1px borders, no shadows, and fine internal row separators instead of independent row cards.
- Updated General, Content, Comment, Image, Subscription, Appearance, Notification, and Advanced under [Settings](../../src/Moonglade.Web/Pages/Admin/Settings). Internal headings/order and control names, identifiers, constraints, and dependency attributes were preserved. Subscription remains half-width; Verification Files retains its B2 table panel.
- Moved Appearance New into [_SettingsHeader.cshtml](../../src/Moonglade.Web/Pages/Admin/Settings/_SettingsHeader.cshtml), preserving the fewer-than-ten-user-themes condition and existing limit notice; ordinary settings still have no global Save.
- Updated [Menu.cshtml](../../src/Moonglade.Web/Pages/Admin/Menu.cshtml) and its scoped CSS: one main panel, theme-aware nested cards, wrapping guards, and the sole page Save first in the fixed toolbar. The native submit explicitly targets `form-settings`, keeps loading disablement, and retains `saveSettings()`; dialog draft forms remain separate.
- Added [MenuPageTests.cs](../../src/Tests/Moonglade.Web.Tests/Pages/Admin/MenuPageTests.cs). `ToolbarSave_AssociatesWithMenuForm_WithoutChangingSubmitBinding` executes the real compiled Razor page and toolbar section, checking native form ownership, the single page submit, loading disablement, and unchanged page/dialog submit bindings.
- Updated the engineering reference with the shared settings/Menu contract. No production C#, JavaScript module, dependency, configuration file, provider, or schema changed.
- The first narrow/light matrix exposed a real 2px Alpha range-row overflow in all six cultures. Removed only its redundant nested Bootstrap grid wrappers, rebuilt, and verified the corrected layout in the final matrix.

**Automated checks**

Commands ran from the repository root:

```powershell
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --filter-class '*MenuPageTests'
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
git --no-pager diff --check
```

- Focused Menu regression: exit 0; **Passed: 1, Failed: 0, Skipped: 0**.
- Initial full W/T: build exit 0 with zero warnings/errors; test exit 0, **Passed: 663, Failed: 0, Skipped: 0**.
- Final W after the range-wrapper/corner cleanup: exit 0, zero warnings/errors.
- Final T: exit 2; **Passed: 662, Failed: 1, Skipped: 0**, total 663. The sole failure was unchanged `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage`, `Assert.True` at `ImageControllerTests.cs:88`. The new Menu regression passed.
- The final failure remains real and unresolved, with the explicit B4-only exception. It was not retried until green, repaired, weakened, or replaced by the initial successful run.
- J was not required or rerun because production JavaScript modules did not change. Prior batches' 32/32 results are historical only.
- Final whitespace/encoding checks cover all fifteen B4 files: valid UTF-8, CRLF, final newlines, and retained existing BOMs.

**Final rendered and native-page evidence**

- All ten routes (nine settings tabs plus Menu), six cultures (`en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, `ja-JP`, `fr-FR`), two accepted sizes (1440 x 900 and 1920 x 1080), and both themes at 100% zoom: **240 final rendered-document samples, zero probe failures**.
- Sequential temporary same-origin frames loaded the actual Razor pages/modules/current local data, waited for loading/fonts/layout, and were removed afterward. These were geometry/style checks, not 240 manually reviewed screenshots.
- Checks covered expected panel counts, content-driven heights, 24px padding, 16px grid/toolbar gaps, 8px radii/1px borders/no shadows, row treatment, nine tabs, conditional Appearance New/order, Menu Save/form association, fixed toolbar during content scrolling, horizontal overflow, and final-panel reachability.
- Final Alpha range rows had no overflow in any combination. At 1440 x 900, General/Content/Advanced required about 89/52/93px of main-body scrolling; Menu with fixtures required 156px. Toolbars stayed fixed. Wide-view panels stayed content-height rather than stretching.
- Native screenshots were separately inspected: Appearance in German at 1440 x 900 light, Menu at 1440 x 900 light and dark, Image at 1440 x 900 dark, Content at 1440 x 900 light, and Subscription at 1920 x 1080 light. Theme previews, nested hierarchy, long fixture labels, sparse panels, and field/button spacing remained usable.
- General favicon/avatar dialogs opened and closed at 1440 x 900 dark; file inputs remained present, dialogs fitted, focus stayed inside and returned to their triggers. No file was uploaded.
- Embedded Content HTML editor opened at 1920 x 1080 light; Appearance CSS editor opened at 1440 x 900 dark. Real CodeMirror editable surfaces and accessible labelling were present; dialogs fitted and settled-state close returned focus. No editor content was saved.
- Appearance New opened the existing theme dialog without creating a theme. Verification Files New opened/closed its existing 560px drawer at 1440 x 900 dark, with fitting bounds/focus containment; no verification file was created.
- Notification's current `Available` capability state and enabled Send button fitted the panel; the button was not invoked. No test email was sent and no external capability/service state was changed.
- Boundary probes found no settings helper leakage into Posts, Activity Logs, Categories, Widgets, or the two blank full-screen editors. Lists retained their panels; card pages retained independent cards. Both editors still lacked the ordinary-admin toolbar/scroller. Public home had no admin stylesheet/settings helpers; its ordinary vertical scrolling was not treated as an admin regression.

**Actual local write checks**

- Temporary fetch observation delegated unchanged requests and recorded only safe method/status/approved payload fields, restoring the original function afterward. Independent fresh rendered GETs checked persistence where noted.
- Content `DefaultEditor` Html -> Markdown -> Html and `EnableViewCount` true -> false -> true each produced exactly one PATCH per interaction, HTTP 204, containing only the edited field; fresh pages confirmed the changed states.
- `PostListPageSize` above maximum 30 stayed invalid in the dialog and issued zero PATCHes. Saving 10 -> 11 produced one PATCH 204; a fresh page confirmed 11. Restoring 10 produced one PATCH 204.
- `PostTitleAlignment` Left -> Center -> Left produced one PATCH 204 per interaction, only that field; a fresh page confirmed Center.
- Comment provider BuiltIn -> ThirdParty -> BuiltIn produced one PATCH 204 each, only `CommentProvider`. Built-in/third-party sections switched correctly without overflow. Existing settings values used in these checks were restored voluntarily, not because D13 required restoration.
- Created two real local root-menu fixtures through the existing dialogs, including a 96-character label and one long nested child, all fixture URLs `/page/about`. Up/down ordering round-tripped. Draft additions/reordering and enablement changes produced zero POSTs before explicit Save.
- Two deliberate top Saves produced exactly two native page submit events and two POSTs, both HTTP 204, with only `isEnabled` and `menuJson`. The first persisted the fixture graph/disabled state, confirmed by a fresh GET; the second restored enabled=true. Reload confirmed the graph.
- The local fixtures remain: `B4 Visual Refresh ` plus six repetitions of `LongMenuLabel`, `B4 Ordering Check`, and child `B4 nested entry ` plus six repetitions of `NestedLabel`. Final graph: four roots/one child, serialized JSON length 685. These are authorized existing-environment test data, not isolated-instance fixtures.

**Limitations and delivery**

- This is not an exhaustive settings/backend/provider test. Alternative Notification capability states, real email delivery, avatar/favicon uploads, theme creation/deletion and the ten-theme limit transition, verification-file writes, and every dependent-field validation path were not exercised. Existing conditions/control attributes/modules were preserved; do not claim those unexecuted states passed.
- The unchanged image-original-storage test remains an unrelated unresolved failure with the explicit B4 exception. User visual acceptance is still required.
- Final host: `https://localhost:10210`, detached session `admin-b4-final-host`, last verified listener PID `21636`, expected Web Debug executable, `/health/ready` HTTP 200. Verify current ownership/PID before any later stop.
- Browser page 2 is left on Appearance for review, with viewport/theme emulation cleared and auto theme preference retained. No test frames/helpers/fetch wrappers, dialogs, drawers, or backdrops remain. The named session-only class-migration helper was removed.
- Fifteen B4 files remain uncommitted on `master`. No staging, commit, push, PR, branch change, deployment, or B5 work occurred.
- User acceptance: pending. User commit: not created. Permission to continue: B4 only; stop here.

### 2026-11-09 - B4 Settings revision and revised delivery

- The user rejected the larger Settings column panels and supplied a Windows 11 Settings reference: mostly separate item cards, with disclosure arrows for related options. This is a revision within B4, not acceptance or B5 authorization.
- Before editing, the user confirmed a maximum-width left-aligned single column, initially expanded groups for enabled features, and the proposed functional grouping/spacing/special-page exceptions. The implementation uses a 1120px maximum width, 8px card gaps, and 16px vertical/24px horizontal item padding.
- Approved expandable groups: General Dublin Core/license and profile/avatar; Content footer/HTML and Call-out/HTML; Comment provider-specific controls and word-filter mode/keywords; Image CDN/endpoint and watermark details; Appearance custom CSS; Notification email details and notification-event options.
- Independent settings remain individual cards. Theme previews and the verification-file table retain purpose-built surfaces. Current save semantics, field attributes, modal editors, conditional sections, and toolbar actions remain; no new frontend dependency or production module was introduced.
- Updated all nine Settings tabs and shared CSS. General, Content, Comment, Image, and Notification each have two meaningful expanders; Appearance has one. Subscription and Advanced retain independent cards without artificial disclosures. Theme previews and the file table use their dedicated surfaces within the same cap.
- A source-level check against HEAD confirmed that all eight ordinary pages retain the complete multiset of input/select/textarea attributes, despite moving related options next to their parent controls. Div counts are balanced. No production JavaScript or backend code changed.
- The first narrow/light probe identified incorrectly nested trailing General widget rows. Corrected the closing-tag placement, rebuilt/retested, and reran the complete final matrix. Simple tag counts alone were not treated as visual proof.

**Fresh automated checks**

```powershell
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
git --no-pager diff --check
```

- Initial revised W/T: exit 0, build zero warnings/errors; **Passed: 663, Failed: 0, Skipped: 0**.
- Final revised W/T after the General nesting correction: exit 0, build zero warnings/errors; **Passed: 663, Failed: 0, Skipped: 0**, including the retained compiled-Razor Menu test.
- These are fresh results for the revised source. The earlier image-test failure/exception remains historical; it was not needed or used to waive any final revised test.
- J was not required or rerun because production modules did not change. No extra unit test was added merely to assert CSS classes; real Bootstrap/Razor disclosure behavior was checked in the browser.
- Final source/format checks cover sixteen B4 files: UTF-8/CRLF/final newlines, existing BOM preservation, whitespace, and retained input/select/textarea attributes.

**Fresh rendered and native-page checks**

- **240 fresh route/culture/viewport/theme samples, zero failures**: all nine settings tabs plus Menu, six existing cultures, 1440 x 900 and 1920 x 1080, light/dark, 100% zoom.
- Temporary same-origin frames loaded real Razor/modules/data and checked the 1120px left-aligned cap, toolbar gap/fixed position, card padding/borders/radii/gaps, tabs/actions, horizontal overflow, absence of nested setting rows, and final-content reachability.
- Every one of the eleven disclosure groups was checked in each culture/size/theme combination: initial state against its feature state, then fully collapsed and expanded states, matching `aria-expanded`/targets/labels, no settings-state changes, and **zero write requests from disclosure clicks**. This is rendered-document/interaction evidence, not 240 manually reviewed screenshots.
- Separate native screenshots were inspected for General and Image at 1440 x 900 light, Comment at 1440 x 900 dark/German, Appearance at 1440 x 900 dark/French, and Content at 1920 x 1080 light. The dedicated theme gallery and file table remain intentional exceptions to ordinary item cards.
- Native Enter collapsed the footer group with visible keyboard focus; Space expanded it. General favicon/avatar, Appearance New, and the HTML/CSS edit dialogs fitted, contained focus, closed, and returned focus to their triggers. No file was uploaded or theme created.
- Verification Files New opened/closed its existing 560px drawer within bounds. Notification's current `Available` state and enabled Send button fitted; Send was not invoked. No external email/service operation occurred.
- Boundary checks retained Posts/Activity Logs list panels, Categories/Widgets independent cards and 24px bodies, and both editors' separate toolbar/scroller branch. Public home had no admin stylesheet or Settings helper leakage.

**Fresh actual writes and local fixture cleanup**

- Footer header switch true -> false -> true while its group was collapsed: exactly one PATCH 204 per interaction, only `ShowPostFooter`. The current group stayed collapsed both times. Fresh GETs confirmed false/initially-collapsed and true/initially-expanded, demonstrating both persistence and the approved reload rule.
- `PostListPageSize` 31 failed native range validation, left the dialog open, and issued zero PATCHes. Saving 11 and restoring 10 each issued one PATCH 204; the dialog closed and focus returned.
- `DefaultEditor` Html -> Markdown -> Html issued one PATCH 204 per interaction, only that field.
- Comment BuiltIn -> ThirdParty -> BuiltIn issued one PATCH 204 per interaction, only `CommentProvider`. Changing provider while collapsed did not expand the group; manually expanding exposed only the correct provider's controls. Switching back preserved the user's expanded state.
- Temporary request observers delegated unchanged fetch calls, recorded only approved scalar fields/status/key lists, and restored the original function in `finally`. Revised Settings checks restored their original scalar values.
- A public-boundary probe found horizontal overflow caused by the **own earlier 96-character B4 menu fixture**, not admin CSS. Removed only the two exact B4 root-menu fixtures (including their fixture child) through the real delete-confirmation UI. Draft deletion produced zero POSTs; one explicit top Save produced one POST 204 with only `isEnabled`/`menuJson`.
- Fresh GETs confirmed the original two root menus, zero fixture labels, enabled=true, and no public/admin horizontal overflow at 1440px. No public CSS/business code was changed. The earlier log's retained four-root/one-child fixture state is historical and no longer current.

**Revised delivery and limitations**

- Disclosure states are deliberately per-render/per-page only, not a saved preference. Arrow clicks do not save; switches do not automatically fold/unfold. Required-field/edit-modal logic remains the existing module's responsibility.
- Alternative email capability states, real email delivery, image uploads, theme creation/deletion/ten-theme transition, verification-file writes, and every server/network failure/dependency path were not exercised in this revision. Do not equate preserved code/attributes with those unexecuted states passing.
- Final host: detached session `admin-b4-settings-revision-final-host`, last verified listener PID `29612`, expected Web Debug executable, readiness HTTP 200. The earlier hosts were stopped for builds.
- Browser page 2 is left on General at the top, with emulation cleared/auto theme retained, and no temporary frames/helpers/fetch wrappers or overlays. Local fixture menus were cleaned up as above.
- Sixteen B4 files remain uncommitted on `master`. User acceptance/commit are pending; no staging, push, PR, branch change, deployment, or B5 work occurred.

### 2026-11-09 - R1 secondary navigation ready for user acceptance

- The user requested redesigning page classifications as top secondary navigation instead of tabs mixed with action buttons. Before editing, the user confirmed the existing blue top bar, one-line independent horizontal scrolling, and text/underline styling for all three identified groups.
- Added an ordinary-admin `adminnavigation` section and three navigation partials for 15 routes. Existing action partials now contain only actions/count/filter controls; ordinary Settings with no action render no empty toolbar.
- The new small admin-only navigation module keeps the selected anchor visible after fonts and navigation-width changes and fully reveals keyboard-focused links. Links retain their existing localized labels, destinations, and ordinary navigation semantics. Actions remain inside the owning main Alpine scope, not the global header.
- The first narrow/light matrix exposed a Japanese sidebar min-content expansion of approximately 1px, breaking exact alignment. The sidebar now has `min-width: 0`; secondary-nav items explicitly reset inherited right margins. Fresh layout verification after those CSS fixes passed.
- W: `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed with zero warnings/errors. T: `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` passed 663/663, zero failures/skips, after all Razor wiring changed. Subsequent corrections were CSS/JavaScript-only, so T was not unnecessarily repeated.
- J: the final `node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` passed 34/34, zero failures/skips. Focused `node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\AdminNavigationTests.mjs` passed both exact named cases:
  - `selected navigation stays visible after fonts load and navigation resizes`: verifies the observed navigation element, no premature font-ready scroll, and exact nearest-axis reveal calls for font readiness and resize.
  - `keyboard focus reveals the focused link rather than the selected link`: verifies separate selected/unselected anchors and exact nearest-axis reveal targets/options.
- Layout matrix: 15 real routes x six cultures x 1440x900/1920x1080 x light/dark = 360 fresh rendered samples, all passed. Checked ordered destinations/current-route semantics, text/underline styling, alignment, single-line navigation, global controls, no document/content horizontal overflow, fixed header/action-toolbar positions while body-scrolling, dedicated Settings width, and no empty action toolbar. Normal labels fit at both approved sizes; overflow was therefore exercised separately rather than claimed from this matrix.
- Native screenshots inspected Posts, Pages, and General in both themes. Actual keyboard Tab/Enter followed Published to Drafts with the visible white focus outline and correct new active route. The Post filter, Appearance New modal, and Verification Files New drawer all opened from the retained main-area controls; closing them performed no writes.
- Overflow stress initially exposed a native browser focus scroll that left the far edge of a long focused link clipped. Added explicit focus reveal, then reran the focused/full J checks and W. Four fresh native stress cases on the resulting host passed at both approved sizes/themes: French labels repeated four times plus long temporary brand/account text stayed single-line, scrollable, and isolated from global actions; selected links remained fully visible through resizing, brand/account text ellipsized, and document scrolling/overflow stayed zero. Actual Tab to the last link at 1440/light now reveals it fully with visible focus. Manual horizontal scrolling remains undisturbed. These are temporary DOM fixtures, not real translations/account or blog-setting changes, and were removed.
- Boundary checks on the final host: 16 fresh 1440/dark rendered samples (English/Japanese) across Menu, Tags, Dashboard, Account, About, both blank editors, and the public homepage passed. No top-navigation/module leakage or horizontal overflow; Menu retains its sole native toolbar Save, and both editors retain their excluded layout branch.
- All 30 scoped changed/new files retain valid UTF-8, CRLF, final newlines, and their existing BOM choices. `git diff --check` passes. No dependencies, resource keys, routes, business handlers, or production settings changed for R1; no external sends/services, destructive actions, or local data writes were required.
- Current final host: `https://localhost:10210`, detached session `admin-r1-acceptance-host`, last verified listener PID `34016`, expected Web Debug executable, readiness HTTP 200. Earlier recorded hosts were stopped for builds; verify current ownership before stopping.
- Existing B4 changes remain preserved and uncommitted. Total worktree scope is 30 files (24 modified, six untracked including the earlier B4 Menu test). R1 is an authorized cross-batch correction, not B4 acceptance or B5. Stop for user acceptance; the user owns commits.

### 2026-11-09 - B4/R1 accepted; B5 baseline and design gate

- At 20:27 +08:00 the user accepted the prior delivery and explicitly requested B5. Confirmed accepted work in user-created commit `1dbcc3ee12e838b1a6c5c8d5b6106f339cef42a2` (`Refresh admin settings and secondary navigation`). B5 starts on clean `master`.
- B5 remains limited to Dashboard, Account, and About presentation; preserve all data, links, forms, authentication behavior, and the accepted B1-B4/R1 surfaces. B6 is not authorized.
- Read the three Razor pages, Dashboard/About isolated CSS, Account shared CSS and dark overrides, Dashboard model/module, and existing Account command regression context. The Dashboard module only formats UTC labels and need not change.
- Fresh 1440x900/light baseline: Dashboard has a scrolling title/overview/New Post header, four metric cards, Posts/Taxonomy panels, empty recent drafts, and two recent published entries. Account has two large centered action cards with shadows and 32px spacing. About has two groups containing 4 and 11 rows; headings sit outside their 8px-padded white table surfaces.
- Two material layout choices need user confirmation: how to place Dashboard's New Post in the fixed action region without a redundant header, and whether Account's existing large action cards move to the content's top-left or retain their centered placement. About can preserve its groups and bottom legal/report actions while putting each heading/table inside a common padded panel.
- The user answered: fix the existing Dashboard title/New Post row, allowing New Post on the right; redesign Account in the same Windows 11 Settings style. These supersede the pending alternatives above and are recorded as D21/D22. Implementation may now proceed; no further authentication scope was authorized.
- Browser reconnected with new page ID 17 and lost its sign-in session. The authorized README default local sign-in succeeded; no credentials/configuration were changed or copied into this record. No account actions or local content writes were performed.
- At this gate only this tracker changed. B5 application edits, build/tests, and acceptance remain pending.

### 2026-11-09 - B5 ready for user acceptance

**Changes and boundaries**

- Starting checkout remains `master` at `1dbcc3ee12e838b1a6c5c8d5b6106f339cef42a2`. B5 changes six application files: `Dashboard.cshtml`, `Dashboard.cshtml.css`, `LocalAccount.cshtml`, `About.cshtml`, `About.cshtml.css` under `src/Moonglade.Web/Pages/Admin`, and `src/Moonglade.Web/wwwroot/css/admin.css`. This tracker and `docs/agent-reference.md` complete the eight-file scope.
- Dashboard's original title/overview and New Post now occupy the fixed action region. New Post remains on the right, as explicitly selected in D21. Four metric cards and four business panels reuse the common surface rules, with 16px gaps, preserved column proportions, and recent-list separators. Counts, destinations, data queries, empty-state wording, and UTC formatting are unchanged.
- Account follows D22: a left-aligned 1120px-capped Settings column with two independent native action rows, icon/text/chevron order, 8px gaps, 16px vertical/24px horizontal padding, and no shadows. Original dialogs, forms, validation, action bindings, and authentication behavior remain unchanged. Added dialog associations and inset keyboard outlines; danger styling remains distinct.
- About keeps its two information groups and all 4 + 11 rows. Each heading and responsive table now shares a standard 24px-padded panel; the group gap is 16px. Existing values, monospace wrapping, legal dialog, and bottom Legal/Report actions remain intact.
- No production JavaScript, resources, dependencies, configuration, handlers, database schema, or test source changed. Accepted Settings/Menu/navigation surfaces and excluded editors were not redesigned. No account actions, external sends, or local content/settings writes were invoked for B5.

**Verification and corrections**

| Check | Exact command | Final result |
| --- | --- | --- |
| W | `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` | Exit 0; 0 warnings, 0 errors; 1.66s |
| T | `dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Exit 0; Passed: 663, Failed: 0, Skipped: 0; total 663; 12.025s |
| J | Not run | Not required: no production JavaScript changes |
| Formatting | `git --no-pager diff --check` and changed-file encoding/newline checks | Eight files pass UTF-8/CRLF/final-newline checks; existing BOM choices preserved |

- Initial W/T also passed. The final commands above ran after both the decorative Account markup correction and the final theme-color correction; no image-test exception, skipped tests, dependency installation, or retry-until-green was needed.
- A focused comparison against HEAD confirmed 16 unchanged Account form/input/label/button contract entries, excluding the two intentionally restyled action buttons. A separate comparison confirmed both original `x-on:click` action bindings unchanged.
- Stress inspection exposed an incorrectly ordered second-row chevron that assigned Account text to a 24px column. Corrected both rows to icon/body/chevron and strengthened checks to require the body in the second grid column, chevron last, 982px text width at 1120px row width, and bounded stress-row height.
- Native keyboard inspection then exposed insufficient dark-mode contrast in the new focus styling. Replaced fixed accent colors only in the affected B5 rules with Bootstrap's theme-aware link-hover/danger-emphasis colors. Final Dashboard focus text/outline contrast is 5.43:1 or better across both viewports/themes; Account action titles are 6.10:1 or better in the checked focus/hover states, and descriptions are 6.78:1 or better. These are focused measurements, not a whole-portal accessibility audit.

**Browser evidence**

- Normal layout matrix: three routes x six cultures x two approved viewports x light/dark = 72 rendered samples, all passed after the Account order correction. Verified fixed Dashboard header ownership/right-side New Post, four metrics/four business panels, common padding/radius/border/no-shadow rules, Account column/order/width/dialog associations, About group/row counts, and no document/body horizontal overflow or empty Account/About toolbar.
- DOM-only stress matrix: the same 72 combinations passed. Dashboard used six temporary recent rows with 128-character unbroken titles, a cloned empty state in the other recent panel, and 19-digit counts. Account used long temporary titles/descriptions; About used a synthetic long Windows path in the Directory cell. Verified wrapping, actual main-body scrolling, reachable final content/actions, and an unmoving Dashboard toolbar. English scroll distances at 1440/1920 were Dashboard 697/319px, Account 413/233px, About 863/333px.
- These stress cases prove rendered layout, not saved translations, database fixtures, or backend query limits. In particular, six recent rows exceed the normal Dashboard query result limit. Real baseline sections contained empty drafts and two recent published entries. All temporary frames and substitutions were removed.
- Boundary matrix: 12 English routes x both approved sizes/themes = 48 samples, all passed: Posts, Pages, General, Appearance, Verification Files, Menu, Categories, Activity Logs, both blank editors, public home, and a real same-origin public article. Checked retained secondary-navigation/current-route semantics where applicable, no empty General toolbar, sole native Menu toolbar Save, excluded editor branches, no public admin-style leakage, and no horizontal overflow.
- The 192 layout/stress/boundary samples preceded the final color-only refinement. The acceptance host then passed eight Dashboard focus-color samples across both sizes/themes and native Account focus/hover checks in both themes; no geometry changed in that refinement.
- Actual Tab/Enter followed the Dashboard Published count to Posts and New Post to the existing blank editor without saving. Native Tab reached count tiles and recent links with visible unclipped focus. Final native screenshots inspected all three pages in both themes; About's 15 values were temporarily hidden for screenshots, then restored without copying server metadata.
- Actual Tab moved through both Account rows with inset focus and correct danger distinction; actual pointer hover exercised both rows in both themes. Neither action was activated and no Account dialog opened.
- About's Legal action opened by keyboard; its 800px modal stayed within 1440x900, focus entered it, Tab reached its close control, and Escape closed it and returned focus. Report's existing destination was checked without opening GitHub. Final native Dashboard/Account/About console samples had no errors, warnings, or issues.

**Delivery gate and runtime**

- B5 is ready for user acceptance, not accepted or committed. B6's complete integrated matrix remains a separate, unauthorized batch. No full-solution/provider tests or account-reset behavior were run for this presentation-only scope.
- The accepted R1 host and earlier B5 hosts were stopped only after verifying ownership, for local builds. The current detached host is `admin-b5-acceptance-host`, `https://localhost:10210`, listener PID `36752`, expected Web Debug executable, readiness HTTP 200. Reverify ownership before any later stop.
- Browser page 17 is signed in on Dashboard with viewport overrides cleared, automatic theme restored, and 100% zoom. No temporary helpers, frames, masks, overlays, or backdrops remain.
- No staging, commit, push, PR, branch change, deployment, or B6 work occurred. The user owns acceptance and commits.

### 2026-11-10 - Settings spacing refinement

- The user requested reducing Settings item gaps to 5px, with a General Settings screenshot identifying the inter-card space. D23 supersedes the earlier 8px Settings spacing, not internal padding or section spacing.
- Confirmed a clean worktree on `master` at user-created commit `da7dea0fabe8e6f0c33db20571d373eea06e1f21` (`Refresh admin dashboard and account surfaces`), containing B5. This confirms the commit, not explicit B5 acceptance or B6 authorization.
- Updated the shared Settings item margin and expandable-group margin to 5px. Nested rows retain zero margins and separators; Account explicitly retains its D22 8px spacing. No forms, behavior, or JavaScript changed.
- Updated the durable frontend guidance and current decision record. `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed, exit 0, zero warnings/errors, 3.59s. T/J were not repeated for this CSS-only spacing refinement.
- Browser verification: nine Settings routes plus Account x 1440x900/1920x1080 x light/dark = 40 English rendered samples. Each combination measured 30 actual adjacent Settings pairs at exactly 5px, plus Account's pair at 8px; expandable-group margins were 5px and their nested rows retained zero margins. Internal item padding stayed 16px/24px, section margins stayed 24px, and no document/content horizontal overflow occurred. Verification Files retained its table and had no item-card pairs. This focused check does not repeat the earlier six-culture matrix.
- The previous host was no longer listening. Started the authorized local review host using `dotnet run --project .\src\Moonglade.Web\Moonglade.Web.csproj --no-build --no-restore`, detached session `admin-settings-spacing-host`, verified listener PID `22168` and expected Web executable, readiness HTTP 200. Used the authorized README default sign-in without changing credentials or content.
- Browser page 1 is left on General Settings with automatic theme/native viewport restored. Temporary frames/helper were removed. Changed-file UTF-8/CRLF/final-newline and `git diff --check` validation passed. This refinement is ready for review, not B6; no commit or data writes were performed.

### 2026-11-10 - Compact action-toolbar buttons

- The user requested making toolbar buttons one size smaller using `btn-sm`. Confirmed a clean `master` checkout at user-created commit `64ee2314927571cbf1efabad7a78ff11a4bfbe19` (`Reduce admin settings item gaps to 5px`), containing the prior D23 refinement. This confirms its commit, not explicit B5 acceptance or B6 authorization.
- Added `btn-sm` to 23 toolbar button/button-link declarations across 11 Razor files: shared Post/Page/Settings action partials, Dashboard, Comments, Mention, Activity Logs, Category, Menu, Tags, and Widgets. Shared partials cover all Post/Page states and both Settings routes with New actions. Kept native links, form ownership, Alpine bindings, conditional visibility, danger styles, counts, and search inputs unchanged.
- Updated the durable frontend guidance and decision record. No CSS override, production JavaScript, dependency, localization, controller, or handler change was needed. Content/dialog buttons and both excluded editors remain unchanged.
- W: `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed, exit 0, zero warnings/errors, 7.16s. T/J were not repeated for this class-only presentation refinement. A source comparison against HEAD confirmed that the 11 Razor files differ only by 23 added `btn-sm` classes.
- Browser verification: 16 toolbar routes x 1440x900/1920x1080 x light/dark = 64 English rendered samples. Each combination checked 31 rendered toolbar controls, including currently hidden bulk/recycle actions. Every control had `btn-sm`, 14px computed font size, and 4px vertical/8px horizontal padding; no document/content horizontal overflow occurred. Menu Save retained its native `form-settings` ownership, and Dashboard New Post retained its destination. No action was activated or data written; this does not repeat the earlier six-culture or interaction matrix.
- Stopped the verified local host PID `22168` for Razor compilation, then started detached session `admin-toolbar-small-host` with the existing no-build/no-restore run command. Verified listener PID `16944`, expected Web Debug executable, readiness HTTP 200.
- Browser page 1 is left on Posts with automatic theme/native viewport restored. Temporary frames/helper were removed. All 13 changed files retain valid UTF-8/CRLF/final newlines and original BOM choices; `git diff --check` passes. Ready for review; no staging, commit, or B6 work occurred.

### 2026-11-10 - Activity Logs table refinement

- The user requested removing striped rows, redistributing column widths, and accent-colored row actions. Started on `master` at `64ee2314927571cbf1efabad7a78ff11a4bfbe19` with the 13 pending D24 files; preserved those toolbar changes. D25 is a focused refinement, not B6.
- Updated only the Activity Logs table markup and added `ActivityLogs.cshtml.css`. The table/header share the card's theme-aware surface, with subdued headers, subtle separators/hover, 12px vertical cell padding, and edge content aligned to the card inset. Fixed Time/Actor/Actions at 14rem/11rem/6rem; Activity receives remaining space. Added column-header scope and unrestricted wrapping for long actor/activity values.
- Detail buttons now use existing `btn-outline-accent btn-sm`; unchanged bindings and drawer associations preserve their behavior. The page-local CSS does not affect About, Verification Files, or other tables. Integrated pagination, loading/empty states, filters, and toolbar danger semantics remain unchanged.
- W: `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed, exit 0, zero warnings/errors, 4.39s. T/J were not repeated for this presentation-only markup/CSS change; no production JavaScript or backend code changed.
- Browser matrix: six cultures x both approved sizes x light/dark = 24 normal samples plus 24 DOM-only long-content samples. All passed. Column widths were 224/176/656/96px at 1440 and 224/176/1136/96px at 1920. Verified flat card/header/cell surfaces, no alternating row paint, accent small detail buttons, wrapping without cell/document/content horizontal overflow, reachable footer, and fixed toolbar during long-content scrolling.
- Stress fixtures temporarily inserted a 160-character unbroken actor suffix and eight repetitions of a synthetic summary containing 128 unbroken characters. They were DOM-only, not persisted history or translation changes; each frame was removed. Native light/dark screenshots were reviewed.
- Native Enter opened an existing row's 480px detail drawer within the viewport; focus entered it, Escape closed it, and focus returned to the triggering button. Native keyboard navigation to page 2 fetched ten different rows, retained accent detail actions/current-page indication, and reset body scroll to zero. No clear/delete/write action was invoked.
- Stopped verified PID `16944` for compilation and started detached `admin-activity-table-host`; verified listener PID `2452`, expected Web Debug executable, readiness HTTP 200. Browser page 1 is left on Activity Logs page 1 with native viewport/automatic theme, 100% zoom, no frames/helpers/overlays.
- Updated durable frontend guidance and this record. All 14 changed/new files pass UTF-8/CRLF/final-newline and whitespace validation. Pending scope is the prior D24 work plus this table refinement; no staging, commit, or B6 occurred.

### 2026-11-10 - Bootstrap-first compact Activity Logs revision

- The user supplied a Fluent-based reference and explicitly requested smaller rows, pagination outside the table/card, and Bootstrap-first implementation with minimal custom overrides. D26 supersedes D25's padded-card/integrated-footer presentation, not its behavior or the accepted toolbar sizing.
- Preserved all pending D24/D25 work on `master` at `64ee2314927571cbf1efabad7a78ff11a4bfbe19`. This revision changes only Activity Logs markup/scoped CSS and the same two documentation files; total pending scope remains 14 files.
- Used Bootstrap `table-sm` for native 4px vertical cell padding, `px-3` for 16px horizontal padding, `py-2`/`fw-semibold`/`text-body-secondary` for headers, and `text-break` for long values. The card now uses `p-0 overflow-hidden`; loading/empty surfaces retain `p-4`. Removed the preceding custom cell padding/font/edge-color/hover overrides. Scoped CSS now only allocates fixed columns, restores the card-colored header against the shared header rule, and avoids a doubled final border.
- Moved the existing pagination and page-size control outside the card into a Bootstrap flex row with `mt-3`, and used `pagination-sm`. Kept existing page calculation, URL updates, control bindings, `x-show.important="hasLogs"` refresh visibility, and accent small detail actions. No Fluent dependency, new pagination feature, production JavaScript, or backend change was introduced.
- W: `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed, exit 0, zero warnings/errors, 3.66s. T/J were not repeated for this presentation-only revision.
- Final browser matrix: six cultures x 1440x900/1920x1080 x light/dark = 24 normal samples plus 24 DOM-only long-content samples, all passed. Normal rows measured 40px or slightly less at the final border; no fixed row height or truncation was added. Page links measured 31px, and the pager sat 16px below and outside the card, without an extra footer divider.
- Column widths were 224/176/704/96px at 1440 and 224/176/1184/96px at 1920. Verified flat card/header/cell surfaces, no striping, accent actions, wrapping with a 160-character actor suffix and eight synthetic long-summary repetitions, no cell/document/content horizontal overflow, reachable external pagination, and a fixed action toolbar while scrolling. All temporary frames/content were removed; fixtures were DOM-only, not saved history.
- Native light/dark screenshots were reviewed against the reference. An initial programmatic-focus/ArrowDown probe observed focus on the scroller; an isolated DOM change and a subsequent native Tab/ArrowDown sequence both retained select focus during a held real GET and after the real 20-row response. No application fix or mocked response was used; temporary fetch instrumentation/listeners were restored and removed.
- Native Enter opened the existing 480px detail drawer outside the table card, with focus inside; Escape closed it and returned focus. Native page-2 navigation at size 20 rendered nine different real rows, updated the current-page state, and reset body scroll to zero. No clear/delete/content/settings writes occurred.
- Stopped verified PID `2452` for compilation and started detached `admin-activity-compact-host`; listener PID `9876`, expected Web Debug executable, readiness HTTP 200. Browser page 1 is left on Activity Logs page 1, default size 10, with native viewport/automatic theme, 100% zoom, no frames/helpers/overlays.
- All 14 changed/new files pass UTF-8/CRLF/final-newline and whitespace validation. Updated durable Bootstrap-first/frontend guidance and this retained record. Ready for review; no staging, commit, or B6 occurred.

### 2026-11-10 - Intermediate Activity Logs row and pagination sizing

- The user requested slightly larger rows and pagination than D26 and explicitly allowed Bootstrap overrides. D27 keeps the reference layout/external pagination and chooses intermediate density rather than returning to D25's generous 12px cell padding.
- Preserved all pending D24-D26 work on `master` at `64ee2314927571cbf1efabad7a78ff11a4bfbe19`. Added only a page-specific pagination wrapper class and scoped sizing rules: 6px vertical table-cell padding; Bootstrap pagination padding variables of .375rem vertically/.75rem horizontally; .375rem vertical padding on the existing small page-size select. Toolbar and detail buttons retain their 31px `btn-sm` size.
- Ordinary rows now measure 44px, with a slightly shorter final border row; page links and the page-size select both measure 35px. Font sizes, column allocation, 16px external-pagination gap, unstriped surface, card/header treatment, and all behavior remain unchanged. Long content can still grow naturally.
- W: `dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore` passed, exit 0, zero warnings/errors, 3.82s. T/J were not repeated for this sizing-only change; production JavaScript/backend code did not change.
- Fresh browser matrix: six cultures x both approved sizes x light/dark = 24 normal and 24 DOM-only long-content samples, all passed. Measured the exact row/control sizes, retained 31px toolbar/detail buttons, external pagination gap, no cell/document/content horizontal overflow, reachable footer, and fixed toolbar during long-content scrolling. Temporary actor/summary substitutions and frames were removed. The native light-mode screenshot was reviewed.
- Updated durable frontend guidance to allow scoped overrides where native Bootstrap styling does not meet the confirmed target. All 14 pending files retain valid UTF-8/CRLF/final newlines and original BOM choices; `git diff --check` passes. The 102 matched Activity Logs behavior/control/accessibility attributes remain unchanged.
- Stopped verified PID `9876` for compilation and started detached `admin-activity-balanced-host`; verified listener PID `24820`, expected Web Debug executable, readiness HTTP 200. Browser page 1 is left on Activity Logs page 1, default size 10, native viewport/automatic theme, 100% zoom, no frames/helpers/overlays.
- No local content/settings writes, staging, commit, or B6 occurred. This sizing refinement is ready for review and has not been accepted by the user.

## Latest handoff

- Current phase: B0-B4/R1 accepted; B5 awaits explicit user acceptance, with D24 toolbar sizing and the D27 Activity Logs intermediate-sizing revision ready for review. Do not begin B6.
- Verified baseline: B5 and D23 were committed by the user. HEAD remains `64ee2314927571cbf1efabad7a78ff11a4bfbe19`; pending scope is eleven Razor files, the new Activity Logs isolated stylesheet, and two documentation files. Do not change branches or commit for the user.
- Application changes: 23 `btn-sm` toolbar classes plus a compact unstriped Activity Logs table, redistributed columns, accent detail actions, external pagination, minimal page-scoped CSS, and related documentation. Settings retain 5px item spacing and Account retains 8px; preserve all action semantics and accepted navigation.
- Verification: historical B5 W/T passed (T Passed: 663, Failed: 0, Skipped: 0), with its browser evidence above. Current final W passed with zero warnings/errors after D27; final sizing passed 24 normal/24 DOM-stress samples. D26's native refresh/focus/detail/pagination checks precede this sizing-only change. T/J were not repeated for these presentation-only refinements; do not treat historical B5 tests as post-D27 execution.
- Decisions: D1-D27 govern the design. Prefer Bootstrap components/utilities with scoped overrides where the confirmed design needs them. Activity Logs retains external pagination, unstriped surfaces, accent actions, roughly 44px ordinary rows, and 35px pagination/page-size controls. Ordinary-admin toolbar buttons use `btn-sm`. Settings item gaps are 5px; Account retains 8px action-row gaps. Dashboard keeps New Post on the right in its fixed title row. Preserve immediate settings writes, explicit Menu save, the existing theme limit, excluded editors, and 100% zoom scope.
- Runtime: `https://localhost:10210`, detached session `admin-activity-balanced-host`, last verified listener PID `24820`, expected Web Debug executable, readiness HTTP 200. Reverify ownership before stopping. Browser page 1 is signed in on Activity Logs page 1, default size 10, at native viewport/automatic theme, 100% zoom, with no temporary helpers, frames, masks, or overlays.
- Fixture permission: local test-data writes/creation without restoration remain explicitly authorized. Own B4 fixture menus/child were cleaned up because their long label overflowed the public navbar; the original two menus remain. Do not send email, trigger external services, or extrapolate to production.
- Immediate next action: wait for user acceptance of B5 and the toolbar/table refinements, then obtain explicit B6 authorization. Earlier user-created commits are confirmed; no autonomous commit or B6.
