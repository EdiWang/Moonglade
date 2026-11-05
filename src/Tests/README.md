# Tests

The 15 test projects follow the production project boundaries and use xUnit v3 with Microsoft.Testing.Platform. They contain both focused unit tests and integration tests for database queries, service registration, and HTTP behavior.

## Layout

| Project | Organization |
| --- | --- |
| `Moonglade.Features.Tests` | Production feature folders: `Post`, `Page`, `Comment`, `Category`, `Tag`, `Dashboard`, and `SiteVerification`. |
| `Moonglade.Web.Tests` | `Controllers`, `Pages`, `Commands`, `Handlers`, `Services`, `Models`, `Caching`, and `Hosting`. Shared test helpers live in `TestSupport`. Page folders mirror the production pages. |
| `Moonglade.Email.Tests` | `Configuration`, `Delivery`, `Notifications`, and `Outbox`. |
| `Moonglade.Utils.Tests` | Content processing tests are split by behavior under `Content`; tag validation and normalization live under `Tags`. Other utilities have their own test files. |
| `Moonglade.Setup.Tests` | Startup, configuration initialization, preservation of existing content and installation metadata, and UTC validation. |
| Other test projects | Keep the existing small, focused suites close to their production ownership. |

Namespaces match folders. Import shared Web helpers from `Moonglade.Web.Tests.TestSupport`; keep helpers used by only one suite local.

## Test Value

Protect business outcomes, public input validation, security boundaries, error handling, cancellation, time handling, and data consistency. Service registration and HTTP pipeline tests remain useful even when their assertions are small: they protect application wiring that isolated handler tests cannot exercise.

Redundant setup is reduced with theories when the same operation and assertions apply to several inputs. Separate tests remain appropriate for different side effects or failure paths. Assertions target observable results, such as persisted values, generated URLs, serialized documents, HTTP responses, and queued notifications.

Tests that only repeat a happy path, pass an uncancelled token without testing cancellation, inspect a trivial getter, or check a startup log add little protection when stronger behavioral coverage already exists. Failing tests are investigated and reported; failure alone is never a reason to delete or weaken a test.

See [AGENTS.md](../../AGENTS.md#development-and-verification) for the repository's verification requirements.

## Database Upgrade Coverage

Startup tests use EF Core InMemory to verify settings initialization and preservation of stored content, manifest JSON, and timestamps for existing databases with missing or older manifests. They do not prove relational schema compatibility.

Startup safety tests exercise each mapped application set as the only existing data and verify that a missing account prevents any repair or reseeding. The opt-in `StartupInstallationDatabaseTests` use the same disposable PostgreSQL and SQL Server fixture as the migration tests, enabled with `MOONGLADE_TEST_CATEGORY_DATABASES=1`. They cover empty/new schemas, repeated startup, transaction rollback at seed/default writes, cancellation, provider retries, lost commit acknowledgment, state-query failures, and preservation of posts/comments when settings or accounts are missing. These fault scenarios must never run against a real blog database. Normal runs explicitly skip this provider group.

Manual upgrade tests for category IDs and PostgreSQL naming use opt-in disposable database containers. See [Migration Verification](../../docs/agent-reference.md#migration-verification) for prerequisites and limits; ordinary runs explicitly skip those relational groups.

## Run Tests

Run commands from the repository root. For a focused change, select the owning project:

```powershell
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
```

To verify all test projects, including test discovery after moving files:

```powershell
dotnet test --solution src/Moonglade.slnx
```

The root `global.json` selects Microsoft.Testing.Platform. Moving tests within a project needs no compile or solution entries; update saved fully qualified test filters when namespaces change.
