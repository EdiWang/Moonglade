# Admin Portal Visual Refresh: B6 Integrated Acceptance

## Current status

- Updated: 2026-11-10.
- Baseline checkout: `master`, `a42645a95` (`Move script guidance into settings dialogs`).
- B0-B5 and all five follow-up refinements were explicitly accepted. Their completed entries and verification history have been removed.
- B6 verification is complete, including the separately approved drawer focus correction. Final integrated acceptance remains user-owned.
- The temporary B6 host is stopped; port 10210 has no listener. Browser-only fixtures/helpers have been removed.
- Retain this file for future conversations. Replace current state and results rather than appending discussion history.

The accepted implementation contracts live in the [frontend engineering reference](../agent-reference.md#frontend-and-localization). C# tests cover business/API/security/protocol behavior, not UI presentation; use browser checks for presentation.

## B6 progress

| Step | Status | Evidence / remaining work |
| --- | --- | --- |
| Clean accepted refinement tracking | Complete | The five accepted refinements and their old checklists/history are removed. |
| Web build | Passed | Zero warnings/errors, exit 0. |
| Full Web tests | Passed with invocation adjustment | Passed: 668, Failed: 0, Skipped: 0. See the exact commands and initial failures below. |
| Existing JavaScript tests | Passed | After the approved focus correction: Passed: 42, Failed: 0, Skipped: 0; exit 0. |
| Approved drawer focus correction | Passed | Eight filter/edit drawers return focus in all 192 visits. Asynchronous card edits return to their visible toggle; 24 Verification File edit visits and native keyboard checks also passed. |
| Integrated browser matrix | Passed within the boundaries below | Geometry, fixtures, Settings editors, draft interactions, auxiliary dialogs, and excluded routes checked. No remaining task-caused defect found. |
| Final checks and handoff | Complete; acceptance pending | Temporary host stopped, browser fixtures removed, and final whitespace/worktree checks passed. No commit created. |

## Route and state matrix

Every required scenario needs actual evidence or an explicit blocked/not-run status. Route loading alone does not prove interaction coverage. DOM-only fixtures prove presentation/interaction, not persistence.

| Group | Routes | Required states | B6 result |
| --- | --- | --- | --- |
| Dashboard | `/admin` | Empty/populated sections, metrics, long titles, fixed title/New Post row | Passed; populated live state plus DOM-only empty/long fixtures |
| Posts | `/admin/post`, `/admin/post/draft`, `/admin/post/scheduled`, `/admin/post/recycle-bin` | Loading, empty/filter-empty, single/many records, secondary navigation, filters, pagination/page size, conditional actions | Passed; read-only refresh/filter checks and DOM fixtures |
| Pages | `/admin/page`, `/admin/page/recycle-bin` | Grid, empty/single states, recycle list, badges/actions | Passed; live card menus and DOM fixtures |
| Taxonomy | `/admin/category`, `/admin/tags` | Long names/descriptions, cards, letter groups, search, drawers, long scrolling | Passed; valid-length Tag fixtures, search, drawer/menu checks |
| Interaction | `/admin/comments`, `/admin/mention` | Multiline content, replies, bulk selection, filters, pagination, refresh focus | Passed; DOM reply/long fixtures, selection, and read-only list operations |
| Widgets and Menu | `/admin/widgets`, `/admin/menu` | Preview types, nested ordering, disabled/empty states, toolbar Save/form ownership | Passed; nested Widget dialogs, Menu draft operations, simulated Save |
| Activity Logs | `/admin/activitylogs` | Table, empty results, filter/detail drawers, long details, pagination, confirmation appearance | Passed; real read-only filter/detail requests, DOM long detail, clear cancellation |
| Settings | `/admin/settings/` routes: `general`, `content`, `comment`, `image`, `subscription`, `appearance`, `notification`, `seo`, `advanced`, `verification-files` | Navigation, cards/disclosures, dialogs, validation, conditional content, theme previews, verification table/editor | Passed; shared editors/disclosures, upload/theme dialogs, simulated Profile save errors/retry |
| Auxiliary | `/admin/account`, `/admin/about` | Focus/hover, dialogs without destructive operations, readable information tables, no empty toolbar | Live surfaces and non-mutating Account dialogs checked; credentials/authenticator not changed |
| Excluded regression surfaces | `/admin/post/edit`, `/admin/page/edit`, public home and a public article | Existing editor layout/actions/scrolling, overlays, no admin style leakage | Passed; 96 route conditions plus 48 editor-overlay conditions |

Acceptance conditions:

- Viewports: **1440x900** and **1920x1080** CSS pixels; light/dark themes; **100% zoom only**. Do not add 125%/150% zoom checks.
- Cultures: `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, `ja-JP`, and `fr-FR`.
- Check wrapping, long/unbroken values, empty/loading/populated states, keyboard navigation, focus visibility/return, and modal/offcanvas ownership.
- During long scrolling, toolbars stay fixed and the final row, pager, card edge, and controls remain reachable. Avoid unintended document/content horizontal overflow; wide tables may use an intentional local scroller.
- Label DOM-only fixtures and simulated API responses accurately. Do not treat them as real saved-data verification or commit screenshots/logs exposing secrets or About environment values.
- Do not infer completion of an unexercised scenario from sample counts, passing automated tests, or compilation.

## Current verification and limitations

Current B6 automation passed:

- Web build: zero warnings/errors.
- Full Web suite: **Passed: 668, Failed: 0, Skipped: 0**.
- Existing JavaScript suite plus the new focused helper cases: **Passed: 42, Failed: 0, Skipped: 0**.
- Session-only TRX: `files\b6-results\Edi_NORDRASSIL_2026-11-10_09_25_35.033978.trx`.

The initial documented `--report-trx` invocation failed at the SDK/runner handshake with zero tests. Inspection established that the current xUnit 4.0.2 application needs an explicitly selected MTP entry point and registers `--report-xunit-trx`, not `--report-trx`. An explicit test-project build with the temporary property followed by the command below passed. No package/project configuration was changed. The default test-guide invocation discrepancy remains outside this visual-refresh batch.

The Web-focused B6 gate does not run the full solution's disposable PostgreSQL/SQL Server migration fixtures and does not establish provider compatibility.

Browser evidence collected so far:

- All 26 ordinary admin routes, including SEO, passed the 24-condition geometry matrix: **624 route conditions**. Checks include exact CSS viewport dimensions, horizontal overflow, fixed toolbar position while scrolling, and scroll-bottom reachability.
- Both excluded editors, public home, and one public article passed **96 route conditions**, with no ordinary-admin toolbar/surface leakage; editor actions and public scroll bottoms are reachable.
- All eight filter/create drawers passed **192 visits** after the focus correction, including horizontal 2:1 actions, viewport fit, and focus return.
- Category/Widget/Page card menus passed **72 visits**, including 29px items, hit testing, Page editor destinations, and asynchronous Category/Widget edit focus return. Verification File asynchronous edit focus passed another **24 visits**.
- Settings' 22 shared edit actions passed **528 dialog conditions** across nine routes, including grouped Profile, CSS/HTML editors, script help, cancellation, focus return, viewport fit, and help cleanup. The 11 disclosure headers passed **132 toggle checks**, **264 full-header hit tests**, and **264 independent value-control hit tests**. Twelve additional script-dialog visits confirmed visible help, inert encoded script examples, and accessible descriptions.
- Posts/Comments/Mentions/Activity Logs page-size selectors remained visible/focused during and after delayed real read-only refreshes; the three populated multi-page lists returned their body scroller to the top on page changes.
- DOM-only loading/empty/single/36-row long-content fixtures passed **1,248 conditions** on 13 list/grid routes across all six cultures, both sizes/themes. Tags used the actual 32-character persisted limit. The initial 128-character Tag stress fixture exceeded the entity limit and overflowed; that unsupported fixture is not treated as an application regression.
- Dashboard DOM-only empty sections and long-title/many-row fixtures passed **48 conditions**. Menu loading/empty/disabled/single/long/expanded fixtures passed **144 conditions**; expansion survived draft ordering and open menus remained clickable.
- Menu draft delete cancellation/confirmation and null-child creation passed without a real write. A simulated toolbar Save preserved only the `isEnabled`/`menuJson` envelope and did not serialize expansion state.
- Activity Log long details passed **24 conditions** and returned focus to their trigger; six localized clear confirmations were cancelled without changing history.
- Account reset, theme creation, avatar, and site-icon dialogs passed **120 conditions** without submitting changes. Nested Widget Link/Button dialogs passed **48 conditions**, retained their owning drawer, and preserved its final focus return; ImageLink fields were also checked.
- Post publishing and Page Properties overlays passed **48 conditions** without saving/publishing. Native Category `Enter`/`ArrowDown`/`Enter`/`Escape` and Post Filter `Enter`/`Escape` returned focus correctly; Menu disclosure responded to both Enter and Space.
- Real read-only filter-empty/clear round trips passed on Posts/Comments/Mentions/Activity Logs, including trigger focus and restored results. Comment/Mention selection showed and then hid their bulk actions without invoking them.
- Grouped Profile required-field validation and no-op suppression caused no requests. Simulated error/retry checked both `Description`/`OwnerName` keys, retained values, pending-save lock, blocked dismissal, successful previews, and focus return.
- Representative light Menu and dark Category screenshots were inspected. The final console had no errors; its only warning was the expected Development-environment notice.

Verification boundaries:

- No real settings saves, content edits/deletions, publishing, credential/authenticator resets, image uploads, or outbound test notifications were performed. Browser fixtures and simulated API responses do not prove backend persistence.
- Empty Dashboard sections were DOM-only fixtures, not a freshly seeded empty database. Existing local data and normal sign-in/page-view side effects were not reset.
- The configured Local authentication UI was exercised; a live OIDC sign-in/provider switch was not.
- The full Web suite ran before the JavaScript-only focus correction; no C# code changed afterward. The post-correction Web build, full 42-test JavaScript run, and browser rechecks passed.
- Screenshots were representative, not 720 individually reviewed images. Route geometry checks and targeted interactions are recorded separately rather than described as exhaustive manual visual review.

The user explicitly approved fixing the pre-existing drawer focus issue discovered by B6. `createOffcanvas` now centralizes return to a visible opening control; asynchronous editors capture their trigger before loading. Focused Node cases include `captures the active opening button and waits for the hidden event`, `explicit opening control survives an asynchronous focus change`, and `returns to the visible dropdown toggle instead of its hidden menu item`, plus reopen/stale/removed/hidden/disabled cases. Activity Logs' existing explicit detail focus return is unchanged. The post-fix Web build also passed with zero warnings/errors.

## Remaining work

- [ ] Obtain final user acceptance. Commits remain user-managed.

## Execution boundaries

- Read [AGENTS.md](../../AGENTS.md) and applicable sections of the [engineering reference](../agent-reference.md) before changes.
- Stay on `master`; inspect Git status before editing and finishing, preserving user changes.
- Do not autonomously stage, commit, amend, change branches, push, create PRs, deploy, or start a subsequent batch.
- Material design/scope/behavior decisions require a question with alternatives and a recommendation. Verification, acceptance, and commits are separate gates.
- Local host startup for B6 and README default-account login are authorized. Verify process ownership before stopping a literal PID; never reset accounts.
- Local test-data changes are authorized, but production/external-service changes, real email sends, and unrelated repairs are not.
- Preserve public pages, full-screen editors, authentication, protocols, business behavior, and existing save semantics.
- Do not add C# presentation tests. Do not install/restore dependencies preemptively.

## Resume instructions and commands

1. Read status, results, unresolved work, and boundaries above.
2. Confirm checkout, worktree, and actual runtime state; do not rely on an old PID.
3. Resume the next incomplete B6 step. Record actual results and preserve this tracker for final acceptance.

Run from the repository root. See the [test guide](../../src/Tests/README.md#run-tests) for native Microsoft.Testing.Platform commands.

```powershell
dotnet build .\src\Moonglade.Web\Moonglade.Web.csproj --no-restore
dotnet build .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-restore -p:UseMicrosoftTestingPlatformRunner=true
dotnet test --project .\src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj --no-build -p:UseMicrosoftTestingPlatformRunner=true --report-xunit-trx
node --test .\src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
```

After automated verification, run the authorized temporary host if needed:

```powershell
dotnet run --project .\src\Moonglade.Web\Moonglade.Web.csproj --no-build --no-restore
```

The development endpoint is `https://localhost:10210`; verify `/health/ready`. Stop the temporary host before handoff.
