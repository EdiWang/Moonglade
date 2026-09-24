# Use Cloudflare R2 for Image Storage

Moonglade reads and writes images through two filesystem paths. It does not contain a Cloudflare R2 or S3 storage provider. To use R2 without changing the application, mount two R2 buckets as Docker named volumes and map them to the standard image paths:

```text
R2 processed-image bucket -> /app/images
R2 original-image bucket  -> /app/images-origin
```

This guide uses the official rclone Docker volume plugin on a Linux Docker host. The plugin and all R2-specific behavior are deployment infrastructure owned by the operator, not part of Moonglade's application compatibility contract. Review [Upgrade to Filesystem-Only Image Storage](upgrade-filesystem-image-storage.md) before migrating an existing installation.

## Requirements

- A Linux host with Docker Engine and Docker Compose.
- Two Cloudflare R2 Standard buckets.
- An R2 API token with Object Read & Write permission scoped only to those two buckets.
- Root access on the Docker host.
- A maintenance window for the final data copy and volume switch.

Use separate buckets for processed and original images. This keeps the private original-image root isolated even if the processed-image bucket is exposed through a CDN later.

The examples below use these placeholders:

| Placeholder | Purpose |
| --- | --- |
| `<account-id>` | Cloudflare account ID |
| `<access-key-id>` | R2 Access Key ID |
| `<secret-access-key>` | R2 Secret Access Key |
| `<processed-bucket>` | Bucket for processed/public images |
| `<original-bucket>` | Bucket for retained original uploads |
| `<architecture>-<version>` | Pinned rclone plugin tag, such as `amd64-1.75.1` |

Do not commit any real value from the R2 credential to Git.

## 1. Create Private R2 Buckets

Create two Standard buckets in the Cloudflare dashboard. Keep both buckets private during the initial deployment:

- Do not enable the public `r2.dev` URL.
- Do not attach a custom domain.
- Never expose the original-image bucket publicly.

Create an account API token with Object Read & Write permission and scope it only to these two buckets. Record its Access Key ID and Secret Access Key when Cloudflare displays them; the secret cannot be viewed again.

Cloudflare documentation:

- [Create R2 buckets](https://developers.cloudflare.com/r2/buckets/create-buckets/)
- [Create bucket-scoped R2 credentials](https://developers.cloudflare.com/r2/get-started/s3/)

## 2. Install the rclone Docker Volume Plugin

Install FUSE and create the directories required by the managed plugin:

```bash
sudo apt-get update
sudo apt-get install -y fuse3
sudo mkdir -p /var/lib/docker-plugins/rclone/config
sudo mkdir -p /var/lib/docker-plugins/rclone/cache
```

Install an exact plugin version for the host architecture. Replace the example tag instead of using a floating tag in production:

```bash
RCLONE_PLUGIN_TAG=<architecture>-<version>

docker plugin install "rclone/docker-volume-rclone:${RCLONE_PLUGIN_TAG}" \
  --alias rclone \
  --grant-all-permissions \
  args="-v"

docker plugin list
```

Available architecture names include `amd64`, `arm64`, and `arm-v7`. See the [official rclone Docker volume plugin documentation](https://rclone.org/docker/) for current tags and installation details.

## 3. Configure the R2 Remote

Create `/var/lib/docker-plugins/rclone/config/rclone.conf` with the following content:

```ini
[r2]
type = s3
provider = Cloudflare
access_key_id = <access-key-id>
secret_access_key = <secret-access-key>
endpoint = https://<account-id>.r2.cloudflarestorage.com
acl = private
no_check_bucket = true
```

`no_check_bucket = true` is required for an object-level token that cannot perform account-level bucket operations.

Protect the configuration:

```bash
sudo chown root:root /var/lib/docker-plugins/rclone/config/rclone.conf
sudo chmod 600 /var/lib/docker-plugins/rclone/config/rclone.conf
```

The access keys belong in `rclone.conf`, not in `compose.yaml`, `.env`, Docker volume options, or shell history. See Cloudflare's [rclone configuration guide](https://developers.cloudflare.com/r2/examples/rclone/).

## 4. Configure Docker Compose

Keep Moonglade's application paths unchanged and attach two rclone volumes:

```yaml
services:
  web:
    image: ediwang/moonglade:latest
    environment:
      ImageStorage__FileSystemPath: /app/images
      ImageStorage__OriginalFileSystemPath: /app/images-origin
    volumes:
      - moonglade-images-r2:/app/images
      - moonglade-images-origin-r2:/app/images-origin

volumes:
  moonglade-images-r2:
    driver: rclone
    driver_opts:
      remote: "r2:<processed-bucket>"
      allow_other: "true"
      vfs_cache_mode: "off"

  moonglade-images-origin-r2:
    driver: rclone
    driver_opts:
      remote: "r2:<original-bucket>"
      allow_other: "true"
      vfs_cache_mode: "off"
```

`allow_other` lets the non-root user in the Moonglade container access the FUSE mount.

Use new volume names when replacing an existing storage backend. Docker does not update the driver or options of an existing named volume. To change volume options, stop the consuming container, remove that named volume, and recreate it.

Validate the Compose model before changing the running service:

```bash
docker compose config --quiet
```

## VFS Cache Mode

The example uses `vfs_cache_mode: "off"`:

- Reads and writes go directly to R2.
- Image bytes are not cached on the VM disk.
- Writes must be sequential and truncate the destination.
- A failed upload cannot be retried from a local cached copy.

Moonglade creates or overwrites complete image files using sequential writes, so this mode satisfies its current filesystem access pattern.

If upload retry behavior and broader filesystem compatibility are more important than avoiding local image storage, use `vfs_cache_mode: "writes"` instead. That mode buffers writes on local disk and requires an explicitly sized, monitored cache directory. Do not select it accidentally: it changes the storage and failure model.

See [rclone mount VFS caching](https://rclone.org/commands/rclone_mount/#vfs-file-caching) for the complete behavior of each mode.

## 5. Validate the Volumes Before Cutover

Create the volumes without starting Moonglade:

```bash
docker compose create web
docker volume list
docker volume inspect <compose-project>_moonglade-images-r2
docker volume inspect <compose-project>_moonglade-images-origin-r2
```

Use a disposable container to test each volume:

```bash
docker run --rm \
  --mount type=volume,src=<compose-project>_moonglade-images-r2,dst=/images \
  alpine:latest \
  sh -c 'printf "r2-volume-test" > /images/r2-volume-test.txt && cat /images/r2-volume-test.txt && rm /images/r2-volume-test.txt'
```

Repeat the test for the original-image volume. Verify that:

- create, read, overwrite, stat, and delete work;
- a closed file appears in the intended R2 bucket;
- the two buckets remain isolated;
- restarting the disposable container preserves objects;
- invalid credentials cause the volume mount to fail instead of falling back to local storage;
- the plugin cache directory contains only plugin state when cache mode is off, not image files.

Remove all test objects before migration.

## 6. Migrate Existing Images

Keep the source storage unchanged until the R2 deployment has passed its observation period. Use `rclone copy`, not `rclone sync`, so a mistake cannot delete destination objects.

The following example copies from existing Docker named volumes. Pin the helper image to the same rclone version used by the plugin:

```bash
RCLONE_VERSION=<version>

docker run --rm \
  --mount type=bind,src=/var/lib/docker-plugins/rclone/config,dst=/config/rclone,readonly \
  --mount type=volume,src=<old-processed-volume>,dst=/source,readonly \
  "rclone/rclone:${RCLONE_VERSION}" \
  copy /source "r2:<processed-bucket>" --progress

docker run --rm \
  --mount type=bind,src=/var/lib/docker-plugins/rclone/config,dst=/config/rclone,readonly \
  --mount type=volume,src=<old-original-volume>,dst=/source,readonly \
  "rclone/rclone:${RCLONE_VERSION}" \
  copy /source "r2:<original-bucket>" --progress
```

Verify every object by downloading and comparing its content:

```bash
docker run --rm \
  --mount type=bind,src=/var/lib/docker-plugins/rclone/config,dst=/config/rclone,readonly \
  --mount type=volume,src=<old-processed-volume>,dst=/source,readonly \
  "rclone/rclone:${RCLONE_VERSION}" \
  check /source "r2:<processed-bucket>" --download

docker run --rm \
  --mount type=bind,src=/var/lib/docker-plugins/rclone/config,dst=/config/rclone,readonly \
  --mount type=volume,src=<old-original-volume>,dst=/source,readonly \
  "rclone/rclone:${RCLONE_VERSION}" \
  check /source "r2:<original-bucket>" --download
```

Also compare file counts and total bytes. ETags are not sufficient for migration verification because multipart uploads can produce different ETags for identical content.

For the final cutover:

1. Stop Moonglade to prevent new image writes.
2. Repeat both `copy` commands for the final delta.
3. Repeat both downloaded-content checks.
4. Switch Compose to the R2 volumes.
5. Start Moonglade and complete the application checks below.

## 7. Application Verification

Before restoring normal traffic:

- Confirm the container mounts the two expected rclone volumes at `/app/images` and `/app/images-origin`.
- Upload a raster image and load its `/image/{filename}` URL.
- Upload an SVG and confirm Moonglade's validation and sanitization still apply.
- Enable original-image retention and confirm processed and original objects go to different buckets.
- Confirm an original-image filename returns `404` through `/image/{filename}`.
- Delete a test image and confirm the object is removed from the intended bucket.
- Test an HTTP byte-range request for a processed image.
- Recreate the application container and confirm images remain available.
- Restart Docker and reboot the host during a planned maintenance window to confirm the plugin and volumes recover.
- Review Docker daemon and application logs for FUSE, I/O, and storage errors.

Moonglade's `/health` endpoint is liveness-only, and `/health/ready` checks database readiness. Neither endpoint validates image storage. Keep an explicit image-storage smoke test in the deployment procedure.

## Rollback

Keep the previous storage and Compose configuration until R2 has passed an observation period.

To roll back:

1. Stop Moonglade.
2. Copy any images created after cutover from R2 back to the previous storage.
3. Verify counts and content.
4. Restore the previous volume configuration.
5. Start Moonglade and repeat the application verification checklist.

Do not run old and new application instances against different writable image datasets at the same time.

## Optional CDN Delivery

R2 storage does not require CDN redirect. When CDN redirect is disabled, Moonglade reads private R2 objects through the mounted filesystem and serves them from `/image/{filename}`.

If direct CDN delivery is enabled later:

- Attach a production custom domain only to the processed-image bucket.
- Keep the original-image bucket private with no custom domain and no `r2.dev` URL.
- Do not use an `r2.dev` URL for production traffic.
- Configure Moonglade's image CDN endpoint to the custom domain.
- Verify content types, cache invalidation, deleted-object behavior, and cached `404` responses.

Cloudflare documents public access and caching in [Public buckets](https://developers.cloudflare.com/r2/buckets/public-buckets/) and [Enable cache for an R2 bucket](https://developers.cloudflare.com/cache/interaction-cloudflare-products/r2/).

## Operations

- Pin the rclone plugin version and plan upgrades explicitly.
- Monitor Docker daemon logs because managed plugin logs are emitted there.
- Monitor R2 storage and Class A/Class B operation counts; FUSE metadata calls also consume operations.
- Treat R2 as primary storage, not as an independent backup.
- Test mount behavior independently after Docker, kernel, FUSE, or plugin upgrades.
- For multiple application replicas, install and configure the plugin on every Docker host and verify cross-replica visibility before serving traffic.

Removing a Docker volume does not delete the objects in its R2 bucket, but always resolve and inspect the exact volume and bucket names before cleanup.
