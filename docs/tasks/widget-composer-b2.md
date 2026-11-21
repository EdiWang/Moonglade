# Widget Composer B2 Storage and Upgrade Verification

Status: accepted on 2026-11-20 (Asia/Taipei). B3 is accepted and B4 is ready for acceptance; B5-B6 implementation is not authorized. Implementation SHA and acceptance state are maintained in the [delivery plan](widget-composer-plan.md).

## Delivered checkpoint

- Widgets use `WidgetRegion` (`BeforeProfile`, `BeforeTags`, `BeforeCategories`, `AfterCategories`) and nonnegative region-local `SortOrder`. The shared mapping persists region names as strings of at most 32 characters. Both providers use the shared contract.
- `ListWidgetsQuery` sorts by region, order, UTC creation time, and ordinal canonical GUID string. The management page uses that result; public rendering selects enabled widgets at the existing four anchors. Built-in settings and URLs are unchanged.
- Creation, updating, seeding, validation, and the existing editor use the new fields. Create/update JSON requires `region` and `sortOrder`; `displayOrder` is removed. No legacy placement bridge is retained. B4 now uses only collection GET/PUT and the inline composer; individual CRUD and offcanvas/dialogs are retired.
- Title validation and storage both allow 128 characters. Content stays limited to 2000 characters. Placement labels and validation text are translated in all five resource cultures.
- Paired manual script `12.widget-regions.sql` converts legacy placement without changing identifiers, content, type, enabled state, or UTC metadata. It applies the approved timestamp/canonical-ID tie rule, rejects invalid or incomplete source state, and preserves later edits on rerun. See the [operator upgrade and recovery steps](../../database/README.md#widget-regionorder-upgrade-12).

Whole-page saving and transaction-level application locks belong to B3; the composer belongs to B4; real theme preview, dragging, and Link List opening-flag correction belong to B5. These features are not delivered in B2.

## Automated checks

Run from the repository root:

| Command | Result |
| --- | --- |
| `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed; zero warnings/errors. |
| `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | 836 passed; zero failed/skipped. |
| `dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj` | 79 passed; zero failed/skipped. |
| `node --test src\Tests\Moonglade.Web.Tests\JavaScript\*.mjs` | 62 passed; zero failed/skipped. |
| `git diff --check` and document/resource/format checks | Passed. |

Coverage includes command persistence, invalid placements rejected before mutation, deterministic query ordering including submillisecond UTC and canonical-ID ties, disabled rows retained for management, required JSON placement fields, title boundaries, and actual editor module payload/validation behavior. No layout or translated wording assertions were added to C# tests.

## Native provider verification

Disposable loopback-only PostgreSQL 18.7 and SQL Server 16.0.4325.4 databases were created using the real old/new provider contexts and startup initializer. These were synthetic task fixtures, not production backups. Safe fixture names were `widgets_b2_legacy`, `widgets_b2_fresh` (empty legacy collection), and `widgets_b2_new` (new-model fresh installation), on each provider.

Both providers passed:

- Upgrade 20 legacy rows: the committed 18-widget fixture plus two deliberately contrasting canonical/native GUID tie cases. Preserve every ID, title, content, type, enabled value, and creation timestamp; verify all band boundaries and 13 widgets before Profile. Unrelated configuration JSON, post IDs, and owner IDs remain unchanged.
- Reject an out-of-band legacy order and a partial source schema without a partial conversion or fixture data loss.
- Run script 12 repeatedly and compare the result. Move a converted widget, set order 1234 and a 128-character title, rerun, and preserve those edits.
- Persist title lengths 100, 101, and 128; reject 129 without replacing the previously stored title.
- Reject an incompatible target title column, then validate the restored target without changing its rows.
- Convert an empty legacy collection and repeat. Create a fresh target schema through the actual initializer (one widget, 11 configuration groups), then run script 12 twice without changing the seed.

The verification tools used native database connections and assertions against complete widget snapshots. Temporary migration harnesses were kept outside the repository and removed after verification.

## Browser verification

The ChatGPT in-app browser exercised the real compiled Razor pages, application services/pipeline, and public sidebar. The host used synthetic local password authentication, independent data directories, SMTP disabled, and cleared default configuration sources. TOTP was disabled for these fixtures. Browser version was not captured.

| Database path | Viewport | Observed result |
| --- | --- | --- |
| PostgreSQL upgrade | 1440x900 | Save a 128-character title, Before Tags, order 1000; reload/reopen preserves fields. Public About link appears before Tags; disabled widget is absent. |
| SQL Server upgrade | 1920x1080 | Save a 128-character title, After Categories, order 1000; reload preserves 20 rows and placement after Categories. |
| PostgreSQL fresh | 1920x1080 | Populate synthetic content, save Before Categories/order 1000, and verify Tags → About → Categories on the public page. |
| SQL Server fresh | 1920x1080 and 1440x900 | Same save/public-order check; reopen at 1440 confirms persisted placement/order. |

Actual placement labels/options were also checked in `zh-Hans`, `zh-Hant`, `ja-JP`, `fr-FR`, and `de-DE`. No application console errors were observed; only the expected Development-mode warning. The Button Link/Link List editor overlap is resolved by B4's type-specific editor.

## Evidence, cleanup, and remaining acceptance

Local evidence root: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/b2/`.

- Automated logs: `web-build.log`, `web-tests.log`, `setup-tests.log`, `node-tests.log`.
- Native verification: `verify-PostgreSql-{upgrade,empty,target,fresh}.log` and `verify-SqlServer-{upgrade,empty,target,fresh}.log`; fresh initialization and helper build/host logs are also retained.
- Browser evidence: `postgresql-upgrade-editor-1440.jpg`, `sqlserver-upgrade-1920.jpg`, `sqlserver-fresh-editor-1440.jpg`, `fresh-browser.json`, and `localization-browser.json`.
- Cleanup: all fixture hosts stopped; B2 database containers and volumes removed; B2 temporary hosts, migration tools, runtime directories, and launch scripts removed. Logs and screenshots remain. B1's authorized reproduction materials remain.
- Private configuration: generated `appsettings.Development.json` copies were removed from B1/B2 temporary build outputs. The source override was used only in process to resolve the explicitly authorized development migration target; it was not modified or copied into artifacts. Future temporary builds must keep default configuration sources cleared and check generated outputs for private configuration copies before retaining artifacts.

## Development database migration

Completed on 2026-11-20 at 19:30 (Asia/Taipei), with explicit user authorization for PostgreSQL `192.168.0.101:5432/moonglade` as the development database. Server version: 18.7. No local application host or other database client was connected during migration. Upgrade 11's OIDC schema prerequisite was already satisfied; only committed PostgreSQL script 12 was applied.

- Full custom-format database backup: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/dev-db-20261120-192814/backup/pre-widget-regions.dump` (1,148,472 bytes). Windows directory permissions restrict access to the current user and SYSTEM. Keep this backup private: it contains application data and protected account/key material.
- Backup SHA-256: `02F7E36355A684176E0BEF3DE2E4BC406A7A9A310138C5FAA6D82829948E46C8`. Script SHA-256: `F8D738EF862B6B3EBAE8ABD3600F3E9EEF1C0F3BA00AF372A8CC40A4AB5C4CE1`.
- Restored the complete backup into an isolated PostgreSQL 18 database. Verified all widget fields and all 22 unrelated public tables, then applied script 12 twice and verified the expected region/order conversion and preservation after both runs.
- Applied script 12 twice to the development database. All five widgets retain their IDs, content, titles, types, enabled flags, and creation timestamps with the expected region-local order. All 22 unrelated public table row counts and data fingerprints remain unchanged.
- Confirmed target schema: required `region` varchar(32), required `sort_order` integer, and title varchar(128); `display_order` is absent. Existing title nullability is preserved.
- Sanitized verification manifest: `C:/Users/Edi/Documents/Codex/Moonglade-Widgets-Design/dev-db-20261120-192814/result.json`. The rehearsal container, volume, and temporary migration helper were removed; the backup and manifest remain. No application host was started as part of this database operation.

No production data, deployment, or runtime configuration change was made. Other real upgrades still require their own backup/restoration rehearsal and stopped hosts. B3 collection concurrency and B4 composer checks are recorded in their batch records; full themes/settings, TOTP and integrated release acceptance remain B6. Next action: obtain B4 user acceptance; B5 requires separate authorization.
