# Cloudflare R2 Image Storage Migration Tracker

## Purpose

This file is the durable task plan and progress record for migrating the `edi.wang` Moonglade production image storage from Azure Files to Cloudflare R2. It is intentionally specific to this deployment and contains enough context for a new AI conversation to resume the work safely.

Update this file after every completed batch. Do not mark a batch complete without recording its evidence, acceptance result, commit hash when applicable, and any deviations from the plan.

## Current Status

| Field | Value |
| --- | --- |
| Overall status | Batch 2 complete; initial image dataset copied and verified in R2; Azure Files remains the production writer |
| Current stage | Batch 2 complete; awaiting Batch 3 maintenance window and cutover authorization |
| Next batch | Batch 3 - final delta copy and production cutover |
| Blocking prerequisite | Batch 3 requires an approved maintenance window, reviewed private Compose change, and explicit instruction to start |
| Decision status | D-001 through D-006 resolved by the user on 2026-09-23 |
| Last verified | 2026-09-24 |
| Last completed batch | Batch 2 |

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
9. Production deployment files live outside this repository at `D:\OneDrive\Projects\Moonglade\prod-compose`. Never copy Compose files, `.env`, `rclone.conf`, credentials, or other production deployment artifacts into this repository, even in sanitized form.

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
- The private local source of truth for production deployment files is `D:\OneDrive\Projects\Moonglade\prod-compose`.
- Production deployment files are intentionally excluded from this GitHub repository to prevent accidental open-source disclosure.
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

## Approved Architecture

```text
Moonglade container
|- /app/images
|  `- Docker named volume -> official rclone Docker volume plugin -> private R2 processed-image bucket
`- /app/images-origin
   `- Docker named volume -> official rclone Docker volume plugin -> private R2 original-image bucket
```

Approved initial properties:

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
| D-001 | Use two buckets or one bucket with two prefixes | Use two buckets to enforce public/original isolation | Approved | Two buckets; 2026-09-23 |
| D-002 | Accept `vfs_cache_mode=off` and its lack of upload retry | Accept it if strict no-local-image-storage remains the priority | Approved | Use `vfs_cache_mode=off`; 2026-09-23 |
| D-003 | Use the official rclone Docker volume plugin or a host systemd mount exposed as a named bind volume | Use the official plugin because the requirement is specifically Docker named volumes and it fails closed when the driver is unavailable | Approved | Use the official rclone Docker volume plugin; 2026-09-23 |
| D-004 | Keep CDN redirect disabled during migration | Keep it disabled; evaluate `img.edi.wang` as a separate later project | Approved | Keep CDN redirect disabled; 2026-09-23 |
| D-005 | R2 post-cutover observation duration | Observe for seven days before Azure cleanup | Approved | Seven-day observation period; 2026-09-23 |
| D-006 | Location and versioning of production deployment files | Keep production deployment files outside the public repository | Approved | Use `D:\OneDrive\Projects\Moonglade\prod-compose`; never add these files to this repository; 2026-09-23 |

## Batch Plan and Progress

### Batch 0 - Freeze and Re-record the Azure Baseline

**Status:** Completed - Azure Files baseline accepted; authenticated app upload/delete flow not exercised

**Prerequisites**

- The primary domain points to the VM.
- The VM deployment has passed its observation period.
- The App Service has been deleted.
- The Azure storage account and both file shares still exist.

**Actions**

1. Confirm the VM is the only image writer.
2. Re-read the private Compose source at `D:\OneDrive\Projects\Moonglade\prod-compose` and the deployed Docker mount state without exposing secrets.
3. Recalculate file count and total bytes for both Azure Files shares.
4. Record the container's effective application UID/GID.
5. Test upload, retrieval, original retention, deletion, and container restart while still using Azure Files.
6. Create and record a rollback checkpoint in the private deployment directory and record the exact Docker volume names. Do not copy the checkpoint into this repository.

**Acceptance**

- Both Azure shares are writable and persistent.
- Processed and original images remain isolated.
- The application survives a container restart.
- The recorded counts and byte totals are reproducible.

**Acceptance result:** Met for mounted-storage behavior and the public image-read path. The authenticated upload and delete controller flows remain unverified because the admin UI requires Microsoft OIDC sign-in and no authenticated session was available.

**Commit**

- Do not commit production deployment files to this repository.
- Commit only this tracker's Batch 0 progress update if a repository checkpoint is wanted.

**Evidence**

- `https://edi.wang/` and `/health` returned HTTP 200; DNS still points to the Azure VM. The user confirmed that the App Service was deleted after the VM migration. The storage account and both Azure Files shares remain present.
- Each named volume is attached only to the running `moonglade-web` container: `moonglade_moonglade-images` -> `/app/images` and `moonglade_moonglade-images-origin` -> `/app/images-origin`. Both use the Docker `local` driver with CIFS-backed mounts.
- The application process runs as UID/GID `1654:1654`.
- Recalculated counts match the historical baseline: processed `2,677` files / `205,616,089` bytes; originals `1,136` files / `96,400,055` bytes.
- As UID/GID `1654:1654`, separate 69-byte PNG probes were written to the two mounts. Their SHA-256 hashes matched; each file was absent from the other mount. `GET /image/{processed filename}` returned `200 image/png` with 69 bytes, while the original probe returned 404.
- Restarted only the `web` service. Both probe hashes remained unchanged, the processed image endpoint again returned `200 image/png` with 69 bytes, and `/health` returned 200. Removed both probes through their respective mounts; the processed URL then returned 404. Final counts and byte totals still match the baseline.
- Created a private rollback checkpoint at `D:\OneDrive\Projects\Moonglade\prod-compose\batch0-rollback-20260923T091437Z` containing `compose.yaml` and `.env`; SHA-256 checks confirmed both copies. No deployment file or secret was added to this repository.
- The admin URL redirected to Microsoft OIDC sign-in. Consequently the authenticated image-upload path (including runtime original-retention behavior) and application delete endpoint were not exercised; the probes validate mounted-volume writes, public reads, isolation, persistence, and deletion only.

### Batch 1 - Create and Validate an Isolated R2 Docker Volume

**Status:** Completed - isolated R2 volumes passed the filesystem and restart tests; production remains on Azure Files

**Prerequisites**

- Batch 0 accepted.

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

**Repository checkpoint 1**

Suggested message: `docs: record validated R2 volume configuration`

Commit only this tracker's progress update. Keep Compose, plugin configuration, `.env`, `rclone.conf`, and all other production deployment artifacts in `D:\OneDrive\Projects\Moonglade\prod-compose` and out of this repository.

**Rollback**

- Remove only the temporary test volumes and test objects after confirming their exact names.
- Leave production Moonglade on Azure Files.

**Evidence**

- Created `moonglade-images` and `moonglade-images-origin` in the Cloudflare account. Both use Standard storage and automatic Asia-Pacific placement; Public Access is disabled, no custom domain is assigned, and Public Development URL is disabled for both buckets. Both buckets are empty.
- Installed host package `fuse3` version `3.18.2-1` and the official managed plugin `rclone/docker-volume-rclone:amd64-1.75.1`; its embedded rclone reports `v1.75.1`. The pulled plugin digest is `sha256:3f4ab4223629af9a20d7df12880eabc01a2559de4a86e53b3c44f314b756b420`.
- Installed the replacement bucket-scoped credential through the no-echo SSH prompt. `/var/lib/docker-plugins/rclone/config/rclone.conf` is `root:root`, mode `0600`; the config directory contains only that file and its sole profile is `[r2]`. The config and cache directories are `root:root`, mode `0700`. No credential values were read into task output or stored in this repository.
- Created temporary volumes `moonglade-r2-batch1-images` -> `r2:moonglade-images` and `moonglade-r2-batch1-origin` -> `r2:moonglade-images-origin`, both with `vfs-cache-mode=off` and `allow-other=true`. A disposable UID/GID `1654:1654` container passed sequential write/close, stat, read-back byte comparison, overwrite, and delete for representative PNG, JPEG, WebP, GIF, and SVG filenames. A lifecycle sentinel remained readable after the disposable container stopped and a new one remounted the volumes. Same-name objects with different contents remained isolated between buckets.
- Restarted the rclone plugin and Docker daemon separately. Docker required `--force` to disable the plugin while named volumes existed; no container was using the temporary volumes at that time. Both volumes remounted and passed read/write checks afterward. The daemon restart also recovered production containers; `moonglade-web` is healthy and its original Azure CIFS volumes remain mounted at `/app/images` and `/app/images-origin`.
- Invalid credentials caused Docker volume mount failure before the disposable container started; there was no local fallback. The cache directory contained only `docker-plugin.state` (2 bytes after cleanup), with no image data. Both R2 buckets were empty after cleanup, and all temporary test volumes were removed.
- An upload of a 256 MiB probe was killed with SIGKILL after one second (container exit 137). The incomplete 48,234,496-byte object was visible in the processed bucket and was removed; both buckets were then confirmed empty. This demonstrates that interruption can leave a partial remote object. With `vfs-cache-mode=off`, rclone does not retry failed uploads, as documented in the rclone mount reference.
- The first Cloudflare Account API token `Moonglade R2 volume plugin` had Object Read & Write limited to the two buckets. Its values were mistakenly pasted into the local PowerShell prompt and echoed as failed commands; the token was immediately deleted and must not be used. No credential file was written to the VM.
- Created a replacement token with the same name, Object Read & Write limited to `moonglade-images` and `moonglade-images-origin`, Forever lifetime, and no IP filter. The replacement is installed on the VM; the first exposed token was deleted and was never used.

**Acceptance result:** Met for isolated-volume filesystem operations, bucket separation/privacy, restart recovery, fail-closed behavior, and absence of image bytes in the plugin cache. The interrupted-upload limitation was observed and removed from R2; it is understood under the user's approved `vfs-cache-mode=off` decision. Production remains on Azure Files.

### Batch 2 - Perform the Initial One-time Copy

**Status:** Completed - initial R2 copy matches Azure Files by object count, byte total, and downloaded content verification

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

- Both R2 buckets were confirmed empty immediately before the copy. Used the official `rclone/rclone:1.75.1` CLI image (`sha256:45401ad7410db1d67ffdb58e19059ad20b0d8e0285a60e38bbec55cc1019c7a5`) in temporary containers on the VM. Each Azure Docker volume and the root-only rclone config were mounted read-only; `rclone copy` wrote only to its corresponding R2 bucket. No `sync` or source mutation was used.
- Processed images: Azure source `2,677` files / `205,616,089` bytes; R2 `moonglade-images` `2,677` objects / `205,616,089` bytes.
- Original images: Azure source `1,136` files / `96,400,055` bytes; R2 `moonglade-images-origin` `1,136` objects / `96,400,055` bytes.
- `rclone check --download` completed for both roots: `0 differences found`, with `2,677` and `1,136` matching files respectively. The command downloaded R2 object bytes for content verification.
- The fully checked processed set includes GIF (4), JPEG (217), PNG (2,450), SVG (2), and WebP (4). The original set includes JPEG (50), PNG (1,085), and WebP (1). The full downloaded check covered all listed objects and formats.
- Final source and R2 counts/bytes still match after the checks. `moonglade-web` remains healthy on the unchanged Azure CIFS volumes; Azure Files is still the only production image writer. No production Compose changes were made.
- Azure remained online as the writer throughout this initial copy, so it is not an atomic snapshot. Batch 3 must stop writes, copy the final delta, and repeat the downloaded check before cutover.

**Acceptance result:** Met. Counts and byte totals match for both roots, both `rclone check --download` runs report zero differences, and production continues to use Azure Files.

### Batch 3 - Final Delta Copy and Production Cutover

**Status:** Not started

**Prerequisites**

- Batch 2 accepted.
- A maintenance window is approved.
- The cutover Compose change in `D:\OneDrive\Projects\Moonglade\prod-compose` is reviewed and a private rollback checkpoint is recorded.

**Actions**

1. Stop the Moonglade web container to stop image writes.
2. Run another one-time `rclone copy` for the final delta in both roots.
3. Repeat `rclone check --download`.
4. Switch the private production Compose to newly named R2 Docker volumes.
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

**Repository checkpoint 2**

Suggested message: `docs: record R2 storage cutover`

Commit only this tracker's cutover evidence. The production Compose change remains outside this repository.

**Rollback**

1. Stop the web container.
2. Compare R2 with Azure Files.
3. If post-cutover objects exist, run one explicit R2-to-Azure `copy` and verify it.
4. Restore the private Compose rollback checkpoint.
5. Recreate the web container with the Azure volumes.
6. Repeat the Azure baseline acceptance tests.

**Evidence**

- Not recorded.

### Batch 4 - Observation Period

**Status:** Not started

**Prerequisites**

- Batch 3 accepted.

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

**Repository checkpoint 3**

Suggested message: `docs: record Azure Files retirement`

Commit only this tracker's cleanup evidence. Keep the production deployment configuration outside this repository.

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

### 2026-09-23 - Architecture decisions recorded

- Status: Planning decisions D-001 through D-006 resolved; no migration batch started.
- Decisions: Use two private R2 buckets, the official rclone Docker volume plugin, `vfs_cache_mode=off`, disabled CDN redirect during migration, and a seven-day observation period.
- Deployment source: Production files moved to `D:\OneDrive\Projects\Moonglade\prod-compose` and must never be committed to this repository.
- Changes: Updated this tracker only; no Compose, VM, Azure, Cloudflare, DNS, or data changes.
- Rollback state: Azure Files remains the only production image storage.

### 2026-09-23 - Batch 0 preflight blocked

- Status: Read-only preflight completed; Batch 0 acceptance not met.
- Verification: `edi.wang` returned HTTP 200 and its DNS address matched an Azure VM NIC. No `ediwang` App Service was found in five enabled Azure subscription contexts. The Azure storage account and both expected shares exist. The private Compose source retains the Azure CIFS volumes at the required container paths.
- Blockers: The preview HEAD probe failed, so its observation completion is unconfirmed. No production VM SSH alias could be identified safely. The current Azure identity cannot list share contents, preventing file counts and byte totals; the Azure Run Command attempt returned no usable Compose/UID/file-count output. Live mounts, effective UID/GID, application storage tests, restart behavior, and rollback checkpoint remain unverified.
- Changes: No image data, container state, Azure resources, Cloudflare settings, DNS, or production deployment files were changed. No commit was created.
- Rollback state: Production remains on the existing Azure Files configuration; no data writes or rollbacks were performed.

### 2026-09-23 - Batch 0 completed

- Status: Accepted; Azure Files remains the production image storage and Batch 1 has not started.
- Resolution: The user confirmed that the App Service had been deleted after the VM migration and authorized key-based SSH access. Live VM state and storage contents were then checked over SSH; no SSH coordinates or credentials are recorded here.
- Verification: Confirmed the web container is the only running container attached to either image volume, both CIFS-backed mounts are writable by UID/GID `1654:1654`, processed/original test files remain isolated, the processed image is readable through `/image/{filename}`, and storage survives a web-container restart. Removed both temporary probes. The final file counts and byte totals match the historical baseline exactly.
- Deviation: The app requires Microsoft OIDC sign-in for upload/delete operations, and no authenticated browser session was available. Direct volume probes do not verify the authenticated upload controller or its runtime original-retention setting, nor the application delete endpoint.
- Checkpoint: Created and hash-verified `D:\OneDrive\Projects\Moonglade\prod-compose\batch0-rollback-20260923T091437Z\compose.yaml` and `.env`; the checkpoint remains outside the repository.
- Commit: Not created.
- Rollback state: Azure Files remains active; only temporary probe files were written and removed. The web container is running after the planned restart.

### 2026-09-24 - Batch 1 completed

- Status: Accepted; R2 has passed isolated disposable-volume checks. Production remains on Azure Files.
- Changes: Installed the replacement bucket-scoped token in the VM's root-only rclone config. Temporary R2 volumes were created for tests and removed afterward; no application or production Compose changes were made.
- Security incident: The first token's one-time values were mistakenly pasted into the local PowerShell terminal and echoed as failed commands. It was deleted and not used. The replacement was entered through an SSH prompt with terminal echo disabled. No token values are recorded in this tracker or repository.
- Verification: Filesystem operations and representative image formats passed; container, plugin, and Docker daemon remount checks passed; invalid credentials failed closed before container start; the plugin cache held no image bytes. A SIGKILL test left a 48,234,496-byte partial object, which was removed. The two buckets are empty after cleanup. The VM config is `root:root`/`0600`, contains only the `[r2]` profile, and the config directory contains no temporary files. The plugin is enabled, no Batch 1 test volumes remain, `moonglade-web` is healthy, and both production image mounts remain on Azure CIFS.
- Deviation: Plugin disable required `--force` while named test volumes existed, despite no running container using them. Interrupted writes may leave partial remote objects and are not retried with the approved cache mode.
- Commit: Tracker-only Batch 1 checkpoint `ae831379fb31a361cc74b55c883537f6fd4468a5` (`docs: record validated R2 volume configuration`).
- Rollback state: Production remains on Azure Files; temporary volumes and test objects were removed; both R2 buckets are empty.

### 2026-09-24 - Batch 2 completed

- Status: Accepted; the initial image dataset is present in the two private R2 buckets and passes full downloaded-content verification. Production remains on Azure Files.
- Changes: Ran one `rclone copy` per Azure source volume to its corresponding R2 bucket from a temporary official rclone 1.75.1 container. Source volumes and the credential config were mounted read-only. No source files were modified or deleted.
- Verification: Processed source and destination both contain 2,677 files/objects totaling 205,616,089 bytes; originals both contain 1,136 files/objects totaling 96,400,055 bytes. `rclone check --download` reported zero differences and all files matching in both buckets. Final counts and bytes were re-read after checking. The production web container is healthy and remains mounted to both Azure CIFS volumes.
- Deviation: The VM did not have a standalone rclone CLI, so the pinned official rclone 1.75.1 container image was used for copy and check. This did not change Compose or production mounts.
- Commit: Tracker-only Batch 2 checkpoint `4ab2e6f4485ce75ff4726472aae1a77cf64c1091` (`docs: record initial R2 image copy`).
- Rollback state: No production rollback is needed; Azure remains the writer. The initial R2 copy is retained as the destination baseline for Batch 3.

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
