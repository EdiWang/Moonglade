# Comment Markdown Links in Admin Settings

## Original Goal

Move the `CommentMarkdown:EnableLinks` application configuration into the admin settings UI, following the earlier default-editor settings migration.

## Background and Scope

The setting is read by the public comment Razor component, admin comment listing, reply command, and new-comment email notification handler. `CommentSettings` already provides database persistence and runtime updates through Settings > Comments.

Add `CommentSettings.EnableMarkdownLinks`, defaulting to false, expose a built-in comment switch with four localized labels, and route all four consumers through the persisted setting. Remove the old application configuration section and unused configuration dependencies. Keep the shared Markdown renderer and its link safety rules unchanged. Existing stored comments are not rewritten; queued emails keep their existing generated content. Legacy application configuration is not imported, consistent with the previous settings migration.

## Task Breakdown and Execution Order

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect settings, rendering callers, tests, and caching | None | Repository searches and nearby implementation | Complete |
| 2 | Add setting/UI/localization and update all consumers | 1 | Matching tests and Web build | Complete |
| 3 | Verify compatibility, persistence, runtime behavior, and documentation | 2 | Configuration/Features/Email/Utils/Web tests and diff review | Complete |

## Current Progress

The working tree was clean at task start. Implemented the setting, UI, consumers, and matching test updates. Existing settings JSON without the property keeps links disabled. Public comments are fetched as raw Markdown and rendered per request, so no rendered-comment cache invalidation is needed. All verification passed.

## Verification Log

| Date | Check | Result |
| --- | --- | --- |
| 2026-10-22 | Inspect all legacy key readers, callers, and settings persistence | Complete |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Configuration.Tests/Moonglade.Configuration.Tests.csproj --no-restore` | 26 passed |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj --no-restore` | 96 passed |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Email.Tests/Moonglade.Email.Tests.csproj --no-restore` | 97 passed |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | 206 passed |
| 2026-10-22 | `dotnet test --project src/Tests/Moonglade.Utils.Tests/Moonglade.Utils.Tests.csproj --no-restore` | 296 passed |
| 2026-10-22 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore --verbosity quiet` | Passed; zero warnings and errors |
| 2026-10-22 | Final diff, localization XML, and legacy-reader checks | Passed |

## Issues and Resolutions

The rendering behavior spans Features, Email, and Web. All readers must use the runtime `IBlogConfig` rather than retaining host-level configuration dependencies.

## Follow-ups

No implementation work remains. No new dependencies, schema migrations, or Markdown parser changes were needed. Browser interaction and live email delivery were not exercised.
