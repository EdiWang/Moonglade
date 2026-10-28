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

Namespaces match folders. Test helpers used by several Web suites are imported explicitly from `Moonglade.Web.Tests.TestSupport`; helpers used by only one suite remain local to that suite. Production project boundaries and test assembly names are unchanged.

## Test Value

The cleanup retains tests for business outcomes, public input validation, security boundaries, error handling, cancellation, time handling, and data consistency. Service registration and HTTP pipeline tests remain useful even when their assertions are small: they protect application wiring that isolated handler tests cannot exercise.

Redundant setup is reduced with theories when the same operation and assertions apply to several inputs. Separate tests remain appropriate for different side effects or failure paths. Assertions target observable results, such as persisted values, generated URLs, serialized documents, HTTP responses, and queued notifications.

Examples of consolidation:

- Configuration creation checks the saved key, JSON, and timestamp range in one test.
- Theme listing checks system ordering and custom properties together. Theme factory tests own the catalog values; color calculation tests have their own class.
- RSS, Atom, and OPML tests check metadata in the generated documents instead of testing constructor assignments separately.
- Tag normalization checks exact slugs, including fixed Unicode byte sequences. Keyword cases share one transformation theory.
- Content rendering, comment link security, CDN replacement, and character classification have separate test classes.

Tests that only repeat a happy path, pass an uncancelled token without testing cancellation, inspect a trivial getter, or check a startup log add little protection when stronger behavioral coverage already exists. Failing tests are investigated and reported; failure alone is never a reason to delete or weaken a test.

See [AGENTS.md](../../AGENTS.md#development-and-verification) for the repository's verification requirements.

## Database Upgrade Coverage

Database upgrades require administrators to run SQL scripts manually. The automatic migration runner, its decision/version-check tests, and the previous embedded SQL scripts have been removed.

Startup tests use EF Core InMemory to verify that existing databases with missing or older manifests still initialize settings while preserving stored post content and existing manifest JSON and timestamps. These tests do not validate relational schema compatibility or manual SQL upgrades against PostgreSQL or SQL Server.

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

Use Microsoft.Testing.Platform options; the runner is selected in the root `global.json`. Moving tests within a project does not require adding compile entries or changing the solution. Namespace changes also change fully qualified test names, so update any locally saved filters that refer to a moved class.
