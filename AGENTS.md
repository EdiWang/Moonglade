# AGENTS.md

Run commands from the repository root. [README.md](README.md) covers deployment, configuration, and local setup. The [engineering reference](docs/agent-reference.md) covers module ownership and implementation contracts. Read only the sections relevant to the task.

## Working Rules

- Inspect `git status --short` before editing and before finishing. Preserve user changes. Read the owning code, its callers, and nearby tests. Keep the diff focused.
- Check effects on public contracts, both database providers, configuration, localization, and postcommit side effects. Preserve existing behavior outside the requested scope.
- Ask before choosing between interpretations that materially change requirements, business behavior, architecture, module ownership, or public contracts. Continue independent work while the decision is pending.
- Obtain explicit authorization before destructive operations, production data changes, deployments, external service changes, or overwriting important human-maintained documentation outside the requested scope.
- Keep credentials and deployment secrets out of source, documentation, logs, and task records. Treat `src/Moonglade.Web/appsettings.Development.json` as a private local override. If you discover a suspected secret, avoid copying it and ask how to handle the affected material.
- Update durable documentation when workflows, architecture, configuration, dependencies, or development practices change. Report changes, verification, and any remaining risk. State when no documentation update was needed.

## Read By Task

| Task | Reference |
| --- | --- |
| Locate the owner or change startup, DI, or LiteBus wiring | [Project responsibilities](docs/agent-reference.md#layers-and-project-responsibilities), [Web entry point](docs/agent-reference.md#web-entry-point) |
| Change data, UTC handling, or migrations | [EF Core](docs/agent-reference.md#ef-core), [migration verification](docs/agent-reference.md#migration-verification) |
| Change settings, defaults, startup initialization, or activity | [Configuration](docs/agent-reference.md#configuration), [activity history](docs/agent-reference.md#activity-history) |
| Change posts, publishing, slugs, or comments | [Blog content](docs/agent-reference.md#blog-content), [comments and moderation](docs/agent-reference.md#comments-and-moderation), [caching and side effects](docs/agent-reference.md#caching-and-side-effects) |
| Change account flows, rate limits, or HTTP security | [Authentication and security](docs/agent-reference.md#authentication-and-security) |
| Change images, CDN, themes, feeds, verification files, or Webmention | [Images, themes, feeds, and protocols](docs/agent-reference.md#images-themes-feeds-and-protocols) |
| Change Razor, JavaScript, the editor, or UI text | [Frontend and localization](docs/agent-reference.md#frontend-and-localization) |

## Architecture and Compatibility

- Keep Web responsible for HTTP binding, authorization, responses, and view data. Put business rules in the owning library. Keep startup orchestration in `Program.cs` and reuse existing service registration and endpoint mapping.
- Support PostgreSQL and SQL Server. Keep provider-specific behavior in the provider projects. Every schema or persisted-data change that requires an upgrade must include manual, idempotent scripts for both providers under [database/](database/README.md). Preserve fresh database creation and seeding.
- Keep application code, configuration, and runtime cloud-platform-neutral. Use standard environment variables and OCI-compatible containers. Keep the public and private image mounts separate. Vendor-specific infrastructure belongs in optional deployment guides.
- Preserve published URLs, protocol endpoints, HTTPS and forwarded-header handling, health semantics, authorization, security headers, and moderation unless the task targets them.

## C# and HTTP

- Target `net10.0`. Follow nearby C# conventions, keep namespaces aligned with folders, and use constructor injection. Preserve existing concrete service lifetimes and avoid interfaces or virtual methods solely for mocking.
- Use nullable-safe async APIs and propagate `CancellationToken` where supported. Persist and pass UTC timestamps across application boundaries. Convert wall-clock input at the UI or HTTP boundary.
- Avoid anonymous object initializers (`new { ... }`) in C# source and tests. Razor files may use them.
- Log with structured placeholders. Log external failures with safe operation and correlation context.
- Validate public input using existing attributes and domain validation. Have handlers express distinct business outcomes and let Web map them to the existing response conventions. Use ProblemDetails-compatible errors for new APIs where practical. Preserve existing response shapes.

## Development and Verification

- Follow `.editorconfig`, including UTF-8, CRLF, final newlines, four-space C#/Razor/JavaScript/JSON/YAML indentation, and tab-indented MSBuild XML.
- Follow [Build & Run](README.md#build--run) and [Run Tests](src/Tests/README.md#run-tests). Use `dotnet test --project` for individual projects under Microsoft.Testing.Platform.
- Cover business, HTTP, security, and protocol behavior in the owning test project. Use `TestContext.Current.CancellationToken` in async xUnit tests. Avoid tests that only prove removed code is absent.
- Run the affected test project. For cross-module or startup registration changes, at least build the Web project. For account release acceptance, follow the [fresh-install, upgrade, and browser checklist for both providers](src/Tests/README.md#owner-account-release-acceptance).
- Verify presentation in a browser. Admin support starts at 1440 CSS pixels. Keep visual layout, translated UI wording, and asset source assertions out of C# tests.
- Verify migration SQL in disposable databases and remove temporary fixtures afterward. Keep migration tests out of the repository.

## Language and License

- Write repository content in English. Localization resources and test data that requires another language are exceptions.
- Preserve the GPL-3.0 license and third-party license files. Add license or copyright headers only when requested.
