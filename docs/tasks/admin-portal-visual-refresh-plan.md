# Admin Portal Visual Refresh: Current Status and Remaining Work

## Current status

- Updated: 2026-11-10.
- Checkout at update: `master`, `45b5970bf` (`Refresh admin menu with collapsible groups`).
- B0-B5 and earlier visual refinements are accepted and committed.
- Pending review: Pages card Edit/Delete actions use the shared top-right ellipsis dropdown. The existing editor link, deletion confirmation, publication badges, card padding, and metadata are retained. Implemented, verified, and committed.
- Pending review: Settings disclosure buttons cover their full header rows, with switches/radios and their labels remaining independently interactive. Implemented, browser-verified, and committed.
- Pending review: General Settings' Profile uses three separated rows: avatar/name/description with one combined Change dialog, independent email editing, and Gravatar. The avatar has a camera/mask hover and keyboard-focus overlay opening the existing upload dialog. Implemented, verified, and committed.
- Pending review: Menu now uses independent Settings-style expandable groups, default collapsed, with flat child rows and separators instead of nested panels. The enable row is independent; ordering buttons remain direct, while parent/child Edit and Delete use compact ellipsis dropdowns. The user confirmed these choices. Implemented, verified, and committed.
- Pending review: Advanced Settings' Head/Foot JavaScript warnings and usage guidance now appear in their respective Change dialogs instead of the list. Existing localized content, editing, and saving behavior are retained. Implemented and verified; not committed.
- Local development host is stopped. The user authorized a temporary script-dialog verification host only; it was stopped after verification and port 10210 is no longer listening.
- B6 integrated acceptance has not started and is not authorized. A user-created commit does not imply acceptance or permission to continue.
- Retain this file for future conversations, keeping current state and open work rather than accumulating discussion logs.

Preserve the current Bootstrap/Razor/Alpine design documented in the [frontend engineering reference](../agent-reference.md#frontend-and-localization). Keep implementation details there rather than duplicating completed work here.

## Current refinement verification

- C# tests cover business logic and functional contracts, not UI presentation. UI-only tests have been removed; presentation remains browser-verified rather than enforced through C# markup assertions. See the [test scope](../../src/Tests/README.md) and [repository rules](../../AGENTS.md#test-selection).
- Page browser checks passed 24 combinations across six cultures, both sizes/themes: 72 dropdown openings across all three current cards and 72 DOM-only long-title/slug checks. Both real published/unpublished states passed. Verified 24px card padding, corner alignment, 14px menu text, 29px items, 128px menu width, metadata/public/editor URLs, and no title/badge overlap or clipped menus.
- Native light/dark hover, ArrowDown/ArrowUp navigation, and Escape collapse/focus return passed. Edit opened the correct existing full-screen editor; returned without saving. Delete opened the existing confirmation and was canceled. No saves/deletions were performed; temporary frames were removed and the original theme restored.
- Settings checks passed for all 11 disclosure headers on six pages across 24 culture/viewport/theme combinations: 264 header checks, 528 title/blank-space clicks, and 1,632 row/control hit-target checks. Full-row coverage, arrow alignment, expansion events, ARIA state, unchanged field values, and no document overflow passed.
- All 312 switch/radio/label activations were DOM-only checks with change propagation blocked and values restored; no setting writes or form submissions occurred. Native Enter/Space activation and visible focus passed on heading-only, radio, and switch headers with normal transitions. Matrix checks disabled transition duration only. Temporary frames/helpers were removed; the original route and theme were retained.
- No build or C# tests were run for the earlier CSS/documentation-only disclosure refinement; no C# UI tests were added.
- Profile browser checks passed all 24 culture/viewport/theme combinations, including three stacked rows, both separators, aligned Change buttons, 80px avatars, 24 DOM-only long-name/description stress checks, 24 grouped-dialog open/cancel checks, and 24 independent DOM-only Gravatar activations.
- Native avatar hover showed the camera and 50% black mask; Enter opened the existing uploader, and Escape returned visible keyboard focus. No avatar was uploaded.
- DOM-only API simulations verified a single PATCH containing both profile fields, independent email/single-field requests, required validation, cancel rollback, no-op suppression, error retention/retry, pending-save locking, preview updates, and focus return. New shared-module regressions passed direct switches, CDN dependency prompts/cancellation/required-state restoration, combined dependency saves, and CSS/HTML code-editor creation, sync, no-op, destruction, and focus behavior. No simulated request reached the server; no real settings were changed.
- Profile verification: Web build passed with zero warnings/errors; JavaScript syntax checks passed; the existing ImageUploader tests passed 9/9. No C# presentation tests were added or run. Pages/disclosure JavaScript tests were not rerun for their earlier CSS/markup changes.
- Menu verification: Web build passed with zero warnings/errors; Menu JavaScript syntax and all 34 existing JavaScript tests passed. Browser checks passed all 24 culture/viewport/theme combinations: 144 disclosure clicks, 120 parent/child dropdown openings, 48 flat-child separator checks, and 72 DOM-only long-text checks. Dropdown hit testing identified and verified a fix for following-row overlap.
- Menu draft checks passed ordering boundaries, expansion following reordered/edited items, parent/child editing, adding to a null child list, delete/clear confirmation and cancellation, expansion cleanup, and the unchanged toolbar Save envelope. Native Enter/Space disclosure, visible focus, dropdown ArrowDown navigation, and Escape focus return passed. Empty, single-menu, loading, and 20-group long-scroll states passed in both sizes/themes, including fixed-toolbar and final-edge reachability.
- Menu fixtures and the single simulated Save were browser-only; no real menu settings were changed. Temporary frames/helpers were removed, the original theme restored, and reload confirmed the original two saved menus default to collapsed. No C# UI tests were added or run. These checks are a refinement, not B6.
- Script-dialog verification: Web build passed with zero warnings/errors; both changed JavaScript modules passed syntax checks and all 34 existing JavaScript tests passed. Browser checks passed 24 culture/viewport/theme combinations with 48 dialog visits: correct localized warning/instructions, encoded script examples, accessible help, focus return, viewport fit, reachable actions, and help cleanup. Alternating Head/Foot, cancel rollback, no-op suppression, error retention/retry, and unchanged PATCH fields passed through DOM-only checks and two intercepted requests; no real settings were saved.
- Updated-helper regressions passed grouped Profile, ordinary single-field editing, and the CSS code editor without script-help leakage. Temporary frames/helpers were removed and the original settings remained unchanged. No C# UI tests were added or run.

## Current business-test status

Latest full Web suite after UI-test cleanup: **Passed: 654, Failed: 0, Skipped: 0**. The affected retained business-test selection also passed: **Passed: 108, Failed: 0, Skipped: 0**. That cleanup changed no production code, JavaScript tests, dependencies, or solution wiring.

Cleanup removed 23 UI-only cases across 10 methods and one empty test source file. The final test-project build passed with zero warnings/errors.

The former twelve Activity Logs HTML-regex failures are no longer outstanding: presentation constraints were removed while meaningful API/security cases were retained and passed. `Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage` passed unchanged; its previous failure was not reproduced or repaired. These runs validate the test cleanup, not B6 integrated acceptance.

Other C# test projects were inspected but unchanged and not rerun. Future failures outside the authorized refinement still require a scope decision before repair.

## Remaining work

- [ ] Obtain review/acceptance of the Pages card action menu.
- [ ] Obtain review/acceptance of full-row Settings disclosure interaction.
- [ ] Obtain review/acceptance of the three-row Profile layout, combined name/description editor, and avatar action.
- [ ] Obtain review/acceptance of the flat Menu groups, default-collapsed disclosure, and compact action menus.
- [ ] Obtain review/acceptance of Head/Foot JavaScript guidance inside the Change dialogs.
- [ ] Obtain explicit authorization to start B6. Until then, wait or execute only the user's separately requested refinement.
- [ ] Execute B6 against the combined checkout, recording actual results and uncovered/blocked scenarios.
- [ ] Obtain final user acceptance. Commits remain user-managed.

### B6 execution scope

Remaining prerequisites: acceptance/commit confirmation of the Pages, Settings disclosure, Profile, Menu, and script-dialog refinements and explicit user authorization to start B6.

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
3. Continue only the latest authorized request. B6 still requires explicit authorization.

The development endpoint is `https://localhost:10210`; the host was stopped at the user's request. Ask before starting it for further work. Check `/health/ready` and its owning process before relying on or restarting an existing host. Do not rely on an old PID.

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
