# AGENTS.md

Moonglade is a cloud-platform-neutral personal blogging platform built with ASP.NET Core / .NET 10, Razor Pages, LiteBus, and EF Core. The Web host is `src/Moonglade.Web`; the solution is `src/Moonglade.slnx`. PostgreSQL is the default database; SQL Server is also supported.

This file contains repository-wide working rules. The [engineering reference](docs/agent-reference.md) contains task-specific constraints and implementation details. Read the applicable sections before changing that area; do not load the entire reference for unrelated work. Paths shown in backticks are relative to the repository root. [README.md](README.md) is the main deployment and configuration guide.

## Working Rules And Decision Boundaries

- Inspect `git status --short` before editing and before finishing. Preserve user changes.
- Read the nearby implementation and tests before changing code. Follow local patterns, keep the diff focused, and avoid unrelated refactors or bulk formatting.
- Identify affected callers, tests, configuration, resources, documentation, and side effects before editing. Use the reference's [change impact checklist](docs/agent-reference.md#change-impact-checklist) for behavior changes.
- Ask the user before choosing between interpretations that change requirements, business behavior, architecture, module ownership, or public contracts. Explain the alternatives and their effects; do not silently choose a new project policy.
- Ask before destructive operations, production-data changes, or actions that change external services unless the user has already explicitly authorized that action. A general implementation request does not authorize deployment or production migration.
- Ask before overwriting important human-maintained documentation outside the requested scope. An explicit request to edit a document authorizes those edits; routine wording and structure changes within that scope do not require another approval.
- If suspected secrets are discovered, do not print or copy them. Ask how to handle the affected material before proceeding with that part of the work.
- Continue clear edits, read-only investigation, and local verification within the authorized scope without repeated confirmation. A pending question blocks only the work that depends on its answer.
- Keep questions specific. Update the appropriate documentation when a confirmed answer changes durable project knowledge.
- Finish by explaining what changed, what was verified, and any remaining risk. If verification was skipped or blocked, say why.

## Read By Task

The linked sections retain the detailed project rules and must be followed when applicable.

| Task | Read before changing |
| --- | --- |
| Locate a project or confirm the stack | [Technology stack](docs/agent-reference.md#technology-stack), [layers and responsibilities](docs/agent-reference.md#layers-and-project-responsibilities) |
| Change startup, service registration, handlers, or module boundaries | [Web entry point](docs/agent-reference.md#web-entry-point), [CQRS and LiteBus](docs/agent-reference.md#cqrs-and-litebus) |
| Change entities, queries, UTC handling, migrations, or upgrade behavior | [EF Core](docs/agent-reference.md#ef-core), [migration verification](docs/agent-reference.md#migration-verification) |
| Change application configuration, persisted blog settings, or defaults | [Configuration and environment](docs/agent-reference.md#configuration-and-environment), [configuration writes and initialization](docs/agent-reference.md#configuration) |
| Change posts, publishing, scheduling, slugs, or routes | [Blog content](docs/agent-reference.md#blog-content), [caching and side effects](docs/agent-reference.md#caching-and-side-effects) |
| Change comments, replies, moderation, or notifications | [Comments and moderation](docs/agent-reference.md#comments-and-moderation), [email configuration](docs/agent-reference.md#configuration-and-environment), [caching and side effects](docs/agent-reference.md#caching-and-side-effects) |
| Change authentication, authorization, rate limits, or HTTP security | [Authentication and security](docs/agent-reference.md#authentication-and-security), [configuration and environment](docs/agent-reference.md#configuration-and-environment) |
| Change images, storage, CDN, themes, feeds, verification files, health, or Webmention | [Images, themes, feeds, and protocols](docs/agent-reference.md#images-themes-feeds-and-protocols), [caching and side effects](docs/agent-reference.md#caching-and-side-effects) |
| Change Razor, JavaScript, editor assets, highlighting, Mermaid, or UI text | [Frontend and localization](docs/agent-reference.md#frontend-and-localization) |
| Start the application locally | [Local runtime](docs/agent-reference.md#local-runtime), [README development guide](README.md) |

## Architecture And Compatibility

- Keep controllers and Razor Pages focused on HTTP binding, authorization, status codes, and view data. Business rules belong in the owning class library command, query, or service.
- Keep `Program.cs` as startup orchestration. Use the existing owning-project DI extensions, Web service composition, and centralized pipeline and endpoint mapping.
- Preserve compatibility with both PostgreSQL and SQL Server. Keep provider-specific behavior in the provider projects.
- Application code, default configuration, and supported runtime behavior must remain cloud-platform-neutral. Do not add cloud SDKs, provider detection, vendor-specific settings, or image-storage providers to core projects to satisfy a deployment example. Vendor-specific guides are optional operational examples.
- Preserve standard environment-variable configuration, OCI/Docker compatibility, and separate durable mounts for `/app/images` and `/app/images-origin`. The Docker image pre-creates both paths for the `app` user; Compose uses separate named volumes.
- Preserve published URLs, protocol endpoints, HTTPS, forwarded-header handling, health semantics, authentication, security headers, and content moderation unless the task explicitly targets them.
- Never commit real connection strings, API keys, tenant IDs, or storage credentials. Use configuration binding and environment-variable overrides.
- Treat `src/Moonglade.Web/appsettings.Development.json` as a local override that may contain developer secrets. Do not quote or copy sensitive values into code, docs, logs, commits, or task records.

## C# Conventions

- The target framework is `net10.0`. Follow the existing C# style, including implicit usings, primary constructors, record request models, and feature-local files.
- The Email and SQL Server projects explicitly reference `System.Security.Cryptography.Pkcs` 10.0.13 to override vulnerable MimeKit/SqlClient transitive dependencies. Keep these references until their upstream dependencies resolve a patched version without overrides.
- Use the latest applicable C# features while staying readable and consistent with nearby code.
- Keep namespaces aligned with folders. Put new code in the project and feature folder that owns the behavior.
- Use constructor injection. Do not introduce service locators or unnecessary static mutable state.
- Startup services (`StartUpInitializer`, `ConfigInitializer`, `MigrationManager`, `SiteIconBuilder`), `DatedGuidFileNameGenerator`, `ImageUploadValidator`, `CommentSubmissionGuard`, and `SyndicationDataSource` use concrete constructor injection with their existing DI lifetimes. Do not add interfaces or virtual methods solely to mock them in tests. Keep contracts with actual application boundaries, such as image storage, configuration, moderation, email, and external communication.
- Use async APIs end to end and pass `CancellationToken` through command/query handlers and EF Core calls where available.
- Use UTC for persisted or cross-boundary timestamps. Convert to user/client time only at the UI or boundary layer.
- Prefer nullable-safe code and explicit validation for public inputs.
- Do not use C# anonymous object initializers (`new { ... }`) in C# source or test files. Razor files are excluded from this rule because route values, HTML attributes, and view component arguments commonly use anonymous objects there.
- Use structured logging placeholders, for example `logger.LogInformation("Post updated with ID: {PostId}", post.Id);`.
- Keep comments sparse and useful. Add comments only for non-obvious compatibility, security, localization, protocol, or business decisions.
- Keep changes cross-platform, especially paths, environment variables, reverse-proxy behavior, container behavior, and mounted-filesystem semantics.

## HTTP And Error Handling

- Domain handlers should express business outcomes; controllers and pages should translate those outcomes into HTTP responses.
- For new APIs, follow existing response styles: `Ok`, `NoContent`, `NotFound`, `Conflict`, `ValidationProblem`, or ProblemDetails-compatible responses.
- Avoid using ambiguous `null` for multiple failure reasons. For new complex behavior, prefer a lightweight result model that distinguishes not found, validation failure, conflict, forbidden, and success.
- Validate public inputs explicitly. Reuse existing attributes such as `[NotEmpty]`, `[Range]`, and `[Required]` where appropriate.
- Use ProblemDetails-compatible responses for API failures where practical, and preserve existing response shapes for public endpoints unless the task explicitly changes them.
- Do not swallow exceptions from external services silently. Log enough context to diagnose the operation, target resource, and available correlation details.

## Language And Licenses

- Repository content must be written in English unless the file is a localization resource, such as `src/Moonglade.Web/Resources/Program.*.resx` or third-party language pack files. Unit test data may also contain non-English values when the behavior under test requires them. Do not add non-English text to Markdown files, source code, comments, configuration, or documentation outside localization resources.
- The README states that this blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.
- The repository license is GPL-3.0. Do not remove or rewrite third-party license files casually.
- Do not add license or copyright headers unless explicitly requested.

## Development And Verification

Run commands from the repository root. Follow `.editorconfig`: UTF-8, CRLF, final newlines, four-space C#/Razor/JavaScript/JSON/YAML indentation, and tab-indented MSBuild XML. Preserve the Markdown trailing-whitespace exception and avoid formatting untouched files.

### Common Commands

```powershell
dotnet restore src/Moonglade.Web/Moonglade.Web.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
```

The test runner is Microsoft.Testing.Platform, selected in `global.json`; keep the `--project` form above. For local Docker deployment, see [README.md](README.md#quick-local-deployment-docker); starting containers is separate from building or testing.

### Test Selection

- Behavior changes should add or update tests in the matching test project, for example:
  - Post/category/tag/comment/page behavior: `src/Tests/Moonglade.Features.Tests`
  - Web controller/handler behavior: `src/Tests/Moonglade.Web.Tests`
  - Middleware: `src/Tests/Moonglade.Web.Middleware.Tests`
  - Auth/configuration/theme/syndication/webmention/image storage/moderation/email/indexnow/background/setup: the matching `Moonglade.*.Tests` project.
- Tests use xUnit v3, Moq, and EF Core InMemory/Sqlite patterns.
- When following the existing async test style, use `TestContext.Current.CancellationToken`.
- Prefer focused unit tests for command/query handlers, services, middleware, validators, and protocol generators. Use integration tests when behavior depends on EF/database setup or host-level wiring.
- Prefer running the affected test project. For cross-module changes or startup registration changes, at least run the Web project build.

## Planning And Documentation

### Task Records

For complex work, first split the request into small sub-tasks that can be implemented, checked, tested, committed, and rolled back independently. Make dependencies explicit, especially when a change crosses projects, affects data shape, changes public endpoints, or alters deployment behavior.

Create a task record under `docs/tasks/` when the work:

- Crosses multiple modules or services.
- Requires multiple rounds of context to complete.
- Is a high-risk refactor or migration.
- Changes architecture, data models, protocol contracts, configuration, or deployment flow.
- Is explicitly requested by the user as a retained task record.

Use `docs/tasks/task-<short-task-name>.md` for task records unless a more specific project convention appears later. Keep the record updated while working so another agent can resume after context compression or interruption. At minimum, record the original goal, background, scope, task breakdown, execution order, dependencies, status, verification log, issues/resolutions, and follow-ups. A starter template lives at [docs/tasks/task-template.md](docs/tasks/task-template.md).

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

Short notes may live here. Longer or multi-issue notes should go in `docs/troubleshooting.md`, with a short summary and link from this file.

Use the [troubleshooting note format](docs/agent-reference.md#troubleshooting-note-format). Link a longer note from this file only after that document exists.
