# Upgrade to Filesystem-Only Image Storage

Moonglade no longer contains Azure Blob Storage or S3-compatible image providers. The application now reads and writes two filesystem roots. Operators are responsible for making durable local, network, or object-backed storage available at those paths before starting the upgraded application.

This is a breaking configuration and deployment change. Existing image URLs and CDN settings do not change, but legacy provider settings do not configure the new filesystem roots.

## Who Must Take Action

Take action before upgrading if any of the following apply:

- `ImageStorage:Provider` is set to `azurestorage` or `s3compatible`.
- Images currently live in an Azure Blob container, S3 bucket, or another object-storage service.
- Primary and original images currently share one filesystem directory.
- Moonglade runs in an ephemeral container filesystem.
- More than one application replica must read and write the same image set.

An existing filesystem installation still needs review because primary and original images now require separate, non-overlapping roots.

## New Configuration

Configure the application with two fully qualified paths:

```json
"ImageStorage": {
  "CacheMinutes": 60,
  "FileSystemPath": "/app/images",
  "OriginalFileSystemPath": "/app/images-origin"
}
```

The equivalent environment variables are:

```text
ImageStorage__FileSystemPath=/app/images
ImageStorage__OriginalFileSystemPath=/app/images-origin
```

`FileSystemPath` contains public, processed images. It is the only root read by `/image/{filename}` and the only root that may be mapped to a CDN origin.

`OriginalFileSystemPath` contains private original uploads when the administrator enables original-image retention. It must not be nested inside the primary root, contain the primary root, or be exposed through the image endpoint, static-file middleware, or CDN origin.

Both paths must be absolute. Moonglade creates a missing directory at startup when the process has permission. If a path is empty, the defaults are `<user-profile>/moonglade/images` and `<user-profile>/moonglade/images-origin`; these defaults are convenient for development but should not be treated as durable container storage.

`CacheMinutes` remains supported and controls metadata caching when Moonglade serves an image through `/image/{filename}`.

Remove these obsolete settings after recording the old locations needed for migration:

- `ImageStorage:Provider`
- `ImageStorage:AzureStorageSettings`
- `ImageStorage:S3CompatibleStorageSettings`
- Associated storage connection strings, access keys, endpoints, regions, container names, and bucket names

Do not start the new version with only legacy provider settings. They no longer select or connect to remote storage; without explicit new paths, Moonglade uses its user-profile defaults instead.

## Storage Contract

The storage presented to Moonglade must provide normal read/write filesystem behavior:

- Create, overwrite, open, stat, stream, and delete regular files by filename.
- Make a completed write visible at the configured path under the same filename.
- Commit data when the writer closes the file. Deferred or asynchronous upload must not expose a permanently partial object.
- Preserve data across application and container restarts.
- Allow the application identity to create directories and create, read, overwrite, and delete files.
- Provide a shared, coherent view to every replica when Moonglade runs with more than one replica.
- Preserve filenames exactly, including case on case-sensitive systems.

Moonglade does not install, configure, or support a cloud mount driver, sidecar, CSI integration, Docker volume plugin, NFS/SMB client, or vendor SDK. Validate the chosen storage adapter independently against this contract.

## Data Migration

Plan a maintenance window or otherwise prevent image uploads during the final copy. A safe cutover sequence is:

1. Record the current primary and original storage locations and back them up.
2. Provision two durable, writable, non-overlapping target roots.
3. Copy public/processed image objects to `FileSystemPath`, preserving each object key as the filename.
4. Copy retained original image objects to `OriginalFileSystemPath`, preserving each filename.
5. If a legacy filesystem installation co-located both kinds of files, separate the retained original files from public files before exposing the primary root to a CDN. Original uploads use generated filenames with the `origin` variant, but verify the result against a backup instead of deleting uncertain files.
6. Set both new paths and remove the obsolete provider settings and secrets.
7. Start one replica and complete the verification checklist below.
8. Start additional replicas only after shared visibility has been verified.

Moonglade does not migrate image data. The official Azure template provisions new Azure Files shares and App Service path mappings, but it does not copy images from legacy Blob containers or any existing filesystem location. Complete that copy before the upgraded application accepts traffic.

Database records and saved post HTML do not require URL rewriting. Upload responses and saved content continue to use `/image/{filename}`.

## CDN Requirements

CDN delivery remains independent of the storage implementation. When CDN redirect is enabled in admin image settings, rendered posts and feeds emit the configured CDN endpoint, and requests to `/image/{filename}` redirect the browser to that endpoint. Image bytes do not pass through Moonglade in this mode.

For a mounted storage backend used as the CDN origin, verify all of the following:

- A file named `example.webp` in the primary root is reachable as the CDN origin key `example.webp` without adding or removing a path prefix.
- The primary root is the only image root exposed to the CDN. The original root remains private.
- The origin or mount adapter assigns the correct HTTP `Content-Type`. A filesystem write cannot portably set vendor-specific object metadata.
- CDN caching, query-string behavior, invalidation, TLS, origin authentication, and access policy are configured outside Moonglade.
- Newly closed files become complete origin objects before the CDN can fetch them.
- Delete behavior is acceptable for the selected CDN cache policy; deleting a primary file does not automatically purge a previously cached CDN response.

## Verification Checklist

Complete these checks before restoring normal traffic:

- Start the application and confirm that it does not fall back to a user-profile directory.
- Upload a raster image and confirm the returned `/image/{filename}` URL loads.
- Upload an SVG test image and confirm the existing validation and sanitization behavior remains intact.
- Enable original-image retention, upload an image, and confirm that the processed and original files land in different roots.
- Confirm that an original filename cannot be retrieved through `/image/{filename}` or the CDN origin.
- Confirm that the application identity can create, read, overwrite, and delete a test file in both roots.
- Restart or replace the application container and confirm both files remain.
- If using multiple replicas, write through one replica and read through another.
- If CDN delivery is enabled, confirm the browser receives the CDN URL directly, the response has the correct media type, and no original file is exposed.
- Exercise a primary image request with a byte range when the application serves images directly.

The `/health` endpoint intentionally remains a liveness check and does not validate storage mounts. `/health/ready` remains database-focused. Storage validation belongs in deployment smoke tests and operational monitoring.

## Rollback

Keep the old image locations and their backup unchanged until the new deployment has been verified. To roll back, stop writes, restore the previous application version and its provider configuration, and reconcile any images uploaded after the cutover. A rollback cannot automatically copy new files back to the former provider.

Do not point both application versions at a partially migrated dataset or expose the private original root while attempting a rollback.

## Official Azure Deployment

The repository's Bicep deployment remains an infrastructure-specific convenience while the application stays vendor-neutral. It provisions one Storage Account, creates separate writable Azure Files shares for primary and original images, mounts them into the Linux App Service container at `/app/images` and `/app/images-origin`, and sets the two filesystem path environment variables.

The template does not provision a CDN and does not migrate legacy Blob data. Live Azure validation must confirm that both App Service Path mappings are present, writable by the container identity, persistent across restart, and isolated so the original-image share is not used as a public or CDN origin.
