# Comment Rate Limit

## Original Goal

Add configurable rate limiting to the public built-in comment creation API. The limit must be partitioned by client IP and post ID.

## Background

The built-in public comment endpoint is `POST /api/comment/{postId}` in `Moonglade.Web.Controllers.CommentController`. Current protection includes optional comment review, word filtering, and comment close-after-days settings. ASP.NET Core's built-in rate limiting middleware fits the Web host layer and can apply a named policy to only the comment creation action.

## Scope

- Add appsettings-backed comment rate limit options.
- Add a named ASP.NET Core rate limiting policy partitioned by client IP and post ID.
- Apply the policy only to the built-in comment creation endpoint.
- Update durable documentation for the new configuration section.
- Add focused tests for partition behavior and disabled behavior.

## Out of Scope

- Adding third-party bot protection.
- Adding an admin UI for the host-level rate limit settings.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add options and policy | Existing Web configuration and Controller route values | Unit tests | Complete |
| 2 | Wire DI, middleware, and endpoint attribute | Task 1 | Web build | Complete |
| 3 | Add appsettings and docs | Task 1 | Search/docs review | Complete |
| 4 | Run focused verification | Tasks 1-3 | Web tests and build | Complete |

## Execution Order

Implement the reusable policy first, then register it in the Web host and attach it to `CommentController.Create`. After behavior is wired, update `appsettings.json`, README, and AGENTS, then run focused tests and a Web build.

## Current Progress

Implemented `CommentRateLimitOptions`, `CommentRateLimitPolicy`, DI/middleware wiring, the `CommentController.Create` rate-limit attribute, appsettings defaults, README/AGENTS documentation, and focused Web tests. Verification passed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-24 | `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` | Passed | 123 tests passed. Existing NU1903 warnings for `System.Security.Cryptography.Xml` 10.0.7 were reported during restore/build. |
| 2026-07-24 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors. |

## Issues and Resolutions

- The first Web test run failed because `RequestPipelineRedirectTests` builds a minimal test host that calls `UseMoongladeRequestPipeline` without the full Web service registration. Added `builder.Services.AddRateLimiter()` to that test host.

## Follow-ups

- Consider content-based spam scoring separately from transport-level rate limiting.

## Notes

The partition key should use `ClientIPHelper.GetClientIP(HttpContext)` to stay aligned with existing forwarded-header behavior.
