# AGENTS.md

This file is for AI agents working in this repository. Before changing code, read this file, then inspect the nearby implementation and tests to confirm the exact local pattern.

## Project Overview

Moonglade is a personal blogging platform built with ASP.NET Core / .NET 10. The main application host is `src/Moonglade.Web`. It targets developer-focused personal blogs and includes posts, pages, categories, tags, comments, archives, themes, widgets, image storage, syndication feeds, Webmention, IndexNow, email notifications, content moderation, an admin portal, and Azure/Docker deployment assets.

The solution file is `src/Moonglade.slnx`. The root `README.md` is the main deployment and configuration guide. `.github/copilot-instructions.md` already contains detailed collaboration guidance; this file follows the same intent and adds concise business, architecture, coding, and verification rules for agents.

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
- Content moderation is abstracted in `Moonglade.Moderation` and supports local and remote providers. Do not put provider-specific moderation behavior directly in controllers.
- Comments, replies, and Webmentions can trigger activity logs and email notifications. Slow external calls should go through existing events, `CannonService`, or background mechanisms instead of blocking request handlers.

### Configuration

- Application-level configuration lives in `src/Moonglade.Web/appsettings.json`, including database, authentication, captcha, image storage, moderation, email, IndexNow, cache durations, and background task switches.
- Runtime blog settings are managed by `Moonglade.Configuration.BlogConfig` and persisted in the `BlogConfiguration` table. When adding a blog setting, follow the `IBlogSettings<T>` pattern, provide a default value, and consider initialization and update commands.
- `/admin/settings` is the main UI for blog settings. Do not hard-code administrator-configurable blog behavior in the Web layer.

### Authentication And Security

- Authentication logic lives in `Moonglade.Auth` and supports local accounts and Microsoft Entra ID.
- Admin Razor Pages are authorized by Razor Pages conventions; API controllers inherit `[Authorize]` from `BlogControllerBase`.
- Controllers use antiforgery validation by default. Use `[IgnoreAntiforgeryToken]` only for deliberate endpoints such as keep-alive or protocol callbacks.
- Do not commit real connection strings, API keys, tenant IDs, storage credentials, or captcha shared keys. Use configuration binding and environment variable overrides.

### Images, Themes, Feeds, And Protocols

- Image storage is abstracted in `Moonglade.ImageStorage` and supports Azure Blob Storage and the local file system. New image behavior should depend on `IBlogImageStorage`, not on a concrete provider.
- Themes and custom CSS are handled by `Moonglade.Theme` and `Moonglade.Web.Middleware.StyleSheetEndpoints`.
- RSS, Atom, and OPML generation lives in `Moonglade.Syndication`; OpenSearch, FOAF, manifest, robots, and sitemap handlers live under `Moonglade.Web/Handlers`.
- Preserve the public protocol endpoints listed in the README, including `/rss`, `/atom`, `/opml`, `/opensearch`, `/foaf.xml`, `/webmention`, and `/health`.

## Code Architecture

### Layers And Project Responsibilities

| Area | Path | Responsibility |
| --- | --- | --- |
| Web host | `src/Moonglade.Web` | ASP.NET Core composition root, Razor Pages, API controllers, view components, filters, handlers, endpoint mapping, and static assets. |
| Blog features | `src/Moonglade.Features` | Post, page, category, tag, comment, asset, recycle bin, and view-count commands/queries. |
| Data model | `src/Moonglade.Data` | EF Core `BlogDbContext`, entities, DTO/read models, provider-neutral mappings, and import/export primitives. |
| Database providers | `src/Moonglade.Data.SqlServer`, `src/Moonglade.Data.PostgreSql` | SQL Server / PostgreSQL EF Core registration and provider-specific behavior. |
| Configuration | `src/Moonglade.Configuration` | Blog setting models, defaults, loading, updates, and initialization-related logic. |
| Authentication | `src/Moonglade.Auth` | Local account, Entra ID, login validation, password updates, and authentication registration. |
| Integrations | `src/Moonglade.Email.Client`, `src/Moonglade.IndexNow.Client`, `src/Moonglade.Moderation`, `src/Moonglade.Webmention` | External service clients, protocol send/receive logic, notifications, and moderation. |
| Startup and background work | `src/Moonglade.Setup`, `src/Moonglade.BackgroundServices` | Startup initialization, database creation/migration, seed data, scheduled publishing, update checks, and fire-and-forget background queueing. |
| Presentation helpers | `src/Moonglade.Theme`, `src/Moonglade.Widgets`, `src/Moonglade.Syndication` | Themes, widgets, feeds, and presentation-oriented read models. |
| Shared utilities | `src/Moonglade.Utils`, `src/Moonglade.Web.Middleware` | Cross-cutting utilities, TagHelpers, and reusable middleware. |
| Tests | `src/Tests/Moonglade.*.Tests` | Tests that match the production project or feature area being changed. |

### Web Entry Point

- `src/Moonglade.Web/Program.cs` should stay as startup orchestration: load business assemblies, create the builder, register services, build the app, run startup initialization, attach the request pipeline, and map endpoints.
- Service registration is centralized in `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`.
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
- Be careful with many-to-many relationships, cascade behavior, slug/route link generation, publish timestamps, and soft-delete fields because posts, lists, archives, tags, feeds, sitemap, and cache invalidation depend on them.

## Coding Guidelines

### C# / ASP.NET Core

- The target framework is `net10.0`. Follow the existing C# style, including implicit usings, primary constructors, record request models, and feature-local files.
- Keep namespaces aligned with folders. Put new code in the project and feature folder that owns the behavior.
- Use constructor injection. Do not introduce service locators or unnecessary static mutable state.
- Use structured logging placeholders, for example `logger.LogInformation("Post updated with ID: {PostId}", post.Id);`.
- Keep comments sparse and useful. Add comments only for non-obvious compatibility, security, localization, protocol, or business decisions.
- Keep changes cross-platform, especially paths, environment variables, container behavior, and Linux App Service scenarios.

### HTTP And Error Handling

- Domain handlers should express business outcomes; controllers and pages should translate those outcomes into HTTP responses.
- For new APIs, follow existing response styles: `Ok`, `NoContent`, `NotFound`, `Conflict`, `ValidationProblem`, or ProblemDetails-compatible responses.
- Avoid using ambiguous `null` for multiple failure reasons. For new complex behavior, prefer a lightweight result model that distinguishes not found, validation failure, conflict, forbidden, and success.
- Validate public inputs explicitly. Reuse existing attributes such as `[NotEmpty]`, `[Range]`, and `[Required]` where appropriate.

### Caching And Side Effects

- Write operations must consider cache impact. Changes to posts, pages, categories, tags, widgets, configuration, themes, comments, and assets can affect page caches, post caches, sitemap, feeds, archives, tag/category lists, and widget caches.
- Existing caching uses `Edi.CacheAside.InMemory` and `BlogCachePartition`. Controllers commonly use the `ClearBlogCache` filter, and workflows sometimes call `cache.Remove` directly.
- Publishing posts can trigger Webmention and IndexNow. Comments, replies, and Webmentions can trigger email notifications. New side effects should usually be events or background work.

### Razor, Static Assets, And Localization

- Public and admin pages are primarily Razor Pages under `src/Moonglade.Web/Pages`.
- Admin JSON operations are primarily API controllers under `src/Moonglade.Web/Controllers`.
- Frontend code is built around the existing Razor layouts, Bootstrap, Alpine.js, TinyMCE, Monaco editor, and Tagify. Do not add a new frontend framework unless explicitly requested.
- Code block language support has two UI surfaces: the public post renderer and the admin TinyMCE code sample dialog. When adding a highlight.js language, register the language before `hljs.highlightElement` in `src/Moonglade.Web/wwwroot/js/app/post.highlight.mjs` and also add the language to `codesample_languages` in `src/Moonglade.Web/wwwroot/js/app/admin.editor.module.mjs`, otherwise authors cannot select it from the editor.
- The TinyMCE language folder README says language packs should not be translated directly; use Crowdin instead.
- Server-rendered UI text should consider resource files. Supported cultures are currently `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`.
- Localization uses shared resources under `src/Moonglade.Web/Resources/Program.*.resx`. Razor pages inject `IStringLocalizer<Program>` as `SharedLocalizer`, and DataAnnotations display names are configured to use the same `Program` resource. When adding or renaming any `SharedLocalizer["..."]` key or `[Display(Name = "...")]` text, update all non-English resource files: `Program.zh-Hans.resx`, `Program.zh-Hant.resx`, `Program.de-DE.resx`, and `Program.ja-JP.resx`.

### Documentation And Licenses

- The README states that this blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.
- The repository license is GPL-3.0. TinyMCE includes a GPL-2.0-or-later license notice. Do not remove or rewrite third-party license files casually.
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

The default local launch URL comes from `src/Moonglade.Web/Properties/launchSettings.json`: `https://localhost:10210`. The admin portal is `/admin`; the default local account is documented in the README.

### Testing Conventions

- Behavior changes should add or update tests in the matching test project, for example:
  - Post/category/tag/comment/page behavior: `src/Tests/Moonglade.Features.Tests`
  - Web controller/handler behavior: `src/Tests/Moonglade.Web.Tests`
  - Middleware: `src/Tests/Moonglade.Web.Middleware.Tests`
  - Auth/configuration/theme/syndication/webmention/image storage/moderation/email/indexnow/background/setup: the matching `Moonglade.*.Tests` project.
- Tests use xUnit v3, Moq, and EF Core InMemory/Sqlite patterns.
- When following the existing async test style, use `TestContext.Current.CancellationToken`.
- Prefer running the affected test project. For cross-module changes or startup registration changes, at least run the Web project build.

## Pre-Change Checklist

1. Which business module owns this change? Prefer the owning class library over putting business rules in `Moonglade.Web`.
2. Does it affect database queries, entities, configuration, or provider behavior? Check SQL Server and PostgreSQL compatibility.
3. Does it change post URLs, publish timestamps, status transitions, caches, feeds, sitemap, Webmention, IndexNow, or email notifications?
4. Does it require updating `Program.LoadAssemblies()`, a DI extension, default configuration, resource files, or tests?
5. Does it add an external call? Make it configurable, testable, logged, and avoid blocking the main request.
6. Does it touch a security boundary? Check authentication, authorization, antiforgery, captcha, moderation, secret configuration, and forwarded headers.
7. Does documentation need to change? The README and this file should reflect important developer-facing behavior.

## Agent Working Rules

- Read nearby code and tests before editing. This repository has clear patterns; follow them.
- Keep changes focused. Do not perform unrelated refactors or bulk-format untouched files.
- Do not overwrite user changes. Check `git status --short` before finishing.
- After changes, explain what changed, what was verified, and any remaining risk.
- If verification commands could not be run, say why.
