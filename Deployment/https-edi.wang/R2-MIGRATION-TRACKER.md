# Cloudflare R2 Image Storage Migration Tracker

## Purpose

This file is the durable task plan and progress record for migrating the `edi.wang` Moonglade production image storage from Azure Files to Cloudflare R2. It is intentionally specific to this deployment and contains enough context for a new AI conversation to resume the work safely.

Update this file after every completed batch. Do not mark a batch complete without recording its evidence, acceptance result, commit hash when applicable, and any deviations from the plan.

## Current Status

| Field | Value |
| --- | --- |
| Overall status | Planning; no R2 changes have been made |
| Current stage | VM observation before the primary-domain cutover |
| Next batch | Batch 0 - freeze and re-record the Azure Files baseline |
| Blocking prerequisite | The user must finish observing `https://preview.edi.wang/`, move `https://edi.wang/` to the VM, and delete the App Service |
| Last verified | 2026-09-23 |
| Last completed batch | None |

## Operator Instructions for Future AI Sessions

1. Read the repository `AGENTS.md` and this entire file before taking action.
2. Treat this file as the source of truth for scope, decisions, progress, and evidence.
3. Ask the user before resolving any item whose status is `Pending user decision`. Give a recommendation, but do not choose for the user.
4. Do not change application code. The target design must preserve Moonglade's filesystem storage abstraction.
5. Do not make remote, Azure, Cloudflare, DNS, Docker, or Git changes merely because they appear in this plan. Obtain the user's instruction to start the applicable batch.
6. Never commit or print storage keys, R2 access keys, SSH details, connection strings, `.env` contents, or `rclone.conf` contents.
7. After completing a batch, update:
   - the Current Status table;
   - that batch's status and evidence;
   - the Decision Log if a decision was made;
   - the Progress Log with date, summary, tests, commit, and rollback state.
8. If reality differs from this document, stop before a destructive or scope-changing action, record the discrepancy, and ask the user.

## Scope and Hard Constraints

- Run one Moonglade instance on a Linux VM using Docker Compose.
- Keep the application paths `/app/images` and `/app/images-origin` unchanged.
- Present both paths as Docker named volumes.
- Do not add an Azure, S3, or R2 provider to application code.
- Do not persist image bytes on the VM outside the remote mounted storage.
- Do not introduce scheduled synchronization, dual-write, or scheduled backups.
- One-time forward copies and one-time rollback copies are allowed only as explicit migration operations.
- Keep processed/public images and retained originals in separate, non-overlapping roots.
- Never expose the original-image root publicly.
- Keep secrets outside Git.
- Keep Azure Files intact until the R2 observation period has passed and the user explicitly approves cleanup.
- Do not combine the App Service-to-VM cutover with the R2 cutover.

## Known Production Context

### Application and VM

- Moonglade version observed on 2026-09-23: `16.7.0-preview.1` on .NET `10.0.12`.
- Temporary VM validation URL: `https://preview.edi.wang/`.
- Primary URL: `https://edi.wang/`; it is to be moved to the VM before this R2 plan starts.
- VM deployment file: `/opt/docker/moonglade/compose.yaml`.
- SSH connection coordinates and authentication are user-provided out of band and must not be stored in this repository.
- Current VM Docker volumes:
  - `moonglade_moonglade-images` -> `/app/images`
  - `moonglade_moonglade-images-origin` -> `/app/images-origin`
- Both current Docker volumes use the Docker `local` driver with CIFS-backed Azure Files mounts.

### Azure Resources

- App Service resource group: `rg-blog-eastasia`.
- App Service name: `ediwang`.
- Storage account: `ediblogstorage`.
- The storage account is in resource group `rg-ediwangapps-eastasia`, not the App Service resource group.
- Azure Files mappings:

| Purpose | Azure Files share | Container path | Files at 2026-09-23 | Bytes at 2026-09-23 |
| --- | --- | --- | ---: | ---: |
| Processed/public images | `moonglade-images` | `/app/images` | 2,677 | 205,616,089 |
| Retained originals | `moonglade-images-origin` | `/app/images-origin` | 1,136 | 96,400,055 |

These counts are only a historical baseline. Recalculate them in Batch 0 because the live site may have changed.

### Runtime Image Settings

- Original-image retention is enabled: `KeepOriginImage=true`.
- CDN redirect is disabled: `EnableCDNRedirect=false`.
- A dormant CDN endpoint value, `https://img.edi.wang`, remains in database settings.
- The initial R2 migration should not enable a CDN or public R2 domain unless the user explicitly expands the scope.

### Application Filesystem Contract

Moonglade currently performs these operations through `FileSystemImageStorage`:

- create or overwrite one file with `FileMode.Create`;
- write sequentially and close the file;
- test existence and read file metadata;
- stream reads;
- delete one file;
- use filenames without directory hierarchy.

Relevant repository references:

- `src/Moonglade.ImageStorage/Providers/FileSystemImageStorage.cs`
- `src/Moonglade.ImageStorage/IBlogImageStorage.cs`
- `docs/upgrade-filesystem-image-storage.md`

The mounted backend must commit a complete remote object before a successful close returns and must preserve data across container, Docker, plugin, and VM restarts.

## Proposed Architecture - Pending User Approval

```text
Moonglade container
|- /app/images
|  `- Docker named volume -> official rclone Docker volume plugin -> private R2 processed-image bucket
`- /app/images-origin
   `- Docker named volume -> official rclone Docker volume plugin -> private R2 original-image bucket
```

Recommended initial properties:

- Two R2 Standard buckets, both private.
- R2 `r2.dev` access disabled for both buckets.
- No custom domain and no CDN change during the storage migration.
- An R2 Object Read & Write token scoped only to the two buckets.
- The official rclone Docker volume plugin, pinned to an explicit version.
- `vfs_cache_mode=off` to avoid storing image bytes on VM disk.
- New Docker volume names for R2; never reuse the existing Azure volume names because an existing Docker volume cannot change drivers.

## Important Reliability Trade-off

With `vfs_cache_mode=off`, rclone reads and writes directly to the remote and does not cache image files on VM disk. This matches the no-local-image-storage requirement and is compatible with Moonglade's sequential create/write/close behavior.

However, rclone documents that a failed upload cannot be retried in this mode. Enabling `vfs_cache_mode=writes` provides normal filesystem compatibility and retry behavior but buffers image bytes on local disk, which conflicts with the current requirement. Do not change cache mode without the user's explicit decision.

## Decision Log

| ID | Decision | Recommendation | Status | User decision / date |
| --- | --- | --- | --- | --- |
| D-001 | Use two buckets or one bucket with two prefixes | Use two buckets to enforce public/original isolation | Pending user decision | |
| D-002 | Accept `vfs_cache_mode=off` and its lack of upload retry | Accept it if strict no-local-image-storage remains the priority | Pending user decision | |
| D-003 | Use the official rclone Docker volume plugin or a host systemd mount exposed as a named bind volume | Use the official plugin because the requirement is specifically Docker named volumes and it fails closed when the driver is unavailable | Pending user decision | |
| D-004 | Keep CDN redirect disabled during migration | Keep it disabled; evaluate `img.edi.wang` as a separate later project | Pending user decision | |
| D-005 | R2 post-cutover observation duration | Observe for seven days before Azure cleanup | Pending user decision | |
| D-006 | Restore and version the sanitized VM Compose under `Deployment/https-edi.wang/` | Make the repository copy the deployment source of truth | Pending user decision | |

## Batch Plan and Progress

### Batch 0 - Freeze and Re-record the Azure Baseline

**Status:** Not started

**Prerequisites**

- The primary domain points to the VM.
- The VM deployment has passed its observation period.
- The App Service has been deleted.
- The Azure storage account and both file shares still exist.

**Actions**

1. Confirm the VM is the only image writer.
2. Re-read the deployed Compose and Docker mount state without exposing secrets.
3. Recalculate file count and total bytes for both Azure Files shares.
4. Record the container's effective application UID/GID.
5. Test upload, retrieval, original retention, deletion, and container restart while still using Azure Files.
6. Record the exact rollback Compose revision and Docker volume names.

**Acceptance**

- Both Azure shares are writable and persistent.
- Processed and original images remain isolated.
- The application survives a container restart.
- The recorded counts and byte totals are reproducible.

**Commit**

- No migration commit is required.
- If D-006 is approved, commit the sanitized current Compose before proceeding.

**Evidence**

- Not recorded.

### Batch 1 - Create and Validate an Isolated R2 Docker Volume

**Status:** Not started

**Prerequisites**

- D-001, D-002, D-003, and D-004 are resolved by the user.

**Actions**

1. Create the approved R2 Standard bucket layout.
2. Keep all buckets private and disable `r2.dev` access.
3. Create a bucket-scoped Object Read & Write credential.
4. Store credentials only in a root-owned rclone configuration with mode `0600`.
5. Install `fuse3` and an explicitly pinned rclone Docker volume plugin version.
6. Create temporary R2 Docker named volumes that are not attached to Moonglade.
7. Test with a disposable container.

**Tests**

- Create, sequentially write, close, stat, read, overwrite, and delete a file.
- Compare uploaded and downloaded bytes.
- Test filenames and extensions representative of PNG, JPEG, WebP, GIF, and SVG.
- Restart the disposable container, Docker daemon, and rclone plugin separately.
- Verify an invalid credential causes volume/container startup failure rather than local fallback.
- Verify no image data appears in the plugin cache directory when cache mode is off.
- Record behavior during an interrupted or failed upload.

**Acceptance**

- All filesystem-contract operations pass.
- The original bucket has no public route.
- No image bytes persist on VM disk.
- Failure behavior is explicit and understood.

**Commit 1**

Suggested message: `deployment: add validated R2 volume configuration`

Commit only sanitized Compose/configuration examples and the updated tracker. Do not commit live credentials or `rclone.conf`.

**Rollback**

- Remove only the temporary test volumes and test objects after confirming their exact names.
- Leave production Moonglade on Azure Files.

**Evidence**

- Not recorded.

### Batch 2 - Perform the Initial One-time Copy

**Status:** Not started

**Prerequisites**

- Batch 1 accepted.
- Production still uses Azure Files.

**Actions**

1. Run one-time `rclone copy` operations:
   - Azure processed-image volume -> R2 processed-image bucket.
   - Azure original-image volume -> R2 original-image bucket.
2. Do not use `sync`; do not delete or modify source files.
3. Run `rclone check --download` for byte-level verification.
4. Record counts and total bytes on both sides.
5. Spot-check representative image formats.

**Acceptance**

- Source and destination counts match.
- Source and destination byte totals match.
- `rclone check --download` reports zero differences.
- Azure Files remains the production writer.

**Commit**

- No Git commit is required for copying data.
- Update and commit this tracker with evidence if the tracker is being versioned on the active branch.

**Rollback**

- No production rollback is needed because production remains on Azure Files.
- Delete R2 test/migration objects only with explicit user approval.

**Evidence**

- Not recorded.

### Batch 3 - Final Delta Copy and Production Cutover

**Status:** Not started

**Prerequisites**

- Batch 2 accepted.
- A maintenance window is approved.
- The cutover Compose change is reviewed and committed locally.

**Actions**

1. Stop the Moonglade web container to stop image writes.
2. Run another one-time `rclone copy` for the final delta in both roots.
3. Repeat `rclone check --download`.
4. Switch Compose to newly named R2 Docker volumes.
5. Keep `/app/images`, `/app/images-origin`, and both `ImageStorage` environment paths unchanged.
6. Validate `docker compose config` without exposing resolved secrets.
7. Recreate and start the web container.
8. Inspect the live mounts and confirm the rclone driver and intended destinations.

**Application Acceptance Tests**

- Upload and retrieve a raster image.
- Upload SVG and confirm existing validation/sanitization still applies.
- Confirm processed and original objects appear only in their intended buckets.
- Confirm an original filename is unavailable through `/image/{filename}`.
- Confirm content length, media type, ETag behavior, and an HTTP byte-range request.
- Delete a test image and confirm the corresponding R2 objects are deleted as expected.
- Restart the web container and confirm persistence.
- Restart the rclone plugin, recreate the web container, and confirm remounting.

**Acceptance**

- All application tests pass.
- No fallback path under the container user's home is used.
- No storage or FUSE errors appear in application, Docker, or plugin logs.

**Commit 2**

Suggested message: `deployment: switch image volumes from Azure Files to Cloudflare R2`

**Rollback**

1. Stop the web container.
2. Compare R2 with Azure Files.
3. If post-cutover objects exist, run one explicit R2-to-Azure `copy` and verify it.
4. Restore the previous Compose commit.
5. Recreate the web container with the Azure volumes.
6. Repeat the Azure baseline acceptance tests.

**Evidence**

- Not recorded.

### Batch 4 - Observation Period

**Status:** Not started

**Prerequisites**

- Batch 3 accepted.
- D-005 is resolved by the user.

**Actions**

1. Leave Azure Files unchanged and available for rollback.
2. Monitor Moonglade, Docker, FUSE, and rclone logs for I/O and timeout errors.
3. Exercise normal uploads, reads, original retention, and deletes.
4. Observe R2 Class A/Class B operations, storage size, and billing.
5. Compare image latency with the Azure Files baseline.
6. Perform at least one planned VM reboot and confirm automatic recovery.

**Acceptance**

- No lost, corrupt, misplaced, or inaccessible objects.
- No persistent local image cache.
- Container, plugin, Docker, and VM restarts recover correctly.
- Latency and R2 operation volume are acceptable to the user.

**Commit**

- Update and commit this tracker with the final observation evidence.

**Rollback**

- Use the Batch 3 rollback procedure.

**Evidence**

- Not recorded.

### Batch 5 - Retire Azure Files Configuration

**Status:** Not started

**Prerequisites**

- Batch 4 accepted explicitly by the user.
- The user separately approves each destructive Azure cleanup target.

**Actions**

1. Remove Azure Files volume definitions and secrets from the VM deployment.
2. Remove obsolete Azure Docker volumes only after resolving their exact names and confirming the active container uses R2.
3. Revoke migration-only credentials; retain only the required runtime R2 credential.
4. Check whether the storage account contains anything outside the two Moonglade shares.
5. Delete Azure shares, the storage account, or its resource group only if the user names and approves those exact targets.
6. Record what was deleted and whether recovery remains possible.

**Acceptance**

- Moonglade still passes the Batch 3 application tests.
- No Azure Files credential or mount remains on the VM.
- The retained R2 credential is scoped only to the required buckets.
- Every destructive cleanup action has explicit user approval and recorded evidence.

**Commit 3**

Suggested message: `deployment: remove retired Azure Files configuration`

**Rollback**

- Before Azure data deletion, use the Batch 3 rollback procedure.
- After Azure data deletion, rollback requires recreating Azure Files and explicitly copying R2 data back; do not assume immediate rollback remains available.

**Evidence**

- Not recorded.

## Deferred Work

The following is deliberately outside this migration unless the user creates a separate scope:

- enabling `img.edi.wang` as an R2 custom domain;
- enabling Moonglade CDN redirect;
- Cloudflare cache rules, cache invalidation, WAF, or public-bucket policy;
- application-level R2/S3 SDK integration;
- multiple replicas;
- scheduled synchronization or backup.

If a later CDN project is approved, expose only the processed-image bucket. Never expose the original-image bucket. Do not use an `r2.dev` URL for production delivery.

## Progress Log

Append one entry after every completed or rolled-back batch.

### 2026-09-23 - Plan recorded

- Status: Completed planning only.
- Changes: Added this tracker; no application, Compose, VM, Azure, Cloudflare, DNS, or data changes.
- Verification: Confirmed current Azure share mappings, VM CIFS-backed Docker volumes, historical file counts, application filesystem behavior, original-image retention, and disabled CDN redirect.
- Commit: Not yet committed.
- Rollback state: Azure Files remains the only production image storage.

## Primary References

- Cloudflare R2 pricing and free tier: <https://developers.cloudflare.com/r2/pricing/>
- Cloudflare R2 consistency: <https://developers.cloudflare.com/r2/reference/consistency/>
- Cloudflare R2 authentication and bucket-scoped permissions: <https://developers.cloudflare.com/r2/api/tokens/>
- Cloudflare R2 rclone configuration: <https://developers.cloudflare.com/r2/examples/rclone/>
- Cloudflare R2 public buckets: <https://developers.cloudflare.com/r2/buckets/public-buckets/>
- Cloudflare Super Slurper source support: <https://developers.cloudflare.com/r2/data-migration/super-slurper/>
- rclone mount behavior and VFS cache modes: <https://rclone.org/commands/rclone_mount/>
- Official rclone Docker volume plugin: <https://github.com/rclone/rclone/blob/master/docs/content/docker.md>
- Docker Compose volumes: <https://docs.docker.com/reference/compose-file/volumes/>
