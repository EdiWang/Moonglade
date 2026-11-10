# Admin Portal Visual Refresh: Current Status and Remaining Work

## Current status

- Updated: 2026-11-10.
- Branch: `master`. Base checkout: `0e5cedc2de3d618a867677960c082cf5877901ec` (`Update admin visual refresh status`).
- Current refinement: the username/avatar opens a Bootstrap dropdown with localized, icon-bearing Account Settings and Sign Out actions, using Bootstrap's default page-theme colors rather than accent colors. As confirmed by the user, OpenID Connect shows Sign Out only. Implemented and verified; not committed and awaiting user review.
- All planned implementation work is complete. B0-B5 and the secondary-navigation work are accepted; B5 acceptance was confirmed on 2026-11-10. The committed sidebar highlight/inset refinement and the new user menu still await explicit user acceptance.
- B6 integrated acceptance has not started and is not authorized. A user-created commit does not imply acceptance or permission to continue.
- Retain this file for future conversations, keeping current state and open work rather than accumulating discussion logs.

## Current design

Detailed implementation conventions live in the [frontend engineering reference](../agent-reference.md#frontend-and-localization). Preserve the following target:

| Area | Current design |
| --- | --- |
| Shell | Existing font, accent colors, and blue top bar; white sidebar and light-gray workspace in light mode, corresponding dark surfaces. Content-height panels use 24px padding, 16px gaps, 8px radii, theme-aware 1px borders, and no shadows. |
| Sidebar | Main and bottom menu groups use Bootstrap `mx-1` for 4px horizontal insets. Current-menu and hover states share a background, without vertical markers: `--bs-gray-200` in light mode and `--bs-tertiary-bg` in dark mode. Preserve route selection, text colors, and keyboard focus. |
| User menu | Username/avatar is a native Bootstrap dropdown button, not an account-page link or separate sign-out icon. Use Bootstrap's default page-theme background/text/hover/focus/active colors, not navbar accent colors. Local authentication shows Account Settings and Sign Out; OpenID Connect shows Sign Out only. Both actions retain their destinations and use localized labels/icons. Preserve claim fallback, safe inline text, an accessible Account label without display claims, keyboard operation, and long-name truncation without clipping the menu. The shared control also appears in editors without changing their other actions/layout. |
| Navigation and toolbar | Posts, Pages, and Settings classifications are independently scrolling secondary navigation in the top bar. Actions remain in the fixed toolbar outside `.admin-main-body`; only content scrolls. Use `btn-sm` for toolbar buttons, actions on the left and counts/search/filters on the right. Omit empty toolbars. |
| Lists and data tables | Lists stay lists in shared panels. Activity Logs and Verification Files use shared unstriped `.admin-data-table` styling and feature-specific columns. Ordinary action rows are about 44px tall; multiline content grows naturally. Activity Logs detail actions use accent colors; preserve Verification Files' distinct action colors. |
| Pagination | Posts, Comments, Mentions, and Activity Logs use `.admin-pagination` outside their cards, with a 16px gap and approximately 35px page links/page-size selects. Preserve navigation destinations and focused controls during populated refresh; hide empty footers with row-based `x-show.important`. |
| Card grids | Pages and Categories retain four columns; Widgets retains three. Independent cards follow the tallest content in each row, with inset footer separators and no viewport-height stretching. |
| Settings and Menu | Windows 11-style settings cards in a left-aligned column capped at 1120px; 5px card/group gaps and 16px vertical/24px horizontal item padding. Related groups expand independently of setting writes. Menu retains one panel and explicit saving through its toolbar-associated form. Preserve immediate ordinary-settings writes, dialogs, theme limits, and dedicated preview/table surfaces. |
| Dashboard and auxiliary pages | Dashboard keeps its title/overview and right-aligned New Post in the fixed action region. Account uses independent native action rows in the 1120px column, with 8px gaps and existing dialogs. About retains its grouped, headerless/borderless information tables and is excluded from shared data-table styling. |

Use the existing Bootstrap/Razor/Alpine stack, with focused CSS overrides where required. Do not introduce Fluent components or another frontend framework.

## Latest verification

These are the most recent recorded checks, not a completed B6 acceptance run.

| Check | Result and coverage |
| --- | --- |
| Web build | Passed with zero warnings/errors after the user-menu change. |
| User-menu browser checks | 80 combinations passed normal and DOM-only long/missing-identity checks: Dashboard, Posts, and General Settings across six cultures, both sizes, and both themes, plus both editor headers in English at both sizes/themes. Verified labels/icons, destinations, opening/dismissal, hit testing, bounded identities, stable header heights, and secondary-navigation separation. Native pointer/Enter/Space/arrow/Escape/Tab checks, account navigation, local sign-out/re-login, and light/dark appearance passed. No external OIDC sign-out was performed. |
| User-menu colors | 12 combinations passed: Dashboard, Posts, and Post editor at both sizes/themes. Background/text/focus/active colors match the shipped Bootstrap dropdown variables; native hover also passed in both themes. Menu dimensions and dismissal are unchanged. This palette follow-up is CSS-only; build and .NET tests were not repeated. |
| Sidebar browser checks | 72 samples: Posts, Activity Logs, and General Settings across all six cultures, both approved sizes, and both themes. Main and bottom groups retain 4px left/right insets, visible selected backgrounds, no markers, and no overflow; sidebar width and main alignment are unchanged. A source comparison confirmed only two spacing classes changed in the shared partial. Native-page appearance was reviewed. |
| Shared table/pagination checks | Five affected routes across six cultures, both sizes, and both themes: 120 combinations passed normal layout, DOM-only long-content, and loading/empty checks. Verification Files was rechecked after its final column adjustment. Native page-size refresh/focus, pagination, drawers, and 16 public/About/editor boundary samples passed. |
| Web tests | Latest targeted run: **Passed: 23, Failed: 0, Skipped: 0**, covering `SearchPageTests` and `AuthControllerTests`. Six new compiled-Razor cases cover Local/OIDC menu actions, root/`/blog` URLs, encoded display-claim fallback, and an accessible label without display claims. Last full-suite run remains **Passed: 650, Failed: 13, Skipped: 0**; the failures below are unresolved and the full suite was not rerun for this refinement. |
| JavaScript tests | Not rerun for the latest presentation-only refinements; no JavaScript changed. Run during B6. |

Targeted command:

```powershell
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-restore --filter-class '*SearchPageTests' --filter-class '*AuthControllerTests'
```

New regression methods in [SearchPageTests.cs](../../src/Tests/Moonglade.Web.Tests/Pages/SearchPageTests.cs): `AdminUserMenu_RendersProviderActionsAndPreservesPathBase` and `AdminUserMenu_WithoutDisplayClaims_RetainsAccessibleAccountLabel`.

### Deferred test failures

The user explicitly chose to record these failures without expanding the visual refresh into repairs. They are not blanket waivers for final acceptance.

- **12 Activity Logs cases:** HTML regexes assume attribute order and do not accommodate CSS-isolation attributes. Relevant files: [ActivityLogHttpTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.cs) and [ActivityLogHttpTests.Changes.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.Changes.cs).
- **1 image-storage case:** `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` fails an original-file existence assertion in [ImageControllerTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ImageControllerTests.cs). The cause is not established by this task.

## Remaining work

- [ ] Obtain review/acceptance of the latest sidebar background highlight and 4px horizontal inset.
- [ ] Obtain review/acceptance of the username/avatar dropdown and its confirmed Local/OIDC menu difference.
- [ ] Obtain explicit authorization to start B6. Until then, wait or execute only the user's separately requested refinement.
- [ ] Before declaring final acceptance, obtain a decision on the 13 deferred test failures: authorize investigation/repair or explicitly agree how they affect the acceptance gate.
- [ ] Execute B6 against the combined checkout, recording actual results and uncovered/blocked scenarios.
- [ ] Obtain final user acceptance. Commits remain user-managed.

### B6 execution scope

Prerequisites: implementation acceptance, confirmed user commits, and explicit B6 authorization.

1. Run the Web build, Web tests, and JavaScript tests once against the combined checkout.
2. Verify the complete route/state matrix below. Do not reuse sample counts as proof of scenarios that were not exercised.
3. Fix only task-caused defects within the approved design; ask before changing accepted design or expanding into deferred issues. Rerun affected checks and identify any acceptance invalidated by a fix.
4. Record outstanding limitations, update applicable durable guidance if needed, and wait for final acceptance. Do not remove this retained tracker.

| Group | Routes | Required states |
| --- | --- | --- |
| Dashboard | `/admin` | Empty/populated sections, metrics, long titles, fixed title/New Post row |
| Posts | `/admin/post`, `/admin/post/draft`, `/admin/post/scheduled`, `/admin/post/recycle-bin` | Loading, empty/filter-empty, single/many records, secondary navigation, filters, pagination/page size, conditional actions |
| Pages | `/admin/page`, `/admin/page/recycle-bin` | Grid, empty/single states, recycle list, badges/actions |
| Taxonomy | `/admin/category`, `/admin/tags` | Long names/descriptions, cards, letter groups, search, drawers, long scrolling |
| Interaction | `/admin/comments`, `/admin/mention` | Multiline content, replies, bulk selection, filters, pagination, refresh focus |
| Widgets and Menu | `/admin/widgets`, `/admin/menu` | Preview types, nested ordering, disabled/empty states, toolbar Save/form ownership |
| Activity Logs | `/admin/activitylogs` | Table, empty results, filter/detail drawers, long details, pagination, confirmation appearance |
| Settings | `/admin/settings/` routes: `general`, `content`, `comment`, `image`, `subscription`, `appearance`, `notification`, `advanced`, `verification-files` | Navigation, cards/disclosures, dialogs, validation, conditional content, theme previews, verification table/editor |
| Auxiliary | `/admin/account`, `/admin/about` | Focus/hover, dialogs without destructive operations, readable information tables, no empty toolbar |
| Excluded regression surfaces | `/admin/post/edit`, `/admin/page/edit`, public home and a public article | Existing editor layout/actions/scrolling, overlays, no admin style leakage |

Acceptance conditions:

- Viewports: **1440x900** and **1920x1080** CSS pixels; light/dark themes; **100% zoom only**. Do not add 125%/150% zoom checks.
- Cultures: `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, `ja-JP`, and `fr-FR`.
- Check wrapping, long/unbroken values, empty/loading/populated states, keyboard navigation, focus visibility/return, and modal/offcanvas ownership.
- During long scrolling, toolbars stay fixed and the final row, pager, card edge, and controls remain reachable. Avoid unintended document/content horizontal overflow; wide tables may use an intentional local scroller.
- Label DOM-only stress substitutions accurately; they do not prove saved-data behavior. Do not commit screenshots or logs exposing secrets or About environment values.
- Every required scenario needs actual evidence or an explicit blocked/not-run status.

## Execution boundaries

- Read [AGENTS.md](../../AGENTS.md) and the applicable [engineering reference](../agent-reference.md) before further implementation.
- Continue on the current branch; ask if it differs from recorded `master`. Inspect Git status before editing and finishing, preserving user changes.
- Do not autonomously stage, commit, amend, change branches, push, create PRs, deploy, or start a subsequent batch.
- Ask about material design/scope/behavior choices, with alternatives and a recommendation. Verification, acceptance, commit confirmation, and continuation are separate gates.
- Local host stop/start and README default-account login are authorized. Verify process ownership before stopping a specific PID; do not reset accounts.
- Local test-data creation/changes are authorized without restoration. This does not authorize production changes, external sends/services, or unrelated repairs.
- Preserve public pages, full-screen-editor layouts/actions, authentication, protocols, business behavior, and existing save semantics. The shared user-menu interaction applies to editor headers too.
- Keep this tracker concise after future updates: replace stale state, record current verification and unresolved work, and do not append discussion histories.

## Resume instructions and commands

1. Read the current status, remaining work, and execution boundaries above.
2. Confirm the checkout and working tree; do not assume the recorded commit or runtime is still current.
3. Continue only the latest authorized request. Without new authorization, the next action is user review, not B6.

The development endpoint is `https://localhost:10210`; check `/health/ready` and its owning process before relying on or restarting an existing host. Do not rely on an old PID.

Run from the repository root. See the [test guide](../../src/Tests/README.md#run-tests) for native Microsoft.Testing.Platform syntax.

```powershell
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj
node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
```

After a successful build, start the authorized local review host when needed:

```powershell
dotnet run --project .\src\Moonglade.Web\Moonglade.Web.csproj --no-build --no-restore
```

Do not install/restore preemptively. Record exact commands, results, actual browser coverage, skipped checks, and unresolved failures; never infer visual correctness from compilation alone.
