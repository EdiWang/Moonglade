# Admin Portal Visual Refresh: Current Status and Remaining Work

## Current status

- Updated: 2026-11-10.
- Checkout at update: `master`, `8c21e5569` (`Update admin refresh acceptance status`).
- Accepted: B0-B5, secondary navigation, sidebar background highlighting with 4px horizontal insets, and the username/avatar dropdown with its Local/OIDC menu difference.
- Pending review: unified Account/Settings item grid, typography, 5px gaps, independent right-side Account action buttons, and theme-aware gray hover backgrounds. Implemented, verified, and committed.
- Pending review: separately requested offcanvas action rows, with the primary action on the left and Cancel/Clear on the right in shared 2:1 columns. Eight action rows are implemented and verified; not committed.
- B6 integrated acceptance has not started and is not authorized. A user-created commit does not imply acceptance or permission to continue.
- Retain this file for future conversations, keeping current state and open work rather than accumulating discussion logs.

Preserve the current Bootstrap/Razor/Alpine design documented in the [frontend engineering reference](../agent-reference.md#frontend-and-localization). Keep implementation details there rather than duplicating completed work here.

## Pending refinement: verification

These results cover the current refinements awaiting review, not B6.

| Check | Result and coverage |
| --- | --- |
| Web build | Passed with zero warnings/errors after the offcanvas action-row change. |
| Offcanvas browser checks | 192 combinations passed: eight action rows across six cultures, both sizes/themes. Verified side-by-side 2:1 widths, 8px gaps, matching button heights, drawer containment, wrapping without clipped labels, and existing form/submit/dismiss/clear bindings. Native Category keyboard checks confirmed Tab from Submit to Cancel and Enter dismissal. No save/reset submissions were made. Temporary verification frames were removed. |
| Account/Settings browser checks | 48 Account/General Settings combinations across six cultures, both sizes/themes passed exact grid/spacing/typography comparisons, control alignment, accessible descriptions, and DOM-only long-description checks. Another 32 combinations cover the other eight Settings routes in English at both sizes/themes; Verification Files has no settings entries and retains its table. Eight native hover checks passed for Account, ordinary Settings, expander headers, and nested entries in both themes. Both Account dialogs opened and dismissed correctly, including keyboard activation; clicking row text does not open them. No credential changes or resets were submitted. |
| Web tests | Latest targeted run: **Passed: 15, Failed: 0, Skipped: 0**, covering `SearchPageTests`, including two new compiled-Razor cases for the Account action buttons, their existing dialog handlers/targets, danger styling, and descriptive context. Existing page/menu cases also passed. Last full-suite run remains **Passed: 650, Failed: 13, Skipped: 0**; the failures below are unresolved and the full suite was not rerun for this refinement. |
| JavaScript tests | Not rerun for the latest presentation-only refinements; no JavaScript changed. Run during B6. |

Targeted command:

```powershell
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-restore --filter-class '*SearchPageTests'
```

Latest regression method in [SearchPageTests.cs](../../src/Tests/Moonglade.Web.Tests/Pages/SearchPageTests.cs): `LocalAccount_ActionButtons_TargetExistingDialogs` (two dialog cases).

## Unresolved test failures

Repair is outside the authorized scope. These failures still require an acceptance-gate decision before final acceptance.

- **12 Activity Logs cases:** HTML regexes assume attribute order and do not accommodate CSS-isolation attributes. Relevant files: [ActivityLogHttpTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.cs) and [ActivityLogHttpTests.Changes.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ActivityLogHttpTests.Changes.cs).
- **1 image-storage case:** `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` fails an original-file existence assertion in [ImageControllerTests.cs](../../src/Tests/Moonglade.Web.Tests/Controllers/ImageControllerTests.cs). The cause is not established by this task.

## Remaining work

- [ ] Obtain review/acceptance of the unified Account/Settings entries, independent Account buttons, and shared hover background.
- [ ] Obtain review/acceptance of the horizontal offcanvas action rows.
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
