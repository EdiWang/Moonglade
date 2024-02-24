# Moonglade Blog

[![Docker Linux x64](https://github.com/EdiWang/Moonglade/actions/workflows/docker.yml/badge.svg)](https://github.com/EdiWang/Moonglade/actions/workflows/docker.yml)
![Man hours](https://manhours.aiursoft.cn/r/github.com/ediwang/moonglade.svg)

A personal blog system that optimized for [**Microsoft Azure**](https://azure.microsoft.com/en-us/). Designed for developers, enabling most common blogging features including posts, comments, categories, archive, tags and pages.

## 📦 Deployment

- Use stable code from [Release](https://github.com/EdiWang/Moonglade/releases) branch rather than master branch.

- HTTPS is required, and it is recommended to enable HTTP/2 support on your web server.

### Full Deploy on Azure

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure. 

There is no automated script to deploy it, you need to manually create all the resources.

![image](https://cdn.edi.wang/web-assets/ediwang-azure-arch-visio-nov2022.png)

### Quick Deploy on Azure (App Service on Linux)

Use automated deployment script to get your Moonglade up and running in 10 minutes with minimal Azure components, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure)

### Quick Deploy with Docker-Compose

Simply go the the root folder of this repo and run:

```bash
docker-compose build
docker-compose up
```

That's it! Now open: [Browser: http://localhost:8080](http://localhost:8080)

## 🐵 Development

Tools | Alternative
--- | ---
[Visual Studio 2022](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/) with [.NET 8.0 SDK](http://dot.net)
[SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-2022) | [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16&WT.mc_id=AZ-MVP-5002809), PostgreSQL or MySQL 

### Setup Database

You can choose from SQL Server, PostgreSQL or MySQL. Update your database connection string in `appsettings.json`, `ConnectionStrings` section.


Database | `DatabaseType` | `MoongladeDatabase` Example
--- | --- | ---
Microsoft SQL Server | `SqlServer` | `Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;`
MySQL | `MySql` | `Server=localhost;Port=3306;Database=moonglade;Uid=root;Pwd=***;`
PostgreSQL | `PostgreSql` | `User ID=***;Password=***;Host=localhost;Port=5432;Database=moonglade;Pooling=true;`

### Build Source

Build and run `./src/Moonglade.sln`
- Home page: `https://localhost:1055`
- Admin: `https://localhost:1055/admin`
  - Default username: `admin`
  - Default password: `admin123`

## ⚙ Configuration

> This section discuss environment settings in **appsettings.json**. For blog settings, please use "/admin/settings" UI.

### Authentication

Moonglade is using local account by default, you can manage accounts in `/admin/settings/account`. You can also use  [Microsoft Entra ID](https://azure.microsoft.com/en-us/services/active-directory/) to login. See [Wiki document](https://github.com/EdiWang/Moonglade/wiki/Use-Microsoft-Entra-ID-Authentication) for setup Microsoft Entra ID.

### Image Storage
`ImageStorage` controls how blog post images are stored.

#### [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) (Preferred)

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 

```json
{
  "Provider": "azurestorage"
  "AzureStorageSettings": {
    "ConnectionString": "YOUR CONNECTION STRING",
    "ContainerName": "YOUR CONTAINER NAME"
  }
}
```

When configured the image storage to use Azure Blob, you can take advantage of CDN for your image resources. Just enable CDN in admin settings, the blog will get images from CDN.

#### [Minio Blob Storage](https://min.io/)

You need to hava an [**Minio Server**](https://docs.min.io/). 

```json
"Provider": "miniostorage"
"MinioStorageSettings": {
  "EndPoint": "Minio Server Endpoint(eg:localhost:9600)",
  "AccessKey": "Your Access Key",
  "SecretKey": "Your Secret Key",
  "BucketName": "Your BucketName",
  "WithSSL": false
}
```

#### File System (Not Recommended)

You can also choose File System for image storage if you don't have a cloud option.

```json
{
  "Provider": "filesystem",
  "FileSystemPath": "C:\\UploadedImages"
}
```

### Comment Moderator

Setup [Moonglade.ContentSecurity](https://github.com/EdiWang/Moonglade.ContentSecurity)  Azure Function to enable comment moderation.

```json
"ContentModerator": {
  "Provider": "",
  "ApiEndpoint": "",
  "ApiKey": ""
}
```

### Email Notification

Setup [Moonglade.Email](https://github.com/EdiWang/Moonglade.Email) Azure Function to enable email notification for new comments, new replies and pingbacks. Then enable notification in admin portal.

```json
"Email": {
  "ApiEndpoint": "",
  "ApiKey": ""
}
```

### Others

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security Headers](https://github.com/EdiWang/Moonglade/wiki/Security-Headers)

## 🎉 Blog Protocols or Standards

Name | Feature | Status | Service Endpoint
--- | --- | --- | ---
RSS | Subscription | Supported | `/rss`
Atom | Subscription | Supported | `/atom`
OPML | Subscription | Supported | `/opml`
Open Search | Search | Supported | `/opensearch`
Pingback | Social | Supported | `/pingback`
Reader View | Reader mode | Supported | N/A
FOAF | Social | Supported | `/foaf.xml`
RSD | Service Discovery | Supported | `/rsd` *If MetaWeblog is enabled*
MetaWeblog | Blogging | Basic Support | `/metaweblog`
Dublin Core Metadata | SEO | Basic Support | N/A
BlogML | Blogging | Not planned | 
APML | Social | Not planned | 
Trackback | Social | Not planned |

## 免责申明

对于中国用户，我们有一份特定的免责申明。请确保你已经阅读并理解其内容：

- [免责申明（仅限中国用户）](./DISCLAIMER_CN.md)
