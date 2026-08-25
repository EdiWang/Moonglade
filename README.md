# 🌙 Moonglade Blog

**Moonglade** is a personal blogging platform built for developers. It features essential blogging tools: posts, comments, categories, tags, archives, and pages.

## What Moonglade Does

Moonglade provides a self-hosted blog with a public reading experience and an authenticated admin portal. The core workflow is authoring posts and pages, organizing them with categories and tags, publishing immediately or on a schedule, and exposing the published content through web pages, feeds, sitemap, Webmention, and search-engine notification protocols.

Key business areas include:

- **Content publishing:** posts, drafts, scheduled posts, Markdown Mermaid diagrams, pages, featured/outdated flags, archives, recycle bin behavior, and route links for published posts.
- **Reader interaction:** comments, replies, Webmentions, comment moderation, view counts, and optional email notifications.
- **Site management:** runtime blog settings, widgets, themes, custom CSS, menus, image storage, site verification files, account settings, and data import/export.
- **Discovery and interoperability:** RSS, Atom, OPML, OpenSearch, FOAF, sitemap, robots.txt, IndexNow, root-level site ownership verification files, reader-friendly markup, and health checks.

## Repository Layout

The main solution file is `src/Moonglade.slnx`.

| Path | Purpose |
| --- | --- |
| `src/Moonglade.Web` | ASP.NET Core web host, Razor Pages, API controllers, endpoint mapping, filters, middleware wiring, and static assets. |
| `src/Moonglade.Features` | Blog feature commands and queries for posts, pages, comments, categories, tags, assets, dashboard data, and recycle bin behavior. |
| `src/Moonglade.Data` | EF Core `BlogDbContext`, entities, DTOs, mappings, and import/export primitives. |
| `src/Moonglade.Data.SqlServer`, `src/Moonglade.Data.PostgreSql` | SQL Server and PostgreSQL provider registration and provider-specific EF Core behavior. |
| `src/Moonglade.Configuration` | Persisted blog settings models, defaults, loading, and update commands. |
| `src/Moonglade.Auth` | Local account and standards-based OpenID Connect authentication support. |
| `src/Moonglade.BackgroundServices` | Scheduled publishing, update checks, and fire-and-forget background work. |
| `src/Moonglade.*` | Supporting projects for image storage, email, IndexNow, moderation, Webmention, syndication, themes, widgets, setup, utilities, and middleware. |
| `src/Tests/Moonglade.*.Tests` | xUnit test projects aligned with the production projects. |
| `Deployment`, `Dockerfile`, `compose.yaml` | Azure and Docker deployment assets. |

## 🚀 Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

- **Stable Code:** Always use the [Release](https://github.com/EdiWang/Moonglade/releases) branch. Avoid deploying from `master`.
- **Security:** Enable **HTTPS** and **HTTP/2** on your web server for optimal security and performance.
- **Deployment Options:** Moonglade can run on any cloud provider or on-premises.

### Quick Azure Deployment (App Service on Linux)

Get started in 10 minutes with minimal Azure resources using our [automated deployment script](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure).

The Bicep template provisions separate writable Azure Files shares for public and original images, mounts them into the App Service container at `/app/images` and `/app/images-origin`, and configures Moonglade with those filesystem paths. It does not provision a CDN or migrate images from an older Blob container.

### Quick Local Deployment (Docker)

For local testing or small-scale use, deploy Moonglade using Docker:

```bash
docker compose up -d
```

The supplied Compose file maps separate writable named volumes to `/app/images` and `/app/images-origin` and configures both `ImageStorage` paths. Keep both volumes when recreating or upgrading the container so public and retained original images remain durable and isolated.

Email notifications are optional. The supplied default Email section intentionally contains no provider credentials; the application still starts, logs a warning, and serves the blog with email notification inactive. Open `/admin/settings/notification` to see the setup guidance after signing in. Add provider configuration through deployment secrets only when email delivery is required.

## 🛠️ Development

| Tools                      | Alternatives                                                                                       |
|----------------------------|----------------------------------------------------------------------------------------------------|
| [Visual Studio 2026](https://visualstudio.microsoft.com/) | [VS Code](https://code.visualstudio.com/) + [.NET 10.0 SDK](http://dot.net)           |
| [SQL Server 2025](https://www.microsoft.com/en-us/sql-server/) | [LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16&WT.mc_id=AZ-MVP-5002809) or PostgreSQL |

### Database Setup

> **Tip:** SQL Server Express (free) is sufficient for most production uses.

| Database         | Example Connection String (`appsettings.json > ConnectionStrings > MoongladeDatabase`)         |
|------------------|----------------------------------------------------------------------------------------------|
| SQL Server       | `Server=(local);Database=moonglade;Trusted_Connection=True;`                                  |
| PostgreSQL       | `User ID=***;Password=***;Host=localhost;Port=5432;Database=moonglade;Pooling=true;`          |

Change `ConnectionStrings:DatabaseProvider` in `appsettings.json` to match your database type.

- SQL Server: `SqlServer`
- PostgreSQL: `PostgreSql`

### Build & Run

The commands below are derived from the project files and launch settings:

```bash
dotnet restore src/Moonglade.Web/Moonglade.Web.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet run --project src/Moonglade.Web/Moonglade.Web.csproj
```

1. Build and run `./src/Moonglade.slnx` or `src/Moonglade.Web/Moonglade.Web.csproj`
2. Access your blog:
    - **Home:** `https://localhost:10210`
    - **Admin:** `https://localhost:10210/admin`
      - Default username: `admin`
      - Default password: `admin123`
      - On first local-account sign-in, scan the authenticator QR code and enter the 6-digit code to enable TOTP.

## ⚙️ Configuration

> These settings are managed in `appsettings.json`. For blog settings, use the `/admin/settings` UI.

### Authentication

- By default: Local accounts with TOTP authenticator app verification (manage via `/admin/account`)
- Local development can disable the TOTP step with `Authentication:Totp:Required=false`; this bypass is ignored outside the `Development` environment.
- To replace a configured authenticator app, use `/admin/account` to reset it; the reset signs out the administrator and starts TOTP setup on the next sign-in.
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

The CDN origin must map a primary-root filename to the same root object key, serve the correct image `Content-Type`, and make a file visible only after its write is closed. CDN caching, invalidation, TLS, origin access, and any storage mount adapter are operator responsibilities. The private original root must not be attached to the CDN origin.

Upgrading from a previous filesystem, Azure Blob, or S3-compatible provider requires manual storage preparation and data migration before the new version starts. See [Upgrade to Filesystem-Only Image Storage](docs/upgrade-filesystem-image-storage.md).

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
  "Provider": "AzureCommunication",
  "AcsConnectionString": "",
  "AcsSenderAddress": "",
  "OutboxWorker": {
    "Enabled": true
  }
}
```

Supported providers are `AzureCommunication` and `smtp`. Use environment variable overrides such as `Email__AcsConnectionString` or `Email__SmtpPassword` for real secrets.

Email configuration never controls application liveness or database readiness:

- Missing required provider values leave email inactive, produce one startup warning, and show setup guidance in `/admin/settings/notification`. The application continues to start normally.
- Unsupported, malformed, out-of-range, or inconsistent values leave email inactive, produce a startup error log, and show secret-safe validation details on the Notification settings page. They do not stop application startup.
- Setting `Email:OutboxWorker:Enabled` to `false` deliberately disables email notification and is shown as a separate non-error state in the admin portal.

While email is unavailable, the worker does not poll or send and notification handlers do not enqueue new messages. Existing pending outbox messages are retained and can be processed after valid configuration is supplied and the application is restarted. The test-email action is disabled while delivery is unavailable.

Email delivery uses at-least-once processing. If the application stops while a message is being sent, a later retry can occasionally send a duplicate notification. Keep secrets outside source control and use the platform's secret or environment-variable configuration mechanism.

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

To ensure your Moonglade process is running, use the liveness health check endpoint:

```
GET /health
```

This endpoint is intentionally process-only and does not check database availability.

For readiness diagnostics that include database connectivity, use:

```
GET /health/ready
```
