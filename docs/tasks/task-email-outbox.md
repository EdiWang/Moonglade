# Email Outbox Migration

## Original Goal

Reduce Moonglade's dependency on Azure by moving the Moonglade.Email Azure Function service into the main Moonglade codebase while preserving asynchronous email delivery through a queue and Azure Communication Services.

## Background

Moonglade currently sends email notifications from the web application through `Moonglade.Email.Client`, which posts to an Azure Function `/api/enqueue` endpoint. The Function validates the payload, stores it in Azure Storage Queue, and processes it with a queue trigger that builds template-based messages and sends them through SMTP or Azure Communication Services.

The blog has very low email volume, usually only a few notifications per month with occasional short bursts from comments. The target architecture should prefer simple, durable behavior over high-throughput enterprise messaging.

Relevant code inspected:

- `src/Moonglade.Email.Client`
- `src/Moonglade.Web/Controllers/CommentController.cs`
- `src/Moonglade.Web/Controllers/MentionController.cs`
- `src/Moonglade.Web/Controllers/SettingsController.cs`
- `src/Moonglade.BackgroundServices/CannonService.cs`
- `E:\GitHub\ediwang\Moonglade.Email\src\Moonglade.Function.Email`

## Scope

- Add an in-repository `Moonglade.Email` core module.
- Move reusable email contract, validation, message building, dispatching, and provider sender logic into the main solution.
- Add focused tests for the new email core module.
- Later batches will add database outbox persistence, a background worker, web integration, configuration updates, and old Function client cleanup.

## Out of Scope

- Do not change the current runtime email path in Batch 0 or Batch 1.
- Do not add database tables or migrations until Batch 2.
- Do not remove `Moonglade.Email.Client` until Batch 6.
- Do not add a separate Docker worker unless a later decision explicitly chooses that path.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 0 | Create this task record | None | `git status --short` | Completed |
| 1 | Add `Moonglade.Email` core project and reusable email delivery code | Task 0 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Completed |
| 2 | Add focused `Moonglade.Email.Tests` project | Task 1 | `dotnet test src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj` | Completed |
| 3 | Add database outbox model and queue store | Task 1 | Build and email tests | Completed |
| 4 | Add background outbox worker | Task 3 | Build and email tests | Completed |
| 5 | Replace Function HTTP enqueue path in Web | Tasks 3-4 | Web and email tests | Completed |
| 6 | Update configuration and notification settings UI | Task 5 | Web tests and build | Completed |
| 7 | Remove old Function client from main solution | Task 6 | Build, Web tests, email tests | Completed |
| 8 | Update README, AGENTS.md, and related docs | Tasks 5-7 | `git diff --check`, build | Not started |

## Execution Order

Work proceeds in small batches. Batch 0 creates the durable task record. Batch 1 introduces the new email core module without changing runtime behavior. Batch 2 adds durable database queue storage. Batch 3 adds a worker. Batch 4 switches web notification enqueue paths. Later batches remove old configuration and documentation references.

## Current Progress

Batch 0 through Batch 6 are complete. The repository now contains a `Moonglade.Email` core project with reusable email message contracts, validation, template message building, dispatching, SMTP sender, Azure Communication Services sender, delivery failure classification, a database-backed email outbox store, and a background outbox worker with a testable message processor. A focused `Moonglade.Email.Tests` project covers the new core behavior, outbox queue state transitions, worker processing decisions, notification event handlers, and email service registration.

`Moonglade.Web` now references `Moonglade.Email`, loads the `Moonglade.Email` assembly for LiteBus discovery, and publishes comment, comment reply, webmention, and test email events to the new database outbox queue path. The outbox worker is registered as an in-process hosted service and controlled by `Email:OutboxWorker:Enabled`. The old `Moonglade.Email.Client` project and tests have been removed from the solution.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 36 tests passed. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 45 tests passed after adding database outbox tests. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Setup.Tests\Moonglade.Setup.Tests.csproj` | Passed | 18 tests passed after migration script changes. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after data model changes. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors after Batch 2. |
| 2026-08-02 | `git diff --check` | Passed with line-ending warnings | Only CRLF normalization warnings were reported. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 54 tests passed after adding the outbox processor and worker options tests. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after Batch 3. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors after Batch 3. |
| 2026-08-02 | `git diff --check` | Passed | No whitespace errors were reported. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 59 tests passed after adding notification event handler tests. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | 135 tests passed after switching Web controllers to new email events. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after Batch 4. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors after Batch 4. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 61 tests passed after registering the outbox hosted worker and adding service registration tests. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | 135 tests passed after updating notification settings UI and resources. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after Batch 5. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors after Batch 5. |
| 2026-08-02 | `dotnet sln src\Moonglade.slnx list` | Passed | Solution no longer lists `Moonglade.Email.Client` or `Moonglade.Email.Client.Tests`. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Email.Tests\Moonglade.Email.Tests.csproj` | Passed | 61 tests passed after removing the old email client project. |
| 2026-08-02 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | 135 tests passed after removing the old email client Web reference. |
| 2026-08-02 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors after Batch 6. |
| 2026-08-02 | `dotnet build src\Moonglade.slnx --no-restore` | Passed | Solution build succeeded with 0 warnings and 0 errors after Batch 6. |

## Issues and Resolutions

- `Moonglade.Email` needed a project reference to `Moonglade.Configuration` after notification event handlers moved into the new email module. Added the reference and verified with email tests.
- The new reply email Web test needed a valid `HttpContext.Request.Scheme` and `Host` because `UrlHelper.GetPostUrl` builds an absolute post URL. Updated the test controller factory to provide `https://blog.example.com`.
- Registering `EmailOutboxWorker` made the old Function API settings misleading in `/admin/settings/notification`. Replaced that UI with provider, sender address, and outbox worker status, and changed the test email success text to indicate queueing.
- Removing the old email client required deleting the Web project reference, removing `Moonglade.Email.Client` from `Program.LoadAssemblies()`, and removing both old client projects from `Moonglade.slnx`.

## Follow-ups

- Revisit whether outbox claim should use provider-specific SQL Server/PostgreSQL atomic update queries if Moonglade later supports multiple active web instances or a separate worker container. Batch 2 uses an EF-based lease plus optimistic concurrency token, which is appropriate for the initial single-worker personal blog target.
- Batch 8 should perform a final documentation pass after the old email client removal is verified.
- Preserve at-least-once semantics and make duplicate email risk explicit.
- Keep SMTP support available as an optional provider even if Azure Communication Services remains the primary provider.

## Notes

The current Function implementation already contains useful validation, template rendering, per-recipient failure isolation, and transient/permanent error classification. The migration should preserve those semantics and avoid preserving the Azure Function host shape merely for compatibility.
