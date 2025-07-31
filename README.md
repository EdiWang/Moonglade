# 🌙 Moonglade Blog

**Moonglade** is a personal blogging platform built for developers, optimized for seamless deployment on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). It features essential blogging tools: posts, comments, categories, tags, archives, and pages.

## 🚀 Deployment

- **Stable Code:** Always use the [Release](https://github.com/EdiWang/Moonglade/releases) branch. Avoid deploying from `master`.
- **Security:** Enable **HTTPS** and **HTTP/2** on your web server for optimal security and performance.
- **Deployment Options:** While Azure is recommended, Moonglade can run on any cloud provider or on-premises.
- **China Regulation:** In China, Moonglade runs in **read-only** mode due to local regulations. If you are in China, please consider alternative platforms.

### Full Azure Deployment

This mirrors how [edi.wang](https://edi.wang) is deployed, utilizing a variety of Azure services for maximum speed and security. **No automated script is provided**—manual resource creation is required.

![Azure Architecture](https://cdn.edi.wang/web-assets/ediwang-azure-arch-visio-oct2024.svg)

### Quick Azure Deploy (App Service on Linux)

Get started in 10 minutes with minimal Azure resources using our [automated deployment script](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure).

## 🛠️ Development

| Tools                      | Alternatives                                                                                       |
|----------------------------|----------------------------------------------------------------------------------------------------|
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | [VS Code](https://code.visualstudio.com/) + [.NET 8.0 SDK](http://dot.net)           |
| [SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-2022) | [LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16&WT.mc_id=AZ-MVP-5002809), PostgreSQL, or MySQL |

### Database Setup

> **Tip:** SQL Server Express (free) is sufficient for most production uses.

| Database         | Example Connection String (`appsettings.json > ConnectionStrings > MoongladeDatabase`)         |
|------------------|----------------------------------------------------------------------------------------------|
| SQL Server       | `Server=(local);Database=moonglade;Trusted_Connection=True;`                                  |
| MySQL            | `Server=localhost;Port=3306;Database=moonglade;Uid=root;Pwd=***;`                             |
| PostgreSQL       | `User ID=***;Password=***;Host=localhost;Port=5432;Database=moonglade;Pooling=true;`          |

Change `ConnectionStrings:DatabaseProvider` in `appsettings.json` to match your database type.` 

- SQL Server: `SqlServer`
- MySQL: `MySql`
- PostgreSQL: `PostgreSql`

### Build & Run

1. Build and run `./src/Moonglade.sln`
2. Access your blog:
    - **Home:** `https://localhost:17251`
    - **Admin:** `https://localhost:17251/admin`
      - Default username: `admin`
      - Default password: `admin123`

## ⚙️ Configuration

> Most settings are managed in `appsettings.json`. For blog settings, use the `/admin/settings` UI.

### Authentication

- By default: Local accounts (manage via `/admin/settings/account`)
- **Microsoft Entra ID** (Azure AD) supported. [Setup guide](https://github.com/EdiWang/Moonglade/wiki/Use-Microsoft-Entra-ID-Authentication)

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

#### **MinIO Blob Storage**

Set up a [MinIO Server](https://docs.min.io/):

```json
{
  "Provider": "miniostorage",
  "MinioStorageSettings": {
    "EndPoint": "localhost:9600",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "BucketName": "YOUR_BUCKET_NAME",
    "WithSSL": false
  }
}
```

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

Enable comment moderation via the [Moonglade.ContentSecurity Azure Function](https://github.com/EdiWang/Moonglade.ContentSecurity):

```json
"ContentModerator": {
  "Provider": "",
  "ApiEndpoint": "",
  "ApiKey": ""
}
```

### Email Notifications

For notifications on new comments, replies, webmentions, and pingbacks, use [Moonglade.Email Azure Function](https://github.com/EdiWang/Moonglade.Email):

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
| Pingback     | Social        | Supported   | `/pingback`     |
| Webmention   | Social        | Supported   | `/webmention`   |
| Reader View  | Reader Mode   | Supported   | N/A             |
| FOAF         | Social        | Supported   | `/foaf.xml`     |
| IndexNow     | SEO           | Supported   | N/A             |
| RSD          | Discovery     | Deprecated  | N/A             |
| MetaWeblog   | Blogging      | Deprecated  | N/A             |
| Dublin Core  | SEO           | Basic       | N/A             |

## 🇨🇳 免责申明

对于中国访客，我们有一份特供的免责申明。请确保你已经阅读并理解其内容：[免责申明（仅限中国访客）](./DISCLAIMER_CN.md)
