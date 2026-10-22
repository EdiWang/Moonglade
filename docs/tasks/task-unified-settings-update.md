# Unified Settings Update

## Original Goal

Unify settings persistence and runtime configuration replacement in one operation, preserving existing behavior and security without introducing test-only abstractions.

## Background

BlogConfig.UpdateAsync changes the singleton before callers persist JSON. SettingsController, SetupAuthenticatorModel, and SaveAssetToCdnHandler duplicate this workflow. TOTP and avatar callers mutate shared objects directly, and image/CDN settings update two related configuration rows separately.

## Scope

- One typed factory creates immutable JSON snapshots for the existing non-generic LiteBus command.
- The command validates all target settings, saves related rows together, then replaces runtime values.
- Missing rows and failed writes prevent runtime changes and subsequent success actions.
- Callers use detached settings objects; image and avatar settings can be submitted together.
- Add regression tests and synchronize repository documentation.

## Out of Scope

Default-settings initialization, database migrations, authentication policy changes, cache redesign, distributed configuration synchronization, and filesystem transactions.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Consolidate command and remove the old runtime update helper | None | Configuration tests and Web build | Complete |
| 2 | Migrate settings, TOTP, and avatar callers | 1 | Web tests using the real configuration handler | Complete |
| 3 | Verify failures and update documentation | 1, 2 | Affected test projects and diff review | Complete |

## Execution Order

Inspect all callers, implement the command, migrate callers, replace mocks of successful configuration writes with real handler execution in tests, then verify and document.

## Current Progress

The old BlogConfig.UpdateAsync and controller persistence helper have been removed. The non-generic LiteBus update command has typed factories for one setting or the actual two-setting image/avatar operation. It validates and persists all rows before replacing detached runtime objects. All three production callers have migrated. Existing Web tests now execute the real update handler against SQLite through a shared command recorder, replacing three duplicated success-only mediators. No production package or interface was added.

Implementation and documentation are complete. Configuration, Web, Auth, and Email tests passed (422 tests total). The Web project builds with zero warnings and errors. No database schema or deployment configuration change is required.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-10-22 | Inspect configuration writers and authentication/avatar callers | Complete | Include paired image/general settings and detached TOTP copies. |
| 2026-10-22 | Web project build, no restore | Passed | Zero warnings and errors. |
| 2026-10-22 | Configuration test project, no restore | Passed | 30 tests: snapshots, paired writes, missing rows, failures, cancellation before/after commit, concurrent commit order, and unknown/null settings. |
| 2026-10-22 | Web test project, no restore | Passed | 247 tests: real LiteBus registration, SQLite rollback, TOTP failures, avatar failures, and existing behavior. |
| 2026-10-22 | Auth test project, no restore | Passed | 48 tests. |
| 2026-10-22 | Email test project, no restore | Passed | 97 tests. |
| 2026-10-22 | Final Web build and diff review | Passed | No build warnings/errors; no schema changes, provider-specific SQL, or production package additions. |

## Issues and Resolutions

- Serialize commit and singleton replacement within the process so simultaneous saves cannot apply in reverse commit order.
- Missing configuration rows are startup/state failures and must not be reported as successful saves.
- JSON snapshots prevent callers from changing runtime configuration through retained request objects after a successful save.

## Follow-ups

Configuration replacement is process-local. Filesystem image writes remain outside database transactions.
