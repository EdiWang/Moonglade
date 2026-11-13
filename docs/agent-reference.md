# Moonglade Engineering Reference

This reference describes implementation contracts by task. Follow [AGENTS.md](../AGENTS.md) for repository-wide rules and [README.md](../README.md) for operator instructions. Paths are relative to the repository root.

## Layers and Project Responsibilities

| Project under `src/` | Responsibility |
| --- | --- |
| `Moonglade.Web` | Host composition, Razor Pages, controllers, middleware wiring, endpoint handlers, and assets |
| `Moonglade.Features` | Post, page, category, tag, comment, asset, recycle bin, and view-count commands and queries |
| `Moonglade.Data` | `BlogDbContext`, entities, mappings, read models, and import/export |
| `Moonglade.Data.PostgreSql`, `Moonglade.Data.SqlServer` | Provider registration and mappings |
| `Moonglade.Configuration`, `Moonglade.Setup` | Ordinary settings and writes, database creation, installation metadata, and initialization |
| `Moonglade.Auth` | Owner profile, avatar, credentials, TOTP, and optional OIDC binding |
| `Moonglade.ActivityLog` | Canonical event catalog, activity persistence, and safe detail validation |
| `Moonglade.BackgroundServices`, `Moonglade.Email` | Scheduled publishing, bounded background work, and SMTP outbox delivery |
| `Moonglade.ImageStorage`, `Moonglade.Theme`, `Moonglade.Widgets`, `Moonglade.Syndication` | Filesystem images, themes, widgets, and feeds |
| `Moonglade.Moderation`, `Moonglade.Webmention`, `Moonglade.IndexNow.Client` | Moderation and protocol integrations |
| `Moonglade.Utils`, `Moonglade.Web.Middleware` | Shared utilities and middleware |

### Web Entry Point

- Put reusable DI registrations in the owning project's extensions. Web composes them in `Extensions/ServiceCollectionExtensions.cs` and sets up the pipeline and endpoints in `Extensions/WebApplicationExtensions.cs`.
- LiteBus commands mutate state, queries read it, and events notify subscribers. Keep handlers near their commands, queries, or events. Check `Program.LoadAssemblies()` when adding a handler assembly.

## EF Core

- Keep shared mappings in `Moonglade.Data.Configurations`. PostgreSQL uses snake_case table and column names, and SQL Server uses PascalCase. Keep C# names provider-neutral and preserve constraint/index behavior in both providers.
- Use async queries and `AsNoTracking()` for read-only work. Review keys, cascades, and set-based writes against both providers.
- Category IDs are database-generated integers starting at 100 and increasing by 1. Leave allocation to the database on create and seed. Admin APIs and editor payloads use integer category IDs, article IDs remain GUIDs, and public category and feed URLs use slugs.
- Persist `*Utc` timestamps as PostgreSQL `timestamp with time zone` and SQL Server `datetime2(7)`. The shared mapping materializes UTC and rejects tracked local/unspecified writes. `PostViewDaily.ViewDateUtc` is a `DateOnly` stored as `date`. Convert browser wall-clock values using the supplied IANA zone in Web before dispatch. Keep legacy Npgsql timestamp behavior disabled.

### Migration Verification

Follow the [database authoring and execution guide](../database/README.md) for script order, transactions, and repeat execution. Check both providers on representative disposable databases. Verify dependent schema and data after the first run and preservation after the second. Use temporary fixtures and remove them after verification. Run `Moonglade.Setup.Tests` for mapping or initialization changes and report any provider left unverified. Startup creates fresh databases and relies on the operator to complete required upgrades before launch.

## Configuration

- `IBlogConfig.Capture()` returns one immutable `BlogSettingsSnapshot`. Capture once per operation or render and pass that root or its relevant group through related calls. Razor uses `ViewData.CaptureSettings` and `BlogSettingsFilter` to share the capture with layouts and components. Keep in-process settings context out of HTTP and durable email payloads.
- Ordinary groups implement `IBlogSettings<T>` and declare `DefaultValue`. Add groups to the snapshot and `BlogConfig.Parse`. Groups and nested menu values are immutable. Auth owns the account, avatar, and OIDC stores. Setup owns `SystemManifestSettings`.
- `UpdateConfigurationCommand` replaces one group. `PatchConfigurationCommand` applies explicitly allowed fields to the latest group inside the same `ConfigurationWriteGate`. Build replacements with `with`, validate with the model's shared rules, save before publishing the complete snapshot, then publish `ConfigurationChangedEvent` for changed persisted JSON without request cancellation. Missing rows and failed writes surface to callers. Unchanged JSON preserves its timestamp and can still refresh the runtime snapshot.
- The write gate also compares effective values for audit. Preserve structural menu equality and keep that comparison independent of persisted JSON equality. `ConfigurationSaveResult` returns only the group, effective change flag, and approved scalar projections. Web uses this result for activity logging. Keep editable-field and audit-field allowlists separate.
- Keep validation shared by startup, HTTP, and internal writes. Preserve the 10,240 UTF-8 byte CSS limit, unlimited feed count at zero, disabled menus with null arrays, watermark constraints, blank SMTP sender-name fallback, and unknown-theme fallback.
- At persisted load, absent fields use current defaults. Preserve explicit valid false, zero, null, and empty arrays, and leave valid partial JSON and timestamps intact. Invalid ordinary JSON or model values trigger whole-group repair. Validate the manifest before repairs. A missing manifest with ordinary configuration is reconstructed with unknown version and current UTC time. Invalid manifests or nonempty databases without recognized installation configuration fail startup.
- `StartUpInitializer` seeds only when every application table is empty. Include new tables in that check. Seed data and settings share one execution-strategy transaction with commit verification. Preserve retry after rollback and existing data. Publish the prepared settings after persistence. Schema creation and noncritical icon generation are separate. Initialization failure exits before serving HTTP or starting business workers.

## Activity History

- `ActivityEvents` owns stable keys, groups, label keys, and safe detail policies. Persist UTC occurrence time and detached actor/target snapshots. Keep `eventKeys` filters string-based and order the catalog by canonical group and key. Clear all is the only admin deletion action, and public comment submissions stay outside it.
- Ordinary settings events store `Changes` with approved `Key`, `OldValue`, and `NewValue` scalar members. An unchanged effective value produces no event. A restricted-only change stores an empty array. Configuration owns the projection and `SettingsAuditFields`. ActivityLog validates the per-event envelope for both writes and reads. Keep supported stored keys readable and render values as text with localized labels.
- Use Web's `ActivityLogDispatcher` after the business operation. Audit failures preserve the business result and use payload-free diagnostics with event key, failure type, and available trace ID. The writer detaches a failed insert without disturbing unrelated tracked changes. Validation, persistence, cache, file, and sign-out failures retain their normal behavior.

## Blog Content

- `PostEntityHelper.SetStatus` assigns status, publication UTC time, scheduled UTC time, and route together. Deletion is an independent flag. Published posts keep date-and-slug routes. Editing a published post preserves its publication date. Manual publication uses the current time and scheduled publication uses the actual execution time while preserving `LastModifiedUtc`.
- Web's `PostManagementCommands` coordinates saves, activity, scheduler wake-up, Webmention, and IndexNow. Simple recycle, publish, and unpublish endpoints dispatch feature commands directly.
- `ScheduledPublishService` includes overdue posts, excludes deleted posts and missing schedule times, and rechecks the enable switch immediately before dispatch. Settings writes wake it even while disabled. Work already dispatched runs to completion.
- `PublishScheduledPostCommand` returns the affected count. After commit it attempts cache notification and independently hands off read-only ID/title/publication UTC snapshots in `ScheduledPostsPublishedEvent`, even if cache notification fails. Preserve the cache exception. Web records one best-effort `post.automatically-published` System event per snapshot and continues after individual failures. The handoff is in-process. Keep Features independent of Web and ActivityLog.

## Comments and Moderation

- `CommentController` applies host rate limiting and the form guard before `CreateCommentCommand`. Feature rules use the captured comment settings and delegate keyword moderation to `Moonglade.Moderation`.
- Share the comment/reply Markdown policy across public views, admin views, and email. Retain URL validation and output encoding.
- Notification handlers capture the recipient and settings before enqueueing. The worker owns SMTP delivery. Keep template builders local to one message. Use `TrustedHtml` only for already-rendered comment/reply bodies and leave names, titles, and URLs in the normal encoded mappings. Keep the explicit patched `System.Security.Cryptography.Pkcs` reference in Email and SQL Server until their transitive dependencies resolve a patched version.

## Authentication and Security

- Auth stores one owner with a stable ID, normalized email, independent Local initialization marker, and revision concurrency. `IOwnerProfileReader` exposes credential-free data used by public rendering, Atom, FOAF, and notifications. Atom retains the author name when the email is absent. Owner writes commit within Auth, and failed writes detach the attempted entity.
- `LocalAccountSetupGateMiddleware` enforces the pending-setup boundary. Keep health and setup reachable and preserve manifest-mapped static assets via `MapStaticAssets().ShortCircuit()`. Setup and later TOTP enrollment keep credentials or secrets in protected, expiring tickets until proof succeeds. Intermediate cookies never grant administrator access.
- Application cookies validate the owner ID and revision. Password changes refresh the current session while retaining sign-in origin and TOTP. Email changes require current-password proof and revoke account sessions. Authenticator reset signs out Local schemes without reopening setup. Preserve the shared client-IP quota for all setup POSTs and the account/IP partitions for sign-in and subsequent TOTP.
- Remembered-device trust uses a separate, non-administrator cookie scheme, issued only after successful login TOTP with explicit opt-in. Validate it only after password authentication, against the owner ID and revision with TOTP still enrolled. Keep the 30-day persistent lifetime absolute, retain trust on ordinary sign-out, and never bind it to IP or User-Agent. Account revision changes revoke trust. Do not apply it to enrollment, management proof, or OIDC.
- Auth registers application Data Protection with `PersistKeysToDbContext<BlogDbContext>()` and the stable `Moonglade` application name. It shares the existing key table with OIDC credentials, whose separate `Moonglade.OidcCredentials` application name and `OidcClientSecret.v1` purpose must remain unchanged. Preserve both namespaces and stored keys across deployments. Database initialization runs before the host starts and protection keys are generated.
- OIDC management uses owner password and applicable TOTP proof. Bind through the framework authorization code flow with PKCE, validated provider identity and correlation, and explicit antiforgery-protected confirmation. Bind proof to the initiating session and configuration revision. Sign-in matches validated issuer and subject against the enabled binding and issues the local principal. OIDC sessions validate the binding revision. Unbinding, disabling, or deleting revokes them, while secret rotation retains their session revision. Protect the client secret with the dedicated database-backed Data Protection key ring.
- Keep admin page authorization conventions and `BlogControllerBase` API authorization. Preserve default controller antiforgery validation and restrict exemptions to intentional protocol or keep-alive endpoints. Keep reverse-proxy trust limited to configured proxies and framework defaults.

## Images, Themes, Feeds, and Protocols

- Depend on `IBlogImageStorage` for article images. Keep public and private roots isolated for reads, writes, and deletes. Validate file content and sanitize SVG before either copy is saved. Keep persisted article image URLs on `/image/{filename}` and apply CDN settings at delivery/render time.
- `OwnerAvatarStore` owns the uploaded avatar and Gravatar preference in `OwnerAvatar`, independently of credentials, ordinary settings, and `BlogAsset` site icons. Concurrent writes preserve the other avatar field. Uploads invalidate the server-side avatar cache. The local avatar route requires HTTP cache revalidation and uses a content ETag for uploaded images. It serves the bundled fallback when no image is uploaded, and public Gravatar needs an owner profile.
- `Moonglade.Theme` and `StyleSheetEndpoints` own themes and custom CSS. `Moonglade.Syndication` owns RSS, Atom, and OPML. Pass captured settings and owner identity to feed generation. Full feeds format the post body by content type and CDN settings, while summary feeds use the abstract.
- Root-level verification files are virtual database-backed responses. Use safe ASCII basenames of up to 128 characters and the text formats `.txt`, `.html`, `.htm`, `.xml`, and `.json`, up to 64 KB. Keep application files and `wwwroot` separate from verification storage.
- Keep Webmention requests on the public-HTTP safety client for source, target, discovery, redirects, and delivery. Connect to validated DNS results. Keep automatic redirects, cookies, proxies, and ambient credentials disabled.

## Caching and Side Effects

- Post, category, and tag mutations publish `PostChangedEvent` after commit without request cancellation when they affect article output. `PostCacheInvalidator` clears the post and category feed partitions and advances post, feed, and sitemap generations. Failed or empty writes and view counters leave those generations alone. Page changes can invalidate sitemap independently.
- Preserve generation-based cache keys so in-flight readers cannot refill active keys with stale content. Feed keys vary by captured General/Feed/Image settings and owner description, with owner email also included for Atom. Sitemap keys vary by the captured SEO canonical prefix. Cache factories use the same captured inputs as their keys. FOAF and sitemap check the current capture's enable switch before reading cache or generating output.
- Configuration events refresh feeds for General/SEO/Feed/Image, sitemap for General/SEO, and theme CSS for Appearance. Advanced changes wake scheduled publishing. Profile changes advance feed generations without evicting articles. Theme creation/deletion advances the theme generation on success. Feed and sitemap entries expire after one hour. Custom CSS retains HTTP ETag behavior.
- Use the existing LiteBus events, `CannonService`, or email outbox for side effects. Keep overflow from the bounded in-process queue off the request path.

## Frontend and Localization

- Build on Razor, Bootstrap, Alpine.js, and existing JavaScript modules under `wwwroot/js/app`. Use shared admin classes and Bootstrap components before adding page-local styles. Third-party assets belong under `wwwroot/lib` or `wwwroot/js/3rd`. Load Moonglade.Editor from the `Moonglade.Editor.StaticAssets` package's `/_content/` assets.
- Keep secondary navigation as links with `aria-current="page"` and action toolbars outside the `.admin-main-body` scroller. Preserve the separate full-screen editor layout. Use shared admin panels, list separators, pagination, tables, and card grids. Keep page-size focus during refresh and return offcanvas focus to the visible opener.
- Settings and Account share the `.admin-settings-page` row layout. Keep disclosure controls keyboard-operable and independent of setting inputs and writes. The profile and Gravatar rows share one group with one divider, and Gravatar saves independently. Preserve the `.blogadmin-avatar` refresh hook. Menu expansion is transient and the toolbar Save submits the menu draft. Advanced script guidance belongs in the field's Change dialog.
- The Account avatar editor previews a circular crop and posts only the selected 300 × 300 JPEG to the existing upload endpoint. Cropping and zooming happen in the browser, and closing the editor discards the selection.
- Use local Mermaid rendering in reading and preview pages and exclude Mermaid blocks from highlight.js. Register new code sample languages in both `post.highlight.mjs` and `admin.editor.module.mjs`.
- Use neutral English resource keys with `IStringLocalizer<Program>` for shared text and DataAnnotations. Update all five `Program.*.resx` translations for changed keys. Keep `zh-Hant` terminology from Hong Kong and `fr-FR` terminology from France. Preserve format placeholders and pass translated strings as text or data attributes.
