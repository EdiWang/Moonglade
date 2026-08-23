# Resilient Email Module Startup

## Goal

Treat email notification as an optional application capability. Missing or invalid `Email` configuration must never prevent Moonglade from starting or make the blog unavailable. When email configuration is unusable, email notification must remain inactive, no delivery attempt should be made, and the Admin Portal Notification settings page must show an actionable error message.

A correctly configured email provider and outbox worker must keep the existing delivery behavior.

## Background

During Docker verification of the filesystem-only image-storage refactor, a clean `docker compose up --detach --build` started SQL Server successfully but the Web container entered its restart policy before becoming healthy. Startup option validation reported that `Email:AcsConnectionString` and `Email:AcsSenderAddress` are required because the default application configuration selects `Email:Provider=AzureCommunication`, while the supplied Compose environment does not provide those values.

An isolated smoke run supplied task-only SMTP settings and disabled the email outbox worker. With those unrelated prerequisites satisfied, the same application image started successfully and `/health` returned HTTP 200. This confirms that the observed default Compose failure is independent of image storage.

The intended behavior is now decided: email configuration errors disable only the email capability; they are not host-startup errors.

## Behavioral Contract

- Invalid or incomplete email provider settings and invalid email outbox worker settings do not fail host startup.
- Email validation rules remain explicit and testable; removing fatal startup validation must not mean treating invalid settings as valid.
- A single email capability/status service reports whether email notification is available and exposes safe, actionable validation messages. The worker, notification producers, test-email endpoint, and Admin Portal must use this shared result instead of duplicating configuration checks.
- When email is unavailable because configuration is invalid, the outbox worker does not poll or attempt delivery, notification event handlers do not enqueue new messages, and ordinary blog operations that would have raised an email notification continue successfully.
- Existing pending outbox messages are retained and may be processed after configuration is corrected and the application is restarted; this task must not delete or mark them as delivered.
- An explicitly disabled outbox worker is shown as disabled and does not run. It is distinct from invalid configuration, but email notification is unavailable in both cases.
- The test-email action must not report that a message was queued when email is unavailable. It should return a clear non-success response that the existing JavaScript can present to the administrator.
- The Notification settings page shows the shared status and all relevant configuration errors without exposing passwords, connection strings, or other secret values. It must not infer readiness directly from raw `IConfiguration` values.
- Email configuration does not affect `/health` or `/health/ready`; database readiness remains the only readiness concern described by the current health endpoint contract.

## Scope

- Replace fatal `ValidateOnStart` behavior for the optional email capability with non-fatal validation and a shared availability/status abstraction owned by `Moonglade.Email`.
- Apply the shared availability result to `EmailOutboxWorker`, notification event handlers, and the test-email workflow so invalid configuration causes a safe no-op rather than request or host failure.
- Update the Admin Portal Notification settings page to display provider/worker status and actionable validation errors from the shared abstraction.
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
| 1 | Define a shared email capability status model that evaluates the existing provider and worker validators, distinguishes invalid configuration from an explicitly disabled worker, and returns secret-safe messages | None | Unit tests for missing provider values, unsupported provider, invalid ports/worker values, disabled worker, and valid SMTP/ACS settings | Not started |
| 2 | Change email service registration so provider or worker validation failures are recorded by the status model instead of being raised by `ValidateOnStart`; preserve normal options binding and validation semantics for consumers | 1 | A host with missing or invalid `Email` settings starts successfully; a valid host resolves all email services | Not started |
| 3 | Guard email execution paths with the shared status: make the worker exit without polling, make notification handlers skip enqueueing without failing the originating blog operation, and preserve existing pending outbox rows | 1, 2 | Worker and event-handler tests prove there is no poll, send, or enqueue while unavailable and valid configuration behaves unchanged | Not started |
| 4 | Make the test-email endpoint reject unavailable email with a clear ProblemDetails-compatible response, and update its client-side handling so the Admin Portal never reports a false queued result | 1, 3 | Controller and JavaScript-facing response tests cover unavailable and available states | Not started |
| 5 | Replace the Notification page's raw `IConfiguration` readiness checks with the shared status, render localized warnings/errors without secret values, and disable or otherwise prevent the test action while unavailable | 1, 4 | Razor/Web tests or rendered-page assertions cover invalid, disabled, and valid status; all supported resource files contain any new keys | Not started |
| 6 | Update README/deployment guidance and run a clean Compose regression without email overrides or real credentials | 2-5 | Focused test projects pass; Web project builds; Web container stays healthy and `/health` returns HTTP 200 while the Notification page reports unavailable email | Not started |

## Execution Order

Implement the shared status contract first so startup, background processing, event production, API behavior, and Razor UI all consume the same decision. Remove fatal startup validation only after that status is available. Then guard server-side execution paths before updating the Admin Portal, ensuring the application cannot enqueue or claim to send email while configuration is invalid. Finish with focused tests, localization, documentation, and a clean Compose smoke test using repository defaults and no provider credentials.

## Acceptance Criteria

1. A clean Production or Docker Compose deployment with the repository's incomplete default email settings starts and serves `/health` successfully.
2. Missing required provider values, an unsupported provider, or invalid provider/worker option values do not throw an `OptionsValidationException` during host startup.
3. The same invalid values are reported by the shared status, logged without secrets, and shown on `/admin/settings/notification`.
4. While email is unavailable, the worker performs no delivery work, notification events enqueue nothing, the test-email API does not return success, and unrelated post/comment/Webmention workflows are not failed by email configuration.
5. Disabling `Email:OutboxWorker:Enabled` is visible in the Admin Portal and prevents email processing without affecting application startup.
6. After valid SMTP or Azure Communication settings are supplied and the worker is enabled, existing notification, outbox retry, and delivery behavior remains unchanged.
7. No automated verification sends external email, and no UI, response, or log message reveals configured secrets.

## Verification Plan

- Run `Moonglade.Email.Tests`, including host-start, capability status, event-handler, and worker coverage.
- Run `Moonglade.Web.Tests` for the test-email endpoint and Notification settings surface.
- Build `src/Moonglade.Web/Moonglade.Web.csproj` to verify DI and Razor compilation.
- Start a clean isolated Docker Compose project with repository defaults, wait for the Web container, and verify `/health` returns HTTP 200 without any email environment override.
- Sign in to the Admin Portal during the smoke test and confirm the Notification settings page identifies the invalid/incomplete email configuration without displaying secret values.

## Current Progress

The product behavior has been clarified and this implementation plan has been revised accordingly. No email source, configuration, deployment asset, test, or operator documentation has yet been changed by this task.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-23 | Clean isolated `docker compose --project-name moonglade-codex-app-smoke up --detach --build` and `/health` poll | Failed | SQL Server became ready; Web restarted because Azure Communication was selected without its required ACS connection string and sender address |
| 2026-08-23 | Isolated Web/SQL Server run with task-only SMTP settings and `Email__OutboxWorker__Enabled=false` | Passed | Application remained running and `/health` returned HTTP 200; no email was sent |

## Issues and Resolutions

- **Default configuration is incomplete for email delivery:** This is accepted for a zero-configuration deployment. The resolution is to keep email validation visible but non-fatal, disable the email capability, and show the validation result in the Admin Portal.
- **Validation logic could diverge between the worker and UI:** Use one status abstraction backed by the existing validators; do not reproduce provider-specific checks in Razor.
- **A disabled worker or invalid provider could allow the outbox to grow indefinitely:** Notification producers must consult the same availability status and skip new enqueue operations while email is unavailable. Existing pending rows remain recoverable.
- **A smoke test must not create an external side effect:** Use incomplete repository defaults to verify graceful disablement and use mocked/fake senders for positive automated tests. Do not use real provider credentials.

## Follow-ups

- Review data-protection key persistence separately if a future task broadens beyond the email startup failure; the original smoke run also emitted the standard ephemeral-key warning.
- Consider live configuration reload separately if operators later need to repair email without restarting the application.

## Notes

- Reproduction came from the Docker follow-up recorded in `docs/tasks/task-filesystem-only-image-storage.md`.
- Keep this task separate from image storage. Its implementation must not alter the two image paths or their volume mappings.
