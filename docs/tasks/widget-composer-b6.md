# Widget Composer B6 Integrated Acceptance and Handoff

Status: ready for user acceptance, 2026-11-22 (Asia/Taipei). B0-B5 are accepted; B6 verification, documentation and local commits are authorized. Deployment, production upgrades, pushes and external service changes are not authorized. Maintain current state only with the [delivery tracker](widget-composer-plan.md).

## Candidate and progress

The final application sources match accepted B5 implementation `31d4401361754d9df1967ede0516218b666056e4`; accepted checkpoint `f2003fe46`. Planning starts at `1e91dea838e593bf73e6799310d7b72c0855260e`, after original application baseline `3169a59557b7acd519c612b9ea1bc543908c68b2`. The accepted B2-B5 implementation SHAs are in the tracker. B6 authority commit: `f8fd631f6` on `codex/widget-composer-b6`.

- [x] Full solution: 2175 tests passed across 15 projects, zero failed/skipped.
- [x] Node suite: 91 passed, zero failed/skipped. Explicit Web build and Release publication passed.
- [x] Both providers: fresh schema/seed, representative upgrade, native backup/restore, preservation, repeated scripts and actual transaction checks.
- [x] All four browser profiles: complete saves, authorization/antiforgery, public/cache/audit, two-tab replacement/restoration, cultures/themes/settings and keyboard access.
- [x] Published Release assets in local Production hosts: real TOTP reset/enrollment/re-login and pending-session rejection, widget save/public cache and keyboard preview scrolling on both providers.
- [x] Reference/final screenshots compared; task browser/hosts/containers/volumes stopped or removed, and local materials retained as authorized.
- [x] Verification/handoff commit SHA: `a32956ae2111779184065e91c4de802c70d51cba`.
- [ ] User release-candidate acceptance.

No B6 application, schema, dependency, startup, configuration or translation change is required. Operator documentation now explicitly explains ending an aborted PostgreSQL script transaction before inspection/retry. Workflow/test/reference documents point to this release-candidate checklist. There is no automatic database upgrade or deployment action.

## Commands and automated checks

Run from the repository root:

```powershell
dotnet test --solution src/Moonglade.slnx
node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs
dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore
```

The full solution includes Web 879 and Setup 79 tests; the final focused Web run also passed 879/879 with zero failed/skipped. Existing real-template HTTP tests cover authorization, antiforgery, complete/invalid/empty envelopes, no preview writes/audit/cache replacement, text/URL/attribute encoding, opening flags and custom-CSS raw-text boundaries. No layout/translated-text/source assertions were added to C# tests. No permanent migration harness, browser dependency or redundant test was introduced.

For the Production checks, publish Release Web into a task-only external folder with a temporary build target removing the private development override from build/publish content. Verify that no `appsettings.Development.json` was copied. An external helper references those published assemblies/static endpoints, uses Production, clears default configuration sources and supplies only synthetic loopback settings. This exercises actual services/Razor/authentication/pipeline with published assets; it is not a real deployment. The ordinary Debug helper likewise references compiled assemblies without copying private overrides.

## Provider verification

Actual providers: PostgreSQL 18.7 and SQL Server 16.00.4325. Task containers were `moonglade-widgets-b6-pg` and `moonglade-widgets-b6-sql`, label `codex.task=moonglade-widgets-b6`, loopback ports 16632/16633. Fresh databases: `widgets_b6_fresh_pg`, `widgets_b6_fresh_sql`; upgraded: `widgets_b6_upgrade_pg`, `widgets_b6_upgrade_sql`. Disposable backup restorations used `widgets_b6_restore_pg`/`widgets_b6_restore_sql`.

| Gate | Both providers observed |
| --- | --- |
| Fresh creation/seed | Target schema created by actual startup, one initial seeded widget, current model/title capacity; then all types/regions and disabled fixtures used for integration. |
| Representative upgrade | 20 legacy rows, including equal-order/equal-UTC GUID ties and more than ten widgets before Profile. IDs/content/enabled/UTC and canonical order matched. All 22 unrelated table datasets remained unchanged during migration/native widget probes. |
| Backup and restoration | Native full backup restored into an independent disposable database; all table datasets matched before upgrading. Hosts serving that target were stopped during the upgrade. |
| Script 12 | Three successful runs per provider, including after edits. Repeated target-schema execution preserved edits and timestamps. Invalid legacy order was rejected without partial conversion; PostgreSQL session explicitly rolled back after the error. |
| Boundaries and transactions | Title 128 persisted and 129 was rejected without writes. A native triggered save failure rolled back all changed rows. Independent writer/reader connections waited on an empty-collection lock; cancellation released the waiter and reads stayed complete. SQL Client may surface explicit cancellation as a database exception; the transaction still rolls back. |

The upgrade fixture retains the current nonwidget schema and recreates the legacy Widget table; scripts 1-11 are prerequisites and were not rerun in this widget-only acceptance. Final synthetic collections contain 18 rows/17 enabled in fresh profiles and 20/19 in upgraded profiles. Browser account/settings/activity writes are intentional test operations; the migration preservation comparison precedes those operations.

## Browser and real HTTP verification

Browser plugin unavailable; regular Playwright CLI 0.1.25 with isolated Microsoft Edge 158.0.4370.33. Development hosts used 18681/18682 (fresh) and 18781/18782 (upgraded); published Production hosts used 18881/18882 against the fresh disposable databases. All host/database bindings were loopback-only and guarded by exact fixture database names.

| Acceptance area | Result |
| --- | --- |
| Core flows in all four profiles | All three types edited and retained after reload, one complete PUT for each initial save, stable surviving creation timestamps, preview/draft separation and actual public cache update. All four regions remain manageable. |
| Security and activity | Anonymous GET/save/preview rejected 401; missing antiforgery or invalid/missing complete collection rejected 400 without mutation. Preview emits no widget audit event; actual saves add widget.updated events. Final preview responses are no-store with scriptless sandbox/CSP and inert content. |
| Two tabs and empty collections | In all four profiles an older complete page removes another tab's addition and restores its deletion using the original ID with new server UTC. PostgreSQL fresh and SQL Server upgrade additionally clear the entire collection, reload empty and restore all IDs from an older page with fresh creation times. |
| Public contracts | Existing About/post URLs, RSS, Atom, sitemap.xml, liveness and readiness returned 200 in every profile. Preview/live sidebar HTML matched, excluding the inactive public subscription modal. Explicit false opens in the same tab; missing legacy flag retains new-tab/rel protection. Disabled widgets are omitted publicly and retained for management. |
| 80 display combinations | Four profiles x five cultures (zh-Hans/zh-Hant/ja-JP/fr-FR/de-DE) x 1440/1920 widths x light/dark. Actual translated built-ins, iframe language, 278px sidebar, desktop media-query viewport and exact public/preview HTML/theme styles matched with no horizontal overflow. Fresh profiles use Blue; upgraded profiles use Green plus captured custom CSS (22px test radius). Custom CSS stays out of admin. |
| Settings and keyboard | Each profile passes left/right, hidden built-ins and disabled sidebar with public/preview parity and four retained anchors. Normal right/all-built-ins settings restored. At 1440x720, Enter selection focuses title, Space activates movement controls, and Tab from Refresh reaches the iframe; PageDown scrolls preview with the toolbar visible. Browser input is tested on the foreground tab. Long titles (128), names and URLs remain editable. Native cross-region drag and movement/placement alternatives retain edits without writes. |
| Failed previews and saves | Invalid drafts retain the last document in all profiles. Injected preview 503 retains draft/document and Refresh recovers; missing images use native fallback without breaking editing. Validation rejection preserves draft/data and retry succeeds. Network abort retains an uncertain draft and requires explicit whole-page overwrite confirmation before another PUT; confirmed retry succeeds. A real injected postcommit cache-eviction failure reports Saved, blocks edits/retry and recovers through confirmed reload; the warmed public cache remains old until its normal invalidation/restart. |
| TOTP and Production | Both providers pass initial account/TOTP setup and a new password-plus-TOTP sign-in; pending second-factor sessions cannot access Widgets. Separately published Production hosts pass reset/re-enrollment/re-login, pending-session 401, actual widget save/public update and keyboard preview scrolling. No authenticator secrets are included in evidence. |
| Final health | Four final pages have no app/Alpine console errors, no horizontal overflow, correct disabled filtering, no-store inert preview and exact public/preview parity. Injected HTTP failures, deliberately missing images and pending-setup favicon 503s are classified test conditions. Production pages have no app console errors. |

## Visual fidelity

Approved `sidebar-composer.jpg` and final 1440/1920 screenshots were viewed together. The left structure/center inline editor/right preview hierarchy, four anchored regions, selected accent edge, expandable item rows, ordinary/advanced field ordering, existing admin typography/theme, top Save/Discard toolbar and independent scrolling match the accepted design. Actual templates/profile/image/data replace the sketch's illustrative content. The existing admin navigation and top toolbar are intentional; built-ins remain read-only. The draft preview follows the real desktop sidebar rather than compressing its media queries.

## Evidence and fixture disposition

Local root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b6/`.

- Automated/native: `solution-tests.log`, `web-tests-final.log`, `node-tests.log`, `web-build.log`, `publish.log`, `host-build.log`, `production-host-build.log`; `{Provider}-{fresh,upgrade}-native.json` and corresponding `-verify-provider.log`.
- Browser: `auth-browser.log`, four `*-core-browser.log`, four `*-cross-tabs-browser.log`, four `*-matrix-browser.log`, `production-browser.log`, `final-browser.log`, `validation-save-browser.log`, `committed-save-browser.log`, `uncertain-save-browser.log`, `preview-recovery-browser.log`, `missing-image-browser.log`.
- Final views: `{Provider}-{fresh,upgrade}-final.jpg`, `{Provider}-production-totp.jpg`; five culture screenshots per profile, `*-{left,hidden,disabled,right}.jpg`, `*-short-keyboard.jpg`, `known-committed.jpg`, `uncertain-overwrite-confirmation.jpg`.
- Canonical synthetic collections: `{Provider}-{fresh,upgrade}-final.json`. Method/path-only counters are scoped to each host lifetime; restart resets them. These are fixture contents, not actual blog data.
- Task browser closed and all six hosts stopped. Ownership-checked containers and volumes, including all six fixture databases and temporary native backups, removed. No listeners remain on 16632/16633/18681/18682/18781/18782/18881/18882.
- Temporary tools/production-tools/runtime/publish directories and helper scripts are retained as user-authorized local reproduction materials (about 668 MiB including evidence). No private development override copies exist. B1/B5 retained materials, B4 pending files and B2 private backup are untouched. Development and production databases were not accessed by B6.

For another machine, reproduce from the committed plan, B1 synthetic fixture specification and the matrix above using newly generated temporary credentials and independent provider databases. Never reinitialize a real blog as a fixture or infer release approval from these test results.

## Operator and acceptance handoff

1. Obtain explicit release/deployment authorization and identify the real target. Stop every application instance and take its own verified full backup.
2. Rehearse required scripts through 11 and provider script 12 on a restored copy; preserve IDs/content/UTC/order and unrelated rows. Repeat 12 after edits and verify no change. Close/ROLLBACK an aborted PostgreSQL session before inspection or retry.
3. Upgrade schema and matching application together. Start hosts and verify protected management, public URLs, complete saves, preview, enabled/order/opening flags, cache and activity. Keep a recovery window.
4. To roll back, stop all hosts, restore the pre-upgrade database backup and matching old application. A binary revert alone cannot restore DisplayOrder or reverse the new schema.
5. User acceptance remains pending. Review the end-to-end edit/preview/drag/save/discard flow and whole-page last-save-wins behavior before accepting the candidate. No production release, deployment, push or external change is included in B6.

Remaining limits: Edge on Windows/loopback and representative Blue/Green/custom themes, not every browser/OS/custom stylesheet or deployment topology. Public caches remain per-instance; another instance may retain warm widgets until its normal invalidation/expiry. Public custom CSS keeps the existing one-hour browser cache, while preview immediately uses captured saved CSS. Real targets require their own backup/restore rehearsal.

Verification/handoff commit SHA: `a32956ae2111779184065e91c4de802c70d51cba`. User acceptance: pending. Next permitted action: obtain explicit B6 acceptance; deployment and real database upgrades require separate authorization.
