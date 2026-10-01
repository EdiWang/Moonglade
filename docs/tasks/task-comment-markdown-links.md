# Comment Markdown Links

## Original Goal

Add an appsettings switch that disables comment Markdown hyperlinks by default while preserving their original input as plain text.

## Background and Scope

Comment rendering is shared by public Razor views, the admin API, reply commands, and email notifications. Keep stored Markdown unchanged, preserve other Markdown formatting, and retain existing link safety checks when links are enabled. Post Markdown is unaffected.

## Task Breakdown and Execution Order

| Task | Dependencies | Status |
| --- | --- | --- |
| Render disabled links using original Markdown source spans | None | Complete |
| Pass CommentMarkdown:EnableLinks through all comment renderers | Shared renderer | Complete |
| Verify default and enabled rendering, build Web, update configuration docs | Configuration wiring | Complete |

## Verification Log

| Date | Check | Result |
| --- | --- | --- |
| 2026-09-27 | Utils tests filtered to MarkdownToCommentHtml | 20 passed |
| 2026-09-27 | Full Utils / Features / Email / Web test projects | 295 / 96 / 88 / 196 passed |
| 2026-09-27 | Web build | Passed, zero warnings and errors |

## Issues and Resolutions

Reference-style links store URLs in separate definitions. Preserve those definitions as literal text as well.

Parallel test builds initially contended for shared project output. Running dependent builds sequentially resolved the file lock.

## Follow-ups

None.
