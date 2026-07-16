# Copilot Instructions

## Project Overview
- Moonglade is an ASP.NET Core personal blogging platform targeting .NET 10. It includes public Razor Pages, an admin portal, APIs, background services, syndication, webmention, image storage, content moderation, email notifications, IndexNow, authentication, and Azure-friendly deployment assets.
- The solution is intentionally split into focused projects under `src/`: `Moonglade.Web` is the composition root and UI host; domain features live in feature-oriented class libraries such as `Moonglade.Features`, `Moonglade.Configuration`, `Moonglade.ActivityLog`, `Moonglade.Theme`, `Moonglade.Widgets`, `Moonglade.Auth`, and `Moonglade.ImageStorage`; integration-focused projects include `Moonglade.Email.Client`, `Moonglade.IndexNow.Client`, `Moonglade.Moderation`, and `Moonglade.Webmention`.
- Data access is centered on EF Core `BlogDbContext` in `Moonglade.Data`, with provider-specific setup in `Moonglade.Data.SqlServer` and `Moonglade.Data.PostgreSql`. Keep SQL Server and PostgreSQL support in mind when changing queries, migrations, mappings, and date/time behavior.

## Architecture Guidelines
- Keep `Moonglade.Web` thin. Use controllers, Razor Pages, view components, map handlers, middleware, and filters for HTTP concerns only. Put business behavior in the relevant class library and invoke it through LiteBus command/query/event mediators.
- Follow the existing CQRS style: define request records such as `CreatePostCommand`, `ListPostsQuery`, or `SaveAssetEvent`, then implement a matching `*Handler` class. Commands mutate state, queries read state, and events trigger side effects.
- Prefer feature-local files and namespaces. Add post behavior under `Moonglade.Features.Post`, comment behavior under `Moonglade.Features.Comment`, configuration behavior under `Moonglade.Configuration`, and so on.
- Register reusable services with `IServiceCollection` extension methods in the owning project. Compose web services in `Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`, and keep request pipeline or endpoint wiring in `Moonglade.Web/Extensions/WebApplicationExtensions.cs`. Avoid adding feature-specific registration details directly to `Program.cs` unless the Web host is genuinely the owner.
- Preserve assembly scanning in `Program.LoadAssemblies()` and LiteBus registration. If a new project contains commands, queries, or events that must be discovered at runtime, ensure the assembly is loaded by the Web host.
- Use constructor injection and primary constructors where they match the existing style. Do not introduce service locators or static mutable state.

## Module Responsibilities
| Area | Project or Folder | Responsibility |
| --- | --- | --- |
| Web host and UI | `Moonglade.Web` | Composition root, controllers, Razor Pages, view components, filters, middleware, endpoint mapping, static assets, and web-only adapters. |
| Blog features | `Moonglade.Features` | Post, page, category, tag, comment, asset, recycle bin, and view-count commands/queries. |
| Configuration | `Moonglade.Configuration` | Blog settings, strongly typed settings models, default configuration, and configuration update commands. |
| Authentication | `Moonglade.Auth` | Local account and Microsoft Entra ID authentication settings, schemas, login/password commands, and auth service registration. |
| Data model | `Moonglade.Data` | EF Core `BlogDbContext`, entities, DTOs/read models, data export/import primitives, and provider-neutral mappings. |
| Database providers | `Moonglade.Data.SqlServer`, `Moonglade.Data.PostgreSql` | Provider-specific EF Core registration, provider-specific context behavior, retry policies, and database compatibility details. |
| Integrations | `Moonglade.Email.Client`, `Moonglade.IndexNow.Client`, `Moonglade.Moderation`, `Moonglade.Webmention` | External service clients, protocol send/receive logic, moderation, notification, and related event handlers. |
| Image storage | `Moonglade.ImageStorage` | Blog image storage abstractions, file naming, local file system storage, Azure Blob storage, and storage-related options. |
| Presentation helpers | `Moonglade.Theme`, `Moonglade.Widgets`, `Moonglade.Syndication` | Theme data, widgets, feed generation, OPML, sitemap, OpenSearch, FOAF, and subscription-oriented read models. |
| App lifecycle | `Moonglade.Setup`, `Moonglade.BackgroundServices` | Startup initialization, migration orchestration, scheduled publishing, update checks, and background queue processing. |
| Shared utilities | `Moonglade.Utils`, `Moonglade.Web.Middleware` | Cross-cutting utilities and reusable middleware that do not own blog domain behavior. |
| Tests | `src/Tests/Moonglade.*.Tests` | Focused tests matching the production project or feature area being changed. |

## Error Handling And Results
- Prefer explicit command/query outcomes over ambiguous `null` returns when adding new behavior. Use or introduce lightweight result models that distinguish not found, validation failure, conflict, forbidden, and success states.
- Keep HTTP status mapping in Web adapters. Domain handlers should express business outcomes, while controllers and Razor Pages translate those outcomes to `IActionResult` or page results.
- Use ProblemDetails-compatible responses for API failures where practical, and preserve existing response shapes for public endpoints unless the task explicitly changes them.
- Do not swallow exceptions from external services silently. Log with enough context to diagnose the operation, target resource, and correlation details available at that boundary.

## Cache Invalidation Rules
- Treat cache invalidation as part of the write workflow. Any command that changes posts, pages, categories, tags, widgets, configuration, themes, comments, or assets should consider affected cache partitions.
- Prefer centralized invalidation through filters or event handlers over scattered controller-level `cache.Remove(...)` calls when adding new write paths.
- For post and page writes, review sitemap, feed/subscription, per-post/page, archive, tag/category, and widget cache impact.
- For settings and theme changes, review site-wide rendering, custom CSS, manifest, robots, FOAF, and sitemap impact.
- When a write triggers IndexNow, Webmention, email, or other external side effects, prefer publishing an event and handling cache cleanup and side effects consistently outside the controller.

## C# And ASP.NET Core Style
- Always use the latest applicable C# features, while staying readable and consistent with nearby code.
- Use async APIs end to end and pass `CancellationToken` through command/query handlers and EF Core calls where available.
- Use UTC for persisted or cross-boundary timestamps. Convert to user/client time only at the UI or boundary layer.
- Prefer nullable-safe code and explicit validation for public inputs. Return `NotFound`, `Conflict`, `NoContent`, `Ok`, or ProblemDetails-compatible responses consistently with existing controllers.
- Keep logs structured with named placeholders, for example `logger.LogInformation("Post scheduled for publish at {PublishTimeUtc} UTC.", publishTimeUtc);`.
- Keep comments sparse. Add comments only when they explain a non-obvious compatibility, security, localization, or protocol decision.

## EF Core And Data Access
- Use `BlogDbContext` for data access. Add entities under `Moonglade.Data.Entities`, DTO/read models under `Moonglade.Data.DTO`, and model configuration under `Moonglade.Data.Configurations` when mappings become non-trivial.
- Use `AsNoTracking()` for read-only queries unless the result must be updated in the same unit of work.
- Keep LINQ provider-compatible for both SQL Server and PostgreSQL. Avoid provider-specific SQL or functions unless they are isolated in the provider-specific projects.
- Use EF Core async methods and set-based operations such as `ExecuteDeleteAsync` when they fit the existing pattern.
- Be careful with cascade behavior, many-to-many mappings, and route/slug uniqueness because posts, tags, categories, feeds, sitemap, and cache invalidation depend on them.

## Caching, Background Work, And Side Effects
- Use the existing `Edi.CacheAside.InMemory` cache partitions and `ClearBlogCache` filter patterns for blog, post, page, sitemap, subscription, and widget cache invalidation.
- Trigger non-blocking side effects through existing services such as `CannonService`, LiteBus events, `IWebmentionSender`, `IIndexNowClient`, and email handlers instead of doing slow external calls inline in request handlers.
- Preserve hosted service patterns for scheduled publishing and update checks. Configuration-driven background behavior should remain controlled by `appsettings` values.

## Razor, APIs, And Frontend
- Public pages are Razor Pages and partials under `Moonglade.Web/Pages`; admin and JSON operations commonly use API controllers under `Moonglade.Web/Controllers`.
- Respect antiforgery defaults. Only use `[IgnoreAntiforgeryToken]` for deliberate endpoints such as keep-alive or protocol callbacks.
- Prefer JavaScript modules (`.mjs`) for application scripts. Keep third-party or bundled library files under the existing `wwwroot/lib` or `wwwroot/js/3rd` conventions.
- Keep frontend changes compatible with Bootstrap, Alpine.js, Monaco editor, Tagify, and the current Razor layout structure. Do not introduce a new frontend framework unless explicitly requested.
- Keep localized UI text in resources when the text is part of server-rendered UI. Supported cultures currently include `en-US`, `zh-Hans`, `zh-Hant`, `de-DE`, and `ja-JP`.

## Configuration And Security
- Do not hard-code secrets, connection strings, API keys, tenant IDs, or storage credentials. Use configuration binding and existing options/settings classes.
- Keep configuration cross-platform: file paths, storage paths, and shell commands must work on Windows and Linux where possible.
- Preserve HTTPS, forwarded header, health check, security header, captcha, authentication, and content moderation behavior unless the task explicitly targets them.
- Authentication supports local accounts and Microsoft Entra ID. Keep auth-provider-specific logic isolated in `Moonglade.Auth` or the existing Web authentication setup.
- When touching deployment assets, keep Azure App Service on Linux, Docker Compose, SQL Server, and PostgreSQL scenarios in mind.

## Testing Guidelines
- Add or update tests in the matching `src/Tests/Moonglade.*.Tests` project when behavior changes. Use xUnit v3, Moq, and EF Core InMemory patterns already used in the repository.
- Prefer focused unit tests for command/query handlers, services, middleware, validators, and protocol generators. Use integration tests only when the behavior depends on EF/database setup or host-level wiring.
- Use `TestContext.Current.CancellationToken` in async tests when following existing test style.
- For local verification, prefer the existing VS Code build task or run `dotnet build src/Moonglade.Web/Moonglade.Web.csproj`. For focused tests, run the relevant test project instead of the whole solution when possible.

## General Project Guidelines
- Code must be cross-platform compatible.
- Do not add license or copyright headers unless explicitly requested.
- Preserve public blogging protocols and endpoints such as RSS, Atom, OPML, OpenSearch, FOAF, Webmention, sitemap, robots.txt, health checks, and IndexNow unless the task explicitly changes them.