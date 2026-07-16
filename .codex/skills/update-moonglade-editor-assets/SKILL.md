---
name: update-moonglade-editor-assets
description: Rebuild and sync the standalone Moonglade.Editor package into this Moonglade ASP.NET Core repository. Use when Moonglade.Editor has changed and Codex needs to update committed static editor assets under src/Moonglade.Web/wwwroot/lib/moonglade-editor, verify copied asset hashes, and run the Moonglade Web build.
---

# Update Moonglade Editor Assets

## Overview

Use this skill to update Moonglade's checked-in Moonglade.Editor browser assets from the sibling `Moonglade.Editor` repository. The main Moonglade app consumes only ESM runtime artifacts and must not gain a Node/frontend build pipeline.

The bundled script is portable:

- By default, it treats this repository as `Moonglade` based on the script location.
- By default, it looks for `Moonglade.Editor` as a sibling directory of this repository.
- Pass `-MoongladeRoot` or `-EditorRoot` if a machine uses different paths.

## Workflow

1. Read `AGENTS.md` in both repositories if present.
2. Check `git status --short` in both repositories before changing files.
3. Run the project-level script from the Moonglade repository root:

```powershell
powershell -ExecutionPolicy Bypass -File ".codex/skills/update-moonglade-editor-assets/scripts/update-moonglade-editor-assets.ps1"
```

The script:

- runs `npm test` in `Moonglade.Editor`;
- runs `npm run build` in `Moonglade.Editor`;
- copies only `moonglade-editor.js`, `moonglade-editor.js.map`, and `moonglade-editor.css` into `src/Moonglade.Web/wwwroot/lib/moonglade-editor/`;
- verifies SHA-256 hashes match between source and target;
- runs `dotnet build src/Moonglade.Web/Moonglade.Web.csproj`;
- prints the final Moonglade `git status --short`.

## Guardrails

- Do not copy `moonglade-editor.global.js`, declaration files, or other `dist/` output into Moonglade unless the integration code changes to require them.
- Do not add npm, Vite, webpack, Rollup, esbuild, or package lock files to the main Moonglade repository for this task.
- Keep the update scoped to `src/Moonglade.Web/wwwroot/lib/moonglade-editor/` unless the latest editor API requires integration code changes.
- If the editor API changed, inspect `src/Moonglade.Web/wwwroot/js/app/admin.editpost.editor.mjs` and update the Moonglade integration deliberately, then run `dotnet build` and a browser smoke test.
- If the script reports unexpected files in the Moonglade editor asset directory, inspect them before deleting anything.
- If `npm test`, `npm run build`, hash verification, or `dotnet build` fails, stop and fix the cause before reporting completion.

## Expected Final Report

Report:

- which editor asset files changed;
- `npm test` result;
- `npm run build` result;
- hash verification result;
- `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` result;
- whether documentation or integration code changes were needed.
