# AGENTS.md

Moonglade is a cloud-platform-neutral personal blogging platform built with ASP.NET Core / .NET 10, Razor Pages, LiteBus, and EF Core. The Web host is `src/Moonglade.Web`; the solution is `src/Moonglade.slnx`. PostgreSQL is the default database; SQL Server is also supported.

This file contains repository-wide working rules. The [engineering reference](docs/agent-reference.md) contains task-specific constraints and implementation details. Read the applicable sections before changing that area; do not load the entire reference for unrelated work. Paths shown in backticks are relative to the repository root. [README.md](README.md) is the main deployment and configuration guide.

## Working Rules And Decision Boundaries

- Inspect `git status --short` before editing and before finishing. Preserve user changes.
- Read the nearby implementation and tests before changing code. Follow local patterns, keep the diff focused, and avoid unrelated refactors or bulk formatting.
- The admin portal supports desktop browsers only, with a viewport at least 1440 CSS pixels wide. Design and verify admin pages within that range.
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
- PostgreSQL table and column names use snake_case; SQL Server retains PascalCase. Keep C# names unchanged. Existing PostgreSQL databases require the [manual naming upgrade](docs/upgrade-postgresql-snake-case.md) after any required category integer ID upgrade.
- Category IDs are database-generated `int` identities starting at 100 and incrementing by 1. Keep article IDs as GUIDs, category foreign keys and editor payloads as integers, and public category/feed URLs slug-based. See [the category upgrade guide](docs/upgrade-category-int-identity.md).
- Database upgrades require administrators to run SQL scripts manually before starting the upgraded application. Do not add automatic schema/data migration or restore the removed cumulative migration scripts. Preserve fresh database creation and seeding.
- Schema or persisted-data changes must include manual, idempotent SQL scripts under `database/` for both PostgreSQL and SQL Server. Follow [database/README.md](database/README.md) and the [migration verification rules](docs/agent-reference.md#migration-verification).
- Auth owns the singleton public Owner Email and optional Local credentials. Account Settings owns the public name and description alongside the avatar and public email. Existing installations require the [manual account cutover](docs/upgrade-owner-account.md); never import legacy credentials. Preserve captured profile identity in Atom cache keys/generation and queued notification recipients. Email changes require the current local password and reauthentication while retaining TOTP. Standalone OIDC installations follow the [local sign-in cutover](docs/upgrade-generic-oidc-authentication.md).
- Auth owns uploaded owner avatars and the Gravatar preference in the singleton `OwnerAvatar` table, independent of ordinary settings, site assets, and credential revision. Existing installations require the [manual avatar cutover](docs/upgrade-owner-avatar.md).
- Auth owns the singleton OIDC configuration and external identity binding. The OIDC client secret uses a database-backed Data Protection key ring dedicated to that credential. The database backup contains both the protected secret and its keys. Existing installations require the [manual OIDC schema upgrade](docs/upgrade-oidc-binding.md).
- Activity history uses canonical event keys and detached actor/target snapshots. Existing legacy activity tables require the [manual event-key upgrade](docs/upgrade-activity-log-events.md) after applicable category/naming prerequisites. Clear pre-release activity history at cutover, including on installations with an already-upgraded preview schema. Schema-script reruns preserve new history. Ordinary settings activity uses only the current change envelope. Use the shared event definitions and safe detail allowlists for persisted values.
- Ordinary settings audit results come from the configuration write gate's effective before/after comparison, not controller snapshots or raw JSON equality. Preserve separate persisted-JSON timestamp/cache behavior, structural menu equality, no-op audit suppression, and restricted-only update entries. Configuration returns only approved projected differences; ActivityLog validates the per-event change envelope before storing or exposing it.
- Scheduled publishing owns committed ID/title/UTC snapshots in Features and hands them to Web through `ScheduledPostsPublishedEvent`. Preserve the integer command result and existing cache-error semantics; attempt the independent activity handoff even when cache notification fails. Web records each `post.automatically-published` System activity independently at its captured occurrence time. Do not add ActivityLog/Web dependencies to Features, durable retries, or multi-host coordination.
- Application code, default configuration, and supported runtime behavior must remain cloud-platform-neutral. Do not add cloud SDKs, provider detection, vendor-specific settings, or image-storage providers to core projects to satisfy a deployment example. Vendor-specific guides are optional operational examples.
- Preserve standard environment-variable configuration, OCI/Docker compatibility, and separate durable mounts for `/app/images` and `/app/images-origin`. The Docker image pre-creates both paths for the `app` user; Compose uses separate named volumes.
- Preserve published URLs, protocol endpoints, HTTPS, forwarded-header handling, health semantics, authentication, security headers, and content moderation unless the task explicitly targets them.
- Never commit real connection strings, API keys, tenant IDs, or storage credentials. Use configuration binding and environment-variable overrides.
- Treat `src/Moonglade.Web/appsettings.Development.json` as a local override that may contain developer secrets. Do not quote or copy sensitive values into code, docs, logs, commits, or task records.

## C# Conventions

- Target `net10.0` and use the latest applicable C# features while following nearby style, including implicit usings, primary constructors, record request models, and feature-local files.
- The Email and SQL Server projects explicitly reference `System.Security.Cryptography.Pkcs` 10.0.13 to override vulnerable MimeKit/SqlClient transitive dependencies. Keep these references until their upstream dependencies resolve a patched version without overrides.
- Keep namespaces aligned with folders. Put new code in the project and feature folder that owns the behavior.
- Use constructor injection. Do not introduce service locators or unnecessary static mutable state.
- Startup services (`StartUpInitializer`, `ConfigInitializer`, `SiteIconBuilder`), `DatedGuidFileNameGenerator`, `ImageUploadValidator`, `CommentSubmissionGuard`, and `SyndicationDataSource` use concrete constructor injection with their existing DI lifetimes. Do not add interfaces or virtual methods solely to mock them in tests. Keep contracts with actual application boundaries, such as image storage, configuration, moderation, email, and external communication.
- Use async APIs end to end and pass `CancellationToken` through command/query handlers and EF Core calls where available.
- Use UTC for persisted or cross-boundary timestamps. Convert to user/client time only at the UI or boundary layer.
- Prefer nullable-safe code.
- Do not use C# anonymous object initializers (`new { ... }`) in C# source or test files. Razor files are excluded from this rule because route values, HTML attributes, and view component arguments commonly use anonymous objects there.
- Use structured logging placeholders, for example `logger.LogInformation("Post updated with ID: {PostId}", post.Id);`.
- Keep comments sparse and useful. Add comments only for non-obvious compatibility, security, localization, protocol, or business decisions.
- Keep changes cross-platform, especially paths, environment variables, reverse-proxy behavior, container behavior, and mounted-filesystem semantics.

## HTTP And Error Handling

- Domain handlers should express business outcomes; controllers and pages should translate those outcomes into HTTP responses.
- For new APIs, follow existing response styles: `Ok`, `NoContent`, `NotFound`, `Conflict`, `ValidationProblem`, or ProblemDetails-compatible responses. Use ProblemDetails-compatible API failures where practical.
- Avoid using ambiguous `null` for multiple failure reasons. For new complex behavior, prefer a lightweight result model that distinguishes not found, validation failure, conflict, forbidden, and success.
- Validate public inputs explicitly. Reuse existing attributes such as `[NotEmpty]`, `[Range]`, and `[Required]` where appropriate.
- Preserve existing public response shapes unless the task explicitly changes them.
- Do not swallow exceptions from external services silently. Log enough context to diagnose the operation, target resource, and available correlation details.

## Language And Licenses

- Repository content must be written in English unless the file is a localization resource, such as `src/Moonglade.Web/Resources/Program.*.resx` or third-party language pack files. Unit test data may also contain non-English values when the behavior under test requires them. Do not add non-English text to Markdown files, source code, comments, configuration, or documentation outside localization resources.
- The README states that this blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.
- The repository license is GPL-3.0. Do not remove or rewrite third-party license files casually.
- Do not add license or copyright headers unless explicitly requested.

## Development And Verification

Run commands from the repository root. Follow `.editorconfig`: UTF-8, CRLF, final newlines, four-space C#/Razor/JavaScript/JSON/YAML indentation, and tab-indented MSBuild XML. Preserve the Markdown trailing-whitespace exception and avoid formatting untouched files.

### Common Commands

See [Build & Run](README.md#build--run) for restore/build/run commands and [Run Tests](src/Tests/README.md#run-tests) for Microsoft.Testing.Platform commands. Keep the `dotnet test --project` form for individual projects; `global.json` selects the runner. [Starting Docker containers](README.md#quick-local-deployment-docker) is separate from building or testing.

### Test Selection

- Do not add tests solely to prove that a removed feature, member, or dependency is absent. Test current behavior and meaningful failure cases instead.
- C# tests cover business logic and functional contracts, not UI presentation. Do not assert Razor/HTML structure, CSS classes/styles, layout, menus/buttons, localized UI wording, or UI asset/source text; verify presentation in the browser instead. Preserve business, API/security, and protocol behavior tests.
- Add or update behavior tests in the owning `Moonglade.*.Tests` project; see the [test layout](src/Tests/README.md#layout). Tests use xUnit v3, Moq, and EF Core InMemory/Sqlite patterns.
- When following the existing async test style, use `TestContext.Current.CancellationToken`.
- Prefer focused unit tests for command/query handlers, services, middleware, validators, and protocol generators. Use integration tests when behavior depends on EF/database setup or host-level wiring.
- Prefer running the affected test project. For cross-module changes or startup registration changes, at least run the Web project build.
- Account release acceptance requires isolated fresh-install, upgrade, and browser checks on both supported database providers. See the [release checklist](src/Tests/README.md#owner-account-release-acceptance).
- Keep database migration tests out of the repository. When working on a migration, AI agents may verify the SQL in disposable databases using temporary scripts or fixtures. Remove temporary verification artifacts when finished.

## Planning And Documentation

### Documentation Sync Rules

After changes, update the appropriate long-lived docs (`README.md`, `AGENTS.md`, or `docs/`) when workflows, build/test/deployment steps, stack/dependencies, architecture, configuration, development conventions, or reusable troubleshooting knowledge are affected. Otherwise, state in the final response that no documentation update was needed.
