# 🌙 Moonglade Blog

**Moonglade** is a self-hosted personal blogging platform for developers, built with ASP.NET Core / .NET 10 and Razor Pages.

## What Moonglade Does

A public blog and authenticated admin portal provide:

- **Content publishing:** posts, drafts, scheduled posts, Markdown Mermaid diagrams, pages, featured/outdated flags, archives, recycle bin behavior, and route links for published posts.
- **Reader interaction:** comments, replies, Webmentions, comment moderation, view counts, and optional email notifications.
- **Site management:** runtime blog settings, widgets, themes, custom CSS, menus, image storage, site verification files, account settings, and data import/export.
- **Activity history:** localized administrator history with time, actor, a single activity description, event/date filters, and readable details. Each automatically published article has its own System entry with the captured title, stable ID, and actual publication time. Ordinary setting updates show approved before/after values only when effective values change; unchanged saves produce no activity entry, while updates without displayable values retain a localized explanation. **Clear all** is the only deletion action; public comment submissions are excluded. After clearing, the application attempts to record the administrator, time, and deleted count. A failed activity write is logged without undoing the business operation.
- **Discovery and interoperability:** RSS, Atom, OPML, OpenSearch, FOAF, sitemap, robots.txt, IndexNow, root-level site ownership verification files, reader-friendly markup, and health checks.

The admin portal supports desktop browsers with a viewport at least 1440 CSS pixels wide.

The interface supports English (`en-US`), Simplified Chinese (`zh-Hans`), Traditional Chinese (`zh-Hant`, using Hong Kong terminology), German (`de-DE`), Japanese (`ja-JP`), and French (`fr-FR`, France). Visitors can choose a display language in the footer; the default language setting controls blog and post metadata for SEO.

## Repository Layout

The solution is `src/Moonglade.slnx`. See the [engineering reference](docs/agent-reference.md#layers-and-project-responsibilities) for module responsibilities.

| Path | Purpose |
| --- | --- |
| `src/Moonglade.Web` | ASP.NET Core web host, Razor Pages, API controllers, endpoint mapping, filters, middleware wiring, and static assets. |
| `src/Moonglade.*` | Feature, data, configuration, authentication, storage, integration, and background-service libraries. |
| `src/Tests/Moonglade.*.Tests` | xUnit test projects aligned with the production projects. See the [test layout and maintenance guide](src/Tests/README.md). |
| `Dockerfile`, `compose.yaml` | Docker deployment assets. |
| `database/` | Manual, idempotent database migration SQL scripts and their conventions. |

## 🚀 Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

- **Stable Code:** Always use the [Release](https://github.com/EdiWang/Moonglade/releases) branch. Avoid deploying from `master`.
- **Security:** Enable **HTTPS** and **HTTP/2** on your web server for optimal security and performance.
- **Deployment Options:** Moonglade can run on any cloud provider or on-premises.

### Database Upgrades

Database upgrades are manual. Before starting a newer release against an existing database, back up the database and have an administrator run any required release-specific SQL scripts for PostgreSQL or SQL Server. Validate the upgrade on a restored backup first; restore the backup if rollback is needed.

See [`database/README.md`](database/README.md) for the script catalog, authoring rules, and repeat-execution verification.

Moonglade does not automatically migrate existing databases or check their version compatibility at startup. The former `AutoDatabaseMigration` setting is no longer supported, and the previous cumulative SQL migration scripts have been removed. Fresh installations still create the current database schema and seed initial data automatically.

Apply upgrades in order when required:

1. [Category integer IDs](docs/upgrade-category-int-identity.md): converts GUID keys; public category/feed URLs retain their slugs, while admin APIs use integers.
2. [PostgreSQL snake_case names](docs/upgrade-postgresql-snake-case.md): requires integer category IDs first. SQL Server retains PascalCase names.
3. [Canonical activity events](docs/upgrade-activity-log-events.md): replaces legacy activity wording with stable event keys on both providers. Stop the old host first; this one-time upgrade discards only legacy activity history. Reruns preserve new history.
4. [SEO settings](docs/upgrade-seo-settings.md) copies existing SEO values from General and Advanced into a dedicated configuration row before startup. A follow-up moves robots.txt into that row.
5. [Independent owner accounts](docs/upgrade-owner-account.md): run the additive `owner-account.sql` and then `owner-account-cutover.sql` for your provider. This preserves the public address, removes only obsolete General email and legacy credentials, and leaves Local installations awaiting new credentials unless already initialized. Restrict access until public setup is complete. End-to-end release acceptance is still pending; do not deploy this development build as a completed release.
6. [Owner profile settings](docs/upgrade-owner-avatar.md): run `owner-avatar.sql` and then `owner-presentation.sql` for your provider to move the uploaded avatar, Gravatar preference, name and description to account storage.

### Quick Local Deployment (Docker)

For local testing or small-scale use, deploy Moonglade using Docker:

```bash
docker compose up -d
```

The supplied Compose file maps separate writable named volumes to `/app/images` and `/app/images-origin` and configures both `ImageStorage` paths. Keep both volumes when recreating or upgrading the container so public and retained original images remain durable and isolated.

Compose starts PostgreSQL and supplies the database connection string to the web container. For a direct `dotnet run`, the default connection string in `appsettings.json` points to PostgreSQL on `localhost:5432`; override it with `ConnectionStrings__MoongladeDatabase` for your environment.

Email is optional and inactive until SMTP is configured. See [Email Notifications](#email-notifications) and `/admin/settings/notification` for setup.

## 🛠️ Development

| Tools                      | Alternatives                                                                                       |
|----------------------------|----------------------------------------------------------------------------------------------------|
| [Visual Studio 2026](https://visualstudio.microsoft.com/) | [VS Code](https://code.visualstudio.com/) + [.NET 10.0 SDK](http://dot.net)           |
| [PostgreSQL](https://www.postgresql.org/) | [SQL Server 2025](https://www.microsoft.com/en-us/sql-server/) or [LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16) |

### Database Setup

PostgreSQL is the default database. SQL Server remains supported when configured explicitly.

| Database         | Example Connection String (`appsettings.json > ConnectionStrings > MoongladeDatabase`)         |
|------------------|----------------------------------------------------------------------------------------------|
| PostgreSQL       | `User ID=***;Password=***;Host=localhost;Port=5432;Database=moonglade;Pooling=true;`          |
| SQL Server       | `Server=(local);Database=moonglade;Trusted_Connection=True;`                                  |

Set `ConnectionStrings:DatabaseProvider` to match your database type (`PostgreSql` by default):

- PostgreSQL: `PostgreSql`
- SQL Server: `SqlServer`

### Build & Run

```bash
dotnet restore src/Moonglade.Web/Moonglade.Web.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet run --project src/Moonglade.Web/Moonglade.Web.csproj
```

With PowerShell, use the startup script from the repository root instead:

```powershell
.\start.ps1
```

The script requires the .NET 10 SDK and a configured, running database (see [Database Setup](#database-setup)). It restores dependencies, builds, and runs the web project with the `Moonglade.Web` launch profile in `Development`. It does not start Docker or change local configuration. Stop the application with `Ctrl+C`.

Open `https://localhost:10210`. With Local authentication, an installation without an initialized account redirects to `/auth/setup-account`. Create the owner account using an email and password; there is no shipped administrator or separate username. By default, scan the authenticator QR code and enter its 6-digit TOTP code before credentials are saved. Successful setup signs in and opens the public homepage. For test commands and coverage guidance, see [Tests](src/Tests/README.md).

Setup is public: any reachable visitor can claim an uninitialized Local installation. Restrict access operationally until the owner completes setup. While setup is pending, browser pages redirect there and business APIs, feeds and protocol endpoints return HTTP 503; setup assets and `/health` and `/health/ready` remain available. OIDC installations bypass this Local setup gate.

Deploy the current application at the URL root. Hosting under a prefix such as `/blog` is not supported by forwarding `X-Forwarded-Prefix` alone; it requires additional host-pipeline and client-URL work.

## ⚙️ Configuration

> Host settings use `src/Moonglade.Web/appsettings.json` and standard environment overrides (`:` becomes `__`). Persisted blog settings use `/admin/settings`.

In blog settings, switches and selections save when changed. For text, code, and typed numbers, choose **Change** and use **Save** or **Cancel** in the dialog.

### Default Post Editor

Choose HTML or Markdown under **Settings > Content > Default editor** (`/admin/settings/content`). The default is HTML. Changes are stored in the database and apply immediately when creating a new post; existing posts retain their own content type. The former `DefaultEditor` application configuration key is no longer used or imported. Startup no longer fills empty content types on historical posts.

### Content Cache Invalidation

Article saves, publication changes (including scheduled publishing), recycle-bin operations, and category/tag updates or deletions invalidate article caches, feeds, and the sitemap. Counters do not trigger invalidation; page-specific sitemap invalidation preserves article/feed caches. Feeds and the sitemap expire after one hour; articles use `PostCacheMinutes`. Invalidation is process-local, so multiple instances require separate coordination. See [Caching and Side Effects](docs/agent-reference.md#caching-and-side-effects) for implementation details.

Scheduled publication commits before cache and activity notifications. It attempts the activity handoff even if postcommit cache notification fails, while retaining that cache failure's existing error behavior. Individual activity failures do not roll back publication or stop the remaining activity writes; already-published articles are excluded from later scheduler passes. This is best-effort, in-process history, not durable delivery or multi-instance deduplication, and adds no further activity schema migration.

### Saving Blog Settings

Settings become active only after a successful database save and publication of one immutable ordinary-settings snapshot; groups submitted together are persisted together. Each operation or page render uses one immutable settings capture, including its related layouts/components or feed data/formatting; a subsequent operation can capture newer values. Updates are serialized within one process; other instances must reload or synchronize separately. See the [configuration write contract](docs/agent-reference.md#configuration).

Settings activity compares the effective previous and saved group inside that same write gate, including partial saves and nested custom menus. Only approved public labels, choices, counts, and switches enter before/after details. URLs, email identities, scripts, CSS, menu contents, filter keywords, and credential values are excluded. A restricted-only change still records the update without naming its restricted fields. Legacy configuration JSON normalization can still update persistence timestamps and caches without an activity entry when effective values are unchanged. Ordinary settings activity requires the before/after details format. Installations that ran a preview with the canonical schema must also clear pre-release activity history before starting the release. See the [activity upgrade guide](docs/upgrade-activity-log-events.md#reset-an-already-upgraded-preview).

If an administrator operation succeeds but activity-log recording fails, the request retains its successful response and a safe diagnostic is recorded at Error level without request values or exception messages. Authenticator reset still signs out the local-account sessions. Validation, settings persistence, cache invalidation, image storage, and sign-out failures are not treated as audit-only failures; they remain visible to the caller. Avatar uploads and Gravatar preferences are saved through Auth-owned account storage.

Successful changes invalidate affected server caches in the current process, including settings saved through the shared write command. Avatar uploads invalidate the local avatar cache without rewriting ordinary settings. Feed and theme caches use separate generations so an older request cannot refill the active cache after a change; settings changes do not flush unrelated article caches. Full-content RSS/Atom feeds use the article body, including Markdown formatting and configured CDN image URLs; summary feeds use the abstract.

| Setting | When the change takes effect |
| --- | --- |
| Site information, feed count/full content, image CDN | Subsequent requests; affected RSS/Atom/category feeds and canonical sitemap outputs are refreshed after saving. |
| Theme selection | Subsequent public, admin, and sign-in page renders. |
| Custom CSS | The server reads the saved value; existing browser caching and ETag revalidation still apply. |
| Watermarks and original-image retention | Future uploads; existing image files are not reprocessed. |
| Notification switches | Future enqueue decisions; already queued email is retained. |
| Notification sender display name | Application restart required, including when changing to/from the deployment fallback. |
| FOAF and sitemap enable/disable | Subsequent requests to the current instance. Disabled endpoints return HTTP 404. |
| Scheduled publishing enable/disable | Saving wakes the worker and it rechecks before dispatching publication; publication already dispatched is not canceled. |

Other rendering and operation settings apply to subsequent requests or operations. Server invalidation does not purge browser, CDN, proxy, or feed-reader caches; these can retain older responses under their existing cache rules.

At startup, malformed ordinary settings JSON, incompatible JSON values, null settings, and semantically invalid settings are restored to that group's default in the existing database row. Recovery replaces the entire group, including its other valid fields. Missing settings are initialized, and valid settings retain their values and modification timestamps. Recovery warnings identify the configuration key without logging its JSON. Invalid `SystemManifestSettings` stop initialization before repair writes with a restoration message; restore the installation history from a trusted database backup before restarting. The obsolete `LocalAccountSettings` row never supplies or repairs credentials; the manual account cutover script removes it.

For ordinary settings, a field completely absent from saved JSON uses the group's current default when loaded. Explicitly saved `false`, `0`, `null`, and empty arrays are retained when they satisfy validation. Loading these defaults does not rewrite valid JSON or change its modification timestamp. Older partial settings can therefore acquire default comment review/Gravatar, watermarking, the About menu, or a 20-item feed limit when those fields were absent; an explicitly saved unlimited feed count of `0` stays unlimited. This rule does not fill missing credential or installation-history fields.

Settings use the same model validation during loading and saving, including non-HTTP writes. Invalid submissions return validation errors without changing saved or runtime settings. Undefined enum values, negative feed counts, invalid nested menus, and invalid script/CSS conditions are rejected. Custom CSS is limited to 10,240 UTF-8 bytes. Disabled menus may have a null menu array; watermark font/text constraints still apply when watermarking is disabled. A blank notification display name uses the deployment-configured sender name, and unknown theme IDs retain the default-theme fallback.

The independent owner table enforces a single account and separates a public profile from completed Local credentials. Initial credentials and any required authenticator binding are committed together after verification. Installation manifests require a non-minimum UTC installation timestamp; historical or empty version strings are preserved.

First-install seeding is allowed only when every application table is empty, including when the current schema already exists. Seed data and all default settings are committed in one database transaction. If installation fails before commit, its data writes are rolled back; fix the dependency failure and restart to retry. A completed installation is not seeded again, including after a lost commit acknowledgment. Schema creation precedes this data transaction, and site-icon generation remains a separate, noncritical operation.

An existing or partially populated database with no installation configuration is preserved and cannot automatically be reseeded. Startup reports the failure; restore from a trusted backup or resolve the incomplete installation manually before restarting. Moonglade does not automatically clear ambiguous data left by older installations. Missing independent account data is instead a valid Local setup state, not permission to reseed content. A missing installation manifest alone, with existing ordinary configuration, is reconstructed with a Warning: its version is unknown and its timestamp records reconstruction, not the original installation time.

If initialization fails, Moonglade logs the error and exits with a nonzero process code before starting HTTP endpoints or business background services. It does not run a diagnostic-only 500 site with active workers. Correct the reported problem and restart; a rolled-back first installation can still be retried.

### Authentication

- By default: one Local account signs in by email with TOTP authenticator app verification (manage via `/admin/account`).
- `Authentication:Totp:Required=false` skips TOTP during initial setup and Local sign-in in every environment. The default is `true`.
- To replace a configured authenticator app, verify the current password at `/admin/account` to reset it; the reset signs out the administrator and starts TOTP setup on the next sign-in when required. It does not reopen initial account setup.
- Password changes keep the current session signed in with a refreshed revision and retain the authenticator. Pre-upgrade Local cookies and stale account-revision cookies must sign in again.
- Initial setup submissions and authenticator proof share one client-IP rate-limit quota, independent of posted email or pending setup tickets. The default allows 10 submissions per minute; ordinary sign-in and TOTP keep their account/IP partitions.
- Owner Email is public and managed only at `/admin/account`, for both authentication providers. It supplies Atom, FOAF, owner Gravatar and owner-directed notifications; The owner name and description are also managed under Account Settings. A real Local email change requires the current password, signs out all Local sessions and keeps the bound authenticator. Authorized OIDC sessions edit the public address without a local password or sign-out; provider email claims never overwrite it. Before an OIDC site's public address is configured, email-derived output and owner notifications are omitted, local avatars remain available, and commenter replies are unaffected.
- TOTP options:

```json
"Authentication": {
  "Totp": {
    "Issuer": "Moonglade",
    "Required": true
  },
  "LocalAccountRateLimit": {
    "Enabled": true,
    "PermitLimit": 10,
    "WindowMinutes": 1
  }
}
```

`Authentication:LocalAccountRateLimit` uses a fixed window. `PermitLimit` is the number of attempts allowed for the same partition during each window. Set `Enabled` to `false` only when another authentication-layer throttle is in place.

- A single standards-based **OpenID Connect** provider is supported as an alternative to the local account. Configure a confidential web client with authorization code flow and PKCE:

```json
"Authentication": {
  "Provider": "OpenIdConnect",
  "OpenIdConnect": {
    "Authority": "https://identity.example.com/",
    "ClientId": "moonglade",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "NameClaimType": "name",
    "Scopes": [ "openid", "profile", "email" ],
    "AllowedSubjects": [ "the-administrator-sub-claim" ]
  }
}
```

Supply `Authentication:OpenIdConnect:ClientSecret` from a secure external configuration source, such as the `Authentication__OpenIdConnect__ClientSecret` environment override; never commit the secret. `AllowedSubjects` contains the exact stable OIDC `sub` value for every administrator. An empty list safely denies all admin access while still allowing an authenticated OIDC user to retrieve their own `iss`, `sub`, and display name from `/auth/identity` for initial setup. Do not use email addresses or display names for authorization.

For Microsoft Entra ID, use the tenant-specific authority `https://login.microsoftonline.com/{tenant-id}/v2.0`. Register both callback URLs shown above in the provider. Existing Entra-specific deployments must follow [the generic OIDC migration guide](docs/upgrade-generic-oidc-authentication.md).

### Comment Markdown Links

Comment Markdown links are disabled by default. Enable **Settings > Comments > Enable Markdown links in comments and replies** (`/admin/settings/comment`) to allow hyperlinks. The setting is stored in the database and takes effect without a restart. The former `CommentMarkdown:EnableLinks` application configuration key is no longer used or imported. When disabled, links retain their original Markdown syntax and URL as plain text; other Markdown formatting still renders. This setting applies to comments and replies in public pages, the admin portal, and newly generated email notifications; emails already queued retain their generated content. Stored comments and post Markdown are unchanged. Enabled links retain protocol validation and `rel="nofollow ugc noopener noreferrer"`.

### Comment Rate Limiting

Built-in comment submissions are rate limited by the combination of client IP address and post ID. Configure the `CommentRateLimit` section in `appsettings.json`:

```json
"CommentRateLimit": {
  "Enabled": true,
  "PermitLimit": 5,
  "WindowMinutes": 10
}
```

`PermitLimit` is the number of comment submissions allowed for the same IP and post during each fixed window. `WindowMinutes` controls the fixed window length. Set `Enabled` to `false` to disable this host-level safeguard.

### Comment Submission Guard

Built-in comment submissions also use a hidden honeypot field and form elapsed-time checks. Configure the `CommentSubmissionGuard` section in `appsettings.json`:

```json
"CommentSubmissionGuard": {
  "Enabled": true,
  "HoneypotEnabled": true,
  "MinimumElapsedSeconds": 3,
  "MaxFormAgeMinutes": 240
}
```

`MinimumElapsedSeconds` rejects submissions that arrive too quickly after the comment form is rendered. `MaxFormAgeMinutes` rejects stale form timestamps; set it to `0` to disable the max-age check. Set `Enabled` to `false` to disable this guard.

### Image Storage

Moonglade stores images through the filesystem only. Local disks, Docker volumes, network filesystems, and object-storage mounts are deployment concerns; the application contains no Azure Blob or S3-compatible storage provider or SDK.

Uploaded image files are validated against their file content before storage. SVG uploads are supported, but active content and unsafe URL references are sanitized before the image is saved.

Configure two absolute, non-overlapping roots:

```json
"ImageStorage": {
  "CacheMinutes": 60,
  "FileSystemPath": "/app/images",
  "OriginalFileSystemPath": "/app/images-origin"
}
```

`FileSystemPath` contains public, processed images and is the only root read by `/image/{filename}`. `OriginalFileSystemPath` contains private original uploads when original-image retention is enabled. Never nest either root inside the other, and never expose the original root through static files or a CDN origin.

Both paths must be writable by the application identity and persistent across process or container replacement. Replicated deployments require shared storage with a coherent view across all replicas. Moonglade attempts to create missing directories, but it does not configure mount drivers, cloud credentials, network filesystems, or volume plugins.

Leave both paths empty only for development. The defaults are `<user-profile>/moonglade/images` and `<user-profile>/moonglade/images-origin`; an unmounted container user profile is not durable storage.

#### CDN Delivery

CDN delivery remains configured in the admin image settings. When enabled, rendered posts and feeds use the configured CDN endpoint directly, and `/image/{filename}` redirects the browser to the CDN URL instead of proxying image bytes through Moonglade.

Manage the uploaded profile avatar and Gravatar preference at `/admin/account` with either authentication provider. The local avatar is served at `/avatar` from account storage, with the bundled image as fallback. Image CDN settings apply to article images. Previously copied avatar files stay in their original storage.

The CDN origin must map a primary-root filename to the same root object key, serve the correct image `Content-Type`, and make a file visible only after its write is closed. CDN caching, invalidation, TLS, origin access, and any storage mount adapter are operator responsibilities. The private original root must not be attached to the CDN origin.

Upgrading from a previous filesystem, Azure Blob, or S3-compatible provider requires manual storage preparation and data migration before the new version starts. See [Upgrade to Filesystem-Only Image Storage](docs/upgrade-filesystem-image-storage.md).

For an optional deployment example using mounted object storage, see [Cloudflare R2 with rclone](docs/cloudflare-r2-image-storage.md).

### Comment Moderation

Moonglade comment moderation runs locally and does not call a remote moderation API. Configure the word filter and keyword list from `/admin/settings/comment`.

### Security HTTP Headers

Moonglade always emits `X-Content-Type-Options: nosniff`. To enable a custom Content Security Policy response header, set `EnableCSP` to `true` and provide the policy in `CSPValue`:

```json
{
  "EnableCSP": true,
  "CSPValue": "default-src 'self'; img-src 'self' https: data:"
}
```

### Email Notifications

Email notifications for new comments, replies, and Webmentions are optional. When available, notifications are queued in the Moonglade database and delivered by the in-process email outbox worker. Configure the `Email` section through deployment configuration or secrets, restart the application, then enable notifications in the admin portal.

```json
"Email": {
  "Enabled": true,
  "SmtpServer": "",
  "SmtpUserName": "",
  "SmtpSenderAddress": "",
  "SmtpPassword": "",
  "SmtpPort": 25,
  "EnableSsl": false
}
```

Email delivery supports SMTP only; no provider selection is required. Adjust the SMTP port and TLS setting for your service, and use the `Email__SmtpPassword` environment variable for real secrets.

Port `465` uses implicit TLS. On other ports, `EnableSsl=true` requires STARTTLS; `EnableSsl=false` uses STARTTLS when the server offers it and otherwise permits an unencrypted connection.

For SMTP, `SmtpUserName` is the authentication username. Set `SmtpSenderAddress` to a bare email address when the sender differs from the authentication username. It controls the `From` and `Sender` headers and the SMTP envelope sender; `SenderDisplayName` controls the display name unless overridden by the blog's Notification settings. When `SmtpSenderAddress` is omitted, empty, or whitespace, Moonglade continues to use `SmtpUserName` as the sender for compatibility. Override it with `Email__SmtpSenderAddress` in deployment configuration. The SMTP service must permit the selected sender address.

Email configuration never controls application liveness or database readiness:

- Missing required SMTP values leave email inactive, produce one startup warning, and show setup guidance in `/admin/settings/notification`. The application continues to start normally.
- Malformed, out-of-range, or inconsistent values leave email inactive, produce a startup error log, and show secret-safe validation details on the Notification settings page. They do not stop application startup.
- Setting `Email:Enabled` to `false` deliberately disables email notification and is shown as a separate non-error state in the admin portal. It defaults to `true`; delivery always uses the outbox.

Replace the former `Email:OutboxWorker:Enabled` deployment override with `Email:Enabled` (or `Email__Enabled` for environment variables), then restart the application. The old key is no longer read.

While email is unavailable, the worker does not poll or send and notification handlers do not enqueue new messages. Existing pending outbox messages are retained and can be processed after valid configuration is supplied and the application is restarted. The test-email action is disabled while delivery is unavailable.

Delivery is at least once: a shutdown during sending can cause a duplicate notification on retry.

#### Resend via SMTP

Resend uses SMTP and requires no additional SDK. Use a sender address on a domain verified in your Resend account, and configure:

```json
"Email": {
  "SmtpServer": "smtp.resend.com",
  "SmtpUserName": "resend",
  "SmtpSenderAddress": "notifications@example.com",
  "SmtpPort": 587,
  "EnableSsl": true
}
```

Replace `notifications@example.com` with your verified sender address. Supply the Resend API key through `Email__SmtpPassword` or your deployment's secret configuration; do not commit it to `appsettings.json`. Port `587` with `EnableSsl=true` requires STARTTLS. Restart Moonglade, enable email sending in `/admin/settings/notification`, and use **Send Test Email** to check delivery. See [Resend's SMTP documentation](https://resend.com/docs/send-with-smtp) for service requirements and supported ports.

### More Settings

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security HTTP Headers](https://github.com/EdiWang/Moonglade/wiki/Security-Headers)

## 📡 Protocols & Standards

| Name         | Feature       | Status      | Endpoint        |
|--------------|---------------|-------------|-----------------|
| RSS          | Subscription  | Supported   | `/rss`          |
| Atom         | Subscription  | Supported   | `/atom`         |
| OPML         | Subscription  | Supported   | `/opml`         |
| Open Search  | Search        | Supported   | `/opensearch`   |
| FOAF         | Social        | Supported   | `/foaf.xml`     |
| Webmention   | Social        | Supported   | `/webmention`   |
| Reader View  | Reader Mode   | Supported   | N/A             |
| u-card	   | SEO           | Supported   | N/A             |
| IndexNow     | SEO           | Supported   | N/A             |
| Dublin Core  | SEO           | Basic       | N/A             |
| RSD          | Discovery     | Deprecated  | N/A             |
| MetaWeblog   | Blogging      | Deprecated  | N/A             |
| Pingback     | Social        | Deprecated  | N/A             |

Incoming and outgoing Webmention URLs must use public HTTP/HTTPS addresses. Moonglade rejects private, loopback, link-local, documentation, reserved, and other special-use IPv4 and IPv6 ranges before fetching a source, following a redirect, discovering an endpoint, or submitting a Webmention. Connections are bound to a validated DNS result, and automatic proxy and redirect handling is disabled to prevent address-check bypasses.

## Health Checks

| Endpoint | Checks |
| --- | --- |
| `GET /health` | Process liveness only; no database check. |
| `GET /health/ready` | Readiness including database connectivity. |

The supplied Compose file checks the web container's `/health` endpoint and PostgreSQL with `pg_isready`. It waits for PostgreSQL to report healthy before starting the web container.
