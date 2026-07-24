# Comment Submission Guard

## Original Goal

Add a honeypot field and submission elapsed-time check to the built-in public comment form.

## Background

The built-in comment form is rendered by `src/Moonglade.Web/Pages/_CommentForm.cshtml` and submitted by `src/Moonglade.Web/wwwroot/js/app/post.mjs` as JSON to `POST /api/comment/{postId}`. Existing protection includes stateless captcha and the new IP-plus-post rate limit. This task adds another lightweight anti-automation layer before comment creation.

## Scope

- Add host-level configuration for comment submission guard behavior.
- Add request fields for a honeypot and form-render timestamp.
- Validate that the honeypot is empty and the form was not submitted too quickly or with a stale timestamp.
- Update the public comment form and post JavaScript payload.
- Add focused Web tests and update long-lived documentation.

## Out of Scope

- Captcha endpoint rate limiting.
- Third-party bot detection services.
- Admin UI for these host-level settings.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add options and guard service | Existing comment request shape | Unit tests | Complete |
| 2 | Add Razor and JS fields | Task 1 | Web build | Complete |
| 3 | Apply guard in controller | Task 1 | Controller tests | Complete |
| 4 | Update config and docs | Tasks 1-3 | Search/docs review | Complete |
| 5 | Run focused verification | Tasks 1-4 | Web tests and build | Complete |

## Execution Order

Implement the reusable guard service first, then update the request payload and UI fields, then call the guard from `CommentController.Create`. After behavior is covered by tests, update `appsettings.json`, README, and AGENTS.

## Current Progress

Implemented `CommentSubmissionGuardOptions`, `CommentSubmissionGuard`, request fields, Razor/JS payload changes, controller validation, appsettings defaults, README/AGENTS documentation, and focused tests. Verification passed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-24 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 129 tests passed. Existing NU1903 warnings for `System.Security.Cryptography.Xml` 10.0.7 were reported during restore/build. |
| 2026-07-24 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed after rerun | Initial parallel run hit Windows file locks in `obj`; serial rerun succeeded with 0 warnings and 0 errors. |
| 2026-07-24 | `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj` | Passed | 87 tests passed. |

## Issues and Resolutions

- Running Web tests and Web build in parallel caused transient CS2012 file-lock errors for shared project `obj` DLLs. Reran `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` serially and it passed.
- PR #988 review noted that refreshing `formRenderedUtc` with `Date.now()` could reject valid submissions when client and server clocks differ. Fixed by returning a new server-issued `FormRenderedUtc` value from `CommentController.Create` and using that value in `post.mjs`.
- PR #988 review noted that `CommentSubmissionGuardResult` used null defaults for non-nullable string properties. Fixed by enabling nullable context in `CommentSubmissionGuard.cs` and marking `ModelStateKey` and `ErrorMessage` as nullable.

## Follow-ups

- Consider stronger signed form tokens if this lightweight timing signal proves too easy to bypass.

## Notes

The post model caches post data, not the rendered Razor page, so a server-rendered form timestamp is generated per page response.
