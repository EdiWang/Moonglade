# Remove Comment Captcha

## Goal

Remove the built-in image captcha feature and related code from public comment submission.

## Background

Public comments are now protected by configurable IP-plus-post rate limiting and `CommentSubmissionGuard` honeypot and elapsed-time checks. The image captcha endpoint, validation filter, request fields, client script, configuration, and documentation are no longer needed.

## Scope

- Remove captcha validation from comment creation.
- Remove captcha API endpoint, filter, client script, package dependency, and default configuration.
- Remove captcha UI fields, styles, and localization entries.
- Update tests and long-lived documentation.

## Out Of Scope

- Changing comment rate limiting behavior.
- Changing `CommentSubmissionGuard` behavior.
- Adding third-party anti-spam services.

## Task Breakdown

| Step | Status |
| --- | --- |
| Inspect current captcha references | Done |
| Remove backend captcha endpoint, validation, configuration, and package dependency | Done |
| Remove frontend captcha UI, script, styles, and resources | Done |
| Update tests and documentation | Done |
| Run targeted verification | Done |

## Verification Log

- `dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj` passed: 87 tests.
- `dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj` passed: 129 tests. Restore reported existing `System.Security.Cryptography.Xml` 10.0.7 NU1903 vulnerability warnings.
- `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` passed with 0 warnings and 0 errors.

## Follow-Ups

- None currently.
