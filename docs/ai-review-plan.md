
## Review Date

2026-08-04

## Scope

This review used read-only inspection only. No build, test, lint, restore, format, package installation, or code-fix command was run.
Reviewed areas:

- Repository structure and long-lived docs: `AGENTS.md`, `README.md`, `docs/tasks/`.
- Solution and project layout: `src/Moonglade.slnx`, project files, module/test project distribution.
- ASP.NET Core host and pipeline: `src/Moonglade.Web/Program.cs`, `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`, `src/Moonglade.Web/Extensions/WebApplicationExtensions.cs`.
- Web controllers and selected pages/scripts: comments, images, settings, posts, widgets, auth, webmention, data export, admin JavaScript modules.
- Feature/data modules: post, comment, widget, image storage, email, webmention, background services, data export, EF context.

Highest-priority improvements should focus on security and operational stability around mutable admin endpoints, public outbound-fetch paths, configuration validation, and authentication throttling. Broad architecture rewrites, framework changes, or dependency upgrades are not recommended as first steps.

## Confirmed Decisions From User

- No external tools are expected to upload images to `/image` without an admin antiforgery token.
- Local login and TOTP verification should both be rate limited at 10 attempts per 1 minute.
- Incoming Webmentions must not allow intranet or self-hosted private-address sources.
- Docker Compose may require `.env`; it does not need a demo password fallback.
- Azure Blob image keys may use virtual folders, so provider-specific image key rules must preserve that compatibility.
- `/health` must remain liveness-only.
- Exact view/request counts are not required across multiple app instances.
- Existing widget `ContentCode` does not need legacy-format compatibility.
- Adding a minimal `.editorconfig` is allowed.

## Findings

| ID | Priority | Type | Location | Description | Impact | Evidence | Suggested Direction |
| --- | --- | --- | --- | --- | --- | --- | --- |
| F-01 | P1 | Security / CSRF | `src/Moonglade.Web/Controllers/ImageController.cs`; `src/Moonglade.Web/Controllers/SettingsController.cs`; `src/Moonglade.Web/wwwroot/js/app/admin.editpost.editor.mjs` | Authenticated mutating endpoints bypass antiforgery protection. Image upload and test-email enqueue are cookie-authenticated admin operations but use `[IgnoreAntiforgeryToken]`. | A cross-site request could trigger image upload or test email enqueue from an authenticated admin session. Image upload is the higher-risk endpoint because it mutates storage. | `ImageController` has `[HttpPost, IgnoreAntiforgeryToken]` at line 70. `SettingsController` has `[HttpPost("email/test")]` plus `[IgnoreAntiforgeryToken]` at lines 63-64. The editor calls `fetch('/image', ...)` directly at `admin.editpost.editor.mjs:20`, while the shared `fetch2` helper sends `XSRF-TOKEN`. | Move image/test-email calls to the existing antiforgery helper or explicitly include the antiforgery header. Keep protocol callbacks such as `/webmention` exempt. |
| F-02 | P1 | Security / SSRF | `src/Moonglade.Webmention/ReceiveWebmentionCommand.cs`; `src/Moonglade.Webmention/MentionSourceInspector.cs` | Webmention source validation blocks only some literal private IPv4/loopback inputs before fetching the source URL. It does not revalidate resolved DNS addresses or cover link-local, carrier-grade NAT, IPv6 private/link-local, or DNS rebinding cases. | A crafted Webmention source could make the server issue outbound requests to internal or metadata endpoints, depending on hosting network and DNS behavior. | `ReceiveWebmentionCommand` calls `sourceInspector.ExamineSourceAsync(sourceUrl, targetUrl)` at line 36 after `IsAllowedUri`; `IsAllowedUri` checks scheme, `uri.IsLoopback`, and only literal IPv4 ranges at lines 91-111. `MentionSourceInspector` fetches with `httpClient.GetAsync(sourceUrl, ResponseHeadersRead)` at line 52. | Centralize outbound URL safety for Webmention: resolve host, reject all private/link-local/special-use IP ranges, disable or validate redirects, and add tests for DNS/private/link-local/IPv6 cases. |
| F-03 | P1 | Security / Authentication | `src/Moonglade.Web/Pages/SignIn.cshtml.cs`; `src/Moonglade.Auth/ValidateLoginCommand.cs`; `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs` | Local-account sign-in has no visible rate limit or lockout policy. The app has rate limiting for comments, but not for login. | Password guessing can be attempted repeatedly. TOTP reduces full account takeover risk, but password validation remains unthrottled and noisy. | `SignInModel.OnPostAsync` sends `ValidateLoginCommand` on each valid request at lines 57 and 83. `ValidateLoginCommandHandler` compares the stored hash and logs failures at lines 15-27. `CommentController` uses `[EnableRateLimiting(CommentRateLimitPolicy.PolicyName)]` at line 26, showing rate limiting is available but scoped to comments. | Add a small IP plus username partitioned login rate limit or lockout-compatible guard. Verify local login, TOTP setup, and Entra ID challenge behavior. |
| F-04 | P1 | Stability / Configuration | `src/Moonglade.Email/ServiceCollectionExtensions.cs`; `src/Moonglade.Email/Core/EmailServiceOptionsValidator.cs`; `src/Tests/Moonglade.Email.Tests/EmailServiceOptionsValidatorTests.cs` | `EmailServiceOptionsValidator` exists and is tested but is not registered for `EmailServiceOptions`. | Invalid email provider settings may pass startup and fail only when the outbox worker attempts delivery. | `AddMoongladeEmail` binds `EmailServiceOptions` at lines 19-20, but only registers `IValidateOptions<EmailOutboxWorkerOptions>` at line 22. `EmailServiceOptionsValidator` implements `IValidateOptions<EmailServiceOptions>` at `EmailServiceOptionsValidator.cs:6`, and dedicated tests exist. | Register `IValidateOptions<EmailServiceOptions>` and call `ValidateOnStart()` for `EmailServiceOptions`. Add a service-registration test. |
| F-05 | P2 | Security / Secret Hygiene | `src/Moonglade.Web/appsettings.json`; `compose.yaml`; `.env.example`; `AGENTS.md` | The default application configuration and Docker Compose fallback include a concrete SQL Server password value instead of a placeholder-only pattern. | Increases risk of accidental reuse in real deployments and weakens the project's own secret-handling guidance. | `appsettings.json` contains a SQL Server connection string with `User Id` and `Password` at line 3. `compose.yaml` has fallback `MSSQL_SA_PASSWORD` values at lines 10 and 25. `.env.example` uses a placeholder at line 1, and `AGENTS.md` says production values should not be committed. | Move committed defaults to placeholder or local-only examples, keep Compose requiring `.env` or a safer documented override, and update README if behavior changes. |
| F-06 | P2 | Stability / API Validation | `src/Moonglade.Web/Controllers/CommentController.cs`; `src/Moonglade.Features/Comment/ListCommentsQuery.cs` | Admin comment listing accepts raw `pageIndex` and `pageSize` without the range annotations or clamping used by similar controllers. | Negative or very large paging values can cause query exceptions or excessive payloads. This is authenticated admin-only, so the operational risk is moderate. | `CommentController.List` accepts `[FromQuery] int pageIndex = 1` and `pageSize = 5` at lines 137-138 and passes them directly to `ListCommentsQuery` at line 142. The handler calculates `startRow` and calls `.Skip(startRow).Take(request.PageSize)` at lines 16 and 58-59. `ActivityLogController` and `MentionController` clamp similar values. | Add `[Range]` or controller-side clamping plus focused Web tests for invalid values. |
| F-07 | P2 | Stability / Background Work | `src/Moonglade.BackgroundServices/CannonService.cs`; `src/Moonglade.Web/Commands/PostManagementCommands.cs`; `src/Moonglade.Web/Controllers/ImageController.cs` | `CannonService` uses an unbounded in-memory channel for external and storage side effects. | A burst or slow downstream service can grow memory. Work is also non-durable across process crashes. Current low-volume personal blog use makes this a controlled improvement, not a rewrite trigger. | `Channel.CreateUnbounded` is used at `CannonService.cs:25`. Webmention and IndexNow are enqueued at `PostManagementCommands.cs:128` and `:144`; original image storage is enqueued at `ImageController.cs:153-154`. | Add configurable bounded capacity and explicit rejection/log behavior. Keep it in-process unless requirements change. |
| F-08 | P2 | Security / Maintainability | `src/Moonglade.Widgets/EditWidgetRequest.cs`; `src/Moonglade.Web/wwwroot/js/app/admin.widgets.render.mjs`; `src/Moonglade.Web/Pages/Admin/Widgets.cshtml`; widget partials | Widget content is stored as a raw JSON string with only max-length validation. Admin preview renders it through `x-html` after string interpolation, while public partials do some URL sanitization. | Malformed or imported widget JSON can break rendering. Stored admin-side XSS risk exists in preview surfaces if widget JSON is malicious. | `EditWidgetRequest.ContentCode` has only `[MaxLength(2000)]` at lines 17-18. The admin page uses `x-html="renderWidgetContent(widget)"` at `Widgets.cshtml:69`. `admin.widgets.render.mjs` builds raw HTML from JSON at line 1 onward. Public partials deserialize JSON and call `SterilizeLink` for URLs. | Add server-side widget content validation per widget type and escape admin preview output, preferably by DOM construction or a small shared escaping helper. |
| F-09 | P2 | Storage Consistency / Defense in Depth | `src/Moonglade.ImageStorage/Providers/AzureBlobImageStorage.cs`; S3 and filesystem providers | Azure Blob image storage validates only that a file extension exists, while S3 and filesystem providers reject path separators and traversal tokens. Azure Blob virtual folders are confirmed to be a supported scenario. | Current upload path generates safe names, but provider behavior differs. A future caller could still create unintended blob keys if validation remains too weak. | Azure `ValidateImageFileName` checks extension only at lines 247-256. S3 checks `Path.GetFileName` and `..` at lines 195-213. Filesystem checks path components and traversal at lines 121-131. | Add provider-specific safe key validation: keep Azure virtual folders valid, but reject traversal, empty segments, invalid/control characters, and unsupported extensions. Add tests for both flat names and Azure virtual-folder keys. |
| F-10 | P2 | Operations / Health | `src/Moonglade.Web/Extensions/ServiceCollectionExtensions.cs`; data provider registrations | `/health` is intentionally liveness-only. There is no separate readiness endpoint for database availability. | Existing monitors stay simple, but deployment diagnostics cannot distinguish a live process from data-store readiness unless another endpoint is added. | `AddMoongladeHealthChecks` registers only `"self"` at lines 201-202. SQL Server/PostgreSQL `AddDbContext` registrations exist in provider service extensions. | Keep `/health` unchanged and add a separate readiness endpoint such as `/health/ready` with database checks. |
| F-11 | P3 | Maintainability | `src/Moonglade.Features/Post/AddRequestCountCommand.cs`; `src/Moonglade.Features/Post/AddViewCountCommand.cs` | View/request count updates rely on static in-process per-post locks, and one source comment uses unprofessional wording. Exact multi-instance count accuracy is confirmed as not required. | Main issue is maintainability and code quality, not a required scalability fix. | `AddRequestCountCommand.cs:13-14` contains a static lock and an inappropriate comment. `AddViewCountCommand.cs:13` uses the same static lock pattern. | Replace the comment with neutral documentation. Do not prioritize distributed atomic count logic unless requirements change. |
| F-12 | P3 | Operations / Cleanup | `src/Moonglade.Data/Exporting/ZippedJsonExporter.cs`; `src/Moonglade.Web/Controllers/DataPortingController.cs` | Export creates zip files under the temp export directory and returns them via `PhysicalFile`; only the temporary JSON directory is deleted. | Repeated exports can accumulate zip files on disk. Risk is low and admin-only. | Zip path is created at `ZippedJsonExporter.cs:30`; temp JSON directory is deleted at line 34; `DataPortingController` returns `PhysicalFile` at line 21 without cleanup hook. | Add bounded cleanup of old export zip files or stream/delete after response completion. |
| F-13 | P3 | Development Hygiene | Repository root and project files | No repo-level `.editorconfig`, central package management, or package lock file was found. | Style and package version drift is easier over time. Current code still follows consistent local style, so this is not urgent. | `AGENTS.md` records no `.editorconfig`, `Directory.Packages.props`, `NuGet.config`, or package lock file. Direct inspection found none at the repository root. Package references are repeated across many project files. | Consider `.editorconfig` first. Central package management can wait until dependency churn justifies it. |

## Phased Improvement Plan

### Task 1: Register Email Service Options Validation

- Priority: P1
- Related findings: F-04
- Goal: Fail fast on invalid email provider configuration.
- Change scope: `Moonglade.Email` service registration and focused email service-registration tests.
- Excludes: Provider behavior changes, email template changes, dependency upgrades.
- Expected result: Invalid SMTP/ACS settings fail during startup validation instead of during outbox delivery.
- Verification: `dotnet test src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj`; optionally `dotnet build src/Moonglade.Web/Moonglade.Web.csproj`.
- Release risk: low.
- Rollback: Remove the validator registration and `ValidateOnStart()` call.
- Needs user confirmation: no.

### Task 2: Restore Antiforgery Protection For Admin Mutations

- Priority: P1
- Related findings: F-01
- Goal: Ensure authenticated admin mutations use the existing antiforgery pattern.
- Change scope: Image upload JavaScript, test-email JavaScript if needed, controller attributes, Web tests.
- Excludes: Public protocol callbacks such as `/webmention`; auth redesign.
- Expected result: Image upload and test email still work from the admin UI but cross-site requests without token fail.
- Verification: `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj`; manual admin image upload smoke test if UI verification is allowed.
- Release risk: medium because editor image upload is user-facing admin workflow.
- Rollback: Restore the endpoint exemptions while retaining tests as skipped/pending evidence.
- Needs user confirmation: no. User confirmed there are no external no-token image upload clients.
- Confirmed constraint: keep `/webmention` and other deliberate protocol callbacks exempt.

### Task 3: Add Local Login Throttling

- Priority: P1
- Related findings: F-03
- Goal: Reduce brute-force risk for local account sign-in and TOTP verification.
- Change scope: rate-limit policy/options for local login and TOTP verification, `SignInModel`, `VerifyAuthenticator`, tests for limited and normal paths.
- Excludes: Password hash migration, account lockout persistence, Entra ID behavior changes.
- Expected result: Local password login and TOTP verification are limited to 10 attempts per 1 minute. Prefer partitioning by client IP plus username/account context to avoid one user affecting unrelated users.
- Verification: `dotnet test src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj` and `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj`.
- Release risk: medium; throttling can affect legitimate admins behind shared IPs if configured too tightly.
- Rollback: Disable the new policy by configuration.
- Needs user confirmation: no. User confirmed 10 attempts per 1 minute and TOTP coverage.

### Task 4: Harden Webmention Outbound Fetch Safety

- Priority: P1
- Related findings: F-02
- Goal: Close SSRF gaps in incoming Webmention source inspection.
- Change scope: Webmention URL safety service, HttpClient redirect behavior if needed, Webmention tests.
- Excludes: Changing Webmention protocol response shapes unless required for safety.
- Expected result: private/special-use DNS results, link-local, IPv6 private ranges, intranet/self-hosted private-address sources, and unsafe redirects are rejected before content fetch.
- Verification: `dotnet test src/Tests/Moonglade.Webmention.Tests/Moonglade.Webmention.Tests.csproj`.
- Release risk: medium because stricter validation intentionally rejects private-address sources.
- Rollback: Feature-flag or relax specific rejected ranges after evidence.
- Needs user confirmation: no. User confirmed intranet/self-hosted private-address sources are not allowed.

### Task 5: Remove Committed Default Database Passwords

- Priority: P2
- Related findings: F-05
- Goal: Align committed config with documented secret-handling rules.
- Change scope: committed default config/Compose examples and README instructions if behavior changes.
- Excludes: Real secret rotation, production deployment changes.
- Expected result: Repository no longer contains a concrete reusable database password in default application/Compose configuration.
- Verification: static search for `Password=` defaults and `MSSQL_SA_PASSWORD`; `dotnet build` only if project config behavior changes.
- Release risk: medium for Docker first-run experience.
- Rollback: Restore the old fallback after documenting the risk.
- Needs user confirmation: no. User confirmed Docker Compose may require `.env`.

### Task 6: Normalize Paging Validation

- Priority: P2
- Related findings: F-06
- Goal: Make admin comment paging behavior match other controllers.
- Change scope: `CommentController.List`, possibly `ListCommentsQuery`, Web tests.
- Excludes: Query redesign or UI pagination redesign.
- Expected result: invalid page index/size returns validation error or is clamped consistently.
- Verification: `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj`.
- Release risk: low.
- Rollback: Revert controller validation.
- Needs user confirmation: no.

### Task 7: Bound CannonService Queue

- Priority: P2
- Related findings: F-07
- Goal: Prevent unbounded memory growth while preserving simple in-process background work.
- Change scope: `CannonService`, optional options class/config defaults, background service tests.
- Excludes: Durable queue, separate worker service, replacing the email outbox.
- Expected result: Background queue has configurable capacity and clear reject logging. Use a conservative default capacity and fail enqueue with a warning when full; do not execute external side effects inline on the request path.
- Verification: `dotnet test src/Tests/Moonglade.BackgroundServices.Tests/Moonglade.BackgroundServices.Tests.csproj`; Web tests if call sites change.
- Release risk: medium if queue capacity is too low for image/upload bursts.
- Rollback: Set capacity high or restore unbounded queue.
- Needs user confirmation: no for planning. If implementation chooses a very low capacity or changes user-visible behavior, confirm before merging.

### Task 8: Validate Widget Content Contracts

- Priority: P2
- Related findings: F-08
- Goal: Ensure widget JSON matches the selected widget type and admin previews do not inject raw unescaped values.
- Change scope: `Moonglade.Widgets`, `WidgetsController`/command validation if needed, admin preview renderer, widget tests.
- Excludes: Widget schema redesign, new frontend framework.
- Expected result: invalid widget JSON is rejected with validation feedback; preview escapes untrusted fields. No legacy `ContentCode` migration is required.
- Verification: widget command tests plus `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` if controller behavior changes.
- Release risk: low to medium because user confirmed no legacy widget content formats; read paths should still remain tolerant of malformed data.
- Rollback: Keep read-side tolerant parsing while reverting stricter write validation.
- Needs user confirmation: no.

### Task 9: Unify Image Filename Validation Across Providers

- Priority: P2
- Related findings: F-09
- Goal: Make image storage validation explicit while preserving Azure Blob virtual-folder keys.
- Change scope: image storage provider/key validation and provider tests.
- Excludes: Changing generated filename format or public `/image/{filename}` route shape.
- Expected result: filesystem and S3-compatible providers continue to require flat safe filenames; Azure Blob provider allows virtual folders but rejects traversal, empty path segments, invalid/control characters, missing extensions, and unsupported key shapes.
- Verification: `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj`.
- Release risk: medium because Azure virtual folders must be preserved.
- Rollback: Keep the older Azure extension-only validation while retaining new tests as pending.
- Needs user confirmation: no. User confirmed Azure Blob virtual-folder keys may exist.

### Task 10: Improve Health Checks For Readiness

- Priority: P2
- Related findings: F-10
- Goal: Improve operational diagnostics while keeping `/health` liveness-only.
- Change scope: health-check registration and a separate readiness route.
- Excludes: Full observability stack, metrics platform, external dependency checks beyond database.
- Expected result: `/health` remains self/liveness only; a new endpoint such as `/health/ready` reports database readiness.
- Verification: Web build and focused Web tests if health endpoint tests exist or are added.
- Release risk: low because the existing `/health` contract is preserved.
- Rollback: Remove the new readiness endpoint.
- Needs user confirmation: no. User confirmed `/health` should remain liveness-only.

### Task 11: Clarify And Later Improve View Count Concurrency

- Priority: P3
- Related findings: F-11
- Goal: Remove inappropriate comment language and document the accepted best-effort count behavior.
- Change scope: comment/documentation cleanup only, unless future requirements change.
- Excludes: Analytics redesign.
- Expected result: source comments are professional and accurately state best-effort, in-process concurrency behavior.
- Verification: no runtime verification needed for comment-only cleanup; optional `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj` if any logic changes are made.
- Release risk: low.
- Rollback: Revert the comment/documentation change.
- Needs user confirmation: no. User confirmed exact multi-instance view/request count accuracy is not required.

### Task 12: Add Export Zip Cleanup

- Priority: P3
- Related findings: F-12
- Goal: Avoid unbounded temp zip accumulation from admin exports.
- Change scope: exporter or controller response-completion cleanup, data export tests.
- Excludes: Import/export format changes.
- Expected result: old export zip files are deleted after send or by bounded retention.
- Verification: focused unit tests for cleanup behavior; Web build if controller changes.
- Release risk: low.
- Rollback: Disable cleanup if downloads are interrupted unexpectedly.
- Needs user confirmation: no.

### Task 13: Add Minimal EditorConfig

- Priority: P3
- Related findings: F-13
- Goal: Preserve existing style with lightweight tooling support.
- Change scope: new `.editorconfig` only, based on existing style.
- Excludes: bulk formatting, analyzer package additions, central package management.
- Expected result: new changes converge on current conventions without touching existing files.
- Verification: static review; no format command unless explicitly approved.
- Release risk: low.
- Rollback: remove `.editorconfig`.
- Needs user confirmation: no. User confirmed a minimal `.editorconfig` is allowed.

## Suggested Execution Order

1. Task 1: Register email options validation.
2. Task 2: Restore antiforgery for admin mutations.
3. Task 3: Add local login throttling.
4. Task 4: Harden Webmention outbound fetch safety.
5. Task 5: Remove committed default database passwords and require `.env` for Docker Compose secrets.
6. Task 6: Normalize comment paging validation.
7. Task 8: Validate widget content contracts.
8. Task 9: Unify image filename validation across providers while preserving Azure Blob virtual folders.
9. Task 10: Add a separate readiness endpoint while keeping `/health` liveness-only.
10. Task 7: Bound `CannonService` queue.
11. Task 13: Add a minimal `.editorconfig`.
12. Task 12: Add export zip cleanup.
13. Task 11: Clean up view/request-count comments and document best-effort behavior.

## Not Recommended Now

- Do not rewrite the ASP.NET Core app model. Razor Pages plus controller APIs match the existing application and tests.
- Do not replace LiteBus or the modular project layout. The current boundaries are understandable and testable.
- Do not introduce a separate worker or external durable queue just for `CannonService`; the email path already uses a database outbox, and other work appears low volume.
- Do not do broad dependency upgrades without a specific security advisory or compatibility goal.
- Do not centralize NuGet package versions before the team wants that maintenance model; `.editorconfig` has a better risk/reward ratio as a first hygiene step.
- Do not change `/health` semantics. Add a separate readiness endpoint instead.
- Do not make view/request counts distributed-exact; best-effort multi-instance behavior is acceptable.
- Do not block Azure Blob virtual-folder image keys; validation should reject unsafe paths without removing supported key shapes.
- Do not preserve Docker Compose demo password fallbacks; requiring `.env` is acceptable.

## Resolved Decisions And Remaining Implementation Defaults

All previously open planning questions have been answered by the user. There are no blocking user questions for the next implementation pass.

Implementation defaults to choose during the relevant tasks:

1. `CannonService` bounded capacity should be conservative and configurable. If a very low default or user-visible rejection behavior is proposed, confirm it before merging.
2. Use `/health/ready` as the tentative readiness route unless project maintainers prefer another route name.
3. Keep the first `.editorconfig` minimal and style-preserving; do not bulk-format existing files.

## Notes For Future Execution

- Follow `AGENTS.md` before making any code changes.
- Keep changes small and independently testable.
- Prefer owning modules over Web-layer business logic.
- Update tests in the matching test project for every behavior change.
- Do not update `AGENTS.md`, `README.md`, or other long-lived docs unless the implemented change affects documented behavior.
- Do not quote or copy real local secrets from `appsettings.Development.json`.
- Keep repository documentation in English unless editing localization resource files.
