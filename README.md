# 🌙 Moonglade Blog

**Moonglade** is a personal blogging platform built for developers, optimized for seamless deployment on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). It features essential blogging tools: posts, comments, categories, tags, archives, and pages.

## What Moonglade Does

Moonglade provides a self-hosted blog with a public reading experience and an authenticated admin portal. The core workflow is authoring posts and pages, organizing them with categories and tags, publishing immediately or on a schedule, and exposing the published content through web pages, feeds, sitemap, Webmention, and search-engine notification protocols.

Key business areas include:

- **Content publishing:** posts, drafts, scheduled posts, pages, featured/outdated flags, archives, recycle bin behavior, and route links for published posts.
- **Reader interaction:** comments, replies, Webmentions, comment moderation, view counts, and optional email notifications.
- **Site management:** runtime blog settings, widgets, themes, custom CSS, menus, image storage, account settings, and data import/export.
- **Discovery and interoperability:** RSS, Atom, OPML, OpenSearch, FOAF, sitemap, robots.txt, IndexNow, reader-friendly markup, and health checks.

## Repository Layout

The main solution file is `src/Moonglade.slnx`.

| Path | Purpose |
| --- | --- |
| `src/Moonglade.Web` | ASP.NET Core web host, Razor Pages, API controllers, endpoint mapping, filters, middleware wiring, and static assets. |
| `src/Moonglade.Features` | Blog feature commands and queries for posts, pages, comments, categories, tags, assets, dashboard data, and recycle bin behavior. |
| `src/Moonglade.Data` | EF Core `BlogDbContext`, entities, DTOs, mappings, and import/export primitives. |
| `src/Moonglade.Data.SqlServer`, `src/Moonglade.Data.PostgreSql` | SQL Server and PostgreSQL provider registration and provider-specific EF Core behavior. |
| `src/Moonglade.Configuration` | Persisted blog settings models, defaults, loading, and update commands. |
| `src/Moonglade.Auth` | Local account and Microsoft Entra ID authentication support. |
| `src/Moonglade.BackgroundServices` | Scheduled publishing, update checks, and fire-and-forget background work. |
| `src/Moonglade.*` | Supporting projects for image storage, email, IndexNow, moderation, Webmention, syndication, themes, widgets, setup, utilities, and middleware. |
| `src/Tests/Moonglade.*.Tests` | xUnit test projects aligned with the production projects. |
| `Deployment`, `Dockerfile`, `compose.yaml` | Azure and Docker deployment assets. |

## 🚀 Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

- **Stable Code:** Always use the [Release](https://github.com/EdiWang/Moonglade/releases) branch. Avoid deploying from `master`.
- **Security:** Enable **HTTPS** and **HTTP/2** on your web server for optimal security and performance.
- **Deployment Options:** While Azure is recommended, Moonglade can run on any cloud provider or on-premises.

### Quick Azure Deployment (App Service on Linux)

Get started in 10 minutes with minimal Azure resources using our [automated deployment script](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure).

### Quick Local Deployment (Docker)

For local testing or small-scale use, deploy Moonglade using Docker:

```bash
docker compose up -d
```

### Full Azure Deployment

This mirrors how [edi.wang](https://edi.wang) is deployed, utilizing a variety of Azure services for maximum speed and security. **No automated script is provided**—manual resource creation is required.

![Azure Architecture](https://img.edi.wang/ediwang-azure-arch-visio-oct2024.svg)

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

Focused tests can be run from the matching test project, for example:

```bash
dotnet test src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
```

1. Build and run `./src/Moonglade.slnx` or `src/Moonglade.Web/Moonglade.Web.csproj`
2. Access your blog:
    - **Home:** `https://localhost:10210`
    - **Admin:** `https://localhost:10210/admin`
      - Default username: `admin`
      - Default password: `admin123`
      - On first local-account sign-in, scan the authenticator QR code and enter the 6-digit code to enable TOTP.

## ⚙️ Configuration

> Most settings are managed in `appsettings.json`. For blog settings, use the `/admin/settings` UI.

### Authentication

- By default: Local accounts with TOTP authenticator app verification (manage via `/admin/account`)
- Local sign-in is two-step after setup: username/password first, then the authenticator code on the next screen.
- To replace a configured authenticator app, use `/admin/account` to reset it; the reset signs out the administrator and starts TOTP setup on the next sign-in.
- Optional TOTP issuer display name for authenticator apps:

```json
"Authentication": {
  "Totp": {
    "Issuer": "Moonglade"
  }
}
```

- **Microsoft Entra ID** (Azure AD) supported. [Setup guide](https://github.com/EdiWang/Moonglade/wiki/Use-Microsoft-Entra-ID-Authentication)

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

Configure the `ImageStorage` section in `appsettings.json` to choose where blog images are stored.

#### **Azure Blob Storage** (Recommended)

Create an [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) container with appropriate permissions:

```json
{
  "Provider": "azurestorage",
  "AzureStorageSettings": {
    "ConnectionString": "YOUR_CONNECTION_STRING",
    "ContainerName": "YOUR_CONTAINER_NAME"
  }
}
```
- Enable CDN in admin settings for faster image delivery.

#### **File System** (Not recommended)

Windows:
```json
{
  "Provider": "filesystem",
  "FileSystemPath": "C:\\UploadedImages"
}
```
Linux:
```json
{
  "Provider": "filesystem",
  "FileSystemPath": "/app/images"
}
```

When using the file system, ensure the path exists and has appropriate permissions. If the path does not exist, Moonglade will attempt to create it. 

Leave the `FileSystemPath` empty to use the default path (`~/home/moonglade/images` on Linux or `%UserProfile%\moonglade\images` on Windows).

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

For notifications on new comments, replies and webmentions, use [Moonglade.Email Azure Function](https://github.com/EdiWang/Moonglade.Email):

```json
"Email": {
  "ApiEndpoint": "",
  "ApiKey": ""
}
```
Enable notifications in the admin portal.

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

## Health Check

To ensure your Moonglade instance is running, you can use the health check endpoint:

```
GET /health
```

This endpoint returns a simple JSON response indicating the status of your Moonglade instance.
