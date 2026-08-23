# Filesystem-Only Image Storage

## Original Goal

Refactor Moonglade so the application supports only filesystem-backed image storage. Cloud object storage, network storage, Docker volume drivers, FUSE/CSI mounts, credentials, and vendor-specific operational behavior are the responsibility of operators and the community; Moonglade will retain direct CDN URL output without maintaining Azure, S3, or optional provider packages.

## Background

Moonglade currently selects `filesystem`, `azurestorage`, or `s3compatible` through `ImageStorage:Provider`. The `Moonglade.ImageStorage` project contains the provider-neutral `IBlogImageStorage` contract plus filesystem, Azure Blob Storage, and S3-compatible implementations. The project directly references `Azure.Storage.Blobs` and `AWSSDK.S3`, startup initialization creates Azure containers, the default configuration contains vendor credentials and bucket/container settings, and the Azure deployment assets provision Blob Storage for images.

The image workload is compatible with a mounted filesystem because it uses whole-file creation, metadata lookup, sequential reads, and deletion. Generated post image names are normally unique. CDN delivery is already independent of the storage SDK: persisted content uses `/image/{filename}`, rendered post/feed content replaces that prefix with the configured CDN endpoint, and the legacy image endpoint permanently redirects to the CDN when CDN delivery is enabled.

The current filesystem provider maps `InsertSecondaryAsync` back to the primary directory, while the Azure and S3 providers can store original images in separate secondary containers or buckets. A filesystem-only design must restore that isolation with two distinct paths so the CDN-facing primary location cannot expose original images.

This is an intentional breaking configuration and deployment change. The approved boundary is that Moonglade documents a generic mounted-filesystem contract but does not ship or maintain vendor-specific mount commands, drivers, sidecars, CSI manifests, provider extensions, or optional storage packages.

## Approved Architecture

- Keep `IBlogImageStorage` as an internal application boundary for testability and separation from the Web layer, but expose only one filesystem implementation.
- Remove provider selection and provider identity concepts, including `ImageStorage:Provider` and the unused storage `Name` contract if repository-wide inspection confirms no runtime consumer.
- Rename secondary-storage terminology to original-image terminology, including `InsertSecondaryAsync` to `InsertOriginalAsync`.
- Use two distinct filesystem roots:
  - `ImageStorage:FileSystemPath` for primary, CDN-facing images.
  - `ImageStorage:OriginalFileSystemPath` for private original images.
- Keep `ImageStorage:CacheMinutes` because it controls metadata caching for the application-served `/image/{filename}` fallback.
- Preserve the current default primary path when `FileSystemPath` is empty, and use a separate sibling `images-origin` default for original images.
- Resolve both paths to fully qualified paths and reject configurations where the roots are equal or nested within each other. Preserve filename/path traversal protections.
- Keep original images outside the primary read path. `/image/{filename}` and the CDN endpoint must never resolve files from `OriginalFileSystemPath`.
- Preserve the existing watermark/original retention business rules unless a focused test exposes an existing defect; changing when originals are retained is outside this refactor.
- Preserve `/image/{filename}`, upload response, rendered post content, feed content, avatar CDN behavior, permanent CDN fallback redirects, cache headers, and public endpoint contracts.
- Do not add storage mount checks to `/health`; `/health` remains liveness-only and `/health/ready` retains its database-readiness contract.
- Treat mount provisioning, durability, remote identity, credentials, permissions, availability, caching semantics, and object metadata as operator responsibilities. Moonglade validates its configured paths but does not attempt to identify the underlying vendor or prove that a path is a remote mount.

### Generic Runtime Contract

The operator-provided filesystem must support the subset Moonglade uses: create or truncate a complete file, sequential write, durable close, stat for length and modification time, sequential/random read as required by ASP.NET Core range processing, and delete. The two configured paths must be writable by the application user, persistent across container replacement, and shared consistently when multiple application replicas are used.

When the primary path is backed by object storage and served through a CDN, the operator must also ensure that:

- A filename written at the primary path becomes the same key at the CDN origin root.
- The mount adapter assigns correct image `Content-Type` metadata for PNG, JPEG, GIF, WebP, SVG, and other supported formats.
- CDN origin access, TLS, cache behavior, query-string behavior, and invalidation are configured outside Moonglade.
- The primary origin may be accessed by the CDN, while the original-image path remains private and is not attached to that CDN origin.
- A successful file close means the object is committed before the upload request is reported as successful.

## Scope

- Add distinct primary and original filesystem path configuration and storage behavior.
- Rename secondary-container concepts to original-image concepts in production code, tests, UI help text, and localization resources.
- Remove Azure Blob Storage and S3-compatible settings, implementations, registrations, initialization, tests, and NuGet dependencies.
- Simplify image storage DI registration to the filesystem implementation only.
- Remove vendor image-storage data from startup diagnostics and default application configuration.
- Update Dockerfile and Docker Compose assets to persist primary and original images in separate volumes.
- Update Azure deployment assets so they no longer provision or configure Blob Storage for Moonglade images; keep the deployment functional with platform-provided filesystem persistence.
- Preserve and verify CDN URL generation, redirects, feeds, avatars, upload behavior, image streaming, cache headers, and range requests.
- Add an upgrade guide for existing filesystem, Azure Blob, and S3-compatible installations without providing vendor-specific mount recipes.
- Synchronize `README.md`, `AGENTS.md`, configuration examples, deployment documentation, and localized UI resources.

## Out of Scope

- Maintaining Azure, AWS, S3-compatible, GCP, R2, or other storage SDK integrations.
- Creating optional provider projects, NuGet packages, plugin interfaces, or a provider marketplace.
- Shipping or supporting BlobFuse, Mountpoint for S3, s3fs, rclone, tigrisfs, CSI, Docker volume plugin, NFS, or SMB configurations.
- Detecting which vendor or mount technology backs a filesystem path.
- Automatically creating cloud buckets, containers, CDN distributions, access policies, identities, or credentials.
- Proxying CDN image traffic through Moonglade.
- Changing post image URL shape, post content, feed contracts, watermark behavior, image validation, maximum upload size, or supported image formats.
- Moving avatars, site icons, or database-backed assets to a new persistence model except where existing CDN avatar behavior uses `IBlogImageStorage`.
- Adding image storage to the public health-check contract.
- Migrating customer data automatically. Operators must copy or mount their existing primary and original objects before starting the upgraded application.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Record the approved boundary, inventory affected code/configuration/deployment/docs, and define the filesystem/CDN contract | None | Repository inspection, task review | Complete |
| 2 | Add separate primary and original filesystem roots, rename the secondary-write API to original-image terminology, and expand filesystem unit tests | 1 | `Moonglade.ImageStorage.Tests` | Complete |
| 3 | Remove Azure/S3 providers, settings, startup initialization, DI selection, provider diagnostics, tests, and SDK package references | 2 | Restore, package-reference inspection, `Moonglade.ImageStorage.Tests`, Web build | Complete |
| 4 | Update Web call sites and localized admin help text; add focused regressions for upload/original isolation and unchanged CDN behavior | 2-3 | `Moonglade.Web.Tests`, `Moonglade.Utils.Tests`, syndication tests | Not started |
| 5 | Update Docker and Azure deployment assets for two filesystem paths and remove image-specific Azure Storage provisioning | 3 | `docker compose config`, Docker smoke test when available, Bicep/PowerShell validation | Not started |
| 6 | Add the breaking-change upgrade guide and synchronize `README.md`, `AGENTS.md`, configuration examples, and deployment guidance | 3-5 | Documentation review and repository-wide stale-reference scan | Not started |
| 7 | Run focused and solution-level verification, inspect the final package graph and diff, and record remaining environmental risks | 2-6 | Focused tests, Web build, solution tests where available, `git status --short` | Not started |

## Execution Order

Work must proceed in independently verifiable batches. Do not start implementation until the user approves this task record.

### Batch 1: Filesystem contract and original-image isolation

1. Introduce `OriginalFileSystemPath` and distinct resolved primary/original paths.
2. Rename secondary-storage APIs and variables to original-image terminology.
3. Ensure primary reads/deletes cannot access the original-image directory.
4. Validate fully qualified paths, distinct resolved roots, safe filenames, and expected default paths.
5. Update and run `Moonglade.ImageStorage.Tests` before committing.

Suggested commit: `refactor: separate primary and original image storage paths`

This batch should leave the existing cloud providers temporarily buildable only if needed to keep the commit coherent. Do not add compatibility shims that would survive the next batch.

### Batch 2: Remove provider infrastructure

1. Delete Azure Blob and S3-compatible implementations and settings.
2. Remove `Azure.Storage.Blobs` and `AWSSDK.S3` package references.
3. Replace provider-switching DI with direct filesystem registration.
4. Remove Azure container preparation from startup initialization.
5. Remove `ImageStorage:Provider`, Azure, and S3 sections from default configuration.
6. Update startup diagnostics to report filesystem storage without logging credentials or unnecessary path details.
7. Replace provider-specific tests with filesystem-only registration and validation tests.
8. Restore, run image-storage tests, and build the Web project before committing.

Suggested commit: `refactor: remove cloud image storage providers`

### Batch 3: Web and CDN regression protection

1. Update `ImageController`, `SaveAssetToCdnHandler`, settings handling, and background original-image writes for renamed APIs.
2. Replace the admin reference to `SecondaryContainerName` with vendor-neutral private original-path guidance and update all four non-English `Program.*.resx` files.
3. Verify upload responses still return `/image/{filename}`.
4. Verify CDN-enabled GET/HEAD requests retain the permanent redirect and do not read storage first.
5. Verify rendered posts and syndication feeds still emit the configured CDN endpoint.
6. Verify primary image streaming, conditional requests, cache headers, range processing, avatar upload, and original-image dispatch remain unchanged.
7. Run the affected Web, Utils, Syndication, and ImageStorage tests before committing.

Suggested commit: `test: preserve image and CDN behavior with filesystem storage`

### Batch 4: Deployment assets

1. Create and permission both `/app/images` and `/app/images-origin` in the container image as required by the non-root `app` user.
2. Configure separate named volumes and environment paths in `compose.yaml`.
3. Remove image Blob Storage parameters/resources/settings from `Deployment/main.bicep` and `Deploy-Azure-App-Service.ps1` when inspection confirms the storage account has no other responsibility.
4. Configure the Azure deployment to use writable persistent filesystem paths supplied by App Service; do not enable CDN or provision a vendor mount.
5. Validate Compose rendering, PowerShell syntax, and Bicep compilation where the corresponding tools are available.

Suggested commit: `build: use filesystem volumes for image persistence`

### Batch 5: Upgrade and long-lived documentation

1. Rewrite the README image-storage section around the generic mounted-filesystem contract and two-path security boundary.
2. Add `docs/upgrade-filesystem-image-storage.md` unless a release-specific upgrade document is selected before implementation.
3. Explain that old provider settings are removed and data must be mounted or copied before upgrade.
4. Document CDN key mapping, MIME metadata, private original storage, persistence, permissions, multi-replica sharing, and commit-on-close requirements without vendor commands.
5. Update `AGENTS.md` technology stack, configuration table, architecture ownership, image rules, deployment notes, and pre-change guidance.
6. Scan the repository for stale provider names, credentials, bucket/container settings, and UI resource keys.

Suggested commit: `docs: document filesystem-only image storage`

### Batch 6: Final verification

Run the focused suites after each batch, then run the Web build and the broadest practical solution test pass. Inspect the final diff for accidental unrelated formatting and confirm that only the task record and approved implementation files changed. Record exact commands, test counts, failures, Docker availability, and any unverified deployment tooling in this task file.

## Acceptance Criteria

- The application has no Azure Blob or S3-compatible image storage implementation, setting, registration, initialization, test, or SDK dependency.
- There is no image storage provider selector and no optional provider/plugin package is created.
- Primary and original images use distinct, non-overlapping filesystem roots and equal or nested resolved roots fail configuration validation.
- `InsertOriginalAsync` writes only to the original-image root; public reads and deletes operate only on the primary root.
- The default Docker deployment persists both roots in separate volumes and runs as the existing non-root application user.
- Existing public image URLs and upload response shapes do not change.
- Enabling CDN still makes rendered posts, feeds, avatars, and legacy image requests use the configured CDN endpoint directly.
- Original images are not reachable through `/image/{filename}` or the documented CDN origin mapping.
- CDN configuration remains vendor-neutral and validates an HTTPS endpoint as it does today.
- Default configuration and startup output contain no cloud storage credentials, service endpoints, buckets, containers, regions, or provider names.
- Azure deployment assets no longer provision Blob Storage solely for Moonglade image storage.
- The upgrade guide clearly states the breaking configuration and manual data/mount prerequisites.
- `README.md` and `AGENTS.md` describe the new responsibility boundary consistently.
- Focused tests and the Web build pass; any unavailable Docker, Azure CLI, or full-solution checks are explicitly recorded.

## Current Progress

Task No. 3 is complete. Azure Blob and S3-compatible implementations, settings, tests, SDK dependencies, provider selection, Azure container initialization, and the Setup-to-ImageStorage project dependency are removed. Image storage registration now always resolves the two filesystem roots, default configuration contains only filesystem settings, and startup diagnostics report file system storage without exposing path details. The remaining `SecondaryContainerName` admin help text and localized resources are intentionally assigned to Task No. 4.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-23 | Inspected `AGENTS.md`, `docs/tasks/task-template.md`, existing task records, image storage implementations, Web call sites, configuration, tests, Docker assets, Azure deployment assets, README, and localization references | Passed | Confirmed the change crosses ImageStorage, Web, Setup, deployment, tests, configuration, and docs |
| 2026-08-23 | Repository-wide `rg` scan for provider/settings/package/deployment references | Passed | Found Azure/S3 references in ImageStorage, Setup, appsettings, startup diagnostics, README, AGENTS, tests, localization, Bicep, and PowerShell deployment |
| 2026-08-23 | CDN flow inspection | Passed | CDN replacement and redirects are filename-based and do not require a vendor SDK |
| 2026-08-23 | `git status --short` before task creation | Clean | No pre-existing working-tree changes were reported |
| 2026-08-23 | `dotnet run --project src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj --no-restore -- -noColor` | Passed | 129/129 tests passed, including separate roots, original isolation, defaults, invalid filenames, and equal/nested path rejection |
| 2026-08-23 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings, 0 errors |
| 2026-08-23 | `dotnet build src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore` | Passed | 0 warnings, 0 errors after the interface rename |
| 2026-08-23 | `dotnet run --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --no-restore -- -class Moonglade.Web.Tests.ImageControllerTests -noColor` | Passed | 18/18 focused controller tests passed |
| 2026-08-23 | `dotnet restore src/Moonglade.Web/Moonglade.Web.csproj` and `dotnet restore src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj` | Passed | Restored the provider-free project graph after removing Azure and AWS SDK references |
| 2026-08-23 | `dotnet list src/Moonglade.ImageStorage/Moonglade.ImageStorage.csproj package --include-transitive` | Passed | No NuGet package references remain in `Moonglade.ImageStorage`; it retains only the ASP.NET Core framework reference |
| 2026-08-23 | `dotnet run --project src/Tests/Moonglade.ImageStorage.Tests/Moonglade.ImageStorage.Tests.csproj --no-restore -- -noColor` | Passed | 70/70 filesystem-only tests passed after provider-specific tests were removed |
| 2026-08-23 | `dotnet build src/Moonglade.Web/Moonglade.Web.csproj --no-restore` | Passed | 0 warnings, 0 errors with the direct filesystem registration and removed Setup dependency |
| 2026-08-23 | Parsed `src/Moonglade.Web/appsettings.json`, scanned non-documentation source for provider code, and ran `git diff --check` | Passed | JSON is valid; cloud provider code/package/startup references are gone. Only the Task No. 4 admin/localization wording remains; diff check reported line-ending notices only |

## Issues and Resolutions

- **Vendor neutrality versus operational portability:** Object stores do not expose one common Docker volume contract. Resolution: Moonglade supports a generic filesystem path only; vendor mount implementation and support remain outside the application and official task scope.
- **Original-image confidentiality:** The current filesystem provider co-locates primary and original files, unlike cloud providers with separate secondary storage. Resolution: require distinct primary and original filesystem roots and expose only the primary root through application reads/CDN mapping.
- **Object metadata through filesystem mounts:** A POSIX write cannot reliably set cloud object metadata across mount adapters. Resolution: correct image `Content-Type` and CDN metadata are explicit operator acceptance requirements; Moonglade will not add vendor APIs to repair metadata.
- **Silent local fallback:** The application cannot generically prove that a path is a remote mount. Resolution: validate path shape, separation, and application access, document that mount identity/durability is an operator responsibility, and avoid claiming that readiness proves a remote mount.
- **Official Azure deployment currently provisions image storage:** The Azure assets are part of this repository even though storage vendors move outside the application boundary. Resolution: keep the deployment path functional using filesystem persistence, but remove image-specific Blob provisioning and do not add BlobFuse or CDN automation.
- **Breaking upgrades from existing cloud providers:** Removing SDK providers cannot migrate remote data automatically. Resolution: publish a generic manual migration checklist that preserves object filenames and separates primary/original data without maintaining vendor commands.
- **The documented `dotnet test <project>` command is incompatible with the currently restored Microsoft.Testing.Platform packages under the installed .NET 10.0.400 SDK:** The legacy VSTest target rejected the run before executing tests, and `dotnet test --project` was parsed as an MSBuild switch because the repository has not opted into the new CLI mode. Resolution for this batch: run the xUnit v3 executable through `dotnet run --project <test-project>`; all focused tests then passed.

## Follow-ups

- Community-maintained vendor mount examples may live outside the Moonglade core repository after this task, but Moonglade will not test or support them as product integrations.
- A future task may add optional operational diagnostics for path writability or storage latency if real production evidence justifies it; it must not identify vendors or change the `/health` liveness contract.
- Select a release-specific upgrade filename before merging if the target Moonglade version is known.

## Notes

- Preserve `IBlogImageStorage` unless implementation work proves that it has no testing or architectural value; retaining the interface does not imply a supported provider extension model.
- `FileSystemPath` remains the primary-path key to minimize disruption for existing filesystem deployments. `OriginalFileSystemPath` is the proposed new key.
- Old `Provider=filesystem` values may be ignored by configuration binding after removal, but all official configuration and upgrade documentation must tell operators to delete the obsolete key and all vendor sections.
- Do not include real connection strings, access keys, SAS tokens, bucket policies, or mount credentials in tests, documentation, logs, or task updates.
- Do not change post routes, image filenames, CDN endpoint semantics, feed HTML, or saved article content as part of this task.
