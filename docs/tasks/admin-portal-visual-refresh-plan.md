# Admin Portal Visual Refresh: Current Status and Remaining Work

## Current status

- Updated: 2026-11-10.
- Checkout at update: `master`, `2acf4d08a` (`Unify widget and category card action menus`).
- Accepted: B0-B5, secondary navigation, sidebar background highlighting with 4px horizontal insets, and the username/avatar dropdown with its Local/OIDC menu difference.
- Pending review: unified Account/Settings item grid, typography, 5px gaps, independent right-side Account action buttons, and theme-aware gray hover backgrounds. Implemented, verified, and committed.
- Pending review: separately requested offcanvas action rows, with the primary action on the left and Cancel/Clear on the right in shared 2:1 columns. Eight action rows are implemented, verified, and committed.
- Pending review: Category card actions sit against the top-right inner border with a theme-aware hover/open background and compact menu items; Category-only body padding is 19px. Original localized actions and edit/delete flows are retained. Latest menu-sizing refinement is verified and uncommitted.
- Pending review: Tags letter-group separators are removed, and zero-reference tag pills use Bootstrap warning borders instead of triangle icons. Localized accessible warning text and existing tag operations are retained. Implemented, verified, and committed.
- Pending review: Widget Edit/Delete actions use the same compact top-right ellipsis dropdown and shared hover/corner styles as Categories. Title dividers are removed; Widget padding, previews, enabled badges, and existing edit/delete flows are retained. Latest refinements are verified and uncommitted.
- B6 integrated acceptance has not started and is not authorized. A user-created commit does not imply acceptance or permission to continue.
- Retain this file for future conversations, keeping current state and open work rather than accumulating discussion logs.

Preserve the current Bootstrap/Razor/Alpine design documented in the [frontend engineering reference](../agent-reference.md#frontend-and-localization). Keep implementation details there rather than duplicating completed work here.

## Pending refinement: verification

These results cover the current refinements awaiting review, not B6.

| Check | Result and coverage |
| --- | --- |
| Web build | Web and test projects rebuilt successfully during the latest targeted Razor regression run. |
| Compact card menus and dividers | Latest height checks passed 48 combinations across Category/Widgets, six cultures, both sizes/themes: 384 dropdown openings and 768 menu items. Items are 29px tall with 4px/12px vertical/horizontal padding; text remains 14px, menus remain 128px wide, and toggles remain 32x31px. Localized labels and menus fit without clipping. Unrelated account-menu sizing matches the unchanged page baseline in all 48 combinations. Widget title dividers remain removed; preview and interaction verification is covered below. No saves/deletions were performed; temporary frames were removed and the original page/theme left unchanged. |
| Widget browser checks | 24 combinations passed across six cultures, both sizes/themes: 120 dropdown openings across all five current cards, plus 24 DOM-only long-title/disabled-badge checks. Verified 24px content padding, matching corner placement, no title overlap/clipped menus, accessible title descriptions, and unchanged action bindings. All five preview HTML fragments match the pre-change page, covering Button Link, Image Link, and Link List previews. Native light/dark hover, keyboard opening/ArrowDown navigation, and Escape collapse/focus return passed. Edit loaded the correct existing drawer; Delete opened the existing confirmation. Both were canceled without saves/deletions. Temporary frames were removed and the original theme restored. |
| Tags browser checks | 24 combinations passed across six cultures, both sizes/themes, covering all 148 current tags and 24 letter groups, including two real zero-reference tags. Group borders are absent while shared spacing remains; only zero-reference pills use Bootstrap's `rgb(255, 193, 7)` warning border. Normal borders follow the light/dark theme. Localized screen-reader warnings and edit/delete bindings remain. Browser input-event checks passed zero-reference, referenced-tag, and no-results filtering; the original filter was restored. No tag data was written; temporary frames/probes were removed. |
| Category browser checks | Regression checks after sharing the menu styles passed 24 combinations across six cultures, both sizes/themes: 264 dropdown openings across all 11 current cards, plus 24 DOM-only long-title checks. Verified retained 19px padding, triggers flush with the 1px top/right inner borders, no rendered-title overlap, and unclipped menus. Existing native light/dark hover, keyboard navigation/focus return, and edit/delete-and-cancel verification remains valid; action handlers are unchanged. Temporary frames were removed. |
| Offcanvas browser checks | 192 combinations passed: eight action rows across six cultures, both sizes/themes. Verified side-by-side 2:1 widths, 8px gaps, matching button heights, drawer containment, wrapping without clipped labels, and existing form/submit/dismiss/clear bindings. Native Category keyboard checks confirmed Tab from Submit to Cancel and Enter dismissal. No save/reset submissions were made. Temporary verification frames were removed. |
| Account/Settings browser checks | 48 Account/General Settings combinations across six cultures, both sizes/themes passed exact grid/spacing/typography comparisons, control alignment, accessible descriptions, and DOM-only long-description checks. Another 32 combinations cover the other eight Settings routes in English at both sizes/themes; Verification Files has no settings entries and retains its table. Eight native hover checks passed for Account, ordinary Settings, expander headers, and nested entries in both themes. Both Account dialogs opened and dismissed correctly, including keyboard activation; clicking row text does not open them. No credential changes or resets were submitted. |
| Web tests | Not rerun for the latest CSS-only height adjustment. Most recent targeted run: **Passed: 4, Failed: 0, Skipped: 0**, covering English/French Category and Widget menus, localized accessible labels/icons, shared menu hooks, and existing action bindings. Last full-suite run remains **Passed: 650, Failed: 13, Skipped: 0**; the failures below are unresolved. |
| JavaScript tests | Not rerun for the latest presentation-only refinements; no JavaScript changed. Run during B6. |

Most recent targeted test command (shared Category/Widget menus, including rebuilding the Web/test projects):

```powershell
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-restore --filter-method '*AdminCard_ActionMenu_PreservesLocalizedActionsAndHandlers'
```

Latest regression method in [SearchPageTests.cs](../../src/Tests/Moonglade.Web.Tests/Pages/SearchPageTests.cs): `AdminCard_ActionMenu_PreservesLocalizedActionsAndHandlers` (Category/Widgets, English/French cases).

## Unresolved test failures

Repair is outside the authorized scope. These failures still require an acceptance-gate decision before final acceptance.

- **12 Activity Logs cases:** HTML regexes assume attribute order and do not accommodate CSS-isolation attributes. Relevant files: [ActivityLogHttpTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.cs) and [ActivityLogHttpTests.Changes.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.Changes.cs).
- **1 image-storage case:** `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` fails an original-file existence assertion in [ImageControllerTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ImageControllerTests.cs). The cause is not established by this task.

## Remaining work

- [ ] Obtain review/acceptance of the unified Account/Settings entries, independent Account buttons, and shared hover background.
- [ ] Obtain review/acceptance of the horizontal offcanvas action rows.
- [ ] Obtain review/acceptance of the Category card action menu, including compact item sizing.
- [ ] Obtain review/acceptance of the Tags separators and zero-reference warning borders.
- [ ] Obtain review/acceptance of the compact Widget card action menu and removed title dividers.
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
