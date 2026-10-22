# Move the Email Enable Switch

## Original Goal

Move `Email:OutboxWorker:Enabled` to `Email:Enabled` while preserving existing email behavior.

## Background and Scope

Email capability evaluation controls notification enqueueing, outbox delivery, and the admin test-email action. Move the default-enabled option to `EmailServiceOptions`, update the admin page and localized labels, and synchronize tests, configuration, README, and AGENTS.md. Outbox delivery and retry settings remain unchanged. No legacy-key fallback is planned.

## Task Breakdown and Execution Order

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Move the option and update consumers, configuration, and documentation | None | Inspect diff and search references | Complete |
| 2 | Update capability, binding, worker, controller, page, and localization tests | 1 | Run Email and Web test projects; build Web | Complete |

## Current Progress

Implemented the option move with the default remaining `true`. Updated capability evaluation, startup logs, controller guidance, admin page state and localized labels, tests, default configuration, README, and AGENTS.md. Removed the page's unused worker-options dependency. Working tree was clean before changes. Verification passed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-22 | Inspect current implementation and callers | Complete | Disabled notifications do not enqueue through built-in handlers; direct queue storage remains available. |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj --no-restore` | Passed | 97 tests; includes new-key binding, default enablement, disabled capability, and worker polling behavior. |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | Passed | 201 tests; includes test-email API guidance, page state, and all four localization resources. |
| 2026-10-22 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity minimal` | Passed | Zero errors; six existing NU1902/NU1903 warnings for System.Security.Cryptography.Pkcs dependencies. |
| 2026-10-22 | `git diff --check` and search for old-key consumers | Passed | Old key remains only in migration documentation and this task's original goal. |

## Issues and Resolutions

Corrected AGENTS.md's imprecise description of disabled-worker enqueueing. Existing dependency vulnerability warnings are outside this configuration rename. No production SMTP delivery or browser visual checks were performed.

## Follow-ups and Risks

Deployment overrides must migrate to `Email:Enabled` / `Email__Enabled` and restart the application. The old key will no longer control email; omitted `Email:Enabled` defaults to `true`. Pending messages may resume delivery when email becomes available.
