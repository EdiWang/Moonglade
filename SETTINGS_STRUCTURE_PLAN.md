# Settings Final Acceptance Plan

## Status And Scope

S6 real-host/browser acceptance is the remaining work and requires explicit authorization. The user limited the 2026-11-07 acceptance run to both database providers and full automated tests. That run passed against committed application baseline `cacdae4f1`. Current rules, completed batches, and recorded checks are in [the task tracker](SETTINGS_RELIABILITY_PLAN.md).

S6 verifies the implemented behavior using automated tests and disposable host/browser environments, then presents evidence for user acceptance. Avatar CDN consolidation and versioned-file work are not active batches. No deployment, physical storage migration, real-data recovery, or automatic deletion of old avatar files is included.

## Execution Checklist

Each checkpoint must record Pass, Fail, or Blocked with concrete evidence. Automated regression passed as a prerequisite; the remaining real-host/browser checkpoints are pending.

| Checkpoint | Work and expected result | Status |
| --- | --- | --- |
| Environment | Read `AGENTS.md`, applicable engineering/runtime guidance, and the tracker. Recheck HEAD and working tree; preserve unrelated changes. Use disposable database, authentication, public-image and original-image paths. Do not expose local Development configuration or real credentials. | Pending |
| Automated regression | Full solution on `cacdae4f1` with PostgreSQL and SQL Server enabled: 1460 passed, 0 failed, 0 skipped. Web build: 0 warnings/errors. Full test compilation reported three existing CS8602 warnings in `CannonServiceTests.cs` (88/188/226). | Pass |
| Startup and recovery | Verify fresh/empty-schema installation, repeated startup, rollback and retry, uncertain commit verification, and preservation of existing data when protected settings are missing/invalid. Failures must prevent the HTTP host and business workers from starting. Confirm ordinary recovery and missing-manifest behavior under the tracker rules. Reuse the provider fault tests rather than injecting faults into a real database. | Pending |
| Settings and rendering | Exercise public, admin, and authentication pages; save the ordinary setting groups and check validation, menus, defaults, and subsequent-operation refresh. Verify handler/layout/partial/component consistency and retained old snapshots with the existing paused-operation tests. A later operation can capture new state in the same HTTP context. | Pending |
| Authentication and audit | Verify local login, password changes, TOTP generation/activation/reset, and applicable OIDC behavior. Preserve sessions/sign-outs and rate limits. Ordinary views must not expose account credentials; authorized enrollment still renders its setup key/QR. Use existing fault tests for audit failure versus real persistence/cache/sign-out failure. | Pending |
| Local avatars and article images | Verify avatar upload, local/default rendering, root and PathBase URLs, and unchanged settings rows on avatar saves. Legacy avatar CDN values must not become image sources. Confirm article-image CDN, watermark/original retention, and Gravatar behavior remain available. | Pending |
| Activation and caches | Test FOAF/sitemap and sender-name restart rules in both directions. Verify scheduler wake, disable-before-dispatch, and already dispatched work. Warm feeds, sitemap, theme CSS, and tag output; verify captured-input isolation, new-operation refresh, and normal cache/ETag behavior. Browser/CDN caches retain their existing policies. | Pending |
| Closeout | Review diff and long-lived documentation, remove only task-created disposable resources, record remaining limits, and update the tracker. Present a reviewable acceptance result; obtain user sign-off. Commit or deploy only if separately authorized. | Pending |

## Verification Commands

Run from the repository root. Follow [the test guide](src/Tests/README.md#run-tests); Docker must be ready for the disposable provider groups.

```powershell
dotnet restore src/Moonglade.slnx
$env:MOONGLADE_TEST_CATEGORY_DATABASES = '1'
dotnet test --solution src/Moonglade.slnx --no-restore
dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore
```

For a fix or targeted investigation, run the owning project with `dotnet test --project`, then repeat the relevant final checks. Record environment overrides and the provider used for browser/host scenarios without copying secrets. Do not read or overwrite a real blog database or image directory.

## Acceptance Evidence

Keep this section concise and replace superseded results. The current evidence covers automated acceptance only; extend it during authorized real-host/browser execution.

| Evidence | Current value |
| --- | --- |
| Source commit / unrelated working changes | `cacdae4f1`; working tree clean before automated acceptance; only acceptance records updated afterward |
| Disposable host, providers, and storage | Disposable PostgreSQL 18 and SQL Server 2022 containers used and removed; no real application host or image directories created |
| Automated commands / counts / warnings | Full solution with `MOONGLADE_TEST_CATEGORY_DATABASES=1`: 1460 passed, 0 failed, 0 skipped; Web build passed. Three existing test compilation warnings noted above |
| Browser / real-host cases and results | Not run |
| Defects / fixes / retests | No test failures; no application fixes required |
| Remaining limitations or decisions | Real-host/browser checks are outside this run; see tracker and ask before new business or architecture choices |
| Resource cleanup | No containers with `moonglade.category-tests=true` remained after the run |
| Commit / deployment | Not authorized |
| User acceptance | Pending |

S6 is complete only when required checks pass and the remaining limits are presented for acceptance. A failure blocks only dependent work: continue independent checks, explain the concrete problem and recommendation, and ask when resolving it changes requirements, architecture, recovery policy, or public behavior. Never reintroduce removed features or treat elapsed time as an answer.
