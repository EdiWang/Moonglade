# Moonglade Engineering Reference

Task-specific guidance supplements [AGENTS.md](../AGENTS.md); read only the applicable sections. Paths in backticks are repository-relative. [README.md](../README.md) covers deployment/configuration. Confirm details in the owning code and tests; ask the user if implementation conflicts with a project constraint.

## Technology Stack

| Area | Confirmed stack |
| --- | --- |
| Language/runtime | C# on .NET 10.0 / ASP.NET Core 10.0; projects target `net10.0` with implicit usings enabled. |
| Web app model | ASP.NET Core Razor Pages for public/admin pages, controller-based APIs for admin JSON and public endpoints, endpoint routing for handlers such as health, robots, manifest, sitemap, FOAF, OpenSearch, and virtual site verification files. |
| Architecture style | Multi-project modular solution with LiteBus command/query/event handlers and feature-oriented folders. |
| Data access | EF Core with `BlogDbContext`; PostgreSQL by default via `Moonglade.Data.PostgreSql`, with optional SQL Server via `Moonglade.Data.SqlServer`. |
| Cache | `Edi.CacheAside.InMemory` with `BlogCachePartition` values `General`, `Post`, `Page`, `RssCategory`, and `AtomCategory`; widgets, sitemap, and uncategorized feeds use keys in the `General` partition. |
| Background work | ASP.NET Core hosted services, `ScheduledPublishService`, `EmailOutboxWorker`, and `CannonService` for queued fire-and-forget work. |
| Authentication | Cookie-based local account authentication and one configurable OpenID Connect provider through the ASP.NET Core OIDC handler. |
| Frontend | Server-rendered Razor, Bootstrap, Bootstrap Icons, Alpine.js, unified Moonglade.Editor for rich HTML post editing plus Markdown/CSS/HTML code-like modes, Tagify, Mermaid.js for Markdown diagrams on post reading pages, and project-local JavaScript modules under `src/Moonglade.Web/wwwroot/js/app`. |
| Image storage | `IBlogImageStorage` with one filesystem implementation and separate public primary and private original-image roots. External storage mounts and CDN origins are operator-owned. |
| External integrations | Webmention, IndexNow, site ownership verification files, email outbox delivery through SMTP, local content moderation, and Gravatar. |
| Package management | NuGet references in project files. |
| Build tools | .NET SDK CLI, Visual Studio, VS Code task `dotnet build ${workspaceFolder}/src/Moonglade.Web/Moonglade.Web.csproj`, Docker multi-stage build, and Docker Compose. |
| Tests | See the [test guide](../src/Tests/README.md); Web tests also use ASP.NET Core TestHost. |
| Formatting | Follow `.editorconfig` and [development conventions](../AGENTS.md#development-and-verification). |

## Configuration And Environment

Host configuration uses `src/Moonglade.Web/appsettings.json`, local Development overrides, and standard environment variables (`:` becomes `__`). See [README](../README.md) for configuration examples. Keep local overrides secret-safe as required by AGENTS.md.

Additional keys and implementation constraints:

| Key or section | Purpose | Required? | Notes |
| --- | --- | --- | --- |
| `ConnectionStrings:MoongladeDatabase` | Database connection string. | Yes | Do not document or commit production values. |
| `ConnectionStrings:DatabaseProvider` | Selects `PostgreSql` (default) or `SqlServer`. | Yes | Keep provider names aligned with `AddMoongladeDatabase`. |
| `Authentication:Provider` | Selects `Local` or `OpenIdConnect` authentication. | Yes | Exactly one provider is active for a deployment. |
| `Authentication:OpenIdConnect` | Single external OIDC provider. | Required for OIDC | See [configuration and bootstrap](upgrade-generic-oidc-authentication.md); preserve validation and exact subject authorization. |
| `Authentication:Totp:Issuer` | Display issuer for local-account authenticator app QR codes. | Optional | Defaults to `Moonglade`; the TOTP secret is stored in `LocalAccountSettings`. |
| `Authentication:Totp:Required` | Requires local-account authenticator verification after password sign-in. | Optional | Defaults to `true`; `false` is honored only in the `Development` environment. |
| `Authentication:LocalAccountRateLimit` | Fixed-window rate limiting for local password sign-in and TOTP verification attempts. | Optional | Defaults to enabled, 10 attempts per 1 minute, partitioned by client IP and account context. |
| `CommentRateLimit` | Built-in comment submission rate limiting by client IP and post ID. | Optional | Uses a fixed window policy. |
| `CommentSubmissionGuard` | Built-in comment honeypot and elapsed-time checks. | Optional | Rejects filled honeypot fields, too-fast submissions, and stale form timestamps. |
| `Webmention` | Webmention options, including source rate limiting. | Optional | Preserve protocol endpoint behavior. |
| `Email` | SMTP and database outbox options. | Optional | See [SMTP configuration, sender fallback, and availability](../README.md#email-notifications). Worker tuning remains under `Email:OutboxWorker`. |
| `IndexNow` | API key, ping targets, and cooldown interval. | Optional | API key also maps the IndexNow verification file endpoint. |
| `ForwardedHeaders` | Reverse proxy/client IP configuration. | Deployment-dependent | Required behind some proxies/load balancers. Only explicitly configured `KnownProxies` are trusted; an empty or invalid list retains ASP.NET Core's loopback-only defaults. |
| `EnableCSP`, `CSPValue` | Optional Content Security Policy response header. | Optional | `X-Content-Type-Options: nosniff` is always emitted; CSP is emitted only when enabled and non-empty. |
| `ImageStorage` | Filesystem roots and metadata cache. | Yes | See [storage configuration and isolation](../README.md#image-storage). |
| `PostCacheMinutes`, `PagesCacheMinutes`, `WidgetCacheMinutes` | Cache durations. | Optional | Revisit when changing rendering or invalidation paths. |
| `CannonService:QueueCapacity` | Capacity for the in-process fire-and-forget background queue. | Optional | Defaults to `1000`; when full, new work is rejected and logged instead of running inline on the request path. |
| `ViewCount` | Crawler user-agent filtering and deduplication window. | Optional | Affects analytics/view-count behavior. |
| `.env.example` / `POSTGRES_PASSWORD` | Docker Compose PostgreSQL password override. | Local/deployment-dependent | Use a strong secret value outside committed files. |

SMTP delivery uses `Edi.TemplateEmail.Smtp` with explicit `FromAddress` and `SenderAddress` set to the effective SMTP sender address. Map `Email:EnableSsl` and the port to `SmtpSettings.SocketOptions`: port 465 uses `SslOnConnect`; other ports use `StartTls` when enabled and `Auto` otherwise. The test email template displays this effective security mode.

`Edi.TemplateEmail` and `Edi.TemplateEmail.Smtp` use version `2026.8.0`. The singleton `IEmailHelper` validates and holds template configuration; each `ForType` call returns an independent `IEmailBuilder` that owns the mappings and builds one message. Keep builders local to each notification and mock `IEmailBuilder` separately from `IEmailHelper` in tests.

HTML email bodies encode ordinary mapped values. Only wrap the already-rendered new-comment body and administrator reply HTML in `TrustedHtml` to preserve the output of `ContentProcessor.MarkdownToCommentHtml`. Keep original comment text, names, titles, and URLs as ordinary mappings; do not pre-encode them. `TrustedHtml` does not sanitize its input.

## Main Business Logic

### Blog Content

- Posts are the core entity, defined by `Moonglade.Data.Entities.PostEntity`. Their lifecycle is represented by `PostStatus`, including draft, published, and scheduled states.
- Post creation and updates are handled by commands such as `Moonglade.Features.Post.CreatePostCommand` and `UpdatePostCommand`. The Web-layer save workflow lives in `Moonglade.Web/Commands/PostManagementCommands.cs` and coordinates create/update, activity logs, scheduled publishing wake-up, Webmention, and IndexNow. Cache invalidation is triggered by feature-level `PostChangedEvent` publication after database commit.
- Recycle, publish, and unpublish endpoints dispatch their feature commands directly from `PostController`, then use the inherited `LogActivityAsync` method. Keep the save workflow for its scheduling and notification coordination; do not add forwarding workflow commands for simple state operations.
- Published posts generate a `RouteLink` based on publish date and slug. Do not casually change the published URL shape because old links, RSS/Atom, sitemap, search engines, and caches depend on it.
- Article state writes use `PostEntityHelper.SetStatus` to assign `PostStatus`, `PubDateUtc`, `ScheduledPublishTimeUtc`, and `RouteLink` together. Only draft, scheduled, and published are valid write states; deletion remains an independent `IsDeleted` flag. Published writes clear scheduled time; draft and scheduled writes clear publication time and route. Callers choose timestamps: editing a published post preserves its publication date, manual publishing uses the current time, and scheduled publishing uses its actual execution time without changing `LastModifiedUtc`. Resolve creation slug conflicts and apply edited slugs before generating the route.
- Scheduled publishing converts client local time to UTC and is handled by `ScheduledPublishWakeUp` / `ScheduledPublishService`. Persisted and cross-boundary timestamps should use UTC.
- Pages, categories, tags, assets, recycle bin behavior, and related blog features live under the relevant feature folders in `Moonglade.Features`.

### Comments And Moderation

- The public comment entry point is `Moonglade.Web.Controllers.CommentController`; comment creation is handled by `Moonglade.Features.Comment.CreateCommentCommand`.
- Whether comments are enabled, require review, close after a number of days, or use word filtering comes from `IBlogConfig.CommentSettings`.
- `CommentSettings.EnableMarkdownLinks` controls comment/reply rendering across public/admin views and newly generated email. Preserve the [documented behavior](../README.md#comment-markdown-links), including default-off links and protocol/rel validation.
- Built-in comment creation is also protected by host-level `CommentRateLimit` settings that partition requests by client IP and post ID.
- Built-in comment creation checks `CommentSubmissionGuard` honeypot and elapsed-time fields before dispatching the create command.
- Content moderation is abstracted in `Moonglade.Moderation` and supports local keyword filtering. Do not put moderation behavior directly in controllers.
- Comments, replies, and Webmentions can trigger activity logs and email notifications. Email notification handlers should enqueue to the database outbox; external delivery belongs in `EmailOutboxWorker` instead of request handlers.

### Configuration

- Ordinary runtime preferences are managed by `Moonglade.Configuration.BlogConfig` and persisted in the `BlogConfiguration` table. When adding an ordinary group, follow `IBlogSettings<T>`, provide `DefaultValue`, and add its property to `BlogSettingsSnapshot` and its typed load to `BlogConfig.LoadFromConfig`. Keep the temporary reader forwarding properties aligned until operation/render migration removes them. This is the single ordinary-default list: loading returns the exact JSON snapshots assigned to memory for `ConfigInitializer` to persist. The nine-group ordinary reader excludes credentials and installation history. Auth owns `LocalAccountSettings`, `ILocalAccountReader`, and `UpdateLocalAccountCommand`; Setup owns `SystemManifestSettings` and may reference Auth. Keep the legacy table, keys, and JSON field names unchanged.
- The nine ordinary groups and nested menu values are sealed, non-positional init-only records; menus/submenus use `ImmutableList`, retaining disabled null menus and legacy JSON arrays. Reuse the current types for HTTP inputs; keep the existing mutable menu JSON envelope. Do not add a parallel DTO/model hierarchy without a demonstrated binding incompatibility. Use `with` to prepare replacements rather than JSON round-trip copying or property mutation.
- `IBlogConfig` is a read-only consumer boundary with `Capture()`; loading and publication belong to the concrete singleton `BlogConfig`. Register that owner and alias the reader to the same instance. A capture retains one immutable nine-group root. Publication exchanges its reference once; old roots/children remain valid. Temporary per-group forwarding reads may each capture different generations: atomic publication alone does not guarantee consistency across a whole operation or render. Capture once and pass the result for related reads; the complete consumer migration is a separate step. Uploaded avatars always render the local asset route through the existing URL helper, preserving PathBase without mutating settings. `GeneralSettings.AvatarUrl` remains inert legacy JSON data and is preserved on general saves for compatibility; never use it for avatar delivery or create a replacement CDN writer.
- Ordinary persisted JSON uses `DefaultValue` for completely absent fields as well as missing groups. `FromJsonWithDefaults` adds only absent top-level properties in memory, using the existing serializer's case-insensitive names and preserving the order of duplicate saved properties. Explicit false/zero/null/empty arrays remain saved values, not missing fields. Valid partial JSON and its timestamp are not rewritten. Keep this behavior at the persisted-loading boundary rather than changing request-model constructors or the shared `FromJson` behavior. Account and manifest parsing is excluded so missing credential/history fields cannot acquire bootstrap defaults.
- The loader and update handler use DataAnnotations and model-owned `IValidatableObject` rules without mutating inputs. Validate every replacement before querying/changing settings rows. `SettingsValidationAttribute` translates command validation failures on settings endpoints to HTTP validation problems; HTTP model validation alone does not protect parsed menu graphs or non-HTTP callers. Keep nested menu validation in its settings model and script/CSS conditions out of controller-only checks.
- Startup restores malformed, incompatible, null, or semantically invalid ordinary settings to whole-group defaults in their existing rows, including otherwise valid sibling fields; valid settings and timestamps are preserved. Explicit saved values are retained only when valid. `AddDefaultConfigurationCommand` serves this startup initialization/repair path only. Invalid persisted `LocalAccountSettings` and `SystemManifestSettings` must stop initialization before repair writes with an actionable key-only error, preserving credentials and migration history. Do not recover these protected settings with shipped defaults or log stored JSON.
- Preserve approved validation compatibility: legacy inactive avatar URL data has no 64-character cap; blank notification names retain deployment fallback; unknown theme IDs retain fallback; feed count allows 0 through `int.MaxValue`; CSS is capped at 10240 UTF-8 bytes; disabled menu arrays may be null. Existing watermark font/text rules apply even when disabled. Account username/hash/salt must be nonblank without imposing new-credential form syntax on saved values; reuse Otp.NET parsing and Auth normalization for populated TOTP secrets, permitting generated-but-disabled setup state. Manifests require a non-minimum UTC time while preserving arbitrary/empty version strings.
- First-install seeding requires all mapped application tables to be empty, not only `BlogConfiguration`. Maintain the emptiness checks and their coverage when adding application tables. An existing empty schema may be initialized. Seed data and default settings share one provider execution-strategy transaction; retries clear tracked inserts and verify a potentially committed installation before reseeding. Never clear existing or ambiguous older partial-install data. Schema creation and noncritical site-icon generation are outside this data transaction.
- `ConfigInitializer.InitializeStoreAsync` validates the account and manifest before preparing any ordinary repairs, then persists detached settings without publishing them. It explicitly stages the two protected defaults after the nine ordinary defaults, keeping all eleven rows in the first-install transaction. Startup reloads committed preferences and publishes the account only afterward. Manifest history has no runtime singleton or ordinary settings writer. A nonempty database with a missing account row fails before any default/repair writes. A missing manifest with a valid account is reconstructed with unknown version and current reconstruction time, with a Warning; valid saved installation metadata remains unchanged. Do not replace a failed new-install transaction with a permanent manual-recovery requirement: after rollback, an empty schema must remain retryable.
- `Moonglade.Setup.WebApplicationExtensions.InitStartUp` runs before the Web pipeline/endpoints and host start. An initialization failure logs Critical and throws, so the process exits nonzero without starting HTTP or hosted business services. Never call `RunAsync` to serve an error-only page on the application host: that also starts scheduled publication and other workers against uninitialized settings. Administrators correct the reported cause and restart; failure does not permanently disable retry.
- Ordinary runtime settings writes use `UpdateConfigurationCommand.Create(settings)`; the two-setting overload saves related image/general settings together. The command captures detached JSON snapshots, validates every target row, saves them in one `SaveChangesAsync`, and only then publishes one complete ordinary root. Inside the shared write gate, apply only submitted groups to the latest captured root before persistence, preserving unrelated concurrent group saves. Same-group saves still replace the entire group; do not introduce field merging or conflict UI. Ordinary and account writers share the singleton `ConfigurationWriteGate` to preserve in-process commit/publication ordering. Missing rows and failed writes throw instead of returning an ignored success code. Callers must prepare new objects or detached copies rather than mutating objects from `IBlogConfig`. Auth readers return detached account copies; `UpdateLocalAccountCommand` captures JSON, validates and saves the legacy row, publishes a detached account, then sends the existing change event with a noncancelable token. Invalid/missing rows and failed saves never publish credentials; no-op saves preserve timestamps. Keep account models/commands as plain classes so generated diagnostic strings cannot include secrets. Unchanged persisted values retain their timestamps while still refreshing the runtime snapshot. Startup initialization remains separate.
- Settings-controller saves and avatar uploads opt in to `BlogControllerBase.LogActivityAfterSaveAsync` only after their save operations complete. This helper isolates activity dispatch failures (including cancellation), preserves the existing success response, and emits Error with the exception, event, operation, and trace identifier, without adding targets, metadata, or credential fields to its diagnostic message. Do not put validation, settings persistence, cache events, asset/file operations, or authentication actions inside its catch. Authenticator reset must still sign out all three local schemes after an audit-only failure; password changes retain their current session behavior. The original `LogActivityAsync`, site-icon auditing, and other controllers keep their existing contracts. Asset/file/settings writes remain separate operations, not a new transaction.
- Changed settings publish one `ConfigurationChangedEvent` after persistence and runtime replacement, while still inside the process write lock, without request cancellation. The Web handler invalidates feeds for General/Feed/Image changes, sitemap for General changes, and theme CSS for Appearance changes; Advanced changes wake the scheduler. No-op, validation, missing-row, failed-save, and pre-commit cancellation paths do not publish. Keep this boundary shared by internal writers rather than adding controller-only cache removals.
- FOAF/sitemap endpoint enable/disable and the singleton email sender display name require a restart; keep settings-page hints accurate in both directions. Scheduled publishing captures its wake token before reading settings, wakes even while disabled, and rechecks cancellation/enable state before dispatch. Do not cancel already dispatched publication when toggling the flag. Notification switches affect new enqueue decisions, not previously queued mail.
- `/admin/settings` is the main UI for blog settings. Do not hard-code administrator-configurable blog behavior in the Web layer.
- `ContentSettings.DefaultEditor` owns the [default post editor](../README.md#default-post-editor). Do not restore the removed host configuration key or historical content-type backfill.

### Authentication And Security

- Authentication logic lives in `Moonglade.Auth` and supports local accounts with TOTP or one standards-based OpenID Connect provider.
- OIDC admin authorization uses the provider-validated issuer boundary plus an exact allowlist of stable `sub` claims. Do not authorize by email, display name, or preferred username.
- `/auth/identity` is available only to an authenticated OIDC user and returns that user's own `iss`, `sub`, and display name for allowlist bootstrap; it does not grant admin access. It returns 404 when local authentication is configured.
- Local-account password sign-in and TOTP setup/verification are protected by `LocalAccountRateLimitPolicy`; keep the default 10 attempts per 1 minute unless there is a deployment-specific reason to adjust `Authentication:LocalAccountRateLimit`.
- Admin Razor Pages are authorized by Razor Pages conventions; API controllers inherit `[Authorize]` from `BlogControllerBase`.
- Controllers use antiforgery validation by default. Use `[IgnoreAntiforgeryToken]` only for deliberate endpoints such as keep-alive or protocol callbacks.

### Images, Themes, Feeds, And Protocols

- Uploaded avatars are database assets served by `/avatar`, with the bundled default used when no asset exists. Only `SaveAssetEventHandler` persists uploaded avatar data; image CDN toggles save `ImageSettings` only. Keep upload validation, avatar cache clearing, and post-save audit isolation. Do not upload/delete avatar copies through `IBlogImageStorage`, rewrite general settings on avatar saves, or reintroduce CDN URL/cache-buster orchestration. Existing legacy CDN files are not automatically removed.

- Image storage is abstracted in `Moonglade.ImageStorage` and has one filesystem implementation. New image behavior should depend on `IBlogImageStorage`, not on `FileSystemImageStorage`, but the interface is an internal application boundary rather than a supported provider extension model.
- `ImageStorage:FileSystemPath` stores public processed images; `ImageStorage:OriginalFileSystemPath` stores private original uploads. The resolved roots must be absolute, distinct, and non-overlapping. Public reads and deletes must never resolve the original root.
- Cloud object storage, network filesystems, mount drivers, sidecars, CSI manifests, Docker volume plugins, credentials, object metadata, and vendor-specific operational behavior remain outside application code and packages. Do not add Azure, S3, or other vendor image-storage providers or optional extensions.
- CDN delivery stays browser-direct and settings-driven. Persisted content and upload responses use `/image/{filename}`; rendered posts/feeds and the image endpoint use `ImageSettings.CDNEndpoint` when redirects are enabled. A CDN origin may expose only the primary root, must preserve filename-to-key mapping and correct media types, and must never expose original images.
- Image uploads are validated by content before storage. SVG upload is supported, but SVG content must pass through the Web-layer sanitizer before primary or original image bytes are saved.
- Themes and custom CSS are handled by `Moonglade.Theme` and `Moonglade.Web.Middleware.StyleSheetEndpoints`.
- RSS, Atom, and OPML generation lives in `Moonglade.Syndication`; OpenSearch, FOAF, manifest, robots, sitemap, and site verification file handlers live under `Moonglade.Web/Handlers`.
- Site verification files are managed from `/admin/settings/verification-files`, persisted in the database, and served as virtual root-level files. They are text-only (`.txt`, `.html`, `.htm`, `.xml`, `.json`), limited to 64 KB, and must use a single safe ASCII file name. Do not write these files into container `wwwroot`.
- Preserve the public protocol endpoints listed in the README, including `/rss`, `/atom`, `/opml`, `/opensearch`, `/foaf.xml`, `/webmention`, `/health`, and `/health/ready`. Keep `/health` liveness-only; use `/health/ready` for database readiness.
- Incoming and outgoing Webmention requests must stay restricted to public HTTP/HTTPS URLs through Edi.AspNetCore.Utils public-HTTP safety services. Reject private, loopback, link-local, documentation, reserved, and other special-use source, target, redirect, or endpoint addresses. Keep socket connections bound to validated DNS results and do not enable automatic redirects, cookies, proxies, or ambient credentials on these clients.

## Code Architecture

### Layers And Project Responsibilities

| Area | Path | Responsibility |
| --- | --- | --- |
| Web host | `src/Moonglade.Web` | ASP.NET Core composition root, Razor Pages, API controllers, view components, filters, handlers, endpoint mapping, and static assets. |
| Blog features | `src/Moonglade.Features` | Post, page, category, tag, comment, asset, recycle bin, and view-count commands/queries. |
| Activity logging | `src/Moonglade.ActivityLog` | Activity log commands, queries, metadata helpers, and event type definitions. |
| Data model | `src/Moonglade.Data` | EF Core `BlogDbContext`, entities, DTO/read models, provider-neutral mappings, and import/export primitives. |
| Database providers | `src/Moonglade.Data.PostgreSql`, `src/Moonglade.Data.SqlServer` | PostgreSQL / SQL Server EF Core registration and provider-specific behavior. |
| Configuration | `src/Moonglade.Configuration` | Blog setting models, defaults, loading, updates, and initialization-related logic. |
| Authentication | `src/Moonglade.Auth` | Local account, TOTP verification, generic OIDC configuration and validation, admin authorization, login validation, password updates, and authentication registration. |
| Image storage | `src/Moonglade.ImageStorage` | Blog image storage abstraction, file naming, filesystem storage, primary/original path isolation, and storage-related options. |
| Integrations | `src/Moonglade.Email`, `src/Moonglade.IndexNow.Client`, `src/Moonglade.Moderation`, `src/Moonglade.Webmention` | Email outbox delivery, external service clients, protocol send/receive logic, notifications, and moderation. |
| Startup and background work | `src/Moonglade.Setup`, `src/Moonglade.BackgroundServices`, `src/Moonglade.Email` | Startup initialization, fresh database creation, seed data, scheduled publishing, email outbox delivery, and fire-and-forget background queueing. |
| Presentation helpers | `src/Moonglade.Theme`, `src/Moonglade.Widgets`, `src/Moonglade.Syndication` | Themes, widgets, feeds, and presentation-oriented read models. |
| Shared utilities | `src/Moonglade.Utils`, `src/Moonglade.Web.Middleware` | Cross-cutting utilities, TagHelpers, and reusable middleware. |
| Tests | `src/Tests/Moonglade.*.Tests` | Tests that match the production project or feature area being changed. |

### Web Entry Point

- `src/Moonglade.Web/Program.cs` should stay as startup orchestration: load business assemblies, create the builder, register services, build the app, run startup initialization, attach the request pipeline, and map endpoints.
- Reusable service registration belongs in `IServiceCollection` extension methods in the owning project. Compose Web-host services in `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`, and avoid adding feature-specific registration details directly to `Program.cs` unless the Web host is genuinely the owner.
- Request pipeline and endpoint mapping are centralized in `src/Moonglade.Web/Extensions/WebApplicationExtensions.cs`.
- If a new project contains LiteBus handlers, confirm `Program.LoadAssemblies()` loads that assembly; otherwise command/query/event handlers may not be discovered at runtime.

### CQRS And LiteBus

- The repository uses LiteBus 7.0.0 with a command/query/event style.
- Commands mutate state and are usually named `CreateXCommand`, `UpdateXCommand`, or `DeleteXCommand`, with matching `*Handler` classes in the same or nearby files.
- Queries read state and are usually named `GetXQuery`, `ListXQuery`, or `CountXQuery`.
- Events trigger side effects such as email, Webmention, IndexNow, or background notifications.

### EF Core

- Use `BlogDbContext` for data access. Add entities under `Moonglade.Data.Entities` and DTO/read models under `Moonglade.Data.DTO`.
- Prefer provider-neutral configuration in `Moonglade.Data.Configurations`; SQL Server/PostgreSQL-specific behavior belongs in the provider projects.
- PostgreSQL converts table and column names to snake_case in `PostgreSqlBlogDbContext`; SQL Server retains PascalCase mappings. Keep C# names unchanged. Existing PostgreSQL databases require the [manual naming upgrade](upgrade-postgresql-snake-case.md) after any required category integer ID upgrade. Constraint, index, and sequence names are retained during this upgrade.
- Keep queries compatible with both SQL Server and PostgreSQL. Avoid scattered provider-specific SQL; isolate it in provider projects if it is truly necessary.
- Prefer `AsNoTracking()` for read-only queries. Use async EF Core APIs and pass `CancellationToken` through write operations and handlers where available.
- Use EF Core set-based operations such as `ExecuteDeleteAsync` when they fit the existing pattern.
- Be careful with many-to-many relationships, cascade behavior, slug/route link generation, publish timestamps, and soft-delete fields because posts, lists, archives, tags, feeds, sitemap, and cache invalidation depend on them.
- `Category.Id` and `PostCategory.CategoryId` are `int`; both supported provider contexts configure category identity generation from 100 with increment 1. Category creation and fresh seeding must leave the ID to the database. Administrative category APIs and post-editor DTOs use integers; public category and subscription URLs retain slugs. Existing GUID category databases require the [manual category upgrade](upgrade-category-int-identity.md).
- Persisted `DateTime` properties whose names end in `Utc` use the shared UTC contract: SQL Server `datetime2(7)`, PostgreSQL `timestamp with time zone`, UTC materialization, and rejection of local or unspecified tracked writes. `PostViewDaily.ViewDateUtc` is a `DateOnly` stored as `date`.
- Browser wall-clock values are accepted only by Web request DTOs. Convert them with the supplied IANA time zone before dispatching feature commands; feature models and persisted values must contain UTC timestamps only.
- Follow the [manual migration rules](../database/README.md) for schema/data changes. Preserve fresh creation/seeding; do not restore automatic upgrades or enable `Npgsql.EnableLegacyTimestampBehavior`.

## Caching And Side Effects

- Write operations must consider cache impact. Changes to posts, pages, categories, tags, widgets, configuration, themes, comments, and assets can affect page caches, post caches, sitemap, feeds, archives, tag/category lists, and widget caches.
- Existing caching uses `Edi.CacheAside.InMemory` and `BlogCachePartition`. Successful post content, metadata, and lifecycle writes, plus category/tag updates and deletes, publish `PostChangedEvent` once after database commit. The Web event handler uses the singleton `PostCacheInvalidator` to clear the entire `Post`, `RssCategory`, and `AtomCategory` partitions and the uncategorized RSS, Atom, and sitemap entries in `General`. Publish without request cancellation so a disconnected client cannot skip invalidation after a successful commit. Failed writes, missing objects, empty batches, and view/request count updates do not invalidate these caches.
- Post readers use `PostCacheInvalidator.GetCacheKey`, feed readers use `GetFeedCacheKey`, and sitemap readers use `SiteMapCacheKey`. Separate versions prevent older in-flight queries from repopulating active caches and let settings refresh feeds or sitemap without flushing article caches. Successful post changes still invalidate all three. Sitemap and feeds expire after one hour. Other controller caches retain their existing rules and `ClearBlogCache` filter. All invalidation is local to the current process.
- Public/admin/auth layouts share `ThemeCacheInvalidator.CacheKey`; appearance saves and successful theme create/delete operations advance its generation. Theme conflict/not-found/failure paths do not invalidate. Preserve unknown-theme fallback. Custom CSS continues to use its HTTP ETag/cache policy; server generations cannot purge downstream caches. Full-feed content comes from `PostContent`, formatted by content type and CDN settings; summary mode uses `ContentAbstract`.
- Prefer centralized invalidation through filters or event handlers over scattered controller-level `cache.Remove(...)` calls when adding new write paths.
- For post and page writes, review sitemap, feed/subscription, per-post/page, archive, tag/category, and widget cache impact.
- For settings and theme changes, review site-wide rendering, custom CSS, manifest, robots, FOAF, and sitemap impact.
- Publishing posts can trigger Webmention and IndexNow. Comments, replies, and Webmentions can trigger email notifications. New side effects should usually be events or background work through existing services such as `CannonService`, LiteBus events, `IWebmentionSender`, `IIndexNowClient`, and the email outbox instead of slow inline request work.
- `CannonService` is an in-process bounded queue controlled by `CannonService:QueueCapacity`; do not run overflow work inline on request paths.
- Preserve hosted service patterns for scheduled publishing. Configuration-driven background behavior should remain controlled by `appsettings` values.

## Frontend And Localization

- Public and admin pages are primarily Razor Pages and partials under `src/Moonglade.Web/Pages`.
- Admin JSON operations are primarily API controllers under `src/Moonglade.Web/Controllers`.
- Prefer JavaScript modules (`.mjs`) for application scripts. Keep third-party or bundled library files under the existing `wwwroot/lib` or `wwwroot/js/3rd` conventions.
- Frontend code is built around the existing Razor layouts, Bootstrap, Alpine.js, unified Moonglade.Editor modes, and Tagify. Do not add a new frontend framework unless explicitly requested.
- Mermaid code blocks in Markdown posts are rendered on the reading and post-preview pages by `src/Moonglade.Web/wwwroot/js/app/post.mermaid.mjs`, using the local Mermaid.js asset in `src/Moonglade.Web/wwwroot/js/3rd`. Keep Mermaid blocks out of highlight.js processing.
- Code block language support has two UI surfaces: the public post renderer and the admin Moonglade.Editor code sample dialog. When adding a highlight.js language, register the language before `hljs.highlightElement` in `src/Moonglade.Web/wwwroot/js/app/post.highlight.mjs` and also add the language to `codeSampleLanguages` in `src/Moonglade.Web/wwwroot/js/app/admin.editor.module.mjs`, otherwise authors cannot select it from the editor.
- Server-rendered UI text should consider resource files. Supported cultures are currently `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`.
- Non-English shared resources live under `src/Moonglade.Web/Resources/Program.*.resx`; neutral English strings are used as resource keys in Razor/C# code. Razor pages inject `IStringLocalizer<Program>` as `SharedLocalizer`, and DataAnnotations display names are configured to use the same `Program` resource. When adding or renaming any `SharedLocalizer["..."]` key or `[Display(Name = "...")]` text, update all non-English resource files: `Program.zh-Hans.resx`, `Program.zh-Hant.resx`, `Program.de-DE.resx`, and `Program.ja-JP.resx`.

Moonglade consumes Moonglade.Editor through the `Moonglade.Editor.StaticAssets` NuGet package, served from `/_content/Moonglade.Editor.StaticAssets/moonglade-editor/`. Do not copy editor build output into `src/Moonglade.Web/wwwroot/lib/moonglade-editor/` or other `wwwroot` paths.

## Local Runtime

Use [Build & Run](../README.md#build--run) for commands, launch URLs, and the default local account; URLs come from `src/Moonglade.Web/Properties/launchSettings.json`. The TOTP bypass applies only in Development when `Authentication:Totp:Required=false`.

## Migration Verification

No automatic migration implementation, embedded migration SQL, or migration version-check tests remain. Startup tests cover existing databases with missing or older installation manifests and preserve stored content and manifest values. Startup does not check database version compatibility; administrators must complete required SQL upgrades before launching the new version.

Run `Moonglade.Setup.Tests` when changing database mappings, initialization, or upgrade documentation. Follow [database verification and execution](../database/README.md#verification-and-execution), including representative pre-upgrade and repeat runs on both target providers. Review UTC conversion, transactions, dependent keys/indexes, and backup rollback. In-memory tests and source review do not prove relational correctness or idempotency; report unavailable database verification explicitly.

Category identity and snake_case migration tests use opt-in disposable PostgreSQL and SQL Server containers. See the [category upgrade guide](upgrade-category-int-identity.md#automated-verification) for the Docker prerequisites and `MOONGLADE_TEST_CATEGORY_DATABASES` command; a normal test run skips these relational groups explicitly.

The filesystem-only image-storage cutover is documented in [upgrade-filesystem-image-storage.md](upgrade-filesystem-image-storage.md).

## Change Impact Checklist

1. Which business module owns this change? Prefer the owning class library over putting business rules in `Moonglade.Web`.
2. Does it affect database queries, entities, configuration, or database-provider behavior? Check SQL Server and PostgreSQL compatibility.
3. Does it change post URLs, publish timestamps, status transitions, caches, feeds, sitemap, Webmention, IndexNow, or email notifications?
4. Does it require updating `Program.LoadAssemblies()`, a DI extension, default configuration, resource files, or tests?
5. Does it add an external call? Make it configurable, testable, logged, and avoid blocking the main request.
6. Does it touch a security boundary? Check authentication, authorization, antiforgery, moderation, secret configuration, and forwarded headers.
7. Does it affect image storage or CDN behavior? Check primary/original path isolation, filename-to-key mapping, direct browser delivery, mount persistence and permissions, multiple replicas, and the filesystem-only vendor boundary.
8. Does documentation need to change? The README, root AGENTS.md, and this reference should reflect important developer-facing behavior.

## Troubleshooting Note Format

The confirmation requirement and placement rules are in [AGENTS.md](../AGENTS.md#troubleshooting--lessons-learned).

```markdown
## Troubleshooting / Lessons Learned

### Issue title

- Symptom:
- Trigger:
- Root cause:
- Fix:
- Verification:
- Prevention:
```
