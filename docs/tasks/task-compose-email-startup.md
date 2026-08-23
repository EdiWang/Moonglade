# Docker Compose Email Startup Configuration

## Original Goal

Record the Docker Compose startup failure caused by the default email configuration so it can be investigated and fixed in a separate task. Do not change email behavior as part of the filesystem-only image-storage work.

## Background

During Docker verification of the filesystem-only image-storage refactor, a clean `docker compose up --detach --build` started SQL Server successfully but the Web container entered its restart policy before becoming healthy. Startup option validation reported that `Email:AcsConnectionString` and `Email:AcsSenderAddress` are required because the default application configuration selects `Email:Provider=AzureCommunication`, while the supplied Compose environment does not provide those values.

An isolated smoke run supplied task-only SMTP settings and disabled the email outbox worker. With those unrelated prerequisites satisfied, the same application image started successfully and `/health` returned HTTP 200. This confirms that the observed default Compose failure is independent of image storage.

## Scope

- Decide the intended zero-configuration Docker Compose email behavior.
- Align default application configuration, Compose environment overrides, and option validation with that decision.
- Add a startup regression check that does not require real email credentials or send external email.
- Update README and deployment guidance if operators must provide explicit email settings before first startup.

## Out of Scope

- Image-storage paths, Docker volumes, storage permissions, CDN behavior, or cloud storage mounts.
- Sending a real email during automated tests.
- Adding production credentials, connection strings, or sender addresses to the repository.
- Changing Azure App Service email deployment behavior unless separately approved.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Confirm the intended default email provider and whether delivery should be disabled until configured | Product decision | Approved configuration contract | Not started |
| 2 | Implement the selected application/Compose configuration behavior without embedding credentials | 1 | Options-validation tests and configuration inspection | Not started |
| 3 | Add a clean Compose startup regression and synchronize documentation | 2 | Docker image build, `/health`, documentation review | Not started |

## Execution Order

Confirm the product behavior first because changing the provider default, relaxing validation, or adding Compose placeholders have different operational and security consequences. Implement and unit-test option behavior before running an isolated Compose startup check. Do not use real provider credentials for verification.

## Current Progress

Issue recorded for later work. No email source, configuration, deployment asset, or documentation has been changed by this task.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-23 | Clean isolated `docker compose --project-name moonglade-codex-app-smoke up --detach --build` and `/health` poll | Failed | SQL Server became ready; Web restarted because Azure Communication was selected without its required ACS connection string and sender address |
| 2026-08-23 | Isolated Web/SQL Server run with task-only SMTP settings and `Email__OutboxWorker__Enabled=false` | Passed | Application remained running and `/health` returned HTTP 200; no email was sent |

## Issues and Resolutions

- **Default configuration is internally incomplete for Compose startup:** The selected provider requires settings that the default configuration and Compose file leave empty. Resolution is intentionally deferred until the intended default email behavior is confirmed.
- **A smoke test must not create an external side effect:** Startup verification needs syntactically valid local-only settings and a disabled worker, or another approved no-delivery configuration. Do not use real provider credentials.

## Follow-ups

- Determine whether this affects only Docker Compose or every clean Production deployment that uses the repository defaults.
- Review data-protection key persistence separately if the future task broadens beyond the email startup failure; the smoke run also emitted the standard ephemeral-key warning.

## Notes

- Reproduction came from the Docker follow-up recorded in `docs/tasks/task-filesystem-only-image-storage.md`.
- Keep this task separate from image storage. A future fix should not alter the two image paths or their volume mappings.
