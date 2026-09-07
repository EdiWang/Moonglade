# Removing Low-Value Unit Tests

## Original Goal

Identify and remove low-value unit tests from the project without reducing meaningful behavioral coverage.

## Background

The repository contains xUnit tests across multiple modules. An assertion-quality and test-anti-pattern review identified construction-only tests, a tautological completed-task assertion, duplicate fallback coverage, duplicate empty-input coverage, timing-sensitive microbenchmarks, and a self-referential deterministic-result test.

## Scope

- Remove high-confidence low-value test cases.
- Make the Webmention request-failure test explicitly assert the non-throwing contract and attempted send.
- Run the affected test projects.

## Out of Scope

- Rewriting tests with meaningful null-result, exception, authorization, or security assertions.
- Production-code changes.
- Establishing performance benchmarks.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Review assertions and anti-patterns | None | Static review of xUnit tests | Complete |
| 2 | Remove low-value tests and strengthen retained resilience coverage | 1 | Review diff | Complete |
| 3 | Run affected test projects | 2 | xUnit v3 in-process runner | Complete |

## Execution Order

Review the suite, remove only redundant or non-behavioral coverage, explicitly preserve the externally observable Webmention error-handling contract, then run the affected test projects.

## Current Progress

The cleanup removed 14 low-value test cases. Seven affected test projects passed their targeted test runs (607 total test cases).

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-09-07 | Static assertion review | Passed | Identified only high-confidence removals. |
| 2026-09-07 | `dotnet run --project` for affected test projects | Passed | BackgroundServices (19), ImageStorage (73), Moderation (46), Web.Middleware (29), Theme (88), Utils (283), and Webmention (69); 607 tests total. |
| 2026-09-07 | `git diff --check` | Passed | No whitespace errors. |

## Issues and Resolutions

The initial assertion-free scan also identified a Webmention request-failure test. It is retained because swallowing an outbound request exception is an intentional resilience contract; its behavior is now asserted explicitly. The normal `dotnet test` invocation was unavailable because the repository has not opted in to the .NET 10 native Microsoft Testing Platform experience, so the xUnit v3 test executables were run through `dotnet run --project` instead.

## Follow-ups

No follow-up work is planned.

## Notes

Tests that only assert a documented null return for absence, invalid input, or security rejection were retained because they protect explicit contracts.
