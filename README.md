# Moonglade Blog

Moonglade is a self-hosted personal blogging platform built with ASP.NET Core on .NET 10 and Razor Pages. It runs on-premises or in the cloud and supports PostgreSQL (default) and SQL Server.

- Publish HTML or Markdown posts, including Mermaid diagrams, with drafts, scheduling, categories, tags, and standalone pages.
- Manage comments, replies, Webmentions, themes, widgets, menus, images, site verification files, and administrator activity from the admin portal. The admin portal supports desktop browsers at 1440 CSS pixels or wider.
- Serve feeds, discovery metadata, and localized pages. Available languages are English, Simplified Chinese, Traditional Chinese (Hong Kong terminology), German, Japanese, and French (France). Visitors choose their display language in the footer. The blog's default language controls blog and post metadata.

## Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

Deploy a published [release](https://github.com/EdiWang/Moonglade/releases) at the URL root. Enable HTTPS and HTTP/2 on the public web server. Behind a reverse proxy, configure trusted addresses under `ForwardedHeaders:KnownProxies` so the application receives the correct public scheme and client IP. Subpath hosting is unsupported.

### Database Upgrades

Fresh installations create the schema and seed initial data automatically. Existing installations require manual upgrades before the new application starts.

Stop all Moonglade hosts, back up the database, and test the required SQL scripts against a restored copy. Follow the [database execution guide](database/README.md#verification-and-execution).

To upgrade a database already at 16.9.0 to 16.10.0, run these scripts in order from the [PostgreSQL](database/PostgreSql) or [SQL Server](database/SqlServer) directory for your provider:

1. `3.activity-log-events.sql` replaces legacy activity history and discards its existing entries.
2. `4.seo-settings.sql` copies legacy SEO settings.
3. `5.seo-robots-txt.sql` copies the existing robots.txt content.
4. `6.owner-account.sql` creates owner account storage.
5. `7.owner-account-cutover.sql` transfers the public owner email and removes legacy local credentials.
6. `8.owner-avatar.sql` transfers the owner avatar and Gravatar preference.
7. `9.owner-presentation.sql` transfers the owner name and description.
8. `10.owner-oidc.sql` creates OIDC storage and the Data Protection key table required for local sign-in and antiforgery tokens. Run it even if you use only local sign-in.

For databases older than 16.9.0, complete the applicable category ID and PostgreSQL naming upgrades (`1` and `2`) first. Keep access restricted when starting the upgraded app. Complete [owner setup](#first-run) if local credentials are uninitialized, then reopen access.

### Quick Local Deployment (Docker)

Set `POSTGRES_PASSWORD` in a local `.env` file using [.env.example](.env.example) as a template, then run:

```bash
docker compose up -d
```

Open `http://localhost:8080` and [complete owner setup](#first-run). Compose starts PostgreSQL and supplies the database connection string to the web container. Keep the database volume and both image volumes when replacing containers.

## Development

Install the .NET 10 SDK and start a PostgreSQL or SQL Server instance. The solution is `src/Moonglade.slnx`, and the web project is `src/Moonglade.Web`.

### Database Setup

Set `ConnectionStrings:DatabaseProvider` to `PostgreSql` or `SqlServer` and supply `ConnectionStrings:MoongladeDatabase`. The default host configuration targets PostgreSQL on `localhost:5432`. Use `ConnectionStrings__MoongladeDatabase` to override the connection string in the environment.

### Build & Run

```bash
dotnet restore src/Moonglade.Web/Moonglade.Web.csproj
dotnet build src/Moonglade.Web/Moonglade.Web.csproj
dotnet run --project src/Moonglade.Web/Moonglade.Web.csproj
```

In PowerShell, `./start.ps1` runs the same project with the Development launch profile. Open `https://localhost:10210`. See the [test guide](src/Tests/README.md#run-tests) for test commands and the [engineering reference](docs/agent-reference.md) for module ownership and implementation contracts.

### First Run

Restrict access to the site until the owner completes setup. Any visitor who can reach an uninitialized installation can claim it at `/auth/setup-account`. Create the owner with an email and password, then verify the authenticator code when TOTP is required. Successful setup signs in and opens the public homepage.

Until setup completes, browser page reads redirect to setup and business APIs, feeds, and protocol endpoints return HTTP 503. Setup assets and health endpoints remain available.

## Configuration

Host options use [appsettings.json](src/Moonglade.Web/appsettings.json) and standard environment overrides (`:` becomes `__`). Manage persisted blog preferences at `/admin/settings` and the owner profile and credentials at `/admin/account`.

### Saving Blog Settings

Switches and selections save when changed. For text, code, or numeric values, open **Change** and use **Save** or **Cancel**. Menu changes use the page's **Save** button.

| Setting | When it takes effect |
| --- | --- |
| Rendering, theme, and feed options | On subsequent requests. Browser, CDN, proxy, and feed-reader caches retain their own lifetimes. |
| FOAF and sitemap switches | On subsequent requests. Disabled endpoints return HTTP 404. |
| Watermark and original-image retention | On future uploads. |
| Notification switches | On future enqueue decisions. Queued email retains its recipient and content. |
| Notification sender display name | After an application restart. |
| Scheduled publishing | Saving wakes the worker. Publication already dispatched runs to completion. |

Runtime settings and cache invalidation are local to each process. Coordinate separately across replicas.

### Default Post Editor

Choose HTML or Markdown under **Settings > Content > Default editor**. The default is HTML. The setting applies to new posts, and existing posts retain their content type.

### Authentication

The owner signs in by email and password. TOTP is required by default. Set `Authentication:Totp:Required` to `false` to skip it in all environments. The Local fixed-window rate limiter defaults to 10 attempts per minute. Initial setup and its TOTP proof share a client-IP quota. Sign-in and later TOTP attempts use account/IP partitions. Tune the policy under `Authentication:LocalAccountRateLimit`.

Select **Remember this device** when verifying an authenticator code to skip TOTP on that browser for 30 days. The password is still required. Trust expires without automatic renewal, survives ordinary sign-out, and is revoked by password or email changes and authenticator reset. It uses a protected, HTTPS-only, host-only persistent cookie, not an IP address or browser fingerprint. Clearing cookies or switching browsers requires verification again. Use this only on a trusted device: a stolen usable cookie and the password can bypass TOTP until trust expires or is revoked.

Manage the public email, name, description, avatar, and Gravatar preference at **Account Settings**. Changing the email requires the current local password and signs out account sessions while retaining TOTP. Changing the password refreshes the current session. Resetting the authenticator requires the current password and signs out the owner so the next local sign-in can enroll an authenticator when required.

Application Data Protection keys are stored in the database with the fixed application name `Moonglade`. This preserves authentication cookies, antiforgery tokens, and temporary authentication proofs across container replacements and lets replicas using the same database read them. Existing expiry and account-revocation checks still apply. The first upgrade from container-local keys requires signing in again; old file-based keys are not imported.

To enable third-party sign-in, configure one public HTTPS OpenID Connect provider under **Account Settings > Third-party sign-in**, register the site's `/signin-oidc` callback URL, and confirm the external account binding. The enabled, bound identity signs in as the owner using the provider's authentication. Disabling the provider, unbinding it, or deleting it revokes its Moonglade sessions. Database backups contain both the encrypted client secret and the Data Protection keys required to decrypt it.

Application and OIDC credential protection share the existing `DataProtectionKeys` table (`data_protection_keys` on PostgreSQL), while OIDC retains its separate `Moonglade.OidcCredentials` application name and protection purpose. Preserve and restore the keys together with the encrypted client secret. Do not clear the key table: a running process may continue using a cached key that becomes unavailable after restart. If the required OIDC key is lost, save the client secret again through the authenticated management page to encrypt it with a persisted key. Database persistence does not itself encrypt the stored keys; restrict database and backup access and provide deployment-appropriate encryption at rest.

### Comments

Configure review, local keyword moderation, and Markdown links at `/admin/settings/comment`. Markdown links are disabled by default. When disabled, links remain visible as Markdown text. The option affects comments and replies on public and admin pages and in newly generated emails. Enabled links retain URL validation and `rel="nofollow ugc noopener noreferrer"`.

| Host option | Default behavior |
| --- | --- |
| `CommentRateLimit` | Allows five submissions per client IP and post in a ten-minute fixed window. |
| `CommentSubmissionGuard` | Checks a honeypot and rejects forms submitted before three seconds or after 240 minutes. Set `MaxFormAgeMinutes` to `0` to disable the age limit. |

Each section has an `Enabled` switch.

### Image Storage

Configure two writable, durable, absolute filesystem roots that do not overlap. Use shared storage for replicas.

| Option | Purpose |
| --- | --- |
| `ImageStorage:FileSystemPath` | Public processed images served at `/image/{filename}`. Compose mounts `/app/images`. |
| `ImageStorage:OriginalFileSystemPath` | Private originals when retention is enabled. Compose mounts `/app/images-origin`. |

Empty paths fall back to `moonglade/images` and `moonglade/images-origin` under the user profile. Set explicit persistent paths for deployment. Uploads are validated by content and SVG files are sanitized before saving.

When CDN delivery is enabled in the image settings, posts and feeds use CDN URLs directly and `/image/{filename}` redirects to the CDN. Expose only the public root at the CDN origin. Preserve filename-to-key mapping and media types, and make files visible only after writing completes. The owner avatar is served separately at `/avatar` from account storage. See the optional [Cloudflare R2 mount example](docs/cloudflare-r2-image-storage.md).

### Security HTTP Headers

Moonglade emits `X-Content-Type-Options: nosniff`. Set `EnableCSP` to `true` and supply `CSPValue` to add a Content Security Policy.

When using OIDC with a CSP `form-action` directive, allow the provider's authorization endpoint origin alongside `'self'`. Browsers can apply this restriction to the external redirect after the account-binding form is submitted; `form-action 'self'` alone can block binding. Keep the allowlist limited to the required provider origins rather than disabling CSP.

### Email Notifications

Configure SMTP through the `Email` section, restart, then enable notifications and send a test email at `/admin/settings/notification`.

| Option | Purpose |
| --- | --- |
| `Enabled` | Enables email capability by default. Set to `false` to disable it explicitly. |
| `SmtpServer`, `SmtpPort` | SMTP endpoint. Port 465 uses implicit TLS. On other ports, `EnableSsl=true` requires STARTTLS and `false` uses STARTTLS when offered and otherwise allows an unencrypted connection. |
| `SmtpUserName`, `SmtpPassword` | SMTP credentials. Supply the password through deployment secrets, for example `Email__SmtpPassword`. |
| `SmtpSenderAddress` | Bare email address used in the sender headers and SMTP envelope. Blank values use `SmtpUserName`. |
| `SenderDisplayName` | Deployment fallback for a blank sender name in Notification settings. |

When email is disabled, incomplete, or invalid, Moonglade continues serving the site. New notifications and test sends are unavailable, and pending mail remains in the database outbox. Delivery resumes after valid configuration and a restart. Delivery is at least once and can duplicate a notification after interruption.

## Protocols & Standards

| Endpoint | Purpose |
| --- | --- |
| `/rss`, `/atom`, `/opml` | Feeds and subscriptions |
| `/opensearch` | OpenSearch discovery |
| `/foaf.xml` | FOAF profile |
| `/webmention` | Webmention reception |
| `/sitemap.xml`, `/robots.txt` | Crawler discovery and directives |

Incoming and outgoing Webmentions require public HTTP or HTTPS addresses. Site verification files are served at the URL root. SEO settings also control metadata and IndexNow.

## Health Checks

- `GET /health` checks application liveness. Compose uses this endpoint for the web container.
- `GET /health/ready` checks readiness, including database connectivity.
