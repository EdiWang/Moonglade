# Resilient Email Module Startup

## Goal

Treat email notification as an optional application capability. Missing or invalid `Email` configuration must never prevent Moonglade from starting or make the blog unavailable. When email configuration is unusable, email notification must remain inactive and no delivery attempt should be made.

Missing configuration is an expected disabled state: log a warning and tell administrators how to enable email by adding the required configuration. Malformed or inconsistent configuration is an error state: log an error and show safe, actionable validation errors in the Admin Portal Notification settings page. Neither state may fail application startup.

A correctly configured email provider and outbox worker must keep the existing delivery behavior.

## Background

During Docker verification of the filesystem-only image-storage refactor, a clean `docker compose up --detach --build` started SQL Server successfully but the Web container entered its restart policy before becoming healthy. Startup option validation reported that `Email:AcsConnectionString` and `Email:AcsSenderAddress` are required because the default application configuration selects `Email:Provider=AzureCommunication`, while the supplied Compose environment does not provide those values.

An isolated smoke run supplied task-only SMTP settings and disabled the email outbox worker. With those unrelated prerequisites satisfied, the same application image started successfully and `/health` returned HTTP 200. This confirms that the observed default Compose failure is independent of image storage.

The intended behavior is now decided: absent configuration disables email with a warning, invalid configuration disables email with an error, and neither condition is a host-startup error.

## Behavioral Contract

- Missing and invalid email provider settings and invalid email outbox worker settings do not fail host startup.
- A single email capability/status service classifies email as `Available`, `NotConfigured`, `Invalid`, or `Disabled`. The worker, notification producers, test-email endpoint, logging, and Admin Portal must use this shared result instead of duplicating configuration checks.
- `NotConfigured` means one or more required provider values are absent or blank. This includes the repository default of selecting Azure Communication while leaving its connection string and sender address empty. It produces one startup warning, not an error, and the Admin Portal shows a localized instruction to add email configuration to enable the feature instead of listing validation errors.
- `Invalid` means a supplied non-empty value is unsupported, malformed, out of range, or inconsistent with another email option. Examples include an unsupported provider, a malformed ACS sender address, an invalid SMTP port, or invalid worker timing/retry values. It produces an error log and the Admin Portal shows the relevant secret-safe validation messages.
- If missing required values and malformed supplied values occur together, `Invalid` takes precedence so the malformed values are not hidden behind the setup prompt.
- `Disabled` means email provider settings are valid but `Email:OutboxWorker:Enabled` is explicitly `false`. It remains a deliberate non-error state, is logged at information level, and is shown separately in the Admin Portal.
- Email validation rules remain explicit and testable; removing fatal startup validation must not mean treating missing or invalid settings as valid.
- Log the capability state once during startup or worker initialization rather than on every poll or notification event. Logs may name configuration keys and safe validation messages but must not include configured secret values.
- When email is `NotConfigured`, `Invalid`, or `Disabled`, the outbox worker does not poll or attempt delivery, notification event handlers do not enqueue new messages, and ordinary blog operations that would have raised an email notification continue successfully.
- Existing pending outbox messages are retained and may be processed after configuration is corrected and the application is restarted; this task must not delete or mark them as delivered.
- An explicitly disabled outbox worker is shown as disabled and does not run. It is distinct from invalid configuration, but email notification is unavailable in both cases.
- The test-email action must not report that a message was queued when email is unavailable. It should return a clear non-success response that the existing JavaScript can present to the administrator.
- The Notification settings page shows setup guidance for `NotConfigured`, validation errors for `Invalid`, and the existing worker-disabled notice for `Disabled`, without exposing passwords, connection strings, or other secret values. It must not infer readiness directly from raw `IConfiguration` values.
- Email configuration does not affect `/health` or `/health/ready`; database readiness remains the only readiness concern described by the current health endpoint contract.

## Scope

- Replace fatal options-pipeline validation for the optional email capability with non-fatal validation and a shared availability/status abstraction owned by `Moonglade.Email`, including the explicit `Available`, `NotConfigured`, `Invalid`, and `Disabled` states. Reuse the existing validation rules through this abstraction rather than allowing `IOptions<T>.Value` resolution to throw before guarded code can inspect the status.
- Apply the shared availability result to `EmailOutboxWorker`, notification event handlers, and the test-email workflow so invalid configuration causes a safe no-op rather than request or host failure.
- Update the Admin Portal Notification settings page to display provider/worker status, setup guidance for missing configuration, and actionable errors only for invalid configuration.
- Add or update localized UI strings in every supported non-English `Program.*.resx` resource when new `SharedLocalizer` keys are introduced.
- Add focused regression coverage for invalid, missing, disabled, and valid configurations.
- Verify clean Docker Compose startup without injecting fake email credentials and update operator documentation to describe email as optional and how to resolve the displayed errors.

## Out of Scope

- Image-storage paths, Docker volumes, storage permissions, CDN behavior, or cloud storage mounts.
- Sending a real email during automated tests.
- Adding production credentials, connection strings, or sender addresses to the repository.
- Changing Azure App Service deployment topology or secret-management mechanisms.
- Adding an email provider, changing the outbox database schema, or deleting pending outbox messages.
- Turning email availability into a liveness or readiness health-check dependency.
- Building live configuration reload; correcting deployment-level email settings may continue to require an application restart.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Define a shared email capability status model with `Available`, `NotConfigured`, `Invalid`, and `Disabled` states; separate absent/blank required values from malformed supplied values, give `Invalid` precedence when both occur, and return only secret-safe messages | None | Unit tests cover repository defaults, partially missing provider values, unsupported provider, malformed addresses, invalid ports/worker values, mixed missing-and-invalid values, disabled worker, and valid SMTP/ACS settings | Not started |
| 2 | Change email service registration so missing or invalid provider/worker configuration is evaluated by the status model instead of being raised by `ValidateOnStart` or later `IOptions<T>.Value` resolution; retain the existing rules as explicitly invoked validators, and emit one Warning for `NotConfigured`, one Error with safe details for `Invalid`, and Information for `Disabled` | 1 | Hosts with missing and invalid `Email` settings start successfully and can resolve the status without an `OptionsValidationException`; captured logs have the correct level and contain no secrets; a valid host resolves all email services | Not started |
| 3 | Guard email execution paths with the shared status: make the worker exit without polling, make notification handlers skip enqueueing without failing the originating blog operation, and preserve existing pending outbox rows | 1, 2 | Worker and event-handler tests prove there is no poll, send, or enqueue while unavailable and valid configuration behaves unchanged | Not started |
| 4 | Make the test-email endpoint reject unavailable email with a clear ProblemDetails-compatible response appropriate to `NotConfigured`, `Invalid`, or `Disabled`, and update its client-side handling so the Admin Portal never reports a false queued result | 1, 3 | Controller and JavaScript-facing response tests cover every unavailable state and the available state | Not started |
| 5 | Replace the Notification page's raw `IConfiguration` readiness checks with the shared status; render a localized enablement prompt for `NotConfigured`, safe error details for `Invalid`, and a distinct notice for `Disabled`; disable or otherwise prevent the test action while unavailable | 1, 4 | Razor/Web tests or rendered-page assertions cover all four states; missing configuration is not rendered as an error; all supported resource files contain any new keys | Not started |
| 6 | Update README/deployment guidance and run a clean Compose regression without email overrides or real credentials | 2-5 | Focused test projects pass; Web project builds; Web container stays healthy and `/health` returns HTTP 200 while the Notification page reports unavailable email | Not started |

## Execution Order

Implement the shared status and classification contract first so startup logging, background processing, event production, API behavior, and Razor UI all consume the same decision. Explicitly test the boundary between absent/blank values and malformed supplied values before removing fatal startup validation. Then guard server-side execution paths before updating the Admin Portal, ensuring the application cannot enqueue or claim to send email while unavailable. Finish with focused tests, localization, documentation, and a clean Compose smoke test using repository defaults and no provider credentials.

## Acceptance Criteria

1. A clean Production or Docker Compose deployment with the repository's incomplete default email settings starts and serves `/health` successfully.
2. Missing required provider values produce a Warning log and a localized Admin Portal prompt explaining that email configuration must be added to enable the feature; they do not produce an Error log or display validation errors.
3. An unsupported provider or malformed/out-of-range/inconsistent supplied values produce an Error log and secret-safe validation details on `/admin/settings/notification`; they do not throw an `OptionsValidationException` during host startup.
4. When missing and invalid conditions coexist, the status is `Invalid` and the invalid supplied values are reported.
5. While email is `NotConfigured`, `Invalid`, or `Disabled`, the worker performs no delivery work, notification events enqueue nothing, the test-email API does not return success, and unrelated post/comment/Webmention workflows are not failed by email configuration.
6. Disabling `Email:OutboxWorker:Enabled` is logged as information, is visible as a distinct non-error state in the Admin Portal, and prevents email processing without affecting application startup.
7. After valid SMTP or Azure Communication settings are supplied and the worker is enabled, existing notification, outbox retry, and delivery behavior remains unchanged.
8. No automated verification sends external email, and no UI, response, or log message reveals configured secrets.

## Verification Plan

- Run `Moonglade.Email.Tests`, including host-start, capability status, event-handler, and worker coverage.
- Run `Moonglade.Web.Tests` for the test-email endpoint and Notification settings surface.
- Build `src/Moonglade.Web/Moonglade.Web.csproj` to verify DI and Razor compilation.
- Start a clean isolated Docker Compose project with repository defaults, wait for the Web container, and verify `/health` returns HTTP 200 without any email environment override.
- Sign in to the Admin Portal during the smoke test and confirm the repository's absent provider credentials produce enablement guidance rather than an error, without displaying secret values.

## Current Progress

The product behavior has been clarified and this implementation plan now distinguishes missing configuration from invalid configuration, including their different log levels and Admin Portal messages. No email source, configuration, deployment asset, test, or operator documentation has yet been changed by this task.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-23 | Clean isolated `docker compose --project-name moonglade-codex-app-smoke up --detach --build` and `/health` poll | Failed | SQL Server became ready; Web restarted because Azure Communication was selected without its required ACS connection string and sender address |
| 2026-08-23 | Isolated Web/SQL Server run with task-only SMTP settings and `Email__OutboxWorker__Enabled=false` | Passed | Application remained running and `/health` returned HTTP 200; no email was sent |

## Issues and Resolutions

- **Default configuration is incomplete for email delivery:** This is accepted for a zero-configuration deployment and classified as `NotConfigured`. The resolution is to disable the email capability, log a warning, and show an enablement prompt in the Admin Portal; it is not an application error.
- **Missing and invalid configuration need different operator signals:** Absent or blank required values are `NotConfigured`; malformed, unsupported, out-of-range, or inconsistent supplied values are `Invalid`. Invalid takes precedence when both are present. Only `Invalid` produces error logs and detailed error messages.
- **Validation logic could diverge between the worker and UI:** Use one status abstraction backed by the existing validators; do not reproduce provider-specific checks in Razor.
- **A disabled worker or invalid provider could allow the outbox to grow indefinitely:** Notification producers must consult the same availability status and skip new enqueue operations while email is unavailable. Existing pending rows remain recoverable.
- **A smoke test must not create an external side effect:** Use incomplete repository defaults to verify graceful disablement and use mocked/fake senders for positive automated tests. Do not use real provider credentials.

## Follow-ups

- Review data-protection key persistence separately if a future task broadens beyond the email startup failure; the original smoke run also emitted the standard ephemeral-key warning.
- Consider live configuration reload separately if operators later need to repair email without restarting the application.

## Notes

- Reproduction came from the Docker follow-up recorded in `docs/tasks/task-filesystem-only-image-storage.md`.
- Keep this task separate from image storage. Its implementation must not alter the two image paths or their volume mappings.
