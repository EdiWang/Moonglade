# S3-Compatible Image Storage

## Original Goal

Extend Moonglade image storage beyond Azure Blob Storage by adding an S3-compatible provider that can support AWS S3 and compatible object storage services.

## Background

Moonglade currently stores blog images through the `IBlogImageStorage` abstraction in `src/Moonglade.ImageStorage`. Existing providers include Azure Blob Storage and local file system storage. Web upload and rendering flows treat provider return values as image file names/object keys, while public URL behavior remains controlled by `/image/{filename}` and optional CDN settings in runtime blog configuration.

The initial implementation is split into batches so configuration, provider implementation, tests, and documentation can be verified and committed independently.

## Scope

- Add S3-compatible image storage configuration.
- Add provider registration for `ImageStorage:Provider=s3compatible`.
- Implement `S3CompatibleImageStorage`.
- Add focused tests for the provider and registration behavior.
- Update app configuration examples and long-lived documentation.

## Out of Scope

- Migrating existing images between storage providers.
- Changing existing `azurestorage` or `filesystem` behavior.
- Changing post content URL shape or CDN redirect behavior.
- Adding provider-specific admin UI for storage credentials.
- Running real cloud integration tests against production buckets.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add S3-compatible configuration and DI scaffold | None | `dotnet build src/Moonglade.ImageStorage/Moonglade.ImageStorage.csproj`; `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj` | Done |
| 2 | Implement `S3CompatibleImageStorage` storage operations | Task 1 | `dotnet build src/Moonglade.ImageStorage/Moonglade.ImageStorage.csproj` | Done |
| 3 | Add provider and registration tests | Task 2 | `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj`; `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Done |
| 4 | Update configuration examples and documentation | Task 3 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj`; `git status --short` | Done |

## Execution Order

Batch 1 introduces only the configuration shape and service registration boundary. Batch 2 replaces the scaffold with real S3-compatible object operations. Batch 3 locks behavior with tests. Batch 4 documents how operators configure the provider and environment variable overrides.

## Current Progress

Batch 4 is implemented on branch `codex/s3-compatible-image-storage`. Default configuration examples and long-lived documentation now describe `Provider=s3compatible`, `S3CompatibleStorageSettings`, environment variable overrides for credentials, and the existing CDN/public URL boundary.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-04 | `git status --short --branch` | Passed | Started from clean `master` before creating feature branch. |
| 2026-08-04 | `dotnet build src/Moonglade.ImageStorage/Moonglade.ImageStorage.csproj` | Passed | 0 warnings, 0 errors. |
| 2026-08-04 | `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj` | Passed | 93 passed, 0 failed, 0 skipped. |
| 2026-08-04 | `dotnet build src/Moonglade.ImageStorage/Moonglade.ImageStorage.csproj` | Passed | Batch 2 implementation build; 0 warnings, 0 errors. |
| 2026-08-04 | `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj` | Passed | Existing ImageStorage tests; 93 passed, 0 failed, 0 skipped. |
| 2026-08-04 | `dotnet test src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj` | Passed | Batch 3 provider and DI tests; 121 passed, 0 failed, 0 skipped. |
| 2026-08-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | 0 warnings, 0 errors. |
| 2026-08-04 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj` | Passed | Batch 4 configuration/documentation verification; 0 warnings, 0 errors. |
| 2026-08-04 | `git status --short` | Passed | Only Batch 4 documentation/configuration files were modified before commit. |

## Issues and Resolutions

- Initial Batch 3 tests assumed `ServiceURL` would not include a trailing slash after AWS SDK client construction. The SDK normalizes it to `https://storage.example.com/`; the assertion now matches the runtime value.
- Initial upload test attempted to read the captured request stream after the provider returned. The provider disposes its upload stream at method exit, so the test now captures bytes inside the Moq callback while the stream is open.
- Batch 4 intentionally did not update `appsettings.Development.json` because that file can contain local developer secrets.

## Follow-ups

- Revisit whether shared image key validation should be factored out once S3 behavior is implemented and tested.
- Run a manual smoke test against at least one S3-compatible service before announcing provider-specific support details in release notes.

## Notes

- Keep `InsertAsync` returning an object key/file name rather than a public URL.
- Keep CDN behavior in existing `ImageSettings` flows.
- Do not quote or copy local development secrets into this task record.
