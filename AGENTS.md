# AGENTS.md

This file is for AI agents working in this repository. Before changing code, read this file, then inspect the nearby implementation and tests to confirm the exact local pattern.

## Project Overview

Moonglade is a personal blogging platform built with ASP.NET Core / .NET 10. The main application host is `src/Moonglade.Web`. It targets developer-focused personal blogs and includes posts, pages, categories, tags, comments, archives, themes, widgets, image storage, syndication feeds, Webmention, IndexNow, email notifications, content moderation, an admin portal, container deployment assets, and optional provider-specific deployment examples.

Moonglade is intended to remain cloud-platform-neutral. Application code, default configuration, and supported runtime behavior must not require Azure or any other cloud provider. Vendor-specific deployment examples may remain in the repository, but they do not define application compatibility requirements and must not drive provider-specific behavior into the core projects.

The solution file is `src/Moonglade.slnx`. The root `README.md` is the main deployment and configuration guide. `AGENTS.md` is the consolidated repository-specific guidance for AI coding agents.

## Technology Stack

| Area | Confirmed stack |
| --- | --- |
| Language/runtime | C# on .NET 10.0 / ASP.NET Core 10.0; projects target `net10.0` with implicit usings enabled. |
| Web app model | ASP.NET Core Razor Pages for public/admin pages, controller-based APIs for admin JSON and public endpoints, endpoint routing for handlers such as health, robots, manifest, sitemap, FOAF, OpenSearch, and virtual site verification files. |
| Architecture style | Multi-project modular solution with LiteBus command/query/event handlers and feature-oriented folders. |
| Data access | EF Core with `BlogDbContext`; SQL Server via `Moonglade.Data.SqlServer`; PostgreSQL via `Moonglade.Data.PostgreSql`. |
| Cache | `Edi.CacheAside.InMemory` with `BlogCachePartition` values `General`, `Post`, `Page`, `RssCategory`, and `AtomCategory`; widgets, sitemap, and uncategorized feeds use keys in the `General` partition. |
| Background work | ASP.NET Core hosted services, `Cronos`, `ScheduledPublishService`, `UpdateCheckService`, `EmailOutboxWorker`, and `CannonService` for queued fire-and-forget work. |
| Authentication | Cookie-based local account authentication and one configurable OpenID Connect provider through the ASP.NET Core OIDC handler. |
| Frontend | Server-rendered Razor, Bootstrap, Bootstrap Icons, Alpine.js, unified Moonglade.Editor for rich HTML post editing plus Markdown/CSS/HTML code-like modes, Tagify, Mermaid.js for Markdown diagrams on post reading pages, and project-local JavaScript modules under `src/Moonglade.Web/wwwroot/js/app`. |
| Image storage | `IBlogImageStorage` with one filesystem implementation and separate public primary and private original-image roots. External storage mounts and CDN origins are operator-owned. |
| External integrations | Webmention, IndexNow, site ownership verification files, email outbox delivery through SMTP or optional Azure Communication Services, local content moderation, and Gravatar. |
| Package management | NuGet package references in project files; no repository-level `Directory.Packages.props`, `NuGet.config`, or package lock file was found at the time this document was updated. |
| Build tools | .NET SDK CLI, Visual Studio, VS Code task `dotnet build ${workspaceFolder}/src/Moonglade.Web/Moonglade.Web.csproj`, Docker multi-stage build, Docker Compose, and optional provider-specific Bicep/PowerShell deployment examples. |
| Tests | xUnit v3, Moq, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, EF Core InMemory/Sqlite patterns, and ASP.NET Core TestHost for Web tests. |
| Formatting/linting | Minimal repository-level `.editorconfig` for UTF-8, CRLF, final newlines, trimmed trailing whitespace, 4-space C#/Razor/JS/JSON/YAML indentation, and tab-indented MSBuild XML. No dedicated analyzer config or lint task was found at the time this document was updated. Follow nearby style and avoid bulk formatting. |

## Configuration And Environment

Primary application configuration is in `src/Moonglade.Web/appsettings.json`, with local overrides in `src/Moonglade.Web/appsettings.Development.json`. ASP.NET Core environment variable overrides use the standard double-underscore form, for example `ConnectionStrings__MoongladeDatabase`.

Important configuration areas:

| Key or section | Purpose | Required? | Notes |
| --- | --- | --- | --- |
| `ConnectionStrings:MoongladeDatabase` | Database connection string. | Yes | Do not document or commit production values. |
| `ConnectionStrings:DatabaseProvider` | Selects `SqlServer` or `PostgreSql`. | Yes | Keep provider names aligned with `AddMoongladeDatabase`. |
| `Authentication:Provider` | Selects `Local` or `OpenIdConnect` authentication. | Yes | Exactly one provider is active for a deployment. |
| `Authentication:OpenIdConnect` | Configures the single external OIDC provider. | Required for OIDC | Requires an HTTPS authority, client ID, externally supplied client secret, callback paths, scopes containing `openid`, name claim type, and an allowed-subject list. An empty list denies all admin access for safe bootstrap. |
| `Authentication:Totp:Issuer` | Display issuer for local-account authenticator app QR codes. | Optional | Defaults to `Moonglade`; the TOTP secret is stored in `LocalAccountSettings`. |
| `Authentication:Totp:Required` | Requires local-account authenticator verification after password sign-in. | Optional | Defaults to `true`; `false` is honored only in the `Development` environment. |
| `Authentication:LocalAccountRateLimit` | Fixed-window rate limiting for local password sign-in and TOTP verification attempts. | Optional | Defaults to enabled, 10 attempts per 1 minute, partitioned by client IP and account context. |
| `CommentRateLimit` | Built-in comment submission rate limiting by client IP and post ID. | Optional | Uses a fixed window policy. |
| `CommentSubmissionGuard` | Built-in comment honeypot and elapsed-time checks. | Optional | Rejects filled honeypot fields, too-fast submissions, and stale form timestamps. |
| `Webmention` | Webmention options, including source rate limiting. | Optional | Preserve protocol endpoint behavior. |
| `Email` | Email provider settings and database outbox worker options. | Optional | Defaults to `smtp` and also supports `AzureCommunication`; store real connection strings and passwords outside source control. `Email:OutboxWorker:Enabled=false` stops in-process delivery but does not prevent enqueueing. |
| `IndexNow` | API key, ping targets, and cooldown interval. | Optional | API key also maps the IndexNow verification file endpoint. |
| `ForwardedHeaders` | Reverse proxy/client IP configuration. | Deployment-dependent | Required behind some proxies/load balancers. Only explicitly configured `KnownProxies` are trusted; an empty or invalid list retains ASP.NET Core's loopback-only defaults. |
| `EnableCSP`, `CSPValue` | Optional Content Security Policy response header. | Optional | `X-Content-Type-Options: nosniff` is always emitted; CSP is emitted only when enabled and non-empty. |
| `ImageStorage` | Configures filesystem image storage through `FileSystemPath`, `OriginalFileSystemPath`, and `CacheMinutes`. | Yes | Both paths must be absolute, writable, durable, distinct, and non-overlapping. Environment overrides are `ImageStorage__FileSystemPath` and `ImageStorage__OriginalFileSystemPath`. |
| `DefaultEditor` | Default post content editor/content type. | Optional | Used during startup backfill for older posts. |
| `PostCacheMinutes`, `PagesCacheMinutes`, `WidgetCacheMinutes` | Cache durations. | Optional | Revisit when changing rendering or invalidation paths. |
| `AutoDatabaseMigration` | Production startup migration behavior. | Optional | Automatic migration is skipped outside `Production`. A stable Production release reads the manifest directly, migrates before configuration initialization writes, and then initializes configuration once. Cumulative scripts update the manifest as their completion marker. When disabled, startup skips migration and version checks; operators are responsible for applying the provider script before deploying a release that requires it. |
| `CannonService:QueueCapacity` | Capacity for the in-process fire-and-forget background queue. | Optional | Defaults to `1000`; when full, new work is rejected and logged instead of running inline on the request path. |
| `EnableUpdateCheck`, `UpdateCheckCron` | GitHub release update check scheduling. | Optional | Cron parsing is handled by `Cronos`. |
| `ViewCount` | Crawler user-agent filtering and deduplication window. | Optional | Affects analytics/view-count behavior. |
| `.env.example` / `MSSQL_SA_PASSWORD` | Docker Compose SQL Server password override. | Local/deployment-dependent | Use a strong secret value outside committed files. |

## Main Business Logic

### Blog Content

- Posts are the core entity, defined by `Moonglade.Data.Entities.PostEntity`. Their lifecycle is represented by `PostStatus`, including draft, published, and scheduled states.
- Post creation and updates are handled by commands such as `Moonglade.Features.Post.CreatePostCommand` and `UpdatePostCommand`. The Web-layer save workflow lives in `Moonglade.Web/Commands/PostManagementCommands.cs` and coordinates create/update, activity logs, cache cleanup, scheduled publishing wake-up, Webmention, and IndexNow.
- Published posts generate a `RouteLink` based on publish date and slug. Do not casually change the published URL shape because old links, RSS/Atom, sitemap, search engines, and caches depend on it.
- Scheduled publishing converts client local time to UTC and is handled by `ScheduledPublishWakeUp` / `ScheduledPublishService`. Persisted and cross-boundary timestamps should use UTC.
- Pages, categories, tags, assets, recycle bin behavior, and related blog features live under the relevant feature folders in `Moonglade.Features`.

### Comments And Moderation

- The public comment entry point is `Moonglade.Web.Controllers.CommentController`; comment creation is handled by `Moonglade.Features.Comment.CreateCommentCommand`.
- Whether comments are enabled, require review, close after a number of days, or use word filtering comes from `IBlogConfig.CommentSettings`.
- Built-in comment creation is also protected by host-level `CommentRateLimit` settings that partition requests by client IP and post ID.
- Built-in comment creation checks `CommentSubmissionGuard` honeypot and elapsed-time fields before dispatching the create command.
- Content moderation is abstracted in `Moonglade.Moderation` and supports local keyword filtering. Do not put moderation behavior directly in controllers.
- Comments, replies, and Webmentions can trigger activity logs and email notifications. Email notification handlers should enqueue to the database outbox; external delivery belongs in `EmailOutboxWorker` instead of request handlers.

### Configuration

- Application-level configuration lives in `src/Moonglade.Web/appsettings.json`, including database, authentication, comment rate limiting, comment submission guard settings, image storage, email, IndexNow, security headers, cache durations, and background task switches.
- Runtime blog settings are managed by `Moonglade.Configuration.BlogConfig` and persisted in the `BlogConfiguration` table. When adding a blog setting, follow the `IBlogSettings<T>` pattern, provide a default value, and consider initialization and update commands.
- `/admin/settings` is the main UI for blog settings. Do not hard-code administrator-configurable blog behavior in the Web layer.

### Authentication And Security

- Authentication logic lives in `Moonglade.Auth` and supports local accounts with TOTP or one standards-based OpenID Connect provider.
- OIDC admin authorization uses the provider-validated issuer boundary plus an exact allowlist of stable `sub` claims. Do not authorize by email, display name, or preferred username.
- `/auth/identity` is available only to an authenticated OIDC user and returns that user's own `iss`, `sub`, and display name for allowlist bootstrap; it does not grant admin access. It returns 404 when local authentication is configured.
- Local-account password sign-in and TOTP setup/verification are protected by `LocalAccountRateLimitPolicy`; keep the default 10 attempts per 1 minute unless there is a deployment-specific reason to adjust `Authentication:LocalAccountRateLimit`.
- Admin Razor Pages are authorized by Razor Pages conventions; API controllers inherit `[Authorize]` from `BlogControllerBase`.
- Controllers use antiforgery validation by default. Use `[IgnoreAntiforgeryToken]` only for deliberate endpoints such as keep-alive or protocol callbacks.
- Do not commit real connection strings, API keys, tenant IDs, or storage credentials. Use configuration binding and environment variable overrides.
- Treat `src/Moonglade.Web/appsettings.Development.json` as a local override that may contain developer secrets; do not quote or copy sensitive values from it into code, docs, logs, commits, or task records.
- Preserve HTTPS, forwarded header, health check, security header, authentication, and content moderation behavior unless the task explicitly targets them.

### Images, Themes, Feeds, And Protocols

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
| Database providers | `src/Moonglade.Data.SqlServer`, `src/Moonglade.Data.PostgreSql` | SQL Server / PostgreSQL EF Core registration and provider-specific behavior. |
| Configuration | `src/Moonglade.Configuration` | Blog setting models, defaults, loading, updates, and initialization-related logic. |
| Authentication | `src/Moonglade.Auth` | Local account, TOTP verification, generic OIDC configuration and validation, admin authorization, login validation, password updates, and authentication registration. |
| Image storage | `src/Moonglade.ImageStorage` | Blog image storage abstraction, file naming, filesystem storage, primary/original path isolation, and storage-related options. |
| Integrations | `src/Moonglade.Email`, `src/Moonglade.IndexNow.Client`, `src/Moonglade.Moderation`, `src/Moonglade.Webmention` | Email outbox delivery, external service clients, protocol send/receive logic, notifications, and moderation. |
| Startup and background work | `src/Moonglade.Setup`, `src/Moonglade.BackgroundServices`, `src/Moonglade.Email` | Startup initialization, database creation/migration, seed data, scheduled publishing, update checks, email outbox delivery, and fire-and-forget background queueing. |
| Presentation helpers | `src/Moonglade.Theme`, `src/Moonglade.Widgets`, `src/Moonglade.Syndication` | Themes, widgets, feeds, and presentation-oriented read models. |
| Shared utilities | `src/Moonglade.Utils`, `src/Moonglade.Web.Middleware` | Cross-cutting utilities, TagHelpers, and reusable middleware. |
| Tests | `src/Tests/Moonglade.*.Tests` | Tests that match the production project or feature area being changed. |

### Web Entry Point

- `src/Moonglade.Web/Program.cs` should stay as startup orchestration: load business assemblies, create the builder, register services, build the app, run startup initialization, attach the request pipeline, and map endpoints.
- Reusable service registration belongs in `IServiceCollection` extension methods in the owning project. Compose Web-host services in `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`, and avoid adding feature-specific registration details directly to `Program.cs` unless the Web host is genuinely the owner.
- Request pipeline and endpoint mapping are centralized in `src/Moonglade.Web/Extensions/WebApplicationExtensions.cs`.
- If a new project contains LiteBus handlers, confirm `Program.LoadAssemblies()` loads that assembly; otherwise command/query/event handlers may not be discovered at runtime.

### CQRS And LiteBus

- The repository uses LiteBus with a command/query/event style.
- Commands mutate state and are usually named `CreateXCommand`, `UpdateXCommand`, or `DeleteXCommand`, with matching `*Handler` classes in the same or nearby files.
- Queries read state and are usually named `GetXQuery`, `ListXQuery`, or `CountXQuery`.
- Events trigger side effects such as email, Webmention, IndexNow, or background notifications.
- Keep the Web layer thin: controllers and Razor Pages handle HTTP binding, authorization, status codes, and view data; business rules belong in the relevant class library command/query/service.

### EF Core

- Use `BlogDbContext` for data access. Add entities under `Moonglade.Data.Entities` and DTO/read models under `Moonglade.Data.DTO`.
- Prefer provider-neutral configuration in `Moonglade.Data.Configurations`; SQL Server/PostgreSQL-specific behavior belongs in the provider projects.
- Keep queries compatible with both SQL Server and PostgreSQL. Avoid scattered provider-specific SQL; isolate it in provider projects if it is truly necessary.
- Prefer `AsNoTracking()` for read-only queries. Use async EF Core APIs and pass `CancellationToken` through write operations and handlers where available.
- Use EF Core set-based operations such as `ExecuteDeleteAsync` when they fit the existing pattern.
- Be careful with many-to-many relationships, cascade behavior, slug/route link generation, publish timestamps, and soft-delete fields because posts, lists, archives, tags, feeds, sitemap, and cache invalidation depend on them.
- Persisted `DateTime` properties whose names end in `Utc` use the shared UTC contract: SQL Server `datetime2(7)`, PostgreSQL `timestamp with time zone`, UTC materialization, and rejection of local or unspecified tracked writes. `PostViewDaily.ViewDateUtc` is a `DateOnly` stored as `date`.
- Browser wall-clock values are accepted only by Web request DTOs. Convert them with the supplied IANA time zone before dispatching feature commands; feature models and persisted values must contain UTC timestamps only.
- Handwritten cumulative migration scripts live under `src/Moonglade.Setup/MigrationScripts`. Preserve explicit UTC conversion, transactional execution, dependent key/index recreation, the final `SystemManifestSettings` completion marker, and backup-based rollback when changing these scripts. Do not re-enable `Npgsql.EnableLegacyTimestampBehavior`.

## Coding Guidelines

### C# / ASP.NET Core

- The target framework is `net10.0`. Follow the existing C# style, including implicit usings, primary constructors, record request models, and feature-local files.
- Use the latest applicable C# features while staying readable and consistent with nearby code.
- Keep namespaces aligned with folders. Put new code in the project and feature folder that owns the behavior.
- Use constructor injection. Do not introduce service locators or unnecessary static mutable state.
- Use async APIs end to end and pass `CancellationToken` through command/query handlers and EF Core calls where available.
- Use UTC for persisted or cross-boundary timestamps. Convert to user/client time only at the UI or boundary layer.
- Prefer nullable-safe code and explicit validation for public inputs.
- Do not use C# anonymous object initializers (`new { ... }`) in C# source or test files. Razor files are excluded from this rule because route values, HTML attributes, and view component arguments commonly use anonymous objects there.
- Use structured logging placeholders, for example `logger.LogInformation("Post updated with ID: {PostId}", post.Id);`.
- Keep comments sparse and useful. Add comments only for non-obvious compatibility, security, localization, protocol, or business decisions.
- Keep changes cross-platform, especially paths, environment variables, reverse-proxy behavior, container behavior, and mounted-filesystem semantics.
- Treat vendor-specific deployment assets, including files under `Deployment`, as optional examples. They must not dictate application architecture, default configuration, storage abstractions, or compatibility requirements. Do not add cloud SDKs, provider detection, or vendor-specific settings to core application projects merely to preserve a deployment example.
- For platform-neutral deployments, preserve standard environment-variable configuration, OCI/Docker compatibility, SQL Server and PostgreSQL support, and separate durable mounts for `/app/images` and `/app/images-origin`. The default Docker image pre-creates both paths for the `app` user, and Compose persists them in separate named volumes.

### HTTP And Error Handling

- Domain handlers should express business outcomes; controllers and pages should translate those outcomes into HTTP responses.
- For new APIs, follow existing response styles: `Ok`, `NoContent`, `NotFound`, `Conflict`, `ValidationProblem`, or ProblemDetails-compatible responses.
- Avoid using ambiguous `null` for multiple failure reasons. For new complex behavior, prefer a lightweight result model that distinguishes not found, validation failure, conflict, forbidden, and success.
- Validate public inputs explicitly. Reuse existing attributes such as `[NotEmpty]`, `[Range]`, and `[Required]` where appropriate.
- Use ProblemDetails-compatible responses for API failures where practical, and preserve existing response shapes for public endpoints unless the task explicitly changes them.
- Do not swallow exceptions from external services silently. Log enough context to diagnose the operation, target resource, and available correlation details.

### Caching And Side Effects

- Write operations must consider cache impact. Changes to posts, pages, categories, tags, widgets, configuration, themes, comments, and assets can affect page caches, post caches, sitemap, feeds, archives, tag/category lists, and widget caches.
- Existing caching uses `Edi.CacheAside.InMemory` and `BlogCachePartition`. Controllers commonly use the `ClearBlogCache` filter, and workflows sometimes call `cache.Remove` directly.
- Prefer centralized invalidation through filters or event handlers over scattered controller-level `cache.Remove(...)` calls when adding new write paths.
- For post and page writes, review sitemap, feed/subscription, per-post/page, archive, tag/category, and widget cache impact.
- For settings and theme changes, review site-wide rendering, custom CSS, manifest, robots, FOAF, and sitemap impact.
- Publishing posts can trigger Webmention and IndexNow. Comments, replies, and Webmentions can trigger email notifications. New side effects should usually be events or background work through existing services such as `CannonService`, LiteBus events, `IWebmentionSender`, `IIndexNowClient`, and the email outbox instead of slow inline request work.
- `CannonService` is an in-process bounded queue controlled by `CannonService:QueueCapacity`; do not run overflow work inline on request paths.
- Preserve hosted service patterns for scheduled publishing and update checks. Configuration-driven background behavior should remain controlled by `appsettings` values.

### Razor, Static Assets, And Localization

- Public and admin pages are primarily Razor Pages and partials under `src/Moonglade.Web/Pages`.
- Admin JSON operations are primarily API controllers under `src/Moonglade.Web/Controllers`.
- Prefer JavaScript modules (`.mjs`) for application scripts. Keep third-party or bundled library files under the existing `wwwroot/lib` or `wwwroot/js/3rd` conventions.
- Frontend code is built around the existing Razor layouts, Bootstrap, Alpine.js, unified Moonglade.Editor modes, and Tagify. Do not add a new frontend framework unless explicitly requested.
- Mermaid code blocks in Markdown posts are rendered on the reading and post-preview pages by `src/Moonglade.Web/wwwroot/js/app/post.mermaid.mjs`, using the local Mermaid.js asset in `src/Moonglade.Web/wwwroot/js/3rd`. Keep Mermaid blocks out of highlight.js processing.
- Code block language support has two UI surfaces: the public post renderer and the admin Moonglade.Editor code sample dialog. When adding a highlight.js language, register the language before `hljs.highlightElement` in `src/Moonglade.Web/wwwroot/js/app/post.highlight.mjs` and also add the language to `codeSampleLanguages` in `src/Moonglade.Web/wwwroot/js/app/admin.editor.module.mjs`, otherwise authors cannot select it from the editor.
- Server-rendered UI text should consider resource files. Supported cultures are currently `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`.
- Non-English shared resources live under `src/Moonglade.Web/Resources/Program.*.resx`; neutral English strings are used as resource keys in Razor/C# code. Razor pages inject `IStringLocalizer<Program>` as `SharedLocalizer`, and DataAnnotations display names are configured to use the same `Program` resource. When adding or renaming any `SharedLocalizer["..."]` key or `[Display(Name = "...")]` text, update all non-English resource files: `Program.zh-Hans.resx`, `Program.zh-Hant.resx`, `Program.de-DE.resx`, and `Program.ja-JP.resx`.

### Documentation And Licenses

- Repository content must be written in English unless the file is a localization resource, such as `src/Moonglade.Web/Resources/Program.*.resx` or third-party language pack files. Unit test data may also contain non-English values when the behavior under test requires them. Do not add non-English text to Markdown files, source code, comments, configuration, or documentation outside localization resources.
- The README states that this blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.
- The repository license is GPL-3.0. Do not remove or rewrite third-party license files casually.
- Do not add license or copyright headers unless explicitly requested.

## Development And Verification

### Common Commands

```powershell
dotnet restore src/Moonglade.Web/Moonglade.Web.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
docker compose up -d
```

The relational migration harness uses `mcr.microsoft.com/mssql/server:2025-latest` and `postgres:18-alpine`. Run `Moonglade.Setup.Tests` with Docker Desktop available when changing database mappings, startup migration order, cumulative SQL, or upgrade documentation. The operator procedure for the v16.4 cutover is documented in `docs/upgrade-v16.4.md`; the filesystem-only image-storage cutover is documented in `docs/upgrade-filesystem-image-storage.md`.

The default local launch URL comes from `src/Moonglade.Web/Properties/launchSettings.json`: `https://localhost:10210`. The admin portal is `/admin`; the default local account is documented in the README. In non-Development environments, or whenever `Authentication:Totp:Required` is `true`, first local-account sign-in after deployment or upgrade requires authenticator app TOTP setup.

Moonglade consumes Moonglade.Editor through the `Moonglade.Editor.StaticAssets` NuGet package, served from `/_content/Moonglade.Editor.StaticAssets/moonglade-editor/`. Do not copy editor build output into `src/Moonglade.Web/wwwroot/lib/moonglade-editor/` or other `wwwroot` paths.

### Testing Conventions

- Behavior changes should add or update tests in the matching test project, for example:
  - Post/category/tag/comment/page behavior: `src/Tests/Moonglade.Features.Tests`
  - Web controller/handler behavior: `src/Tests/Moonglade.Web.Tests`
  - Middleware: `src/Tests/Moonglade.Web.Middleware.Tests`
  - Auth/configuration/theme/syndication/webmention/image storage/moderation/email/indexnow/background/setup: the matching `Moonglade.*.Tests` project.
- Tests use xUnit v3, Moq, and EF Core InMemory/Sqlite patterns.
- When following the existing async test style, use `TestContext.Current.CancellationToken`.
- Prefer focused unit tests for command/query handlers, services, middleware, validators, and protocol generators. Use integration tests when behavior depends on EF/database setup or host-level wiring.
- Prefer running the affected test project. For cross-module changes or startup registration changes, at least run the Web project build.

## Pre-Change Checklist

1. Which business module owns this change? Prefer the owning class library over putting business rules in `Moonglade.Web`.
2. Does it affect database queries, entities, configuration, or database-provider behavior? Check SQL Server and PostgreSQL compatibility.
3. Does it change post URLs, publish timestamps, status transitions, caches, feeds, sitemap, Webmention, IndexNow, or email notifications?
4. Does it require updating `Program.LoadAssemblies()`, a DI extension, default configuration, resource files, or tests?
5. Does it add an external call? Make it configurable, testable, logged, and avoid blocking the main request.
6. Does it touch a security boundary? Check authentication, authorization, antiforgery, moderation, secret configuration, and forwarded headers.
7. Does it affect image storage or CDN behavior? Check primary/original path isolation, filename-to-key mapping, direct browser delivery, mount persistence and permissions, multiple replicas, and the filesystem-only vendor boundary.
8. Does documentation need to change? The README and this file should reflect important developer-facing behavior.

## Agent Working Rules

- Read nearby code and tests before editing. This repository has clear patterns; follow them.
- Keep changes focused. Do not perform unrelated refactors or bulk-format untouched files.
- Do not overwrite user changes. Check `git status --short` before finishing.
- After changes, explain what changed, what was verified, and any remaining risk.
- If verification commands could not be run, say why.

### Complex Task Breakdown

For complex work, first split the request into small sub-tasks that can be implemented, checked, tested, committed, and rolled back independently. Make dependencies explicit, especially when a change crosses projects, affects data shape, changes public endpoints, or alters deployment behavior.

Create a task record under `docs/tasks/` when the work:

- Crosses multiple modules or services.
- Requires multiple rounds of context to complete.
- Is a high-risk refactor or migration.
- Changes architecture, data models, protocol contracts, configuration, or deployment flow.
- Is explicitly requested by the user as a retained task record.

Use `docs/tasks/task-<short-task-name>.md` for task records unless a more specific project convention appears later. Keep the record updated while working so another agent can resume after context compression or interruption. At minimum, record the original goal, background, scope, task breakdown, execution order, dependencies, status, verification log, issues/resolutions, and follow-ups. A starter template lives at `docs/tasks/task-template.md`.

### Documentation Sync Rules

After development, bug fixes, refactors, or configuration changes, check whether the change affects:

- Project positioning or business workflows.
- Runtime, build, test, or deployment steps.
- Technology stack or dependencies.
- Code architecture or module boundaries.
- Environment variables or configuration keys.
- Development conventions.
- Reusable troubleshooting knowledge.

If it does, update the appropriate long-lived docs, including `README.md`, `AGENTS.md`, and relevant files under `docs/`. If it does not, say in the final response that no documentation update was needed.

### Troubleshooting / Lessons Learned

Only add troubleshooting notes after the issue has been fixed and the user has confirmed the outcome, unless the user explicitly asks for a note earlier. Add the note when future developers or agents are likely to hit the same issue, the root cause is project-specific, the fix is non-obvious, or the user asks to preserve it.

Short notes may live here. Longer or multi-issue notes should go in `docs/troubleshooting.md`, with a short summary and link from this file. Use this structure:

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

### Communication Rules

Ask the user before proceeding when business meaning cannot be confirmed, multiple technical interpretations are plausible, a command could affect external services or production data, a change may overwrite important human-maintained documentation, suspected secrets are discovered, or module boundaries are unclear. Keep the question specific and update the relevant documentation after the answer changes project knowledge.
